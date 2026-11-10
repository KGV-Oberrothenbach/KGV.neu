-- P11 AP-02A / RPC 7: local PostgreSQL acceptance for initial lease registration.
-- Synthetic fixtures only; the transaction is always rolled back.
begin;

create temporary table p11_rpc7_ids (
  parzelle_id bigint not null,
  other_parzelle_id bigint not null,
  hauptpaechter_id bigint not null,
  mitpaechter_id bigint not null,
  wrong_member_id bigint not null,
  dokument_id bigint not null,
  wrong_parzelle_dokument_id bigint not null,
  wrong_member_dokument_id bigint not null,
  granted_user uuid not null,
  denied_user uuid not null,
  revoked_admin_user uuid not null,
  invalid_actor uuid not null
) on commit drop;

insert into auth.users(id,aud,role,email,encrypted_password,email_confirmed_at,raw_app_meta_data,raw_user_meta_data,created_at,updated_at)
values
 ('00000000-0000-0000-0000-000000000701','authenticated','authenticated','p11-rpc7-granted@example.test','x',now(),'{}','{}',now(),now()),
 ('00000000-0000-0000-0000-000000000702','authenticated','authenticated','p11-rpc7-denied@example.test','x',now(),'{}','{}',now(),now()),
 ('00000000-0000-0000-0000-000000000703','authenticated','authenticated','p11-rpc7-revoked@example.test','x',now(),'{}','{}',now(),now()),
 ('00000000-0000-0000-0000-000000000704','authenticated','authenticated','p11-rpc7-invalid@example.test','x',now(),'{}','{}',now(),now());

insert into public.mitglied(name,vorname,aktiv,aufnahme_status,auth_user_id)
values
 ('P11 RPC7 Grant','Test',true,'aufgenommen','00000000-0000-0000-0000-000000000701'),
 ('P11 RPC7 Deny','Test',true,'aufgenommen','00000000-0000-0000-0000-000000000702'),
 ('P11 RPC7 Revoke','Test',true,'aufgenommen','00000000-0000-0000-0000-000000000703'),
 ('P11 RPC7 Hauptpächter','Test',true,'aufgenommen',null),
 ('P11 RPC7 Mitpächter','Test',true,'aufgenommen',null),
 ('P11 RPC7 FalscherPächter','Test',true,'aufgenommen',null)
returning id;

update public.app_user set role='user',permission_grants=524288,permission_revocations=0
 where user_id='00000000-0000-0000-0000-000000000701';
update public.app_user set role='user',permission_grants=0,permission_revocations=0
 where user_id='00000000-0000-0000-0000-000000000702';
update public.app_user set role='admin',permission_grants=0,permission_revocations=524288
 where user_id='00000000-0000-0000-0000-000000000703';

insert into public.parzelle(garten_nr,"Anlage") values ('P11-RPC7-A','P11'),('P11-RPC7-B','P11');
select id as parzelle_id from public.parzelle where garten_nr='P11-RPC7-A' \gset
select id as other_parzelle_id from public.parzelle where garten_nr='P11-RPC7-B' \gset
select id as hauptpaechter_id from public.mitglied where name='P11 RPC7 Hauptpächter' \gset
select id as mitpaechter_id from public.mitglied where name='P11 RPC7 Mitpächter' \gset
select id as wrong_member_id from public.mitglied where name='P11 RPC7 FalscherPächter' \gset

-- The workflow trigger is intentionally bypassed only to construct finalized,
-- synthetic source documents. The RPC's own provenance checks remain active.
set local session_replication_role=replica;
insert into public.kommunikationsvorgang(vorgangstyp,dokumenttyp,status,mitglied_id,parzelle_id,revision,entwurf_daten)
values ('vertrag_signatur','pachtvertrag','entwurf',:'hauptpaechter_id',:'parzelle_id',1,'{}')
returning id as vorgang_id \gset
insert into public.dokument(parzelle_id,storage_path,titel,dateiname,mime_type,source_kommunikationsvorgang_id,source_revision)
values (:'parzelle_id','p11/rpc7-valid.pdf','P11 RPC7 gültig','p11-rpc7-valid.pdf','application/pdf',:'vorgang_id',1)
returning id as dokument_id \gset
update public.kommunikationsvorgang
   set status='letzte_pruefung', final_dokument_id=:'dokument_id'
 where id=:'vorgang_id';
