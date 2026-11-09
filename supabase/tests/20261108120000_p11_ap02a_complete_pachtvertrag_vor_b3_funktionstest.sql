-- P11 AP-02A / P11-R01: real local-database test for service-only prepared
-- lease completion. Fixture data are synthetic and rolled back.
begin;

create temporary table p11_lease_test_ids (
  vorgang_id bigint not null, invalid_vorgang_id bigint not null,
  granted_user uuid not null, denied_user uuid not null, revoked_admin_user uuid not null,
  foreign_granted_user uuid not null
) on commit drop;

insert into auth.users(id,aud,role,email,encrypted_password,email_confirmed_at,raw_app_meta_data,raw_user_meta_data,created_at,updated_at)
values
 ('00000000-0000-0000-0000-000000000301','authenticated','authenticated','p11-lease-granted@example.test','x',now(),'{}','{}',now(),now()),
 ('00000000-0000-0000-0000-000000000302','authenticated','authenticated','p11-lease-denied@example.test','x',now(),'{}','{}',now(),now()),
 ('00000000-0000-0000-0000-000000000303','authenticated','authenticated','p11-lease-revoked@example.test','x',now(),'{}','{}',now(),now()),
 ('00000000-0000-0000-0000-000000000304','authenticated','authenticated','p11-lease-foreign@example.test','x',now(),'{}','{}',now(),now());

insert into public.mitglied(name,vorname,aktiv,aufnahme_status,auth_user_id,is_demo)
values
 ('P11 Pacht Recht','Test',true,'aufgenommen','00000000-0000-0000-0000-000000000301',false),
 ('P11 Pacht KeinRecht','Test',true,'aufgenommen','00000000-0000-0000-0000-000000000302',false),
 ('P11 Pacht Entzug','Test',true,'aufgenommen','00000000-0000-0000-0000-000000000303',false),
 ('P11 Pacht Fremd','Test',true,'aufgenommen','00000000-0000-0000-0000-000000000304',false),
 ('P11 Pacht Pächter','Test',true,'aufgenommen',null,false)
returning id;

update public.app_user set role='user',permission_grants=524288,permission_revocations=0,is_demo_account=false where user_id in ('00000000-0000-0000-0000-000000000301','00000000-0000-0000-0000-000000000304');
update public.app_user set role='user',permission_grants=0,permission_revocations=0,is_demo_account=false where user_id='00000000-0000-0000-0000-000000000302';
update public.app_user set role='admin',permission_grants=0,permission_revocations=524288,is_demo_account=false where user_id='00000000-0000-0000-0000-000000000303';

insert into public.parzelle(garten_nr,"Anlage",is_demo) values ('P11-RPC3','P11',false) returning id as parzelle_id \gset
select id as applicant_mitglied_id from public.mitglied where name='P11 Pacht Pächter' \gset

-- Construct finalized and intentionally invalid workflow rows without
-- lifecycle-trigger side effects; the RPC's own checks remain active.
set local session_replication_role=replica;
insert into public.kommunikationsvorgang(vorgangstyp,dokumenttyp,status,mitglied_id,parzelle_id,revision,zugewiesen_an,entwurf_daten)
values ('vertrag_signatur','pachtvertrag','entwurf',:'applicant_mitglied_id',:'parzelle_id',1,'00000000-0000-0000-0000-000000000301','{"draft":{"startDate":"2026-11-08","includeSecondaryMember":false}}')
returning id as vorgang_id \gset
insert into public.dokument(parzelle_id,storage_path,titel,dateiname,mime_type,source_kommunikationsvorgang_id,source_revision)
values (:'parzelle_id','p11/complete-pachtvertrag.pdf','P11 Pachtvertrag','p11.pdf','application/pdf',:'vorgang_id',1)
returning id as final_dokument_id \gset
update public.kommunikationsvorgang set status='letzte_pruefung',signatur_revision=1,signatur_gestartet_am=now(),final_dokument_id=:'final_dokument_id' where id=:'vorgang_id';
insert into public.kommunikationsvorgang(vorgangstyp,dokumenttyp,status,mitglied_id,parzelle_id,revision,zugewiesen_an,entwurf_daten)
values ('vertrag_signatur','pachtvertrag','entwurf',:'applicant_mitglied_id',null,1,'00000000-0000-0000-0000-000000000301','{}')
returning id as invalid_vorgang_id \gset
set local session_replication_role=origin;

insert into p11_lease_test_ids values (:'vorgang_id',:'invalid_vorgang_id',
 '00000000-0000-0000-0000-000000000301','00000000-0000-0000-0000-000000000302','00000000-0000-0000-0000-000000000303',
 '00000000-0000-0000-0000-000000000304');
grant select on p11_lease_test_ids to service_role,authenticated;

set local role service_role;
select set_config('request.jwt.claim.role','service_role',true);
do $$
declare v record; v_done boolean; v_lease bigint;
begin
 select * into v from p11_lease_test_ids;
 begin perform public.complete_prepared_pachtvertrag_vor_b3(v.vorgang_id,1,v.foreign_granted_user); raise exception 'foreign assignment unexpectedly succeeded'; exception when sqlstate '42501' then null; end;
 begin perform public.complete_prepared_pachtvertrag_vor_b3(v.vorgang_id,1,v.denied_user); raise exception 'no-right unexpectedly succeeded'; exception when sqlstate '42501' then null; end;
 begin perform public.complete_prepared_pachtvertrag_vor_b3(v.vorgang_id,1,v.revoked_admin_user); raise exception 'revoked admin unexpectedly succeeded'; exception when sqlstate '42501' then null; end;
 select pachtverhaeltnis_id,bereits_abgeschlossen into v_lease,v_done from public.complete_prepared_pachtvertrag_vor_b3(v.vorgang_id,1,v.granted_user);
 if v_lease is null or v_done then raise exception 'granted lease completion did not create a lease'; end if;
 begin perform public.complete_prepared_pachtvertrag_vor_b3(v.invalid_vorgang_id,1,v.granted_user); raise exception 'invalid workflow unexpectedly succeeded'; exception when sqlstate '23514' then null; end;
end;
$$;
reset role;
set local role authenticated;
select set_config('request.jwt.claim.role','authenticated',true);
do $$ begin perform public.complete_prepared_pachtvertrag_vor_b3(1,1,'00000000-0000-0000-0000-000000000301'); raise exception 'direct authenticated call unexpectedly succeeded'; exception when sqlstate '42501' then null; end $$;
reset role;
rollback;
