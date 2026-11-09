-- Real local wrapper test; changes are rolled back.
begin;
create temporary table p11_ersatz_ids(fassung bigint, vorgang bigint, granted uuid, denied uuid, revoked uuid, foreign_actor uuid) on commit drop;
insert into auth.users(id,aud,role,email,encrypted_password,email_confirmed_at,raw_app_meta_data,raw_user_meta_data,created_at,updated_at) values
 ('00000000-0000-0000-0000-000000000501','authenticated','authenticated','p11-e1@test','x',now(),'{}','{}',now(),now()),('00000000-0000-0000-0000-000000000502','authenticated','authenticated','p11-e2@test','x',now(),'{}','{}',now(),now()),('00000000-0000-0000-0000-000000000503','authenticated','authenticated','p11-e3@test','x',now(),'{}','{}',now(),now()),('00000000-0000-0000-0000-000000000504','authenticated','authenticated','p11-e4@test','x',now(),'{}','{}',now(),now());
insert into public.app_user(user_id,role,permission_grants,permission_revocations) values ('00000000-0000-0000-0000-000000000501','user',524288,0),('00000000-0000-0000-0000-000000000502','user',0,0),('00000000-0000-0000-0000-000000000503','admin',0,524288),('00000000-0000-0000-0000-000000000504','user',524288,0);
-- Use a real predecessor and already-active replacement to exercise the
-- wrapper's permission, assignment and idempotency contract without changing
-- the separately reserved phase-9 creator.
set local session_replication_role=replica;
insert into public.parzelle(garten_nr,"Anlage") values ('P11-RPC5','P11') returning id as parzelle_id \gset
insert into public.dokument(parzelle_id,storage_path,titel,dateiname,mime_type) values (:'parzelle_id','p11/rpc5.pdf','P11 RPC5','p11.pdf','application/pdf') returning id as dokument_id \gset
insert into public.pachtverhaeltnis(parzelle_id) values (:'parzelle_id') returning id as lease_id \gset
insert into public.pachtvertragsfassung(pachtverhaeltnis_id,dokument_id,fassungstyp) values (:'lease_id',:'dokument_id','erstvertrag') returning id as fassung_id \gset
insert into public.kommunikationsvorgang(vorgangstyp,dokumenttyp,status,ersetzt_pachtvertragsfassung_id,zugewiesen_an) values ('vertrag_signatur','pachtvertrag','vorbereitet',:'fassung_id','00000000-0000-0000-0000-000000000501') returning id as vorgang_id \gset
set local session_replication_role=origin;
insert into p11_ersatz_ids values(:'fassung_id',:'vorgang_id','00000000-0000-0000-0000-000000000501','00000000-0000-0000-0000-000000000502','00000000-0000-0000-0000-000000000503','00000000-0000-0000-0000-000000000504');
grant select on p11_ersatz_ids to authenticated;
set local role authenticated;
do $$ declare v record; x bigint; begin select * into v from p11_ersatz_ids;
 perform set_config('request.jwt.claim.sub',v.granted::text,true); select kommunikationsvorgang_id into x from public.prepare_pachtvertrag_ersatz(v.fassung,v.granted); if x<>v.vorgang then raise exception 'grant/idempotency failed'; end if;
 perform set_config('request.jwt.claim.sub',v.denied::text,true); begin perform public.prepare_pachtvertrag_ersatz(v.fassung,v.denied); raise exception 'no right'; exception when sqlstate '42501' then null; end;
 perform set_config('request.jwt.claim.sub',v.revoked::text,true); begin perform public.prepare_pachtvertrag_ersatz(v.fassung,v.revoked); raise exception 'revoked'; exception when sqlstate '42501' then null; end;
 perform set_config('request.jwt.claim.sub',v.foreign_actor::text,true); begin perform public.prepare_pachtvertrag_ersatz(v.fassung,v.granted); raise exception 'foreign'; exception when sqlstate '42501' then null; end;
 perform set_config('request.jwt.claim.sub',v.granted::text,true); begin perform public.prepare_pachtvertrag_ersatz(-1,v.granted); raise exception 'invalid'; exception when sqlstate 'P0002' then null; end;
end $$;
rollback;
