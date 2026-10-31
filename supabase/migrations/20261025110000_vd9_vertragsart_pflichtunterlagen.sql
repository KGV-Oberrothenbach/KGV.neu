-- VD-9: Contract-specific mandatory evidence.  Do not run from this repository;
-- deployment remains an explicitly authorised production operation.

-- A paper acknowledgement is valid even where the historical printed version is
-- not known.  Digital evidence, in contrast, always remains version-bound.
alter table public.vertragsversand_anlage_snapshot
  drop constraint if exists vertragsversand_anlage_snapshot_source_chk;
alter table public.vertragsversand_anlage_snapshot
  add constraint vertragsversand_anlage_snapshot_source_chk check (
    (art='vertrag' and bindungsstatus='digital_gebunden' and dokument_id is not null and vereinsdokument_fassung_id is null and storage_path_snapshot is not null)
    or (art='anlage' and ((bindungsstatus='digital_gebunden' and vereinsdokument_fassung_id is not null and storage_path_snapshot is not null and dateiname_snapshot is not null)
      or (bindungsstatus='papier_ausgehaendigt' and dokument_id is null and storage_path_snapshot is null and dateiname_snapshot is null)))
  );
create or replace function public.dokumentiere_prepared_contract_papieruebergabe(p_kommunikationsvorgang_id bigint,p_dokumenttyp text,p_fassung text,p_uebergabe_am timestamptz,p_bemerkung text default null) returns public.kommunikationsvorgang language plpgsql security definer set search_path=public,pg_temp as $$
declare v public.kommunikationsvorgang%rowtype; a jsonb; required text[]; f record;
begin
 if not public.is_admin_or_vorstand() or auth.uid() is null then raise exception 'Keine Berechtigung für Papierübergabe' using errcode='42501'; end if;
 select * into v from public.kommunikationsvorgang where id=p_kommunikationsvorgang_id for update;
 required:=case v.dokumenttyp when 'mitgliedsantrag' then array['satzung'] when 'pachtvertrag' then array['kleingartenordnung','beitragsordnung','arbeitsordnung'] else array[]::text[] end;
 if v.status<>'vorbereitet' or not p_dokumenttyp=any(required) or p_uebergabe_am is null or p_uebergabe_am>now() then raise exception 'Papierübergabe kann nur vor Signatur für eine Pflichtanlage dokumentiert werden' using errcode='23514'; end if;
 if nullif(btrim(p_fassung),'') is not null then select id,fassung,gueltig_ab,gueltig_bis into f from public.vereinsdokument_fassung where dokumenttyp_code=p_dokumenttyp and fassung=btrim(p_fassung); if f.id is null then raise exception 'Die angegebene Papierfassung ist nicht als Vereinsdokumentfassung bekannt' using errcode='23514'; end if; end if;
 a:=jsonb_strip_nulls(jsonb_build_object('dokumenttyp',p_dokumenttyp,'vereinsdokument_fassung_id',f.id,'fassung',f.fassung,'gueltig_ab',f.gueltig_ab,'gueltig_bis',f.gueltig_bis,'bereitstellungsart','papier','bereitgestellt_am',p_uebergabe_am,'bemerkung',nullif(btrim(p_bemerkung),'')));
 update public.kommunikationsvorgang set entwurf_daten=jsonb_set(v.entwurf_daten,'{vereinsunterlagenNachweis}',coalesce((select jsonb_agg(case when x->>'dokumenttyp'=p_dokumenttyp then a else x end) from jsonb_array_elements(coalesce(v.entwurf_daten->'vereinsunterlagenNachweis','[]')) x),'[]'::jsonb),true),revision=v.revision+1 where id=v.id returning * into v; return v;
