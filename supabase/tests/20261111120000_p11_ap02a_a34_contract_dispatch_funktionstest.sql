-- P11 A3.4: synthetic local PostgreSQL test; no mail transport is invoked and all data rolls back.
begin;

insert into auth.users(id,aud,role,email,encrypted_password,email_confirmed_at,raw_app_meta_data,raw_user_meta_data,created_at,updated_at)
values
 ('00000000-0000-0000-0000-000000000f41','authenticated','authenticated','p11-a34-grant@example.test','x',now(),'{}','{}',now(),now()),
 ('00000000-0000-0000-0000-000000000f42','authenticated','authenticated','p11-a34-denied@example.test','x',now(),'{}','{}',now(),now()),
 ('00000000-0000-0000-0000-000000000f43','authenticated','authenticated','p11-a34-revoked@example.test','x',now(),'{}','{}',now(),now()),
 ('00000000-0000-0000-0000-000000000f44','authenticated','authenticated','p11-a34-foreign@example.test','x',now(),'{}','{}',now(),now());

insert into public.mitglied(name,vorname,aktiv,aufnahme_status,email,email_info_einwilligung)
values ('P11 A34 Vertragspartei','Test',true,'aufgenommen','p11-a34-recipient@example.test',true)
returning id as member_id \gset

insert into public.app_user(user_id,role,permission_grants,permission_revocations)
values
 ('00000000-0000-0000-0000-000000000f41','user',1073741824,0),
 ('00000000-0000-0000-0000-000000000f42','user',0,0),
 ('00000000-0000-0000-0000-000000000f43','admin',0,1073741824),
 ('00000000-0000-0000-0000-000000000f44','user',1073741824,0);

set local session_replication_role=replica;
insert into public.kommunikationsvorgang(vorgangstyp,dokumenttyp,status,mitglied_id,revision,signatur_revision,signatur_gestartet_am,zugewiesen_an,erstellt_von,bearbeitet_von)
values ('vertrag_signatur','wartungsvertrag','erledigt',:'member_id',1,1,now(),'00000000-0000-0000-0000-000000000f41','00000000-0000-0000-0000-000000000f41','00000000-0000-0000-0000-000000000f41')
returning id as contract_id \gset
insert into public.dokument(mitglied_id,bucket,storage_path,titel,dateiname,mime_type,drive_file_id,source_kommunikationsvorgang_id,source_revision)
values (:'member_id','dokumente','p11/a34/final.pdf','P11 A34 Final','p11-a34-final.pdf','application/pdf','p11-a34-drive-file',:'contract_id',1)
returning id as final_document_id \gset
update public.kommunikationsvorgang set final_dokument_id=:'final_document_id' where id=:'contract_id';
insert into public.prepared_contract_signatur(kommunikationsvorgang_id,signatur_revision,signatur_schluessel,sort_order,pflicht,status,unterzeichner_typ,mitglied_id,unterzeichner_name_snapshot,storage_bucket,storage_path,signatur_sha256,unterzeichnet_am,unterzeichnet_von_auth_user_id)
values (:'contract_id',1,'member.application',1,true,'unterschrieben','mitglied',:'member_id','P11 A34 Vertragspartei','prepared-contract-signatures',:'contract_id' || '/1/member.application/00000000-0000-0000-0000-000000000f41.png',repeat('a',64),now(),'00000000-0000-0000-0000-000000000f41');

-- A foreign final document, an invalid workflow status and a stale document revision.
insert into public.kommunikationsvorgang(vorgangstyp,dokumenttyp,status,mitglied_id,revision,signatur_revision,signatur_gestartet_am,zugewiesen_an,erstellt_von,bearbeitet_von,final_dokument_id)
values
 ('vertrag_signatur','wartungsvertrag','erledigt',:'member_id',1,1,now(),'00000000-0000-0000-0000-000000000f41','00000000-0000-0000-0000-000000000f41','00000000-0000-0000-0000-000000000f41',:'final_document_id'),
 ('vertrag_signatur','wartungsvertrag','letzte_pruefung',:'member_id',1,1,now(),'00000000-0000-0000-0000-000000000f41','00000000-0000-0000-0000-000000000f41','00000000-0000-0000-0000-000000000f41',:'final_document_id'),
 ('vertrag_signatur','wartungsvertrag','erledigt',:'member_id',2,2,now(),'00000000-0000-0000-0000-000000000f41','00000000-0000-0000-0000-000000000f41','00000000-0000-0000-0000-000000000f41',:'final_document_id');