insert into public.dokument(parzelle_id,storage_path,titel,dateiname,mime_type)
values (:'other_parzelle_id','p11/rpc7-wrong-parcel.pdf','P11 RPC7 falsche Parzelle','p11-rpc7-wrong-parcel.pdf','application/pdf')
returning id as wrong_parzelle_dokument_id \gset
insert into public.kommunikationsvorgang(vorgangstyp,dokumenttyp,status,mitglied_id,parzelle_id,revision,entwurf_daten)
values ('vertrag_signatur','pachtvertrag','entwurf',:'hauptpaechter_id',:'other_parzelle_id',1,'{}')
returning id as wrong_member_vorgang_id \gset
insert into public.dokument(parzelle_id,storage_path,titel,dateiname,mime_type,source_kommunikationsvorgang_id,source_revision)
values (:'other_parzelle_id','p11/rpc7-wrong-member.pdf','P11 RPC7 falscher Pächter','p11-rpc7-wrong-member.pdf','application/pdf',:'wrong_member_vorgang_id',1)
returning id as wrong_member_dokument_id \gset
update public.kommunikationsvorgang
   set status='letzte_pruefung', final_dokument_id=:'wrong_member_dokument_id'
 where id=:'wrong_member_vorgang_id';
set local session_replication_role=origin;

insert into p11_rpc7_ids values
  (:'parzelle_id',:'other_parzelle_id',:'hauptpaechter_id',:'mitpaechter_id',:'wrong_member_id',:'dokument_id',:'wrong_parzelle_dokument_id',:'wrong_member_dokument_id',
   '00000000-0000-0000-0000-000000000701','00000000-0000-0000-0000-000000000702','00000000-0000-0000-0000-000000000703','00000000-0000-0000-0000-000000000704');
grant select on p11_rpc7_ids to service_role,authenticated;

set local role service_role;
select set_config('request.jwt.claim.role','service_role',true);
do $$
declare v p11_rpc7_ids%rowtype; v_lease bigint; v_fassung bigint; v_before bigint;
begin
  select * into v from p11_rpc7_ids;
  -- The effective grant may register, and all three relational artifacts exist.
  select pachtverhaeltnis_id,pachtvertragsfassung_id into v_lease,v_fassung
    from public.register_pachtverhaeltnis_erstvertrag(v.parzelle_id,v.hauptpaechter_id,date '2026-11-10',v.dokument_id,date '2026-11-10',date '2026-11-10',v.granted_user,v.mitpaechter_id);
  if v_lease is null or v_fassung is null then raise exception 'RPC7 grant did not return lease and version IDs'; end if;
  if not exists(select 1 from public.pachtverhaeltnis p where p.id=v_lease and p.parzelle_id=v.parzelle_id and p.urspruenglicher_pachtbeginn=date '2026-11-10' and p.created_by=v.granted_user) then raise exception 'RPC7 lease was not persisted correctly'; end if;
  if (select count(*) from public.pachtverhaeltnis_beteiligung b where b.pachtverhaeltnis_id=v_lease and b.gueltig_bis is null and ((b.rolle='hauptpaechter' and b.mitglied_id=v.hauptpaechter_id) or (b.rolle='mitpaechter' and b.mitglied_id=v.mitpaechter_id))) <> 2 then raise exception 'RPC7 parties were not linked correctly'; end if;
  if not exists(select 1 from public.pachtvertragsfassung f where f.id=v_fassung and f.pachtverhaeltnis_id=v_lease and f.dokument_id=v.dokument_id and f.fassungstyp='erstvertrag' and f.wirksam_ab=date '2026-11-10' and f.vorgaenger_fassung_id is null) then raise exception 'RPC7 version was not persisted correctly'; end if;
  -- Missing right, revocation, and an actor without an app_user/member binding cannot impersonate the grant.
  begin perform public.register_pachtverhaeltnis_erstvertrag(v.other_parzelle_id,v.hauptpaechter_id,date '2026-11-11',v.dokument_id,date '2026-11-11',date '2026-11-11',v.denied_user); raise exception 'RPC7 missing right unexpectedly succeeded'; exception when sqlstate '42501' then null; end;
  begin perform public.register_pachtverhaeltnis_erstvertrag(v.other_parzelle_id,v.hauptpaechter_id,date '2026-11-11',v.dokument_id,date '2026-11-11',date '2026-11-11',v.revoked_admin_user); raise exception 'RPC7 revoked admin unexpectedly succeeded'; exception when sqlstate '42501' then null; end;
  begin perform public.register_pachtverhaeltnis_erstvertrag(v.other_parzelle_id,v.hauptpaechter_id,date '2026-11-11',v.dokument_id,date '2026-11-11',date '2026-11-11',v.invalid_actor); raise exception 'RPC7 invalid actor unexpectedly succeeded'; exception when sqlstate '42501' then null; end;
  -- Invalid data must fail before writes, including mismatched date, parcel and party.
  select count(*) into v_before from public.pachtverhaeltnis;
  begin perform public.register_pachtverhaeltnis_erstvertrag(v.other_parzelle_id,v.hauptpaechter_id,date '2026-11-11',v.dokument_id,date '2026-11-11',date '2026-11-12',v.granted_user); raise exception 'RPC7 mismatched dates unexpectedly succeeded'; exception when sqlstate '23514' then null; end;
  begin perform public.register_pachtverhaeltnis_erstvertrag(v.other_parzelle_id,v.hauptpaechter_id,date '2026-11-11',v.dokument_id,date '2026-11-11',date '2026-11-11',v.granted_user); raise exception 'RPC7 document parcel mismatch unexpectedly succeeded'; exception when sqlstate '23514' then null; end;
  begin perform public.register_pachtverhaeltnis_erstvertrag(v.other_parzelle_id,v.wrong_member_id,date '2026-11-11',v.wrong_member_dokument_id,date '2026-11-11',date '2026-11-11',v.granted_user); raise exception 'RPC7 workflow party mismatch unexpectedly succeeded'; exception when sqlstate '23514' then null; end;
  if (select count(*) from public.pachtverhaeltnis) <> v_before then raise exception 'RPC7 invalid requests left a partial lease'; end if;
  -- A second first-contract registration on the same parcel is rejected and leaves the original intact.
  begin perform public.register_pachtverhaeltnis_erstvertrag(v.parzelle_id,v.hauptpaechter_id,date '2026-11-10',v.dokument_id,date '2026-11-10',date '2026-11-10',v.granted_user); raise exception 'RPC7 duplicate registration unexpectedly succeeded'; exception when sqlstate '23505' then null; end;
  if (select count(*) from public.pachtverhaeltnis where parzelle_id=v.parzelle_id and beendet_am is null) <> 1 then raise exception 'RPC7 duplicate protection changed active lease count'; end if;