end $$;
-- Preserve the frozen required sets: MA only Satzung; PV only KGO, BuGO and AO.
create or replace function public.ensure_vertragsversand_snapshot(p_kommunikationsvorgang_id bigint) returns void language plpgsql security definer set search_path=public,pg_temp as $$
declare v public.kommunikationsvorgang%rowtype; d public.dokument%rowtype; r record; i smallint:=1; e jsonb; typ text;
begin
 select * into v from public.kommunikationsvorgang where id=p_kommunikationsvorgang_id for key share;
 if not found or v.final_dokument_id is null or v.dokumenttyp not in ('mitgliedsantrag','pachtvertrag','wartungsvertrag','wartungsvertrag_verlaengerung') then raise exception 'Kein versandfähiger finaler Vertragsvorgang' using errcode='23514'; end if;
 select * into d from public.dokument where id=v.final_dokument_id and archiviert_at is null;
 if not found or d.source_kommunikationsvorgang_id is distinct from v.id or d.source_revision is distinct from coalesce(v.signatur_revision,v.revision) then raise exception 'Finaldokument-Provenance fehlt' using errcode='23514'; end if;
 insert into public.vertragsversand_vertragspartei_snapshot(kommunikationsvorgang_id,revision,rolle,mitglied_id,name_snapshot,quelle)
 select v.id,coalesce(v.signatur_revision,v.revision),case when p.signatur_schluessel in ('member.application','mitglied','lease.member') then 'hauptmitglied' when p.signatur_schluessel='lease.second_party' then 'paechter_2' when p.signatur_schluessel like 'nebenmitglied.%' then 'nebenmitglied' else 'gesetzliche_vertretung' end,p.mitglied_id,coalesce(nullif(btrim(p.unterzeichner_name_snapshot),''),'Unbekannte Person'),'prepared_contract_signatur' from public.prepared_contract_signatur p where p.kommunikationsvorgang_id=v.id and p.signatur_revision=coalesce(v.signatur_revision,v.revision) and p.status='unterschrieben' and p.unterzeichner_typ<>'vereinsunterzeichner' and p.signatur_schluessel in ('member.application','mitglied','lease.member','lease.second_party','nebenmitglied.antrag','gesetzliche_vertretung.antrag') on conflict do nothing;
 if not exists(select 1 from public.vertragsversand_vertragspartei_snapshot where kommunikationsvorgang_id=v.id and revision=coalesce(v.signatur_revision,v.revision)) then insert into public.vertragsversand_vertragspartei_snapshot values(v.id,coalesce(v.signatur_revision,v.revision),'hauptmitglied',v.mitglied_id,coalesce((select nullif(btrim(concat_ws(' ',m.vorname,m.name)),'') from public.mitglied m where m.id=v.mitglied_id),'Mitglied #'||v.mitglied_id),'vorgang') on conflict do nothing; end if;
 insert into public.vertragsversand_anlage_snapshot(kommunikationsvorgang_id,revision,sortierung,art,dokument_id,bezeichnung_snapshot,dateiname_snapshot,mime_type_snapshot,bucket_snapshot,storage_path_snapshot,drive_file_id_snapshot) values(v.id,coalesce(v.signatur_revision,v.revision),0,'vertrag',d.id,coalesce(d.titel,'Vertragsdokument'),coalesce(d.dateiname,'vertrag.pdf'),coalesce(d.mime_type,'application/pdf'),coalesce(d.bucket,'dokumente'),coalesce(d.storage_path,''),d.drive_file_id) on conflict do nothing;
 if not exists(select 1 from public.vertragsversand_anlage_snapshot where kommunikationsvorgang_id=v.id and revision=coalesce(v.signatur_revision,v.revision) and sortierung=0 and length(storage_path_snapshot)>0 and drive_file_id_snapshot is not null) then raise exception 'Finaldokument ist nicht sicher abrufbar' using errcode='23514'; end if;
 foreach typ in array case v.dokumenttyp when 'mitgliedsantrag' then array['satzung']::text[] when 'pachtvertrag' then array['kleingartenordnung','beitragsordnung','arbeitsordnung']::text[] else array[]::text[] end loop
  select value into e from jsonb_array_elements(coalesce(v.entwurf_daten->'vereinsunterlagenNachweis','[]'::jsonb)) value where value->>'dokumenttyp'=typ limit 1;
  if e is null or nullif(e->>'bereitgestellt_am','') is null then raise exception 'Pflichtanlage % ist weder digital gebunden noch als Papierübergabe dokumentiert',typ using errcode='23514'; end if;
  if e->>'bereitstellungsart'='papier' then
   select * into r from public.vereinsdokument_fassung where id=nullif(e->>'vereinsdokument_fassung_id','')::bigint and dokumenttyp_code=typ;
   insert into public.vertragsversand_anlage_snapshot(kommunikationsvorgang_id,revision,sortierung,art,bindungsstatus,dokumenttyp_code,vereinsdokument_fassung_id,bezeichnung_snapshot,bemerkung_snapshot,bereitgestellt_am_snapshot) values(v.id,coalesce(v.signatur_revision,v.revision),i,'anlage','papier_ausgehaendigt',typ,r.id,typ||case when r.id is null then ' – Papierübergabe (Fassung unbekannt)' else ' – Fassung '||r.fassung end,nullif(e->>'bemerkung',''),(e->>'bereitgestellt_am')::timestamptz) on conflict do nothing;
  elsif e->>'bereitstellungsart'='digital' then
   select * into r from public.vereinsdokument_fassung where id=nullif(e->>'vereinsdokument_fassung_id','')::bigint and dokumenttyp_code=typ;
   if r.id is null or r.storage_path is null or r.dateiname is null or r.sha256 is null then raise exception 'Die digitale Pflichtanlage % hat keine eingefrorene Fassung',typ using errcode='23514'; end if;
   insert into public.vertragsversand_anlage_snapshot(kommunikationsvorgang_id,revision,sortierung,art,bindungsstatus,dokumenttyp_code,vereinsdokument_fassung_id,bezeichnung_snapshot,dateiname_snapshot,mime_type_snapshot,bucket_snapshot,storage_path_snapshot,sha256_snapshot,bereitgestellt_am_snapshot) values(v.id,coalesce(v.signatur_revision,v.revision),i,'anlage','digital_gebunden',typ,r.id,typ||' – Fassung '||r.fassung,r.dateiname,'application/pdf','vereinsregelwerke',r.storage_path,r.sha256,(e->>'bereitgestellt_am')::timestamptz) on conflict do nothing;
  else raise exception 'Pflichtanlage % ist weder digital gebunden noch als Papierübergabe dokumentiert',typ using errcode='23514'; end if; i:=i+1;
 end loop;
end $$;
