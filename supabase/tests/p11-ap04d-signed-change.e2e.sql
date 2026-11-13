-- P11-T07-P (first executable slice): real authenticated preparation and
-- idempotency.  Final PDF/signature completion remains in the Edge workflow.
begin;
insert into auth.users(id,aud,role,email,encrypted_password,email_confirmed_at,raw_app_meta_data,raw_user_meta_data,created_at,updated_at) values ('00000000-0000-0000-0000-000000009701','authenticated','authenticated','p11-t07p@example.test','x',now(),'{}','{}',now(),now());
insert into public.mitglied(name,vorname,aktiv,aufnahme_status,auth_user_id) values('P11 T07P','Pächter',true,'aufgenommen','00000000-0000-0000-0000-000000009701') returning id as member_id \gset
insert into public.app_user(user_id,mitglied_id,role,permission_grants,permission_revocations) values('00000000-0000-0000-0000-000000009701',:member_id,'user',524288,0);
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
do $$ begin
 if (select aenderungsstand->>'flaeche_qm' from public.pacht_vertragsaenderung where id=:'pacht_vertragsaenderung_id') <> '120' then raise exception 'frozen area missing'; end if;
 if not exists(select 1 from public.kommunikationsvorgang where id=:'kommunikationsvorgang_id' and entwurf_daten #>> '{draft,additionalAgreements}' like '%Fläche 120%' and entwurf_daten #>> '{draft,additionalAgreements}' like '%Strom ja%') then raise exception 'visible frozen clause missing'; end if;
end $$;
select * from public.bereite_signierte_parzellen_vertragsaenderung_vor(:predecessor_id,120,true,false,'P11-T07-P Fläche und Strom');
do $$ begin perform public.bereite_signierte_parzellen_vertragsaenderung_vor(:predecessor_id,121,true,false,'P11-T07-P Fläche und Strom'); raise exception 'different area accepted'; exception when sqlstate '23505' then null; end $$;
rollback;