set local session_replication_role=origin;

create temporary table p11_a34_ids(
  contract_id bigint,
  wrong_document_contract_id bigint,
  invalid_status_contract_id bigint,
  stale_revision_contract_id bigint
) on commit drop;
insert into p11_a34_ids
select :contract_id,
       min(id) filter (where final_dokument_id=:'final_document_id' and status='erledigt' and id<>:'contract_id'),
       min(id) filter (where status='letzte_pruefung'),
       min(id) filter (where revision=2)
from public.kommunikationsvorgang
where id>=:'contract_id';
create temporary table p11_a34_dispatches(initial_dispatch_id bigint, redispatch_id bigint) on commit drop;
grant select on p11_a34_ids,p11_a34_dispatches to authenticated,anon,service_role;
grant insert, update on p11_a34_dispatches to authenticated;

-- Positive individual grant: creates one snapshot, preserves the completed signature state and is idempotent.
set local role authenticated;
select set_config('request.jwt.claim.sub','00000000-0000-0000-0000-000000000f41',true);
select set_config('request.jwt.claim.role','authenticated',true);
do $$
declare ids p11_a34_ids%rowtype; first_id bigint; repeated_id bigint;
begin
  select * into ids from p11_a34_ids;
  first_id:=public.prepare_contract_dispatch(ids.contract_id,null);
  repeated_id:=public.prepare_contract_dispatch(ids.contract_id,null);
  if first_id<>repeated_id then raise exception 'initial dispatch was not idempotent'; end if;
  insert into p11_a34_dispatches(initial_dispatch_id) values(first_id);
end $$;

-- No grant, revoked admin and an equally permitted but foreign caseworker are rejected without a second dispatch.
select set_config('request.jwt.claim.sub','00000000-0000-0000-0000-000000000f42',true);
select set_config('request.jwt.claim.role','authenticated',true);
do $$ declare ids p11_a34_ids%rowtype; begin select * into ids from p11_a34_ids;
  begin perform public.prepare_contract_dispatch(ids.contract_id,null); raise exception 'missing right succeeded'; exception when sqlstate '42501' then null; end;
end $$;
select set_config('request.jwt.claim.sub','00000000-0000-0000-0000-000000000f43',true);
select set_config('request.jwt.claim.role','authenticated',true);
do $$ declare ids p11_a34_ids%rowtype; begin select * into ids from p11_a34_ids;
  begin perform public.prepare_contract_dispatch(ids.contract_id,null); raise exception 'revoked admin succeeded'; exception when sqlstate '42501' then null; end;
end $$;
select set_config('request.jwt.claim.sub','00000000-0000-0000-0000-000000000f44',true);
select set_config('request.jwt.claim.role','authenticated',true);
do $$ declare ids p11_a34_ids%rowtype; d p11_a34_dispatches%rowtype; begin select * into ids from p11_a34_ids; select * into d from p11_a34_dispatches;
  begin perform public.prepare_contract_dispatch(ids.contract_id,null); raise exception 'foreign caseworker succeeded'; exception when sqlstate '42501' then null; end;
  begin perform public.claim_contract_dispatch(d.initial_dispatch_id); raise exception 'foreign claim succeeded'; exception when sqlstate '40001' then null; end;
end $$;

