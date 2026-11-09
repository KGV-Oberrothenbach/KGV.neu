-- P11 AP-02A / P11-R01: real, rolled-back database test for contract discard.
begin;
create temporary table p11_discard_ids(vorgang_id bigint, abgeschlossen_id bigint, granted uuid, denied uuid, revoked uuid, foreign_actor uuid) on commit drop;
insert into auth.users(id,aud,role,email,encrypted_password,email_confirmed_at,raw_app_meta_data,raw_user_meta_data,created_at,updated_at) values
 ('00000000-0000-0000-0000-000000000401','authenticated','authenticated','p11-discard-granted@example.test','x',now(),'{}','{}',now(),now()),
 ('00000000-0000-0000-0000-000000000402','authenticated','authenticated','p11-discard-denied@example.test','x',now(),'{}','{}',now(),now()),
 ('00000000-0000-0000-0000-000000000403','authenticated','authenticated','p11-discard-revoked@example.test','x',now(),'{}','{}',now(),now()),
 ('00000000-0000-0000-0000-000000000404','authenticated','authenticated','p11-discard-foreign@example.test','x',now(),'{}','{}',now(),now());
insert into public.mitglied(name,vorname,aktiv,aufnahme_status) values ('P11 Verwerfen','Antrag',false,'antrag_offen') returning id as applicant_id \gset
insert into public.app_user(user_id,role,permission_grants,permission_revocations) values
 ('00000000-0000-0000-0000-000000000401','user',524304,0),('00000000-0000-0000-0000-000000000402','user',0,0),('00000000-0000-0000-0000-000000000403','admin',0,524304),('00000000-0000-0000-0000-000000000404','user',524304,0);
set local session_replication_role=replica;
insert into public.kommunikationsvorgang(vorgangstyp,dokumenttyp,status,mitglied_id,revision,zugewiesen_an) values ('vertrag_signatur','mitgliedsantrag','entwurf',:'applicant_id',1,'00000000-0000-0000-0000-000000000401') returning id as vorgang_id \gset
insert into public.dokument(mitglied_id,storage_path,titel,dateiname,mime_type,source_kommunikationsvorgang_id,source_revision) values (:'applicant_id','p11/discard.pdf','P11 Vertrag','p11.pdf','application/pdf',:'vorgang_id',1) returning id as final_dokument_id \gset
update public.kommunikationsvorgang set status='letzte_pruefung',final_dokument_id=:'final_dokument_id' where id=:'vorgang_id';
insert into public.kommunikationsvorgang(vorgangstyp,dokumenttyp,status,mitglied_id,revision,zugewiesen_an) values ('vertrag_signatur','mitgliedsantrag','erledigt',:'applicant_id',1,'00000000-0000-0000-0000-000000000401') returning id as abgeschlossen_id \gset
set local session_replication_role=origin;
insert into p11_discard_ids values (:'vorgang_id',:'abgeschlossen_id','00000000-0000-0000-0000-000000000401','00000000-0000-0000-0000-000000000402','00000000-0000-0000-0000-000000000403','00000000-0000-0000-0000-000000000404');
grant select on p11_discard_ids to service_role,authenticated;
set local role service_role; select set_config('request.jwt.claim.role','service_role',true);
do $$ declare v record; begin select * into v from p11_discard_ids;
 begin perform public.discard_prepared_contract(v.vorgang_id,1,v.denied); raise exception 'no right'; exception when sqlstate '42501' then null; end;
 begin perform public.discard_prepared_contract(v.vorgang_id,1,v.revoked); raise exception 'revoked'; exception when sqlstate '42501' then null; end;
 begin perform public.discard_prepared_contract(v.vorgang_id,1,v.foreign_actor); raise exception 'foreign'; exception when sqlstate '40001' then null; end;
 perform public.discard_prepared_contract(v.vorgang_id,1,v.granted);
 if not exists(select 1 from public.kommunikationsvorgang where id=v.vorgang_id and status='verworfen' and final_dokument_id is not null) or exists(select 1 from public.dokument where source_kommunikationsvorgang_id=v.vorgang_id and archiviert_at is null) then raise exception 'discard data consistency failed'; end if;
 begin perform public.discard_prepared_contract(v.abgeschlossen_id,1,v.granted); raise exception 'completed'; exception when sqlstate '23514' then null; end;
end $$;
reset role; set local role authenticated; select set_config('request.jwt.claim.role','authenticated',true);
do $$ begin perform public.discard_prepared_contract(1,1,'00000000-0000-0000-0000-000000000401'); raise exception 'direct'; exception when sqlstate '42501' then null; end $$;
reset role; rollback;
