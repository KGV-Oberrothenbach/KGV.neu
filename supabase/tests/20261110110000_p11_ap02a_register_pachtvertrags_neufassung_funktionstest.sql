-- P11 AP-02A / RPC 8: real local PostgreSQL acceptance, synthetic and rolled back.
begin;
create temporary table p11_rpc8_ids(parcel bigint, member_id bigint, actor uuid, denied uuid, revoked uuid, invalid uuid, initial_doc bigint, revision_doc bigint, lease_id bigint, predecessor_id bigint) on commit drop;
insert into auth.users(id,aud,role,email,encrypted_password,email_confirmed_at,raw_app_meta_data,raw_user_meta_data,created_at,updated_at) values
 ('00000000-0000-0000-0000-000000000801','authenticated','authenticated','p11-rpc8-grant@example.test','x',now(),'{}','{}',now(),now()),
 ('00000000-0000-0000-0000-000000000802','authenticated','authenticated','p11-rpc8-denied@example.test','x',now(),'{}','{}',now(),now()),
 ('00000000-0000-0000-0000-000000000803','authenticated','authenticated','p11-rpc8-revoked@example.test','x',now(),'{}','{}',now(),now()),
 ('00000000-0000-0000-0000-000000000804','authenticated','authenticated','p11-rpc8-invalid@example.test','x',now(),'{}','{}',now(),now());
insert into public.mitglied(name,vorname,aktiv,aufnahme_status,auth_user_id) values
 ('P11 RPC8 Grant','Test',true,'aufgenommen','00000000-0000-0000-0000-000000000801'),('P11 RPC8 Denied','Test',true,'aufgenommen','00000000-0000-0000-0000-000000000802'),('P11 RPC8 Revoked','Test',true,'aufgenommen','00000000-0000-0000-0000-000000000803'),('P11 RPC8 Tenant','Test',true,'aufgenommen',null);
