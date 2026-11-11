-- P11 A3.3: synthetic local PostgreSQL test; all fixture data is rolled back.
begin;
insert into auth.users(id,aud,role,email,encrypted_password,email_confirmed_at,raw_app_meta_data,raw_user_meta_data,created_at,updated_at) values
 ('00000000-0000-0000-0000-000000000e31','authenticated','authenticated','p11-a33-grant@example.test','x',now(),'{}','{}',now(),now()),
 ('00000000-0000-0000-0000-000000000e32','authenticated','authenticated','p11-a33-denied@example.test','x',now(),'{}','{}',now(),now()),
 ('00000000-0000-0000-0000-000000000e33','authenticated','authenticated','p11-a33-revoked@example.test','x',now(),'{}','{}',now(),now());
insert into public.mitglied(name,vorname,aktiv,aufnahme_status) values ('P11 A33 Applicant','Test',false,'antrag_offen') returning id as member_id \gset
insert into public.app_user(user_id,role,permission_grants,permission_revocations) values ('00000000-0000-0000-0000-000000000e31','user',524288,0),('00000000-0000-0000-0000-000000000e32','user',0,0),('00000000-0000-0000-0000-000000000e33','admin',0,524288);
insert into public.vereinsdokument_fassung(dokumenttyp_code,fassung,gueltig_ab) values ('satzung','P11-A33',current_date-1) returning id as version_id \gset
set local session_replication_role=replica;
insert into public.kommunikationsvorgang(vorgangstyp,dokumenttyp,status,mitglied_id,revision,zugewiesen_an,erstellt_von,bearbeitet_von,entwurf_daten) values ('vertrag_signatur','mitgliedsantrag','vorbereitet',:'member_id',1,'00000000-0000-0000-0000-000000000e31','00000000-0000-0000-0000-000000000e31','00000000-0000-0000-0000-000000000e31','{}') returning id as contract_id \gset
set local session_replication_role=origin;
create temporary table p11_a33_ids(contract_id bigint,member_id bigint) on commit drop; insert into p11_a33_ids values (:'contract_id',:'member_id'); grant select on p11_a33_ids to authenticated;
set local role authenticated; select set_config('request.jwt.claim.sub','00000000-0000-0000-0000-000000000e31',true);
do $$ declare x p11_a33_ids%rowtype; begin select * into x from p11_a33_ids; perform public.dokumentiere_prepared_contract_papieruebergabe(x.contract_id,'satzung','P11-A33',now()-interval '1 minute','P11'); end $$;
select set_config('request.jwt.claim.sub','00000000-0000-0000-0000-000000000e32',true);
do $$ declare x p11_a33_ids%rowtype; begin select * into x from p11_a33_ids; begin perform public.dokumentiere_prepared_contract_papieruebergabe(x.contract_id,'satzung','P11-A33',now()-interval '1 minute',null); raise exception 'missing right succeeded'; exception when sqlstate '42501' then null; end; end $$;
select set_config('request.jwt.claim.sub','00000000-0000-0000-0000-000000000e33',true);
do $$ declare x p11_a33_ids%rowtype; begin select * into x from p11_a33_ids; begin perform public.dokumentiere_prepared_contract_papieruebergabe(x.contract_id,'satzung','P11-A33',now()-interval '1 minute',null); raise exception 'revoked admin succeeded'; exception when sqlstate '42501' then null; end; end $$;
reset role;
do $$ declare x p11_a33_ids%rowtype; begin select * into x from p11_a33_ids; if (select revision from public.kommunikationsvorgang where id=x.contract_id)<>2 or not ((select entwurf_daten from public.kommunikationsvorgang where id=x.contract_id)->'vereinsunterlagenNachweis' @> '[{"dokumenttyp":"satzung","bereitstellungsart":"papier"}]') then raise exception 'paper handover was not persisted correctly'; end if; end $$;
rollback;