-- Provenance, status, revision and manipulated redispatch references must not create partial snapshots.
select set_config('request.jwt.claim.sub','00000000-0000-0000-0000-000000000f41',true);
select set_config('request.jwt.claim.role','authenticated',true);
do $$ declare ids p11_a34_ids%rowtype; before_count integer; after_count integer; begin select * into ids from p11_a34_ids;
  select count(*) into before_count from public.versandinstanz where kommunikationsvorgang_id=ids.contract_id;
  begin perform public.prepare_contract_dispatch(ids.wrong_document_contract_id,null); raise exception 'wrong document succeeded'; exception when sqlstate '23514' then null; end;
  begin perform public.prepare_contract_dispatch(ids.invalid_status_contract_id,null); raise exception 'invalid status succeeded'; exception when sqlstate '23514' then null; end;
  begin perform public.prepare_contract_dispatch(ids.stale_revision_contract_id,null); raise exception 'stale revision succeeded'; exception when sqlstate '23514' then null; end;
  begin perform public.prepare_contract_dispatch(ids.contract_id,987654321); raise exception 'manipulated redispatch reference succeeded'; exception when sqlstate '23514' then null; end;
  select count(*) into after_count from public.versandinstanz where kommunikationsvorgang_id=ids.contract_id;
  if before_count<>after_count then raise exception 'rejected dispatch changed state'; end if;
end $$;
reset role;

set local role service_role;
select set_config('request.jwt.claim.role','service_role',true);
do $$ declare ids p11_a34_ids%rowtype; begin select * into ids from p11_a34_ids;
  if not public.prepared_contract_signaturen_vollstaendig(ids.contract_id,1) then raise exception 'fixture signatures are incomplete'; end if;
end $$;
reset role;

-- A completed initial dispatch may create exactly one open redispatch; repeated calls return it.
update public.versandinstanz set status='versendet',versendet_am=now() where id=(select initial_dispatch_id from p11_a34_dispatches);
set local role authenticated;
select set_config('request.jwt.claim.sub','00000000-0000-0000-0000-000000000f41',true);
select set_config('request.jwt.claim.role','authenticated',true);
do $$ declare d p11_a34_dispatches%rowtype; first_id bigint; repeated_id bigint; begin select * into d from p11_a34_dispatches;
  first_id:=public.create_contract_redispatch(d.initial_dispatch_id);
  repeated_id:=public.create_contract_redispatch(d.initial_dispatch_id);
  if first_id<>repeated_id then raise exception 'redispatch was not idempotent'; end if;
  update p11_a34_dispatches set redispatch_id=first_id;
  begin perform public.create_contract_redispatch(first_id); raise exception 'redispatch from unfinished dispatch succeeded'; exception when sqlstate '23514' then null; end;
end $$;
reset role;

-- The client API is not exposed to anon, whereas trusted service_role remains available.
set local role anon;
select set_config('request.jwt.claim.role','anon',true);
do $$ declare ids p11_a34_ids%rowtype; begin select * into ids from p11_a34_ids;
  begin perform public.prepare_contract_dispatch(ids.contract_id,null); raise exception 'anon direct call succeeded'; exception when sqlstate '42501' then null; end;
end $$;
reset role;
do $$
declare ids p11_a34_ids%rowtype; d p11_a34_dispatches%rowtype;
begin
  select * into ids from p11_a34_ids; select * into d from p11_a34_dispatches;
  if not has_function_privilege('authenticated','public.prepare_contract_dispatch(bigint,bigint)','EXECUTE')
     or has_function_privilege('anon','public.prepare_contract_dispatch(bigint,bigint)','EXECUTE')
     or not has_function_privilege('service_role','public.prepare_contract_dispatch(bigint,bigint)','EXECUTE') then
    raise exception 'unexpected dispatch RPC grants';
  end if;
  if (select count(*) from public.versandinstanz where kommunikationsvorgang_id=ids.contract_id and erneuter_versand_von_id is null)<>1
     or (select count(*) from public.versandinstanz where kommunikationsvorgang_id=ids.contract_id and erneuter_versand_von_id=d.initial_dispatch_id)<>1 then
    raise exception 'dispatch history is not preserved';
  end if;
end $$;

rollback;
