-- P11-T07-P: real PostgreSQL preparation contract. The protected change table
-- is inspected only after RESET ROLE; authenticated callers use the RPC.
begin;
insert into auth.users(id,aud,role,email,encrypted_password,email_confirmed_at,raw_app_meta_data,raw_user_meta_data,created_at,updated_at) values ('00000000-0000-0000-0000-000000009701','authenticated','authenticated','p11-t07p@example.test','x',now(),'{}','{}',now(),now());
insert into public.mitglied(name,vorname,aktiv,aufnahme_status,auth_user_id) values('P11 T07P','Pächter',true,'aufgenommen','00000000-0000-0000-0000-000000009701') returning id as member_id \gset
insert into public.app_user(user_id,mitglied_id,role,permission_grants,permission_revocations) values('00000000-0000-0000-0000-000000009701',:member_id,'vorstand',524288,0) on conflict (user_id) do update set mitglied_id=excluded.mitglied_id,role=excluded.role,permission_grants=excluded.permission_grants,permission_revocations=excluded.permission_revocations;
set local session_replication_role=replica;
insert into public.parzelle(garten_nr,"Anlage",flaeche_qm,hat_strom,hat_wasser) values('P11-T07P','Test',100,false,false) returning id as parcel_id \gset
insert into public.kommunikationsvorgang(vorgangstyp,dokumenttyp,status,mitglied_id,parzelle_id,revision,entwurf_daten) values('vertrag_signatur','pachtvertrag','erledigt',:member_id,:parcel_id,1,'{"draft":{"startDate":"2026-01-01"}}') returning id as source_id \gset
insert into public.dokument(parzelle_id,storage_path,titel,dateiname,mime_type,source_kommunikationsvorgang_id,source_revision) values(:parcel_id,'p11/t07p.pdf','P11','p11.pdf','application/pdf',:source_id,1) returning id as doc_id \gset
insert into public.pachtverhaeltnis(parzelle_id,urspruenglicher_pachtbeginn) values(:parcel_id,'2026-01-01') returning id as lease_id \gset
insert into public.pachtverhaeltnis_beteiligung(pachtverhaeltnis_id,mitglied_id,rolle,gueltig_ab) values(:lease_id,:member_id,'hauptpaechter','2026-01-01');
insert into public.pachtvertragsfassung(pachtverhaeltnis_id,dokument_id,fassungstyp) values(:lease_id,:doc_id,'erstvertrag') returning id as predecessor_id \gset
set local session_replication_role=origin;
set local role authenticated;
select set_config('request.jwt.claim.sub','00000000-0000-0000-0000-000000009701',true);
select * from public.bereite_signierte_parzellen_vertragsaenderung_vor(:predecessor_id,120,true,false,'P11-T07-P Fläche und Strom') \gset
select set_config('p11.change_id', :pacht_vertragsaenderung_id::text, true);
select set_config('p11.process_id', :kommunikationsvorgang_id::text, true);
select set_config('p11.predecessor_id', :predecessor_id::text, true);
reset role;

set local role authenticated;
select set_config('request.jwt.claim.sub','00000000-0000-0000-0000-000000009701',true);
select * from public.bereite_signierte_parzellen_vertragsaenderung_vor(:predecessor_id,120,true,false,'P11-T07-P Fläche und Strom') \gset repeat_
select set_config('p11.repeat_change_id', :repeat_pacht_vertragsaenderung_id::text, true);
select set_config('p11.repeat_process_id', :repeat_kommunikationsvorgang_id::text, true);
reset role;
do $$ begin
 if current_setting('p11.change_id') <> current_setting('p11.repeat_change_id') or current_setting('p11.process_id') <> current_setting('p11.repeat_process_id') then raise exception 'identical retry was not idempotent'; end if;
end $$;

set local role authenticated;
select set_config('request.jwt.claim.sub','00000000-0000-0000-0000-000000009701',true);
do $$ begin
 perform public.bereite_signierte_parzellen_vertragsaenderung_vor(current_setting('p11.predecessor_id')::bigint,121,true,false,'P11-T07-P Fläche und Strom');
 raise exception 'different retry accepted';
exception when sqlstate '23505' then null; end $$;
reset role;
update public.app_user set permission_revocations=524288 where user_id='00000000-0000-0000-0000-000000009701';
set local role authenticated;
select set_config('request.jwt.claim.sub','00000000-0000-0000-0000-000000009701',true);
do $$ begin
 perform public.bereite_signierte_parzellen_vertragsaenderung_vor(current_setting('p11.predecessor_id')::bigint,120,true,false,'P11-T07-P Fläche und Strom');
 raise exception 'individual revocation accepted';
exception when sqlstate '42501' then null; end $$;
reset role;
do $$ declare c public.pacht_vertragsaenderung%rowtype; v_expected_hash text; v_clause text; begin
 select * into c from public.pacht_vertragsaenderung where id=current_setting('p11.change_id')::bigint;
 v_expected_hash:=encode(pg_catalog.sha256(convert_to(c.aenderungsstand::text,'utf8')),'hex');
 v_clause:=public.p11_contract_change_clause(c.aenderungsstand,c.begruendung);
 if c.aenderungsstand <> '{"flaeche_qm": 120, "hat_strom": true, "hat_wasser": false}'::jsonb or c.aenderungs_hash<>v_expected_hash or c.begruendung<>'P11-T07-P Fläche und Strom' then raise exception 'snapshot, SHA-256, or reason is incorrect'; end if;
 if not exists(select 1 from public.kommunikationsvorgang k where k.id=current_setting('p11.process_id')::bigint and k.entwurf_daten #>> '{draft,p11_contract_change_hash}'=c.aenderungs_hash and position(v_clause in coalesce(k.entwurf_daten #>> '{draft,additionalAgreements}',''))>0) then raise exception 'visible frozen clause is incorrect'; end if;
end $$;
rollback;
