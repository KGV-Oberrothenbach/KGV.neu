-- Phase 8: server-side lifecycle and authorization hardening for membership applications.
-- Source only. This migration is deliberately not executed by this change.

create or replace function public.prepare_mitglied_fuer_neuen_mitgliedsantrag()
returns trigger
language plpgsql
security definer
set search_path=public,pg_temp as $$
declare
  v_mitglied public.mitglied%rowtype;
begin
  if new.vorgangstyp <> 'vertrag_signatur'
     or new.dokumenttyp <> 'mitgliedsantrag'
     or new.status <> 'vorbereitet' then
    return new;
  end if;

  select * into v_mitglied
  from public.mitglied
  where id=new.mitglied_id
  for update;

  if not found or v_mitglied.hauptmitglied_id is not null then
    raise exception 'Für diesen Antragsteller kann kein Mitgliedsantrag angelegt werden.' using errcode='23514';
  end if;
  if v_mitglied.aufnahme_status='aufgenommen' then
    raise exception 'Für ein bereits aufgenommenes Mitglied kann kein neuer Mitgliedsantrag angelegt werden.' using errcode='23514';
  end if;
  if v_mitglied.aufnahme_status='abgelehnt' then
    update public.mitglied
    set aufnahme_status='antrag_offen', aktiv=false, mitglied_seit=null
    where id=v_mitglied.id;
  end if;
  return new;
end;
$$;

drop trigger if exists trg_prepare_mitglied_fuer_neuen_mitgliedsantrag on public.kommunikationsvorgang;
create trigger trg_prepare_mitglied_fuer_neuen_mitgliedsantrag
before insert on public.kommunikationsvorgang
for each row execute function public.prepare_mitglied_fuer_neuen_mitgliedsantrag();

alter table public.versandinstanz drop constraint if exists versandinstanz_vertragsart_check;
alter table public.versandinstanz add constraint versandinstanz_vertragsart_check
check (vertragsart in ('mitgliedsantrag','aufnahmebestaetigung','pachtvertrag','wartungsvertrag','wartungsvertrag_verlaengerung'));
create or replace function public.ensure_vertragsversand_snapshot(p_kommunikationsvorgang_id bigint) returns void language plpgsql security definer set search_path=public,pg_temp as $$
declare v public.kommunikationsvorgang%rowtype; d public.dokument%rowtype; r record; i smallint:=1; e jsonb; typ text;
begin
 select * into v from public.kommunikationsvorgang where id=p_kommunikationsvorgang_id for key share;
 if not found or v.final_dokument_id is null or v.dokumenttyp not in ('mitgliedsantrag','aufnahmebestaetigung','pachtvertrag','wartungsvertrag','wartungsvertrag_verlaengerung') then raise exception 'Kein versandfähiger finaler Vertragsvorgang' using errcode='23514'; end if;
 select * into d from public.dokument where id=v.final_dokument_id and archiviert_at is null;
 if not found or d.source_kommunikationsvorgang_id is distinct from v.id or d.source_revision is distinct from coalesce(v.signatur_revision,v.revision) then raise exception 'Finaldokument-Provenance fehlt' using errcode='23514'; end if;
 if v.dokumenttyp<>'aufnahmebestaetigung' then
  insert into public.vertragsversand_vertragspartei_snapshot(kommunikationsvorgang_id,revision,rolle,mitglied_id,name_snapshot,quelle)
  select v.id,coalesce(v.signatur_revision,v.revision),case when p.signatur_schluessel in ('member.application','mitglied','lease.member') then 'hauptmitglied' when p.signatur_schluessel='lease.second_party' then 'paechter_2' when p.signatur_schluessel like 'nebenmitglied.%' then 'nebenmitglied' else 'gesetzliche_vertretung' end,p.mitglied_id,coalesce(nullif(btrim(p.unterzeichner_name_snapshot),''),'Unbekannte Person'),'prepared_contract_signatur' from public.prepared_contract_signatur p where p.kommunikationsvorgang_id=v.id and p.signatur_revision=coalesce(v.signatur_revision,v.revision) and p.status='unterschrieben' and p.unterzeichner_typ<>'vereinsunterzeichner' and p.signatur_schluessel in ('member.application','mitglied','lease.member','lease.second_party','nebenmitglied.antrag','gesetzliche_vertretung.antrag') on conflict do nothing;
 end if;
 if not exists(select 1 from public.vertragsversand_vertragspartei_snapshot where kommunikationsvorgang_id=v.id and revision=coalesce(v.signatur_revision,v.revision)) then
  insert into public.vertragsversand_vertragspartei_snapshot(kommunikationsvorgang_id,revision,rolle,mitglied_id,name_snapshot,quelle)
  select v.id,coalesce(v.signatur_revision,v.revision),'hauptmitglied',m.id,coalesce(nullif(btrim(concat_ws(' ',m.vorname,m.name)),''),case when m.mitgliedsnummer is not null then 'Mgl.-Nr. '||m.mitgliedsnummer::text else 'Mitglied' end),'vorgang' from public.mitglied m where m.id=v.mitglied_id on conflict do nothing;
 end if;
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