end;
$$;
reset role;

-- The public/authenticated API cannot invoke this internal service-only RPC.
set local role authenticated;
select set_config('request.jwt.claim.role','authenticated',true);
do $$
declare v p11_rpc7_ids%rowtype;
begin
  select * into v from p11_rpc7_ids;
  begin perform public.register_pachtverhaeltnis_erstvertrag(v.parzelle_id,v.hauptpaechter_id,date '2026-11-10',v.dokument_id,date '2026-11-10',date '2026-11-10',v.granted_user); raise exception 'RPC7 direct authenticated call unexpectedly succeeded'; exception when sqlstate '42501' then null; end;
end;
$$;
reset role;

do $$
begin
  if not exists(select 1 from pg_proc p join pg_namespace n on n.oid=p.pronamespace where n.nspname='public' and p.oid='public.register_pachtverhaeltnis_erstvertrag(bigint,bigint,date,bigint,date,date,uuid,bigint)'::regprocedure and p.prosecdef and coalesce(p.proconfig,'{}'::text[]) && array['search_path=public, pg_temp','search_path=public,pg_temp']) then raise exception 'RPC7 lacks SECURITY DEFINER or fixed search_path'; end if;
  if has_function_privilege('anon','public.register_pachtverhaeltnis_erstvertrag(bigint,bigint,date,bigint,date,date,uuid,bigint)','execute') or has_function_privilege('authenticated','public.register_pachtverhaeltnis_erstvertrag(bigint,bigint,date,bigint,date,date,uuid,bigint)','execute') then raise exception 'RPC7 is directly executable by a client role'; end if;
  if not has_function_privilege('service_role','public.register_pachtverhaeltnis_erstvertrag(bigint,bigint,date,bigint,date,date,uuid,bigint)','execute') then raise exception 'RPC7 service role grant is missing'; end if;
end;
$$;

rollback;
