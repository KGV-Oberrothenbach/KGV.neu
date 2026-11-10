-- P11 AP-02A / A3.2: synthetic, real PostgreSQL test; all data rolls back.
begin;

insert into auth.users(id,aud,role,email,encrypted_password,email_confirmed_at,raw_app_meta_data,raw_user_meta_data,created_at,updated_at)
values
 ('00000000-0000-0000-0000-000000000d31','authenticated','authenticated','p11-a32-signer@example.test','x',now(),'{}','{}',now(),now()),
 ('00000000-0000-0000-0000-000000000d32','authenticated','authenticated','p11-a32-foreign@example.test','x',now(),'{}','{}',now(),now());
insert into public.mitglied(name,vorname,aktiv,aufnahme_status,auth_user_id)
values ('P11 A32 Signer','Test',true,'aufgenommen','00000000-0000-0000-0000-000000000d31') returning id as signer_member_id \gset
insert into public.mitglied(name,vorname,aktiv,aufnahme_status,auth_user_id)
values ('P11 A32 Foreign','Test',true,'aufgenommen','00000000-0000-0000-0000-000000000d32') returning id as foreign_member_id \gset

set local session_replication_role=replica;
insert into public.kommunikationsvorgang(vorgangstyp,dokumenttyp,status,mitglied_id,revision,signatur_revision,signatur_gestartet_am,zugewiesen_an,erstellt_von,bearbeitet_von)
values ('vertrag_signatur','mitgliedsantrag','zur_signatur',:'signer_member_id',1,1,now(),'00000000-0000-0000-0000-000000000d31','00000000-0000-0000-0000-000000000d31','00000000-0000-0000-0000-000000000d31') returning id as contract_id \gset
insert into public.prepared_contract_signatur(kommunikationsvorgang_id,signatur_revision,signatur_schluessel,sort_order,pflicht,status,unterzeichner_typ,mitglied_id)
values (:'contract_id',1,'member.application',1,true,'offen','mitglied',:'signer_member_id') returning id as signature_id \gset
set local session_replication_role=origin;
select :'contract_id' || '/1/member.application/00000000-0000-0000-0000-000000000001.png' as storage_path \gset
insert into storage.objects(bucket_id,name) values ('prepared-contract-signatures',:'storage_path');
create temporary table p11_a32_ids(contract_id bigint,signer_member_id bigint,foreign_member_id bigint,storage_path text) on commit drop;
insert into p11_a32_ids values (:'contract_id',:'signer_member_id',:'foreign_member_id',:'storage_path');
grant select on p11_a32_ids to authenticated, service_role;

set local role authenticated;
select set_config('request.jwt.claim.sub','00000000-0000-0000-0000-000000000d31',true);
do $$
declare v record; ids p11_a32_ids%rowtype;
begin
  select * into ids from p11_a32_ids; select * into v from public.speichere_prepared_contract_signatur(ids.contract_id,1,'member.application',ids.signer_member_id,null,null,null,null,ids.storage_path,repeat('a',64));
  if v.status <> 'unterschrieben' or v.mitglied_id <> ids.signer_member_id or v.storage_path <> ids.storage_path then raise exception 'P11 A3.2 positive signer save failed'; end if;
  -- Exact retry is idempotent; altered identity or revision cannot overwrite it.
  perform public.speichere_prepared_contract_signatur(ids.contract_id,1,'member.application',ids.signer_member_id,null,null,null,null,ids.storage_path,repeat('a',64));
  begin perform public.speichere_prepared_contract_signatur(ids.contract_id,1,'member.application',ids.foreign_member_id,null,null,null,null,ids.storage_path,repeat('a',64)); raise exception 'foreign signer succeeded'; exception when sqlstate '42501' then null; end;
  begin perform public.speichere_prepared_contract_signatur(ids.contract_id,2,'member.application',ids.signer_member_id,null,null,null,null,ids.storage_path,repeat('a',64)); raise exception 'stale revision succeeded'; exception when sqlstate '23514' then null; end;
end $$;
select set_config('request.jwt.claim.sub','00000000-0000-0000-0000-000000000d32',true);
do $$ declare ids p11_a32_ids%rowtype; begin select * into ids from p11_a32_ids;
  begin perform public.speichere_prepared_contract_signatur(ids.contract_id,1,'member.application',ids.signer_member_id,null,null,null,null,ids.storage_path,repeat('a',64)); raise exception 'manipulated caller succeeded'; exception when sqlstate '42501' then null; end;
end $$;
reset role;
set local role service_role;
select set_config('request.jwt.claim.role','service_role',true);
do $$ declare ids p11_a32_ids%rowtype; begin select * into ids from p11_a32_ids;
  if not public.prepared_contract_signaturen_vollstaendig(ids.contract_id,1) then raise exception 'signature completeness did not reflect persisted signature'; end if;
  if (select count(*) from public.prepared_contract_signatur where kommunikationsvorgang_id=ids.contract_id and status='unterschrieben') <> 1 then raise exception 'failed calls changed signature state'; end if;
end $$;
reset role;
rollback;