create or replace function public.freeze_vertragsversand_snapshot() returns trigger language plpgsql security definer set search_path=public,pg_temp as $$
begin
 if new.final_dokument_id is not null and new.final_dokument_id is distinct from old.final_dokument_id and new.dokumenttyp in ('mitgliedsantrag','aufnahmebestaetigung','pachtvertrag','wartungsvertrag','wartungsvertrag_verlaengerung') then perform public.ensure_vertragsversand_snapshot(new.id); end if;
 return new;
end $$;

create or replace function public.prepare_contract_dispatch(p_kommunikationsvorgang_id bigint,p_erneuter_versand_von_id bigint default null)
returns bigint language plpgsql security definer set search_path=public,pg_temp as $$
declare v public.kommunikationsvorgang%rowtype; x bigint; p record; allowed boolean; reason text;
begin
 if not public.kann_vertragsversand_ausfuehren() then raise exception 'Keine Berechtigung für Vertragsversand' using errcode='42501'; end if;
 select * into v from public.kommunikationsvorgang where id=p_kommunikationsvorgang_id for update;
 if not found or v.final_dokument_id is null or v.status<>'erledigt' or v.dokumenttyp not in ('mitgliedsantrag','aufnahmebestaetigung','pachtvertrag','wartungsvertrag','wartungsvertrag_verlaengerung') then raise exception 'Vertragsvorgang ist noch nicht abgeschlossen' using errcode='23514'; end if;
 if p_erneuter_versand_von_id is null then select id into x from public.versandinstanz where kommunikationsvorgang_id=v.id and revision=coalesce(v.signatur_revision,v.revision) and erneuter_versand_von_id is null; if found then return x; end if; end if;
 perform public.ensure_vertragsversand_snapshot(v.id);
 if p_erneuter_versand_von_id is not null and not exists(select 1 from public.versandinstanz z where z.id=p_erneuter_versand_von_id and z.status='versendet' and z.kommunikationsvorgang_id=v.id and z.revision=coalesce(v.signatur_revision,v.revision)) then raise exception 'Erneuter Versand muss auf einen erfolgreichen Versand desselben Vertragsstands verweisen' using errcode='23514'; end if;
 insert into public.versandinstanz(kommunikationsvorgang_id,revision,vertragsart,betreff_snapshot,text_snapshot,erneuter_versand_von_id) values(v.id,coalesce(v.signatur_revision,v.revision),v.dokumenttyp,case when v.dokumenttyp='aufnahmebestaetigung' then 'Ihre Aufnahme in den Kleingartenverein' else 'Wichtige Unterlagen zu Ihrer Mitgliedschaft im Kleingartenverein' end,public.vertragsversand_mailtext_snapshot(),p_erneuter_versand_von_id) returning id into x;
 for p in select s.*,m.email,m.email_info_einwilligung,m.email_rechnung_einwilligung from public.vertragsversand_vertragspartei_snapshot s left join public.mitglied m on m.id=s.mitglied_id where s.kommunikationsvorgang_id=v.id and s.revision=coalesce(v.signatur_revision,v.revision) loop
  allowed:=p.email is not null and btrim(p.email)~*'^[^[:space:]@]+@[^[:space:]@]+[.][^[:space:]@]+$' and (coalesce(p.email_info_einwilligung,false) or coalesce(p.email_rechnung_einwilligung,false)); reason:=case when p.email is null or btrim(p.email)='' then 'Keine E-Mail-Adresse' when not (coalesce(p.email_info_einwilligung,false) or coalesce(p.email_rechnung_einwilligung,false)) then 'Keine E-Mail-Einwilligung' when not btrim(p.email)~*'^[^[:space:]@]+@[^[:space:]@]+[.][^[:space:]@]+$' then 'Ungültige E-Mail-Adresse' else null end;
  insert into public.versandinstanz_empfaenger_snapshot(versandinstanz_id,rolle,mitglied_id,name_snapshot,email_snapshot,email_normalisiert,einwilligung_info_snapshot,einwilligung_rechnung_snapshot,versand_erlaubt,nicht_versandfaehig_grund,status) values(x,p.rolle,p.mitglied_id,p.name_snapshot,p.email,lower(btrim(p.email)),coalesce(p.email_info_einwilligung,false),coalesce(p.email_rechnung_einwilligung,false),allowed,reason,case when allowed then 'bereit' else 'nicht_versandfaehig' end);
 end loop;
 if not exists(select 1 from public.versandinstanz_empfaenger_snapshot where versandinstanz_id=x and versand_erlaubt) then raise exception 'Kein Empfänger mit gültiger E-Mail-Einwilligung' using errcode='23514'; end if;
 insert into public.versandinstanz_dokument_snapshot select x,sortierung,bezeichnung_snapshot,bindungsstatus,dateiname_snapshot,mime_type_snapshot,bucket_snapshot,storage_path_snapshot,drive_file_id_snapshot,sha256_snapshot,dokumenttyp_code,bereitgestellt_am_snapshot,bemerkung_snapshot from public.vertragsversand_anlage_snapshot where kommunikationsvorgang_id=v.id and revision=coalesce(v.signatur_revision,v.revision);
 return x;
