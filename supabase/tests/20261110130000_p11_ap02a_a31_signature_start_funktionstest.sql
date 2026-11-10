-- P11 AP-02A / A3.1: real local PostgreSQL acceptance; synthetic and rolled back.
begin;
create temporary table p11_a31_ids(granted uuid,denied uuid,revoked uuid,granted_vorgang bigint,denied_vorgang bigint,revoked_vorgang bigint) on commit drop;
insert into public.mitglied(name,vorname,aktiv,aufnahme_status) values
 ('P11 A3.1 Applicant grant','Test',false,'antrag_offen'),('P11 A3.1 Applicant denied','Test',false,'antrag_offen'),('P11 A3.1 Applicant revoked','Test',false,'antrag_offen'),('P11 A3.1 Signer One','Test',true,'aufgenommen'),('P11 A3.1 Signer Two','Test',true,'aufgenommen');
insert into public.vereinsfunktion(schluessel,bezeichnung) values ('p11_a31_signer_one','P11 A3.1 signer one'),('p11_a31_signer_two','P11 A3.1 signer two');
insert into public.vereinsfunktion_verwendung(vereinsfunktion_id,verwendungszweck)
select id,'vertragsunterzeichnung' from public.vereinsfunktion where schluessel in ('p11_a31_signer_one','p11_a31_signer_two');
insert into public.vereinsfunktion_besetzung(vereinsfunktion_id,mitglied_id,gueltig_ab)
select f.id,m.id,current_date from public.vereinsfunktion f join public.mitglied m on (f.schluessel='p11_a31_signer_one' and m.name='P11 A3.1 Signer One') or (f.schluessel='p11_a31_signer_two' and m.name='P11 A3.1 Signer Two');
set local session_replication_role=replica;
insert into public.kommunikationsvorgang(vorgangstyp,dokumenttyp,status,mitglied_id,revision,zugewiesen_an,erstellt_von,bearbeitet_von) values
 ('vertrag_signatur','mitgliedsantrag','vorbereitet',(select id from public.mitglied where name='P11 A3.1 Applicant grant'),1,'00000000-0000-0000-0000-000000000b31','00000000-0000-0000-0000-000000000b31','00000000-0000-0000-0000-000000000b31'),
 ('vertrag_signatur','mitgliedsantrag','vorbereitet',(select id from public.mitglied where name='P11 A3.1 Applicant denied'),1,'00000000-0000-0000-0000-000000000b32','00000000-0000-0000-0000-000000000b32','00000000-0000-0000-0000-000000000b32'),
 ('vertrag_signatur','mitgliedsantrag','vorbereitet',(select id from public.mitglied where name='P11 A3.1 Applicant revoked'),1,'00000000-0000-0000-0000-000000000b33','00000000-0000-0000-0000-000000000b33','00000000-0000-0000-0000-000000000b33');
set local session_replication_role=origin;
insert into public.app_user(user_id,role,permission_grants,permission_revocations) values
 ('00000000-0000-0000-0000-000000000b31','user',524288,0),('00000000-0000-0000-0000-000000000b32','user',0,0),('00000000-0000-0000-0000-000000000b33','admin',0,524288);
insert into p11_a31_ids values ('00000000-0000-0000-0000-000000000b31','00000000-0000-0000-0000-000000000b32','00000000-0000-0000-0000-000000000b33',(select id from public.kommunikationsvorgang where zugewiesen_an='00000000-0000-0000-0000-000000000b31'),(select id from public.kommunikationsvorgang where zugewiesen_an='00000000-0000-0000-0000-000000000b32'),(select id from public.kommunikationsvorgang where zugewiesen_an='00000000-0000-0000-0000-000000000b33'));
grant select on p11_a31_ids to authenticated;
set local role authenticated;
do $$ declare v p11_a31_ids%rowtype; v_result public.kommunikationsvorgang%rowtype; begin select * into v from p11_a31_ids;
 perform set_config('request.jwt.claim.sub',v.granted::text,true); select * into v_result from public.start_prepared_contract_signature(v.granted_vorgang,1,v.granted);
 if v_result.status <> 'zur_signatur' or v_result.signatur_revision <> 1 then raise exception 'P11 A3.1 positive start did not return the frozen signature revision'; end if;
 perform set_config('request.jwt.claim.sub',v.denied::text,true); begin perform public.start_prepared_contract_signature(v.denied_vorgang,1,v.denied); raise exception 'P11 A3.1 denied caller succeeded'; exception when sqlstate '42501' then null; end;
 perform set_config('request.jwt.claim.sub',v.revoked::text,true); begin perform public.start_prepared_contract_signature(v.revoked_vorgang,1,v.revoked); raise exception 'P11 A3.1 revoked admin succeeded'; exception when sqlstate '42501' then null; end;
 perform set_config('request.jwt.claim.sub',v.granted::text,true); begin perform public.start_prepared_contract_signature(v.granted_vorgang,0,v.granted); raise exception 'P11 A3.1 stale revision succeeded'; exception when sqlstate '40001' then null; end;
 begin perform public.start_prepared_contract_signature(v.denied_vorgang,1,v.denied); raise exception 'P11 A3.1 manipulated actor succeeded'; exception when sqlstate '42501' then null; end;
end $$;
reset role;
do $$ declare v p11_a31_ids%rowtype; begin select * into v from p11_a31_ids;
 if not exists(select 1 from public.kommunikationsvorgang where id=v.granted_vorgang and status='zur_signatur' and signatur_revision=1)
 or not exists(select 1 from public.prepared_contract_signatur_snapshot_person where kommunikationsvorgang_id=v.granted_vorgang and signatur_revision=1)
 or exists(select 1 from public.kommunikationsvorgang where id in(v.denied_vorgang,v.revoked_vorgang) and status<>'vorbereitet') then raise exception 'P11 A3.1 rejected calls changed workflow state'; end if;
end $$;
rollback;