update public.app_user set role='user',permission_grants=524288 where user_id='00000000-0000-0000-0000-000000000801';
update public.app_user set role='user',permission_grants=0,permission_revocations=0 where user_id='00000000-0000-0000-0000-000000000802';
update public.app_user set role='admin',permission_grants=0,permission_revocations=524288 where user_id='00000000-0000-0000-0000-000000000803';
insert into public.parzelle(garten_nr,"Anlage") values('P11-RPC8','P11') returning id as parcel \gset
select id as member_id from public.mitglied where name='P11 RPC8 Tenant' \gset
set local session_replication_role=replica;
insert into public.kommunikationsvorgang(vorgangstyp,dokumenttyp,status,mitglied_id,parzelle_id,revision,entwurf_daten) values('vertrag_signatur','pachtvertrag','entwurf',:'member_id',:'parcel',1,'{}') returning id as initial_vorgang \gset
insert into public.dokument(parzelle_id,storage_path,titel,dateiname,mime_type,source_kommunikationsvorgang_id,source_revision) values(:'parcel','p11/rpc8-initial.pdf','P11 RPC8 Erst','p11-rpc8-initial.pdf','application/pdf',:'initial_vorgang',1) returning id as initial_doc \gset
update public.kommunikationsvorgang set status='erledigt',final_dokument_id=:'initial_doc' where id=:'initial_vorgang';
insert into public.kommunikationsvorgang(vorgangstyp,dokumenttyp,status,mitglied_id,parzelle_id,revision,entwurf_daten) values('vertrag_signatur','pachtvertrag','entwurf',:'member_id',:'parcel',1,'{}') returning id as revision_vorgang \gset
insert into public.dokument(parzelle_id,storage_path,titel,dateiname,mime_type,source_kommunikationsvorgang_id,source_revision) values(:'parcel','p11/rpc8-revision.pdf','P11 RPC8 Neu','p11-rpc8-revision.pdf','application/pdf',:'revision_vorgang',1) returning id as revision_doc \gset
update public.kommunikationsvorgang set status='letzte_pruefung',final_dokument_id=:'revision_doc' where id=:'revision_vorgang';
set local session_replication_role=origin;
insert into p11_rpc8_ids(parcel,member_id,actor,denied,revoked,invalid,initial_doc,revision_doc) values(:'parcel',:'member_id','00000000-0000-0000-0000-000000000801','00000000-0000-0000-0000-000000000802','00000000-0000-0000-0000-000000000803','00000000-0000-0000-0000-000000000804',:'initial_doc',:'revision_doc');
grant select,update on p11_rpc8_ids to service_role,authenticated;
set local role service_role; select set_config('request.jwt.claim.role','service_role',true);
do $$ declare v p11_rpc8_ids%rowtype; n bigint; before_count bigint; begin
 select * into v from p11_rpc8_ids;
 select pachtverhaeltnis_id,pachtvertragsfassung_id into v.lease_id,v.predecessor_id from public.register_pachtverhaeltnis_erstvertrag(v.parcel,v.member_id,date '2026-11-10',v.initial_doc,date '2026-11-10',date '2026-11-10',v.actor);
 update p11_rpc8_ids set lease_id=v.lease_id,predecessor_id=v.predecessor_id;
 select pachtvertragsfassung_id into n from public.register_pachtvertrags_neufassung(v.lease_id,v.predecessor_id,v.revision_doc,date '2026-11-11',date '2026-11-11',v.actor);
 if not exists(select 1 from public.pachtvertragsfassung f where f.id=n and f.pachtverhaeltnis_id=v.lease_id and f.vorgaenger_fassung_id=v.predecessor_id and f.dokument_id=v.revision_doc and f.fassungstyp='neufassung' and f.vorheriges_vertragsdatum=date '2026-11-10') then raise exception 'RPC8 did not persist successor correctly'; end if;
 if (select count(*) from public.pachtverhaeltnis where id=v.lease_id)<>1 or not exists(select 1 from public.pachtverhaeltnis_beteiligung where pachtverhaeltnis_id=v.lease_id and mitglied_id=v.member_id and rolle='hauptpaechter' and gueltig_bis is null) then raise exception 'RPC8 changed lease or parties'; end if;
 select count(*) into before_count from public.pachtvertragsfassung where pachtverhaeltnis_id=v.lease_id;
 begin perform public.register_pachtvertrags_neufassung(v.lease_id,v.predecessor_id,v.revision_doc,date '2026-11-11',date '2026-11-11',v.actor); raise exception 'duplicate successor succeeded'; exception when sqlstate '23505' then null; end;
 begin perform public.register_pachtvertrags_neufassung(v.lease_id,n,v.revision_doc,date '2026-11-11',date '2026-11-11',v.denied); raise exception 'missing right succeeded'; exception when sqlstate '42501' then null; end;
 begin perform public.register_pachtvertrags_neufassung(v.lease_id,n,v.revision_doc,date '2026-11-11',date '2026-11-11',v.revoked); raise exception 'revoked admin succeeded'; exception when sqlstate '42501' then null; end;
 begin perform public.register_pachtvertrags_neufassung(v.lease_id,n,v.revision_doc,date '2026-11-11',date '2026-11-11',v.invalid); raise exception 'invalid actor succeeded'; exception when sqlstate '42501' then null; end;
 begin perform public.register_pachtvertrags_neufassung(999999999,n,v.revision_doc,date '2026-11-11',date '2026-11-11',v.actor); raise exception 'unknown lease succeeded'; exception when sqlstate 'P0002' then null; end;
 begin perform public.register_pachtvertrags_neufassung(v.lease_id,n,v.revision_doc,date '2026-11-09',date '2026-11-09',v.actor); raise exception 'invalid dates succeeded'; exception when sqlstate '23514' then null; end;
 if (select count(*) from public.pachtvertragsfassung where pachtverhaeltnis_id=v.lease_id)<>before_count then raise exception 'failed RPC8 calls left partial revision'; end if;
end $$;
reset role; set local role authenticated; select set_config('request.jwt.claim.role','authenticated',true);
do $$ declare v p11_rpc8_ids%rowtype; begin select * into v from p11_rpc8_ids; begin perform public.register_pachtvertrags_neufassung(v.lease_id,v.predecessor_id,v.revision_doc,date '2026-11-11',date '2026-11-11',v.actor); raise exception 'authenticated RPC8 succeeded'; exception when sqlstate '42501' then null; end; end $$;
reset role; rollback;