end $$;

create or replace function public.start_mitgliedsaufnahme_vorstandsbearbeitung(p_kommunikationsvorgang_id bigint)
returns bigint language plpgsql security definer set search_path=public,pg_temp as $$
declare v public.kommunikationsvorgang%rowtype; b_id bigint; r record; p_id bigint; caller uuid:=auth.uid();
begin
 if caller is null or not coalesce(public.is_admin_or_vorstand(),false) then raise exception 'Keine Berechtigung zum Start der Vorstandsbearbeitung' using errcode='42501'; end if;
 select * into v from public.kommunikationsvorgang where id=p_kommunikationsvorgang_id for update;
 if not found or v.vorgangstyp<>'vertrag_signatur' or v.dokumenttyp<>'mitgliedsantrag' or v.status<>'letzte_pruefung' or v.final_dokument_id is null or v.signatur_revision is distinct from v.revision then raise exception 'Mitgliedsantrag ist nicht zur Vorstandsbearbeitung bereit' using errcode='23514'; end if;
 select id into b_id from public.mitgliedsaufnahme_vorstandsbearbeitung where kommunikationsvorgang_id=v.id; if found then return b_id; end if;
 if not exists(select 1 from public.dokument d where d.id=v.final_dokument_id and d.archiviert_at is null and d.source_kommunikationsvorgang_id=v.id and d.source_revision=v.revision) then raise exception 'Finaldokument passt nicht zum Vorgang' using errcode='23514'; end if;
 insert into public.mitgliedsaufnahme_vorstandsbearbeitung(kommunikationsvorgang_id,gestartet_von_auth_user_id) values(v.id,caller) returning id into b_id;
 for r in select * from public.mitgliedsaufnahme_berechtigungen('mitgliedsaufnahme_abstimmung',current_date) loop
   insert into public.mitgliedsaufnahme_abstimmung_snapshot_person(vorstandsbearbeitung_id,mitglied_id,name_snapshot)
   select b_id,r.mitglied_id,coalesce(nullif(btrim(concat_ws(' ',m.vorname,m.name)),''),case when m.mitgliedsnummer is not null then 'Mgl.-Nr. '||m.mitgliedsnummer::text else 'Mitglied' end) from public.mitglied m where m.id=r.mitglied_id
   on conflict(vorstandsbearbeitung_id,mitglied_id) do update set name_snapshot=excluded.name_snapshot returning id into p_id;
   insert into public.mitgliedsaufnahme_abstimmung_snapshot_berechtigung(snapshot_person_id,berechtigungsart,vereinsfunktion_id,vereinsfunktion_besetzung_id,funktion_schluessel_snapshot,funktionsbezeichnung_snapshot,vertretung_id,vertretene_vereinsfunktion_id,vertretene_besetzung_id,vertretene_funktionsbezeichnung_snapshot) values(p_id,r.berechtigungsart,r.vereinsfunktion_id,r.vereinsfunktion_besetzung_id,r.funktion_schluessel,r.funktionsbezeichnung,r.vertretung_id,r.vertretene_vereinsfunktion_id,r.vertretene_besetzung_id,r.vertretene_funktionsbezeichnung) on conflict do nothing;
 end loop;
 if not exists(select 1 from public.mitgliedsaufnahme_abstimmung_snapshot_person where vorstandsbearbeitung_id=b_id) then raise exception 'Keine Stimmberechtigten konfiguriert' using errcode='23514'; end if;
 return b_id;
end; $$;

create or replace view public.mitgliedsaufnahme_vorstand_liste with (security_invoker=true) as
select b.id bearbeitung_id,b.kommunikationsvorgang_id,v.mitglied_id,v.final_dokument_id,spc.name_snapshot antragsteller,b.gestartet_am,b.entscheidung,
 count(sp.id) snapshot_personen,count(s.stimme) abgegebene_stimmen,count(s.stimme) filter(where s.stimme='ja') ja_stimmen,count(s.stimme) filter(where s.stimme='nein') nein_stimmen,
 (count(sp.id)>0 and count(s.stimme)=count(sp.id) and count(s.stimme) filter(where s.stimme='ja')=count(sp.id)) einstimmig_ja,
 (count(s.stimme) filter(where s.stimme='nein')>0) konflikt,
 confirmation.id aufnahmebestaetigung_vorgang_id,confirmation.status aufnahmebestaetigung_status,confirmation.final_dokument_id aufnahmebestaetigung_dokument_id
from public.mitgliedsaufnahme_vorstandsbearbeitung b
join public.kommunikationsvorgang v on v.id=b.kommunikationsvorgang_id
join public.mitglied m on m.id=v.mitglied_id
left join lateral(select coalesce(nullif(btrim(concat_ws(' ',m.vorname,m.name)),''),case when m.mitgliedsnummer is not null then 'Mgl.-Nr. '||m.mitgliedsnummer::text else 'Mitglied' end) name_snapshot) spc on true
left join lateral (
  select d.id,d.status,d.final_dokument_id
  from public.kommunikationsvorgang d
  where d.vorgangstyp='dokument_erzeugung'
    and d.dokumenttyp='aufnahmebestaetigung'
    and (d.entwurf_daten->>'mitgliedsantrag_kommunikationsvorgang_id')::bigint=v.id
  limit 1
) confirmation on true
left join public.mitgliedsaufnahme_abstimmung_snapshot_person sp on sp.vorstandsbearbeitung_id=b.id
left join lateral(select stimme from public.mitgliedsaufnahme_abstimmung_stimme where snapshot_person_id=sp.id order by version desc limit 1) s on true
group by b.id,v.mitglied_id,v.final_dokument_id,spc.name_snapshot,confirmation.id,confirmation.status,confirmation.final_dokument_id;

revoke all on function public.prepare_mitglied_fuer_neuen_mitgliedsantrag() from public,anon,authenticated;
revoke all on function public.start_mitgliedsaufnahme_vorstandsbearbeitung(bigint) from public,anon;
grant execute on function public.start_mitgliedsaufnahme_vorstandsbearbeitung(bigint) to authenticated;
