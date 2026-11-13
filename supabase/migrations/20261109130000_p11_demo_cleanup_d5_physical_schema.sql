begin;

-- D5 is deliberately explicit.  All project-local functions are replaced
-- before the compatibility columns and helpers are removed; no DROP CASCADE
-- and no data deletion is used.
CREATE OR REPLACE FUNCTION public.complete_prepared_mitgliedsantrag(p_kommunikationsvorgang_id bigint, p_expected_revision integer, p_created_by uuid)
 RETURNS TABLE(vertreter_mitglied_id bigint, bereits_abgeschlossen boolean)
 LANGUAGE plpgsql
 SECURITY DEFINER
 SET search_path TO 'public', 'pg_temp'
AS $function$
declare v public.kommunikationsvorgang%rowtype; d public.dokument%rowtype; rep jsonb; representative_id bigint; current_relation record; start_date date; member_row public.mitglied%rowtype;
begin
  if auth.role() is distinct from 'service_role' then raise exception 'Der Mitgliedsantragsabschluss ist nur serverseitig zulässig' using errcode='42501'; end if;
  if not exists (
    select 1
    from public.app_user au
    where au.user_id = p_created_by
      and (((public.permission_role_base(au.role) | coalesce(au.permission_grants, 0))
            & ~coalesce(au.permission_revocations, 0) & 524288) = 524288)
  ) then raise exception 'Keine Berechtigung' using errcode='42501'; end if;
  select * into v from public.kommunikationsvorgang where id=p_kommunikationsvorgang_id for update;
  if not found or v.vorgangstyp <> 'vertrag_signatur' or v.dokumenttyp <> 'mitgliedsantrag' then raise exception 'Der Vorgang ist kein Mitgliedsantrag' using errcode='23514'; end if;
  if v.status='erledigt' then
    select vertreter_mitglied_id into representative_id from public.mitglied_gesetzlicher_vertreter where minderjaehriges_mitglied_id=v.mitglied_id and gueltig_bis is null order by id desc limit 1;
    return query select representative_id, true; return;
  end if;
  if v.status <> 'letzte_pruefung' or v.revision <> p_expected_revision or v.signatur_revision <> p_expected_revision or v.zugewiesen_an is distinct from p_created_by or v.final_dokument_id is null then raise exception 'Der Mitgliedsantrag ist nicht abschließbar' using errcode='40001'; end if;
  select * into d from public.dokument where id=v.final_dokument_id and archiviert_at is null for key share;
  if not found or d.source_kommunikationsvorgang_id is distinct from v.id or d.source_revision is distinct from v.revision then raise exception 'Finaldokument passt nicht zum Vorgang' using errcode='23514'; end if;
  select * into member_row from public.mitglied where id=v.mitglied_id for key share;
  if not found then raise exception 'Mitglied fehlt' using errcode='P0002'; end if;
  rep := v.entwurf_daten->'representative'; start_date := (v.entwurf_daten->>'startDate')::date;
  if rep is not null and jsonb_typeof(rep)='object' then
    if rep->>'mode'='existing' then
      representative_id := nullif(rep->>'memberId','')::bigint;
      if representative_id is null or representative_id=v.mitglied_id or not exists(select 1 from public.mitglied where id=representative_id) then raise exception 'Ungültiger gesetzlicher Vertreter' using errcode='23514'; end if;
    elsif rep->>'mode'='manual' then
      if coalesce(nullif(trim(rep->>'vorname'),''),'')='' or coalesce(nullif(trim(rep->>'nachname'),''),'')='' then raise exception 'Vertreterdaten sind unvollständig' using errcode='23514'; end if;
      insert into public.mitglied(hauptmitglied_id,vorname,name,adresse,plz,ort,mitglied_seit,whatsapp_einwilligung,aktiv,mitglied_ende)
      values (coalesce(member_row.hauptmitglied_id, member_row.id), trim(rep->>'vorname'), trim(rep->>'nachname'), case when coalesce((rep->>'adresseAbweichend')::boolean,false) then trim(rep->>'adresse') else member_row.adresse end, case when coalesce((rep->>'adresseAbweichend')::boolean,false) then trim(rep->>'plz') else member_row.plz end, case when coalesce((rep->>'adresseAbweichend')::boolean,false) then trim(rep->>'ort') else member_row.ort end, start_date,false,true,null) returning id into representative_id;
    else raise exception 'Vertretermodus fehlt' using errcode='23514'; end if;
    select * into current_relation from public.mitglied_gesetzlicher_vertreter where minderjaehriges_mitglied_id=v.mitglied_id and gueltig_ab<=start_date and (gueltig_bis is null or gueltig_bis>=start_date) order by (gueltig_bis is null) desc, gueltig_ab desc, id desc limit 1 for update;
    if found and current_relation.vertreter_mitglied_id=representative_id then
      update public.mitglied_gesetzlicher_vertreter set gueltig_ab=start_date,bemerkung='Automatisch aus Mitgliedsantrag übernommen.',updated_at=now() where id=current_relation.id;
    else
      if found then update public.mitglied_gesetzlicher_vertreter set gueltig_bis=start_date-1,updated_at=now() where id=current_relation.id; end if;
      insert into public.mitglied_gesetzlicher_vertreter(minderjaehriges_mitglied_id,vertreter_mitglied_id,gueltig_ab,gueltig_bis,bemerkung,created_at,updated_at) values(v.mitglied_id,representative_id,start_date,null,'Automatisch aus Mitgliedsantrag übernommen.',now(),now());
    end if;
  end if;
  update public.kommunikationsvorgang set status='erledigt',bearbeitet_von=p_created_by where id=v.id and status='letzte_pruefung' and revision=p_expected_revision and zugewiesen_an=p_created_by;
  if not found then raise exception 'Mitgliedsantrag wurde zwischenzeitlich geändert' using errcode='40001'; end if;
  return query select representative_id,false;
end; $function$;

CREATE OR REPLACE FUNCTION public.complete_prepared_pachtvertrag_vor_b3(p_kommunikationsvorgang_id bigint, p_expected_revision integer, p_created_by uuid)
 RETURNS TABLE(pachtverhaeltnis_id bigint, pachtvertragsfassung_id bigint, bereits_abgeschlossen boolean)
 LANGUAGE plpgsql
 SECURITY DEFINER
 SET search_path TO 'public', 'pg_temp'
AS $function$
declare
  v_vorgang public.kommunikationsvorgang%rowtype; v_final_dokument public.dokument%rowtype;
  v_aktive_verhaeltnisse integer; v_pachtverhaeltnis_id bigint; v_vorgaenger_id bigint;
  v_letzte_fassungen integer; v_start_text text; v_effective_date_text text; v_pachtbeginn date;
  v_vertragsdatum date; v_wirksam_ab date; v_include_mitpaechter boolean; v_mitpaechter_id bigint;
  v_aktive_mitpaechter integer; v_aktiver_mitpaechter_id bigint; v_fassung_id bigint;
begin
  if auth.role() is distinct from 'service_role' then raise exception 'Der Pachtvertragsabschluss ist nur serverseitig zulässig' using errcode = '42501'; end if;
  if p_kommunikationsvorgang_id is null or p_expected_revision is null or p_created_by is null then raise exception 'Kommunikationsvorgang, Revision und auslösender Benutzer sind erforderlich' using errcode = '22023'; end if;
  if not exists (
    select 1 from public.app_user au join public.mitglied m on m.id=au.mitglied_id and m.auth_user_id=au.user_id
    where au.user_id=p_created_by and au.mitglied_id is not null and au.mitglied_id>0
      and (((public.permission_role_base(au.role) | coalesce(au.permission_grants,0)) & ~coalesce(au.permission_revocations,0) & 524288)=524288)
  ) then raise exception 'Der auslösende Benutzer besitzt nicht das erforderliche Recht CanCreateMitglied' using errcode = '42501'; end if;
  select * into v_vorgang from public.kommunikationsvorgang where id=p_kommunikationsvorgang_id for update;
  if not found then raise exception 'Unbekannter Kommunikationsvorgang: %',p_kommunikationsvorgang_id using errcode='P0002'; end if;
  if v_vorgang.vorgangstyp<>'vertrag_signatur' or v_vorgang.dokumenttyp<>'pachtvertrag' then raise exception 'Der Vorgang ist kein vorbereiteter Pachtvertragsvorgang' using errcode='23514'; end if;
  if v_vorgang.revision<>p_expected_revision then raise exception 'Der Vertragsvorgang wurde zwischenzeitlich geändert' using errcode='40001'; end if;
  if v_vorgang.zugewiesen_an is distinct from p_created_by then raise exception 'Der Vertragsvorgang ist nicht dem auslösenden Benutzer zugewiesen' using errcode='42501'; end if;
  if v_vorgang.final_dokument_id is null then raise exception 'Der Pachtvertragsvorgang enthält kein Finaldokument' using errcode='23514'; end if;
  select * into v_final_dokument from public.dokument where id=v_vorgang.final_dokument_id and archiviert_at is null for key share;
  if not found then raise exception 'Das Finaldokument existiert nicht oder ist archiviert' using errcode='23514'; end if;
  if v_final_dokument.source_kommunikationsvorgang_id is distinct from v_vorgang.id or v_final_dokument.source_revision is distinct from v_vorgang.revision then raise exception 'Das Finaldokument gehört nicht zur eingefrorenen Revision dieses Kommunikationsvorgangs' using errcode='23514'; end if;
  if v_vorgang.status='erledigt' then
    select f.pachtverhaeltnis_id,f.id into v_pachtverhaeltnis_id,v_fassung_id from public.pachtvertragsfassung f where f.dokument_id=v_vorgang.final_dokument_id;
    if found then return query select v_pachtverhaeltnis_id,v_fassung_id,true; end if;
    raise exception 'Der Vorgang ist bereits erledigt, besitzt aber keine registrierte Vertragsfassung' using errcode='23514';
  end if;
  if v_vorgang.status<>'letzte_pruefung' or v_vorgang.mitglied_id is null or v_vorgang.parzelle_id is null then raise exception 'Der Pachtvertragsvorgang ist nicht vollständig zur letzten Prüfung bereit' using errcode='23514'; end if;
  if v_vorgang.signatur_gestartet_am is null or v_vorgang.signatur_revision is null or v_vorgang.signatur_revision<>v_vorgang.revision or v_vorgang.signatur_revision<>p_expected_revision then raise exception 'Der Pachtvertragsvorgang besitzt keinen gültigen eingefrorenen Signaturstand' using errcode='23514'; end if;
  v_start_text:=v_vorgang.entwurf_daten#>>'{draft,startDate}';
  if v_start_text is null or v_start_text !~ '^\d{4}-\d{2}-\d{2}$' then raise exception 'Im vorbereiteten Pachtvertrag fehlt ein gültiger Pachtbeginn' using errcode='23514'; end if;
  v_pachtbeginn:=v_start_text::date; v_vertragsdatum:=(v_final_dokument.created_at at time zone 'Europe/Berlin')::date;
  v_include_mitpaechter:=coalesce((v_vorgang.entwurf_daten#>>'{draft,includeSecondaryMember}')::boolean,false);
  if v_include_mitpaechter then
    if coalesce(v_vorgang.entwurf_daten#>>'{draft,secondaryMemberId}','') !~ '^\d+$' then raise exception 'Der vorbereitete Erstvertrag enthält keine konkrete Mitpächter-ID' using errcode='23514'; end if;
    v_mitpaechter_id:=(v_vorgang.entwurf_daten#>>'{draft,secondaryMemberId}')::bigint;
  end if;
  perform pg_advisory_xact_lock(hashtextextended('pachtverhaeltnis:parzelle:'||v_vorgang.parzelle_id::text,0));
  select count(*),min(id) into v_aktive_verhaeltnisse,v_pachtverhaeltnis_id from public.pachtverhaeltnis where parzelle_id=v_vorgang.parzelle_id and beendet_am is null;
  if v_aktive_verhaeltnisse>1 then raise exception 'Inkonsistenter Bestand: mehrere aktive Pachtverhältnisse für die Parzelle' using errcode='23514'; end if;
  if v_aktive_verhaeltnisse=0 then
    select r.pachtverhaeltnis_id,r.pachtvertragsfassung_id into v_pachtverhaeltnis_id,v_fassung_id from public.register_pachtverhaeltnis_erstvertrag(v_vorgang.parzelle_id,v_vorgang.mitglied_id,v_pachtbeginn,v_vorgang.final_dokument_id,v_vertragsdatum,v_pachtbeginn,p_created_by,v_mitpaechter_id) r;
  else
    select count(*),min(mitglied_id) into v_aktive_mitpaechter,v_aktiver_mitpaechter_id from public.pachtverhaeltnis_beteiligung where pachtverhaeltnis_id=v_pachtverhaeltnis_id and rolle='mitpaechter' and gueltig_bis is null;
    if v_include_mitpaechter and (v_aktive_mitpaechter<>1 or v_aktiver_mitpaechter_id<>v_mitpaechter_id) then raise exception 'Beteiligungsänderungen bei einer Neufassung sind noch nicht unterstützt' using errcode='23514'; end if;
    if not v_include_mitpaechter and v_aktive_mitpaechter<>0 then raise exception 'Das Entfernen eines Mitpächters bei einer Neufassung ist noch nicht unterstützt' using errcode='23514'; end if;
    v_effective_date_text:=v_vorgang.entwurf_daten#>>'{draft,effectiveDate}';
    if v_effective_date_text is null or v_effective_date_text !~ '^\d{4}-\d{2}-\d{2}$' then raise exception 'Im vorbereiteten Pachtvertrag fehlt das Wirksamkeitsdatum der Neufassung' using errcode='23514'; end if;
    v_wirksam_ab:=v_effective_date_text::date;
    select count(*),min(f.id) into v_letzte_fassungen,v_vorgaenger_id from public.pachtvertragsfassung f where f.pachtverhaeltnis_id=v_pachtverhaeltnis_id and not exists(select 1 from public.pachtvertragsfassung n where n.vorgaenger_fassung_id=f.id);
    if v_letzte_fassungen=0 then raise exception 'Für das aktive Pachtverhältnis existiert keine aktuelle Vertragsfassung' using errcode='23514'; end if;
    if v_letzte_fassungen<>1 then raise exception 'Für das aktive Pachtverhältnis ist keine eindeutige aktuelle Vertragsfassung bestimmbar' using errcode='23514'; end if;
    select r.pachtvertragsfassung_id into v_fassung_id from public.register_pachtvertrags_neufassung(v_pachtverhaeltnis_id,v_vorgaenger_id,v_vorgang.final_dokument_id,v_vertragsdatum,v_wirksam_ab,p_created_by) r;
  end if;
  update public.kommunikationsvorgang set status='erledigt',bearbeitet_von=p_created_by where id=v_vorgang.id and status='letzte_pruefung' and revision=p_expected_revision and signatur_revision=p_expected_revision and final_dokument_id=v_vorgang.final_dokument_id and zugewiesen_an=p_created_by;
  if not found then raise exception 'Der Vertragsvorgang konnte nicht abgeschlossen werden' using errcode='40001'; end if;
  return query select v_pachtverhaeltnis_id,v_fassung_id,false;
end;
$function$;

CREATE OR REPLACE FUNCTION public.confirm_arbeitseinsatz_arbeitsstunde(p_arbeitseinsatz_anmeldung_id bigint, p_stunden numeric, p_art_der_arbeit text DEFAULT NULL::text)
 RETURNS arbeitsstunde
 LANGUAGE plpgsql
 SECURITY DEFINER
 SET search_path TO 'public', 'pg_temp'
AS $function$
declare v_registration public.arbeitseinsatz_anmeldung; v_assignment public.arbeitseinsatz; v_saison_id integer; v_reviewer integer; v_row public.arbeitsstunde; v_end timestamp without time zone;
begin
  if not public.has_effective_permission(1048576) or not public.has_effective_permission(256) then raise exception 'Für die Arbeitsstundenbestätigung fehlen ManageWorkAssignments und ManageWorkHours.' using errcode = '42501'; end if;
  if p_stunden is null or p_stunden <= 0 then raise exception 'Die Stunden müssen größer als 0 sein.' using errcode = '22023'; end if;
  v_reviewer := public.current_mitglied_id(); if v_reviewer is null then raise exception 'Der Bearbeiter ist keinem Mitglied zugeordnet.' using errcode = '42501'; end if;
  select * into v_registration from public.arbeitseinsatz_anmeldung where id = p_arbeitseinsatz_anmeldung_id for update;
  if not found then raise exception 'Die Anmeldung existiert nicht.' using errcode = 'P0002'; end if;
  select * into v_assignment from public.arbeitseinsatz where id = v_registration.arbeitseinsatz_id;
  if not found then raise exception 'Der Arbeitseinsatz wurde nicht gefunden.' using errcode = 'P0002'; end if;
  if v_registration.status = 'teilgenommen' or exists (select 1 from public.arbeitsstunde where arbeitseinsatz_anmeldung_id = v_registration.id) then raise exception 'Für diese Anmeldung existiert bereits eine Arbeitsstunde.' using errcode = '23505'; end if;
  v_end := v_assignment.datum + coalesce(v_assignment.end_uhrzeit, v_assignment.start_uhrzeit, time '23:59'); if public.kgv_local_now() < v_end then raise exception 'Arbeitsstunden können erst nach Einsatzende bestätigt werden.' using errcode = '42501'; end if;
  select id into v_saison_id from public.saison where jahr = extract(year from v_assignment.datum)::integer limit 1; if v_saison_id is null then raise exception 'Für das Einsatzjahr ist keine Saison hinterlegt.' using errcode = 'P0002'; end if;
  insert into public.arbeitsstunde(mitglied_id,saison_id,datum,stunden,art_der_arbeit,status,freigegeben,genehmigt_von,genehmigt_am,arbeitseinsatz_anmeldung_id) values (v_registration.mitglied_id,v_saison_id,v_assignment.datum,p_stunden,coalesce(nullif(btrim(p_art_der_arbeit),''),v_assignment.titel),'genehmigt',true,v_reviewer,now(),v_registration.id) returning * into v_row;
  update public.arbeitseinsatz_anmeldung set status = 'teilgenommen', updated_at = now() where id = v_registration.id;
  return v_row;
end; $function$;

CREATE OR REPLACE FUNCTION public.discard_prepared_contract(p_kommunikationsvorgang_id bigint, p_expected_revision integer, p_discarded_by uuid)
 RETURNS TABLE(id bigint, status text, final_dokument_id bigint)
 LANGUAGE plpgsql
 SECURITY DEFINER
 SET search_path TO 'public', 'pg_temp'
AS $function$
declare v_vorgang public.kommunikationsvorgang%rowtype;
begin
  if auth.role() is distinct from 'service_role' then raise exception 'Das Verwerfen ist nur serverseitig zulässig' using errcode='42501'; end if;
  if not exists (
    select 1 from public.app_user au where au.user_id=p_discarded_by
      and (((public.permission_role_base(au.role)|coalesce(au.permission_grants,0)) & ~coalesce(au.permission_revocations,0) & 524304)=524304)
  ) then raise exception 'Keine Berechtigung zum Verwerfen' using errcode='42501'; end if;
  select * into v_vorgang from public.kommunikationsvorgang kv where kv.id=p_kommunikationsvorgang_id for update;
  if not found then raise exception 'Unbekannter Kommunikationsvorgang' using errcode='P0002'; end if;
  if v_vorgang.vorgangstyp<>'vertrag_signatur' or v_vorgang.dokumenttyp not in ('mitgliedsantrag','pachtvertrag') then raise exception 'Der Vorgang ist kein Vertragsvorgang' using errcode='23514'; end if;
  if v_vorgang.status='verworfen' then return query select v_vorgang.id,v_vorgang.status,v_vorgang.final_dokument_id; return; end if;
  if v_vorgang.revision<>p_expected_revision or v_vorgang.zugewiesen_an is distinct from p_discarded_by then raise exception 'Der Vorgang wurde zwischenzeitlich geändert oder ist nicht zugewiesen' using errcode='40001'; end if;
  if v_vorgang.status not in ('vorbereitet','zur_signatur','letzte_pruefung') then raise exception 'Abgeschlossene Vertragsvorgänge können nicht verworfen werden' using errcode='23514'; end if;
  update public.dokument set archiviert_at=now(),archiviert_by=p_discarded_by,archiviert_begruendung='Verworfener Vertragsvorgang' where source_kommunikationsvorgang_id=v_vorgang.id and archiviert_at is null;
  update public.kommunikationsvorgang kv set status='verworfen',bearbeitet_von=p_discarded_by where kv.id=v_vorgang.id and kv.revision=p_expected_revision and kv.status in ('vorbereitet','zur_signatur','letzte_pruefung') and kv.zugewiesen_an=p_discarded_by;
  if not found then raise exception 'Der Vertragsvorgang wurde zwischenzeitlich geändert' using errcode='40001'; end if;
  return query select v_vorgang.id,'verworfen'::text,v_vorgang.final_dokument_id;
end;
$function$;

CREATE OR REPLACE FUNCTION public.manage_arbeitseinsatz_anmeldung(p_arbeitseinsatz_id bigint, p_mitglied_id bigint, p_action text)
 RETURNS arbeitseinsatz_anmeldung
 LANGUAGE plpgsql
 SECURITY DEFINER
 SET search_path TO 'public', 'pg_temp'
AS $function$
declare v_assignment public.arbeitseinsatz; v_row public.arbeitseinsatz_anmeldung; v_begin timestamp without time zone; v_count integer; v_registration_exists boolean;
begin
  if auth.uid() is null or not public.has_effective_permission(1048576) then raise exception 'Für die Teilnehmerverwaltung fehlt ManageWorkAssignments.' using errcode = '42501'; end if;
  if p_action not in ('anmelden', 'absagen', 'nicht_erschienen') then raise exception 'Ungültige Teilnehmeraktion.' using errcode = '22023'; end if;
  select * into v_assignment from public.arbeitseinsatz where id = p_arbeitseinsatz_id for update;
  if not found then raise exception 'Arbeitseinsatz wurde nicht gefunden.' using errcode = 'P0002'; end if;
  v_begin := v_assignment.datum + coalesce(v_assignment.start_uhrzeit, time '23:59');
  select * into v_row from public.arbeitseinsatz_anmeldung where arbeitseinsatz_id = p_arbeitseinsatz_id and mitglied_id = p_mitglied_id for update;
  v_registration_exists := found;
  if p_action = 'anmelden' then
    if not exists (select 1 from public.mitglied where id = p_mitglied_id and aktiv = true) then raise exception 'Mitglied ist nicht aktiv.' using errcode = '42501'; end if;
    if v_assignment.aktiv is distinct from true then raise exception 'Anmeldung nicht möglich: Arbeitseinsatz ist nicht aktiv.'; end if;
    if public.kgv_local_now() >= v_begin then raise exception 'Anmeldung nicht möglich: Arbeitseinsatz hat bereits begonnen.'; end if;
    if v_registration_exists and v_row.status = 'angemeldet' then return v_row; end if;
    if v_registration_exists and v_row.status in ('teilgenommen', 'nicht_erschienen') then raise exception 'Dieser Teilnehmerstatus kann nicht wieder angemeldet werden.' using errcode = '42501'; end if;
    if v_assignment.max_teilnehmer is not null then select count(*) into v_count from public.arbeitseinsatz_anmeldung where arbeitseinsatz_id = p_arbeitseinsatz_id and status = 'angemeldet'; if v_count >= v_assignment.max_teilnehmer then raise exception 'Anmeldung nicht möglich: maximale Teilnehmerzahl erreicht.'; end if; end if;
    if v_registration_exists then update public.arbeitseinsatz_anmeldung set status = 'angemeldet', angemeldet_am = now(), updated_at = now() where id = v_row.id returning * into v_row; else insert into public.arbeitseinsatz_anmeldung(arbeitseinsatz_id, mitglied_id, status) values (p_arbeitseinsatz_id, p_mitglied_id, 'angemeldet') returning * into v_row; end if;
  elsif p_action = 'absagen' then
    if not v_registration_exists then raise exception 'Es besteht keine Anmeldung für diesen Arbeitseinsatz.' using errcode = 'P0002'; end if;
    if v_row.status = 'teilgenommen' then raise exception 'Teilgenommen kann nicht über die Teilnehmerverwaltung geändert werden.' using errcode = '42501'; end if;
    if v_row.status <> 'abgesagt' then update public.arbeitseinsatz_anmeldung set status = 'abgesagt', updated_at = now() where id = v_row.id returning * into v_row; end if;
  else
    if public.kgv_local_now() < v_begin then raise exception 'Nicht erschienen kann erst nach Einsatzbeginn gesetzt werden.' using errcode = '42501'; end if;
    if not v_registration_exists or v_row.status not in ('angemeldet', 'nicht_erschienen') then raise exception 'Nicht erschienen ist nur für angemeldete Teilnehmer möglich.' using errcode = '42501'; end if;
    if v_row.status <> 'nicht_erschienen' then update public.arbeitseinsatz_anmeldung set status = 'nicht_erschienen', updated_at = now() where id = v_row.id returning * into v_row; end if;
  end if;
  return v_row;
end;
$function$;

CREATE OR REPLACE FUNCTION public.prepare_pachtvertrag_ersatz_vor_phase9(p_vorgaenger_fassung_id bigint, p_assigned_to uuid)
 RETURNS TABLE(kommunikationsvorgang_id bigint)
 LANGUAGE plpgsql
 SECURITY DEFINER
 SET search_path TO 'public', 'pg_temp'
AS $function$
declare v_fassung public.pachtvertragsfassung%rowtype; v_quelle public.kommunikationsvorgang%rowtype; v_parzelle_id bigint; v_hauptmitglied_id bigint; v_entwurf jsonb; v_id bigint;
begin
 if auth.uid() is null or p_assigned_to is distinct from auth.uid() then raise exception 'Der Ersatzvorgang muss dem angemeldeten Benutzer zugewiesen werden' using errcode='42501'; end if;
 if not public.has_effective_permission(524288) then raise exception 'CanCreateMitglied required' using errcode='42501'; end if;
 select f.* into v_fassung from public.pachtvertragsfassung f where f.id=p_vorgaenger_fassung_id for update;
 if not found then raise exception 'Unbekannte Pachtvertragsfassung: %',p_vorgaenger_fassung_id using errcode='P0002'; end if;
 if v_fassung.nicht_mehr_massgeblich_am is not null or exists(select 1 from public.pachtvertragsfassung n where n.vorgaenger_fassung_id=v_fassung.id) then raise exception 'Nur die aktuell maßgebliche Vertragsfassung kann ersetzt werden' using errcode='23514'; end if;
 select pv.parzelle_id,min(pb.mitglied_id) into v_parzelle_id,v_hauptmitglied_id from public.pachtverhaeltnis pv join public.pachtverhaeltnis_beteiligung pb on pb.pachtverhaeltnis_id=pv.id and pb.rolle='hauptpaechter' and pb.gueltig_bis is null where pv.id=v_fassung.pachtverhaeltnis_id and pv.beendet_am is null group by pv.parzelle_id;
 if v_parzelle_id is null or v_hauptmitglied_id is null then raise exception 'Für die Vertragsfassung besteht kein aktives Pachtverhältnis' using errcode='23514'; end if;
 select kv.* into v_quelle from public.dokument d join public.kommunikationsvorgang kv on kv.id=d.source_kommunikationsvorgang_id where d.id=v_fassung.dokument_id and kv.dokumenttyp='pachtvertrag' and kv.status='erledigt';
 if not found then raise exception 'Die maßgebliche Vertragsfassung besitzt keinen abgeschlossenen Prepared-Ursprung' using errcode='23514'; end if;
 v_entwurf:=jsonb_set(v_quelle.entwurf_daten,'{draft,effectiveDate}',to_jsonb(current_date::text),true);
 insert into public.kommunikationsvorgang(vorgangstyp,dokumenttyp,status,mitglied_id,parzelle_id,entwurf_daten,revision,zugewiesen_an,erstellt_von,bearbeitet_von,ersetzt_pachtvertragsfassung_id) values ('vertrag_signatur','pachtvertrag','vorbereitet',v_hauptmitglied_id,v_parzelle_id,v_entwurf,1,p_assigned_to,auth.uid(),auth.uid(),v_fassung.id) returning id into v_id;
 return query select v_id;
end;
$function$;

CREATE OR REPLACE FUNCTION public.sign_off_from_arbeitseinsatz(p_arbeitseinsatz_id bigint, p_mitglied_id bigint)
 RETURNS arbeitseinsatz_anmeldung
 LANGUAGE plpgsql
 SECURITY DEFINER
 SET search_path TO 'public', 'pg_temp'
AS $function$
declare v_assignment public.arbeitseinsatz; v_row public.arbeitseinsatz_anmeldung; v_begin timestamp without time zone;
begin
  if auth.uid() is null or public.current_mitglied_id() is null or p_mitglied_id <> public.current_mitglied_id() then raise exception 'Abmeldung ist nur für das eigene Mitglied möglich.' using errcode = '42501'; end if;
  select * into v_assignment from public.arbeitseinsatz where id = p_arbeitseinsatz_id for update;
  if not found then raise exception 'Arbeitseinsatz wurde nicht gefunden.' using errcode = 'P0002'; end if;
  select * into v_row from public.arbeitseinsatz_anmeldung where arbeitseinsatz_id = p_arbeitseinsatz_id and mitglied_id = p_mitglied_id for update;
  if not found then raise exception 'Es besteht keine Anmeldung für diesen Arbeitseinsatz.' using errcode = 'P0002'; end if;
  if v_row.status = 'abgesagt' then return v_row; end if;
  if v_row.status in ('teilgenommen', 'nicht_erschienen') then raise exception 'Abmeldung nicht möglich: Der Teilnehmerstatus wird bereits verwaltet.' using errcode = '42501'; end if;
  v_begin := v_assignment.datum + coalesce(v_assignment.start_uhrzeit, time '23:59');
  if public.kgv_local_now() >= v_begin then raise exception 'Abmeldung nicht möglich: Arbeitseinsatz hat bereits begonnen.'; end if;
  update public.arbeitseinsatz_anmeldung set status = 'abgesagt', updated_at = now() where id = v_row.id returning * into v_row;
  return v_row;
end;
$function$;

CREATE OR REPLACE FUNCTION public.sign_up_for_arbeitseinsatz(p_arbeitseinsatz_id bigint, p_mitglied_id bigint)
 RETURNS arbeitseinsatz_anmeldung
 LANGUAGE plpgsql
 SECURITY DEFINER
 SET search_path TO 'public', 'pg_temp'
AS $function$
declare
  v_assignment public.arbeitseinsatz;
  v_row public.arbeitseinsatz_anmeldung;
  v_registration_exists boolean;
  v_begin timestamp without time zone;
  v_count integer;
begin
  if auth.uid() is null or public.current_mitglied_id() is null or p_mitglied_id <> public.current_mitglied_id() then raise exception 'Anmeldung ist nur für das eigene Mitglied möglich.' using errcode = '42501'; end if;
  select * into v_assignment from public.arbeitseinsatz where id = p_arbeitseinsatz_id for update;
  if not found then raise exception 'Arbeitseinsatz wurde nicht gefunden.' using errcode = 'P0002'; end if;

  select * into v_row from public.arbeitseinsatz_anmeldung where arbeitseinsatz_id = p_arbeitseinsatz_id and mitglied_id = p_mitglied_id for update;
  v_registration_exists := found;
  if v_registration_exists and v_row.status = 'angemeldet' then return v_row; end if;
  if v_registration_exists and v_row.status in ('teilgenommen', 'nicht_erschienen') then raise exception 'Anmeldung nicht möglich: Der Teilnehmerstatus wird bereits verwaltet.' using errcode = '42501'; end if;

  v_begin := v_assignment.datum + coalesce(v_assignment.start_uhrzeit, time '23:59');
  if v_assignment.aktiv is distinct from true then raise exception 'Anmeldung nicht möglich: Arbeitseinsatz ist nicht aktiv.'; end if;
  if public.kgv_local_now() >= v_begin then raise exception 'Anmeldung nicht möglich: Arbeitseinsatz hat bereits begonnen.'; end if;
  if v_assignment.anmeldung_bis is not null and public.kgv_local_now() > v_assignment.anmeldung_bis then raise exception 'Anmeldung nicht möglich: Anmeldeschluss ist erreicht.'; end if;
  if v_assignment.max_teilnehmer is not null then
    select count(*) into v_count from public.arbeitseinsatz_anmeldung where arbeitseinsatz_id = p_arbeitseinsatz_id and status = 'angemeldet';
    if v_count >= v_assignment.max_teilnehmer then raise exception 'Anmeldung nicht möglich: maximale Teilnehmerzahl erreicht.'; end if;
  end if;

  if v_registration_exists then
    update public.arbeitseinsatz_anmeldung set status = 'angemeldet', angemeldet_am = now(), updated_at = now() where id = v_row.id returning * into v_row;
  else
    insert into public.arbeitseinsatz_anmeldung(arbeitseinsatz_id, mitglied_id, status) values (p_arbeitseinsatz_id, p_mitglied_id, 'angemeldet') returning * into v_row;
  end if;
  return v_row;
end;
$function$;

CREATE OR REPLACE FUNCTION public.start_mitgliedsaufnahme_vorstandsbearbeitung(p_kommunikationsvorgang_id bigint)
 RETURNS bigint
 LANGUAGE plpgsql
 SECURITY DEFINER
 SET search_path TO 'public', 'pg_temp'
AS $function$
declare v public.kommunikationsvorgang%rowtype; b_id bigint; r record; p_id bigint; caller uuid:=auth.uid();
begin
 if caller is null or not public.has_effective_permission(524288) then raise exception 'Keine Berechtigung zum Start der Vorstandsbearbeitung' using errcode='42501'; end if;
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
end;
$function$;

CREATE OR REPLACE FUNCTION public.submit_arbeitseinsatz_arbeitsstunde(p_arbeitseinsatz_anmeldung_id bigint, p_stunden numeric, p_art_der_arbeit text DEFAULT NULL::text)
 RETURNS arbeitsstunde
 LANGUAGE plpgsql
 SECURITY DEFINER
 SET search_path TO 'public', 'pg_temp'
AS $function$
declare v_registration public.arbeitseinsatz_anmeldung; v_assignment public.arbeitseinsatz; v_saison_id integer; v_row public.arbeitsstunde; v_end timestamp without time zone;
begin
  if auth.uid() is null or public.current_mitglied_id() is null then raise exception 'Es ist ein angemeldetes Mitglied erforderlich.' using errcode = '42501'; end if;
  if p_stunden is null or p_stunden <= 0 then raise exception 'Die Stunden müssen größer als 0 sein.' using errcode = '22023'; end if;
  select * into v_registration from public.arbeitseinsatz_anmeldung where id = p_arbeitseinsatz_anmeldung_id for update;
  if not found or v_registration.mitglied_id <> public.current_mitglied_id() then raise exception 'Die Anmeldung gehört nicht zum aktuellen Mitglied.' using errcode = '42501'; end if;
  select * into v_assignment from public.arbeitseinsatz where id = v_registration.arbeitseinsatz_id;
  if not found or v_assignment.aktiv is distinct from true then raise exception 'Der Arbeitseinsatz ist nicht aktiv.' using errcode = '42501'; end if;
  if v_registration.status <> 'angemeldet' then raise exception 'Arbeitsstunden können nur für angemeldete Teilnehmer eingereicht werden.' using errcode = '42501'; end if;
  v_end := v_assignment.datum + coalesce(v_assignment.end_uhrzeit, v_assignment.start_uhrzeit, time '23:59');
  if public.kgv_local_now() < v_end then raise exception 'Arbeitsstunden können erst nach Einsatzende eingereicht werden.' using errcode = '42501'; end if;
  select id into v_saison_id from public.saison where jahr = extract(year from v_assignment.datum)::integer limit 1;
  if v_saison_id is null then raise exception 'Für das Einsatzjahr ist keine Saison hinterlegt.' using errcode = 'P0002'; end if;
  if exists (select 1 from public.arbeitsstunde where arbeitseinsatz_anmeldung_id = v_registration.id) then raise exception 'Für diese Anmeldung wurden bereits Arbeitsstunden erfasst.' using errcode = '23505'; end if;
  insert into public.arbeitsstunde(mitglied_id,saison_id,datum,stunden,art_der_arbeit,status,freigegeben,genehmigt_von,genehmigt_am,arbeitseinsatz_anmeldung_id) values (v_registration.mitglied_id,v_saison_id,v_assignment.datum,p_stunden,coalesce(nullif(btrim(p_art_der_arbeit),''),v_assignment.titel),'offen',false,null,null,v_registration.id) returning * into v_row;
  return v_row;
end; $function$;

CREATE OR REPLACE FUNCTION public.acquire_browser_edit_lock_core(p_entity_type text, p_entity_id text, p_client_instance_id uuid, p_timeout_seconds integer DEFAULT 600)
 RETURNS TABLE(acquired boolean, locked_by_user_id uuid, locked_by_display_name text, expires_at timestamp with time zone)
 LANGUAGE plpgsql
 SECURITY DEFINER
 SET search_path TO 'public'
AS $function$
declare
  v_user_id uuid := auth.uid();
  v_now timestamptz := now();
  v_timeout integer := greatest(60, least(coalesce(p_timeout_seconds, 600), 1800));
  v_locked_by uuid;
  v_locked_client_instance_id uuid;
  v_expires_at timestamptz;
  v_legacy_locked_by uuid;
  v_legacy_locked_at timestamp without time zone;
  v_rows integer;
  v_acquired boolean := false;
begin
  if v_user_id is null then
    raise exception 'Anmeldung erforderlich.' using errcode = '42501';
  end if;
  if p_client_instance_id is null then
    raise exception 'Client-Instanz erforderlich.' using errcode = '22023';
  end if;
  if not exists (select 1 from public.app_user where user_id = v_user_id) then
    raise exception 'Keine KGV-Berechtigung vorhanden.' using errcode = '42501';
  end if;
  if p_entity_type is null or p_entity_type !~ '^[a-z0-9_]{1,64}$'
     or p_entity_id is null or char_length(p_entity_id) not between 1 and 128 then
    raise exception 'Ungueltiger Sperrschluessel.' using errcode = '22023';
  end if;

  if p_entity_type in ('arbeitseinsatz', 'arbeitseinsatz_anmeldung') then
    if not public.has_effective_permission(1048576) or p_entity_id !~ '^[0-9]+$' then
      raise exception 'Für Arbeitseinsätze fehlt ManageWorkAssignments oder der Schlüssel ist ungültig.' using errcode = '42501';
    end if;
    if p_entity_type = 'arbeitseinsatz' and not exists (
      select 1 from public.arbeitseinsatz a
      where a.id = p_entity_id::bigint

    ) then
      raise exception 'Arbeitseinsatz liegt außerhalb des zulässigen Scopes.' using errcode = '42501';
    end if;
    if p_entity_type = 'arbeitseinsatz_anmeldung' and not exists (
      select 1
      from public.arbeitseinsatz_anmeldung aa
      join public.arbeitseinsatz a on a.id = aa.arbeitseinsatz_id
      where aa.id = p_entity_id::bigint

    ) then
      raise exception 'Anmeldung liegt außerhalb des zulässigen Scopes.' using errcode = '42501';
    end if;
  end if;
  if p_entity_type = 'termin' and (
    not public.has_effective_permission(2097152)
    or not exists (
      select 1 from public.termin t
      where t.id = p_entity_id::bigint

    )
  ) then
    raise exception 'Termin liegt außerhalb des zulässigen Scopes.' using errcode = '42501';
  end if;
  if p_entity_type = 'bekanntmachung' and (
    not public.has_effective_permission(4194304)
    or not exists (
      select 1 from public.bekanntmachung b
      where b.id = p_entity_id::bigint

    )
  ) then
    raise exception 'Bekanntmachung liegt außerhalb des zulässigen Scopes.' using errcode = '42501';
  end if;

  delete from public.browser_edit_lock l where l.expires_at <= v_now;

  insert into public.browser_edit_lock (
    entity_type, entity_id, locked_by_user_id, client_instance_id, locked_at, expires_at
  ) values (
    p_entity_type, p_entity_id, v_user_id, p_client_instance_id, v_now,
    v_now + make_interval(secs => v_timeout)
  )
  on conflict (entity_type, entity_id) do update
    set expires_at = excluded.expires_at
  where browser_edit_lock.expires_at <= v_now
     or (
       browser_edit_lock.locked_by_user_id = v_user_id
       and browser_edit_lock.client_instance_id is not distinct from p_client_instance_id
     )
  returning browser_edit_lock.locked_by_user_id, browser_edit_lock.client_instance_id, browser_edit_lock.expires_at
    into v_locked_by, v_locked_client_instance_id, v_expires_at;

  if v_locked_by is null then
    select l.locked_by_user_id, l.client_instance_id, l.expires_at
      into v_locked_by, v_locked_client_instance_id, v_expires_at
    from public.browser_edit_lock l
    where l.entity_type = p_entity_type and l.entity_id = p_entity_id;
  end if;

  v_acquired := v_locked_by = v_user_id
    and v_locked_client_instance_id is not distinct from p_client_instance_id;

  -- Preserve the legacy lock fields required by older RPCs and triggers.
  if v_acquired and p_entity_type = 'mitglied' and p_entity_id ~ '^[0-9]+$' then
    update public.mitglied
       set lockedbyuserid = v_user_id,
           lockat = v_now at time zone 'UTC'
     where id = p_entity_id::bigint
       and (
         lockedbyuserid is null
         or lockedbyuserid = v_user_id
         or (lockat is not null and lockat < (v_now at time zone 'UTC') - make_interval(secs => v_timeout))
       );
    get diagnostics v_rows = row_count;
    if v_rows = 0 then
      select lockedbyuserid, lockat into v_legacy_locked_by, v_legacy_locked_at
      from public.mitglied where id = p_entity_id::bigint;
      delete from public.browser_edit_lock l
      where l.entity_type = p_entity_type and l.entity_id = p_entity_id
        and l.locked_by_user_id = v_user_id
        and l.client_instance_id is not distinct from p_client_instance_id;
      v_locked_by := v_legacy_locked_by;
      v_locked_client_instance_id := null;
      v_expires_at := (v_legacy_locked_at at time zone 'UTC') + make_interval(secs => v_timeout);
      v_acquired := false;
    end if;
  elsif v_acquired and p_entity_type = 'arbeitsstunde' and p_entity_id ~ '^[0-9]+$' then
    update public.arbeitsstunde
       set lockedbyuserid = v_user_id,
           lockat = v_now at time zone 'UTC'
     where id = p_entity_id::integer
       and (
         lockedbyuserid is null
         or lockedbyuserid = v_user_id
         or (lockat is not null and lockat < (v_now at time zone 'UTC') - make_interval(secs => v_timeout))
       );
    get diagnostics v_rows = row_count;
    if v_rows = 0 then
      select lockedbyuserid, lockat into v_legacy_locked_by, v_legacy_locked_at
      from public.arbeitsstunde where id = p_entity_id::integer;
      delete from public.browser_edit_lock l
      where l.entity_type = p_entity_type and l.entity_id = p_entity_id
        and l.locked_by_user_id = v_user_id
        and l.client_instance_id is not distinct from p_client_instance_id;
      v_locked_by := v_legacy_locked_by;
      v_locked_client_instance_id := null;
      v_expires_at := (v_legacy_locked_at at time zone 'UTC') + make_interval(secs => v_timeout);
      v_acquired := false;
    end if;
  end if;

  return query select
    v_acquired,
    v_locked_by,
    coalesce(public.browser_edit_lock_display_name(v_locked_by), 'Ein anderer Benutzer'),
    v_expires_at;
end;
$function$;

CREATE OR REPLACE FUNCTION public.begin_historical_contract_upload(p_operation_id uuid, p_actor uuid, p_member_id bigint, p_contract_type text, p_upload_request jsonb, p_drive_file_id text, p_file_name text)
 RETURNS jsonb
 LANGUAGE plpgsql
 SECURITY DEFINER
 SET search_path TO 'public', 'pg_temp'
AS $function$
declare v_op public.historical_contract_operation%rowtype;
begin
 if auth.role() is distinct from 'service_role' then raise exception 'Backend required' using errcode='42501'; end if;
 if p_operation_id is null or p_actor is null or p_upload_request is null or p_drive_file_id is null or p_file_name is null then raise exception 'Incomplete operation' using errcode='22023'; end if;
 insert into public.historical_contract_operation(operation_id,created_by,mitglied_id,contract_type,upload_request,drive_file_id,file_name)
 values(p_operation_id,p_actor,p_member_id,p_contract_type,p_upload_request,p_drive_file_id,p_file_name)
 on conflict(operation_id) do nothing;
 select o.* into strict v_op from public.historical_contract_operation o where o.operation_id=p_operation_id for update;
 if v_op.created_by is distinct from p_actor or v_op.mitglied_id is distinct from p_member_id
 or v_op.contract_type is distinct from p_contract_type or v_op.upload_request is distinct from p_upload_request then
 raise exception 'Vorgangs-ID wurde bereits mit anderen Uploaddaten verwendet.' using errcode='22023'; end if;
 return to_jsonb(v_op);
end; $function$;

create or replace function public.can_access_historical_contract_member(p_mitglied_id bigint)
returns boolean language sql stable security definer set search_path=public,pg_temp as $$
  select coalesce(public.can_register_historical_contracts(), false)
    and exists (select 1 from public.mitglied m where m.id=p_mitglied_id);
$$;

create or replace function public.can_access_historical_contract_document(p_dokument_id bigint,p_require_registered boolean default false)
returns boolean language sql stable security definer set search_path=public,pg_temp as $$
  select coalesce(public.can_register_historical_contracts(),false) and exists(
    select 1 from public.dokument d where d.id=p_dokument_id and d.archiviert_at is null
      and d.source_kommunikationsvorgang_id is null and d.source_wartungsvertrag_zuordnung_id is null
      and public.can_access_historical_contract_member(d.mitglied_id)
      and ((public.is_registered_historical_contract_document(d.id)
        and not exists(select 1 from public.pachtvertragsfassung f where f.dokument_id=d.id
          and (not exists(select 1 from public.pachtverhaeltnis_beteiligung b where b.pachtverhaeltnis_id=f.pachtverhaeltnis_id
            and b.gueltig_ab<=f.wirksam_ab and (b.gueltig_bis is null or b.gueltig_bis>=f.wirksam_ab))
          or exists(select 1 from public.pachtverhaeltnis_beteiligung b where b.pachtverhaeltnis_id=f.pachtverhaeltnis_id
            and b.gueltig_ab<=f.wirksam_ab and (b.gueltig_bis is null or b.gueltig_bis>=f.wirksam_ab)
            and not public.can_access_historical_contract_member(b.mitglied_id)))))
        or (not p_require_registered and d.dokumenttyp_id is null
          and d.historical_contract_type in ('historischer_ma_pv_erstvertrag','historischer_pv_folgevertrag')
          and d.created_by=auth.uid())));
$$;

CREATE OR REPLACE FUNCTION public.end_membership(p_main_member_id bigint, p_secondary_decision text DEFAULT NULL::text)
 RETURNS TABLE(success boolean, message text, updated_main_member jsonb, updated_secondary_member jsonb, secondary_decision text)
 LANGUAGE plpgsql
 SECURITY DEFINER
 SET search_path TO 'public', 'pg_temp'
AS $function$
DECLARE
    v_user_id uuid := auth.uid();
    v_main_member public.mitglied%ROWTYPE;
    v_secondary_member public.mitglied%ROWTYPE;
    v_has_secondary boolean := false;
    v_is_productive_admin boolean := false;
    v_end_date date := (public.kgv_local_now())::date;
    v_now_utc timestamp without time zone := now() AT TIME ZONE 'UTC';
    v_message text;
BEGIN
    IF v_user_id IS NULL THEN
        RAISE EXCEPTION 'Anmeldung erforderlich.' USING ERRCODE = '42501';
    END IF;

    v_is_productive_admin := public.is_productive_admin_or_vorstand();
    IF NOT v_is_productive_admin THEN
        RAISE EXCEPTION 'Keine Berechtigung zum Beenden von Mitgliedschaften.' USING ERRCODE = '42501';
    END IF;

    SELECT m.*
      INTO v_main_member
      FROM public.mitglied AS m
     WHERE m.id = p_main_member_id
     FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Hauptmitglied wurde nicht gefunden.';
    END IF;


    IF v_main_member.hauptmitglied_id IS NOT NULL THEN
        RAISE EXCEPTION 'Der Folgeentscheid ist nur für Hauptmitglieder verfügbar.';
    END IF;

    IF v_main_member.mitglied_ende IS NOT NULL THEN
        RAISE EXCEPTION 'Die Mitgliedschaft ist bereits beendet.';
    END IF;

    IF v_main_member.lockedbyuserid IS DISTINCT FROM v_user_id
       OR v_main_member.lockat IS NULL
       OR v_main_member.lockat <= v_now_utc - INTERVAL '10 minutes' THEN
        RAISE EXCEPTION 'Kein gültiger Lock auf dem Hauptmitglied.';
    END IF;

    SELECT m.*
      INTO v_secondary_member
      FROM public.mitglied AS m
     WHERE m.hauptmitglied_id = p_main_member_id
     ORDER BY m.id
     LIMIT 1
     FOR UPDATE;
    v_has_secondary := FOUND;

    IF NOT v_has_secondary THEN
        IF p_secondary_decision IS NOT NULL THEN
            RAISE EXCEPTION 'Ohne Nebenmitglied ist keine Folgeentscheidung zulässig.';
        END IF;
    ELSIF p_secondary_decision IS NULL
       OR p_secondary_decision NOT IN ('end_secondary', 'promote_secondary') THEN
        RAISE EXCEPTION 'Für das vorhandene Nebenmitglied ist eine gültige Folgeentscheidung erforderlich.';
    END IF;


    IF v_has_secondary
       AND v_secondary_member.lockedbyuserid IS NOT NULL
       AND v_secondary_member.lockedbyuserid <> v_user_id
       AND (
           v_secondary_member.lockat IS NULL
           OR v_secondary_member.lockat + INTERVAL '10 minutes' > v_now_utc
       ) THEN
        RAISE EXCEPTION 'Das Nebenmitglied ist aktuell gesperrt.';
    END IF;

    IF v_has_secondary THEN
        IF p_secondary_decision = 'end_secondary' THEN
            UPDATE public.mitglied AS m
               SET mitglied_ende = v_end_date,
                   aktiv = false
             WHERE m.id = v_secondary_member.id
            RETURNING m.* INTO v_secondary_member;

            v_message := 'Haupt- und Nebenmitglied wurden beendet.';
        ELSE
            UPDATE public.mitglied AS m
               SET hauptmitglied_id = NULL,
                   mitglied_ende = NULL,
                   aktiv = true
             WHERE m.id = v_secondary_member.id
            RETURNING m.* INTO v_secondary_member;

            v_message := 'Hauptmitglied wurde beendet und das Nebenmitglied zum Hauptmitglied gemacht.';
        END IF;
    ELSE
        v_message := 'Mitgliedschaft wurde beendet.';
    END IF;

    UPDATE public.mitglied AS m
       SET mitglied_ende = v_end_date,
           aktiv = false
     WHERE m.id = v_main_member.id
    RETURNING m.* INTO v_main_member;

    RETURN QUERY
    SELECT
        true,
        v_message,
        to_jsonb(v_main_member),
        CASE WHEN v_has_secondary THEN to_jsonb(v_secondary_member) ELSE NULL::jsonb END,
        p_secondary_decision;
END;
$function$;

CREATE OR REPLACE FUNCTION public.enforce_administrative_arbeitsstunde_insert()
 RETURNS trigger
 LANGUAGE plpgsql
 SECURITY DEFINER
 SET search_path TO 'public'
AS $function$
begin
  if new.status = 'genehmigt'
     or new.freigegeben = true
     or new.genehmigt_von is not null
     or new.genehmigt_am is not null then
    if not public.has_effective_permission(256) then
      raise exception 'Für die administrative Arbeitsstundenerfassung fehlt ManageWorkHours.' using errcode = '42501';
    end if;

    if public.current_mitglied_id() is null then
      raise exception 'Der Bearbeiter ist keinem Mitglied zugeordnet.' using errcode = '42501';
    end if;


    -- Genehmiger und Zeitpunkt stammen ausschließlich aus dem aktuellen Kontext.
    new.status := 'genehmigt';
    new.freigegeben := true;
    new.genehmigt_von := public.current_mitglied_id();
    new.genehmigt_am := now();
  end if;

  return new;
end;
$function$;

CREATE OR REPLACE FUNCTION public.ensure_wartungsvertrag_dokument_workflow(p_zuordnung_id bigint)
 RETURNS SETOF wartungsvertrag_dokument_workflow
 LANGUAGE plpgsql
 SECURITY DEFINER
 SET search_path TO 'public', 'pg_temp'
AS $function$
DECLARE
    v_assignment public.wartungsvertrag_zuordnungen%ROWTYPE;
BEGIN
    IF NOT public.is_admin_or_vorstand() THEN
        RAISE EXCEPTION 'Nur Admin/Vorstand darf Wartungsvertragsdokumente vorbereiten.'
            USING ERRCODE = '42501';
    END IF;

    SELECT * INTO v_assignment
    FROM public.wartungsvertrag_zuordnungen
    WHERE id = p_zuordnung_id;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Die Wartungsvertragszuordnung wurde nicht gefunden.'
            USING ERRCODE = 'P0002';
    END IF;

    INSERT INTO public.wartungsvertrag_dokument_workflow (
        wartungsvertrag_zuordnung_id,
        status,
        fehlermeldung
    )
    VALUES (
        v_assignment.id,
        'dokument_ausstehend',
        'Zuordnung gespeichert, Wartungsvertragsdokument noch nicht erzeugt: Es fehlt eine freigegebene Wartungsvertragsvorlage.'
    )
    ON CONFLICT (wartungsvertrag_zuordnung_id) DO NOTHING;

    RETURN QUERY
    SELECT * FROM public.wartungsvertrag_dokument_workflow
    WHERE wartungsvertrag_zuordnung_id = v_assignment.id;
END;
$function$;

CREATE OR REPLACE FUNCTION public.review_arbeitsstunde(p_arbeitsstunde_id integer, p_aktion text, p_begruendung text, p_datum date DEFAULT NULL::date, p_stunden numeric DEFAULT NULL::numeric, p_art_der_arbeit text DEFAULT NULL::text)
 RETURNS jsonb
 LANGUAGE plpgsql
 SECURITY DEFINER
 SET search_path TO 'public'
AS $function$
declare
  v_entry public.arbeitsstunde%rowtype;
  v_before jsonb;
  v_after jsonb;
  v_reviewer_id integer;
  v_action text := lower(btrim(coalesce(p_aktion, '')));
  v_comment text := btrim(coalesce(p_begruendung, ''));
  v_audit_action text;
begin
  if p_arbeitsstunde_id is null or p_arbeitsstunde_id <= 0 then
    raise exception 'Eine gültige Arbeitsstunden-ID ist erforderlich.' using errcode = '22023';
  end if;
  if v_action not in ('freigeben', 'ablehnen', 'korrigieren', 'loeschen') then
    raise exception 'Die Prüfaktion ist ungültig.' using errcode = '22023';
  end if;
  if v_comment = '' then
    raise exception 'Für jede Prüfaktion ist eine Begründung erforderlich.' using errcode = '22023';
  end if;
  if not public.has_effective_permission(256) then
    raise exception 'Für die Prüfung fehlt ManageWorkHours.' using errcode = '42501';
  end if;

  v_reviewer_id := public.current_mitglied_id();
  if v_reviewer_id is null then
    raise exception 'Der Prüfer ist keinem Mitglied zugeordnet.' using errcode = '42501';
  end if;

  select * into v_entry
  from public.arbeitsstunde
  where id = p_arbeitsstunde_id
  for update;

  if not found then
    raise exception 'Die Arbeitsstunde existiert nicht mehr.' using errcode = 'P0002';
  end if;
  if v_entry.status <> 'offen' or v_entry.freigegeben <> false then
    raise exception 'Die Arbeitsstunde wurde bereits geprüft oder geändert.' using errcode = '40001';
  end if;
  if v_action = 'korrigieren'
     and (p_datum is null or p_stunden is null or p_stunden <= 0 or nullif(btrim(p_art_der_arbeit), '') is null) then
    raise exception 'Für die Korrektur sind Datum, Stunden größer als 0 und Art der Arbeit erforderlich.' using errcode = '22023';
  end if;

  v_before := to_jsonb(v_entry);
  perform set_config('app.g7_work_hour_review', 'true', true);

  if v_action = 'loeschen' then
    insert into public.arbeitsstunde_pruefverlauf (
      arbeitsstunde_id, aktion, begruendung, geprueft_von, geprueft_am, vorher_snapshot, nachher_snapshot
    ) values (
      v_entry.id, 'geloescht', v_comment, v_reviewer_id, now(), v_before, null
    );
    delete from public.arbeitsstunde where id = v_entry.id;
    return jsonb_build_object('arbeitsstunde_id', p_arbeitsstunde_id, 'aktion', 'geloescht');
  end if;

  if v_action = 'freigeben' then
    update public.arbeitsstunde
    set status = 'genehmigt', freigegeben = true, genehmigt_von = v_reviewer_id, genehmigt_am = now(), lockedbyuserid = null, lockat = null
    where id = v_entry.id
    returning to_jsonb(arbeitsstunde) into v_after;
    v_audit_action := 'freigegeben';
  elsif v_action = 'ablehnen' then
    update public.arbeitsstunde
    set status = 'abgelehnt', freigegeben = false, genehmigt_von = null, genehmigt_am = null, lockedbyuserid = null, lockat = null
    where id = v_entry.id
    returning to_jsonb(arbeitsstunde) into v_after;
    v_audit_action := 'abgelehnt';
  else
    update public.arbeitsstunde
    set datum = p_datum, stunden = p_stunden, art_der_arbeit = btrim(p_art_der_arbeit),
        status = 'genehmigt', freigegeben = true, genehmigt_von = v_reviewer_id, genehmigt_am = now(), lockedbyuserid = null, lockat = null
    where id = v_entry.id
    returning to_jsonb(arbeitsstunde) into v_after;
    v_audit_action := 'korrigiert';
  end if;

  insert into public.arbeitsstunde_pruefverlauf (
    arbeitsstunde_id, aktion, begruendung, geprueft_von, geprueft_am, vorher_snapshot, nachher_snapshot
  ) values (
    v_entry.id, v_audit_action, v_comment, v_reviewer_id, now(), v_before, v_after
  );

  return jsonb_build_object('arbeitsstunde_id', p_arbeitsstunde_id, 'aktion', v_audit_action, 'nachher_snapshot', v_after);
end;
$function$;

create or replace function public.mark_meter_defective(p_zaehler_id bigint)
returns boolean language plpgsql security definer set search_path=public,pg_temp as $$
declare v_zaehler public.zaehler;
begin
  select z.* into v_zaehler from public.zaehler z where z.id=p_zaehler_id;
  if not found then raise exception 'Zähler % wurde nicht gefunden.',p_zaehler_id; end if;
  if v_zaehler.status<>'aktiv'::public.zaehler_status then raise exception 'Zähler % ist nicht aktiv.',p_zaehler_id; end if;
  if v_zaehler.ausgebaut_am is not null then raise exception 'Zähler % ist bereits ausgebaut.',p_zaehler_id; end if;
  if not (public.is_productive_admin_or_vorstand() or (not public.is_admin_or_vorstand() and exists(
    select 1 from public.parzellen_belegung pb where pb.parzelle_id=v_zaehler.parzelle_id
      and pb.mitglied_id=public.current_mitglied_id()
      and (pb.von_datum is null or pb.von_datum<=current_date)
      and (pb.bis_datum is null or pb.bis_datum>=current_date)))) then
    raise exception 'Keine Berechtigung für Zähler %.',p_zaehler_id;
  end if;
  update public.zaehler set defekt=true where id=p_zaehler_id;
  return true;
end; $$;

CREATE OR REPLACE FUNCTION public.get_historical_contract_lease_relationships(p_mitglied_id bigint)
 RETURNS SETOF jsonb
 LANGUAGE plpgsql
 STABLE SECURITY DEFINER
 SET search_path TO 'public', 'pg_temp'
AS $function$
begin
  if not public.can_access_historical_contract_member(p_mitglied_id) then
    raise exception 'Die Pachtbeziehungen liegen außerhalb des historischen Vertragsbereichs.' using errcode = '42501';
  end if;
  return query select jsonb_build_object('pachtverhaeltnisId',r.id,'parzelleId',r.parzelle_id,'gartenNr',p.garten_nr,'anlage',p."Anlage",'rolle',b.rolle,
      'gueltigAb',b.gueltig_ab,'gueltigBis',b.gueltig_bis,'urspruenglicherPachtbeginn',r.urspruenglicher_pachtbeginn,'beendetAm',r.beendet_am)
    from public.pachtverhaeltnis_beteiligung as b join public.pachtverhaeltnis as r on r.id = b.pachtverhaeltnis_id
      join public.parzelle as p on p.id = r.parzelle_id
    where b.mitglied_id = p_mitglied_id
    order by b.gueltig_ab desc, r.id;
end;
$function$;

CREATE OR REPLACE FUNCTION public.get_historical_contract_members()
 RETURNS SETOF jsonb
 LANGUAGE plpgsql
 STABLE SECURITY DEFINER
 SET search_path TO 'public', 'pg_temp'
AS $function$
begin
  if not coalesce(public.can_register_historical_contracts(), false) then
    raise exception 'Altvertragsberechtigung fehlt.' using errcode = '42501';
  end if;
  return query select jsonb_build_object('id',m.id,'mitgliedsnummer',m.mitgliedsnummer,'vorname',m.vorname,'name',m.name,'mitglied_seit',m.mitglied_seit,'aktiv',m.aktiv)
    from public.mitglied as m
    order by m.name, m.vorname, m.id;
end;
$function$;

CREATE OR REPLACE FUNCTION public.get_historische_pachtvertragsvorschau(p_pachtverhaeltnis_ids bigint[], p_vertragsdatum date)
 RETURNS TABLE(pachtverhaeltnis_id bigint, pachtvertragsfassung_id bigint, wirksam_ab date, gueltig_bis date, historische_vertragsnummer integer, vertragspartei_mitglied_ids bigint[], ist_vorgaenger boolean, ist_nachfolger boolean, vorgeschlagenes_vorgaengerende date)
 LANGUAGE plpgsql
 STABLE SECURITY DEFINER
 SET search_path TO 'public', 'pg_temp'
AS $function$
begin
  if not public.can_register_historical_contracts() then
    raise exception 'Für die Nacherfassung historischer Verträge fehlt die Berechtigung.' using errcode = '42501';
  end if;
  if exists (select 1 from public.pachtverhaeltnis as r
      where r.id = any(p_pachtverhaeltnis_ids) and
        (not exists (select 1 from public.pachtverhaeltnis_beteiligung as b where b.pachtverhaeltnis_id = r.id)
         or exists (select 1 from public.pachtverhaeltnis_beteiligung as b where b.pachtverhaeltnis_id = r.id
           and not public.can_access_historical_contract_member(b.mitglied_id))))
     or exists (select 1 from unnest(p_pachtverhaeltnis_ids) as ids(id)
       where not exists (select 1 from public.pachtverhaeltnis as r where r.id = ids.id)) then
    raise exception 'Die Vorschau liegt außerhalb des historischen Vertragsbereichs.' using errcode = '42501';
  end if;
  return query
  with versions as (
    select f.*, d.historische_vertragsnummer
    from public.pachtvertragsfassung f join public.dokument d on d.id = f.dokument_id
    where f.pachtverhaeltnis_id = any(p_pachtverhaeltnis_ids)
  )
  select v.pachtverhaeltnis_id, v.id, v.wirksam_ab, v.gueltig_bis, v.historische_vertragsnummer,
    coalesce(array(select b.mitglied_id from public.pachtverhaeltnis_beteiligung b where b.pachtverhaeltnis_id = v.pachtverhaeltnis_id and b.gueltig_ab <= v.wirksam_ab and (b.gueltig_bis is null or b.gueltig_bis >= v.wirksam_ab) order by b.mitglied_id), '{}'::bigint[]),
    v.wirksam_ab = (select max(x.wirksam_ab) from public.pachtvertragsfassung x where x.pachtverhaeltnis_id = v.pachtverhaeltnis_id and x.wirksam_ab < p_vertragsdatum),
    v.wirksam_ab = (select min(x.wirksam_ab) from public.pachtvertragsfassung x where x.pachtverhaeltnis_id = v.pachtverhaeltnis_id and x.wirksam_ab > p_vertragsdatum),
    case when v.wirksam_ab = (select max(x.wirksam_ab) from public.pachtvertragsfassung x where x.pachtverhaeltnis_id = v.pachtverhaeltnis_id and x.wirksam_ab < p_vertragsdatum) then p_vertragsdatum - 1 end
  from versions v order by v.pachtverhaeltnis_id, v.wirksam_ab, v.id;
end;
$function$;

CREATE OR REPLACE FUNCTION public.register_historischen_pachtvertrag_g11b1(p_dokument_id bigint, p_dokumenttyp_code text, p_vertragsdatum date, p_unterzeichnet boolean, p_pachtverhaeltnis_ids bigint[], p_vorherige_enddaten jsonb DEFAULT '{}'::jsonb, p_mitglied_seit_korrektur_mitglied_id bigint DEFAULT NULL::bigint, p_mitglied_seit_korrektur_datum date DEFAULT NULL::date, p_vertragspartei_mitglied_ids bigint[] DEFAULT NULL::bigint[], p_mitgliedschaftsenden jsonb DEFAULT '[]'::jsonb, p_aktiv_status_aenderungen jsonb DEFAULT '[]'::jsonb)
 RETURNS TABLE(dokument_id bigint, dokumenttyp_id bigint, pachtverhaeltnis_id bigint, pachtvertragsfassung_id bigint, historische_vertragsnummer integer)
 LANGUAGE plpgsql
 SECURITY DEFINER
 SET search_path TO 'public', 'pg_temp'
AS $function$
#variable_conflict error
declare
  v_document public.dokument%rowtype;
  v_relation public.pachtverhaeltnis%rowtype;
  v_current public.pachtverhaeltnis_beteiligung%rowtype;
  v_member public.mitglied%rowtype;
  v_party_id bigint;
  v_relation_id bigint;
  v_command jsonb;
  v_command_member_id bigint;
  v_end_date date;
  v_active boolean;
  v_predecessor_start date;
  v_successor_start date;
  v_next_party_start date;
  v_party_end date;
  v_removed_ids bigint[] := '{}'::bigint[];
  v_context_ids bigint[];
  v_today date := public.kgv_local_now()::date;
  v_has_effective_lease boolean;
begin
  if not coalesce(public.can_register_historical_contracts(), false) then
    raise exception 'Für die Nacherfassung historischer Verträge fehlt die Berechtigung.' using errcode = '42501';
  end if;
  if p_vertragsdatum is null or not isfinite(p_vertragsdatum) or p_unterzeichnet is null
     or p_dokumenttyp_code is null or p_dokumenttyp_code not in ('historischer_ma_pv_erstvertrag', 'historischer_pv_folgevertrag') then
    raise exception 'Vertragsart, endliches Vertragsdatum und Signaturstatus sind erforderlich.' using errcode = '22023';
  end if;
  if coalesce(cardinality(p_pachtverhaeltnis_ids), 0) = 0
     or exists (select 1 from unnest(p_pachtverhaeltnis_ids) as ids(id) where ids.id is null or ids.id <= 0)
     or exists (select 1 from unnest(p_pachtverhaeltnis_ids) as ids(id) group by ids.id having count(*) > 1)
     or coalesce(cardinality(p_vertragspartei_mitglied_ids), 0) = 0
     or exists (select 1 from unnest(p_vertragspartei_mitglied_ids) as ids(id) where ids.id is null or ids.id <= 0)
     or exists (select 1 from unnest(p_vertragspartei_mitglied_ids) as ids(id) group by ids.id having count(*) > 1) then
    raise exception 'Eindeutige Pachtbeziehungen und vorhandene Vertragsparteien sind erforderlich.' using errcode = '22023';
  end if;
  if jsonb_typeof(p_vorherige_enddaten) is distinct from 'object'
     or jsonb_typeof(p_mitgliedschaftsenden) is distinct from 'array'
     or jsonb_typeof(p_aktiv_status_aenderungen) is distinct from 'array' then
    raise exception 'Ungültiges Format der bestätigten Änderungen.' using errcode = '22023';
  end if;
  if exists (select 1 from jsonb_object_keys(p_vorherige_enddaten) as keys(id)
    where not exists (select 1 from unnest(p_pachtverhaeltnis_ids) as ids(id) where ids.id::text = keys.id)) then
    raise exception 'Vorgängerenddaten dürfen nur ausgewählte Pachtbeziehungen betreffen.' using errcode = '22023';
  end if;

  select d.* into v_document from public.dokument as d where d.id = p_dokument_id for update;
  if not found or v_document.archiviert_at is not null or v_document.mitglied_id is null then
    raise exception 'Das Vertragsdokument benötigt ein vorhandenes Mitglied.' using errcode = 'P0002';
  end if;
  if not (v_document.mitglied_id = any(p_vertragspartei_mitglied_ids)) then
    raise exception 'Das Dokumentmitglied muss Vertragspartei sein.' using errcode = '23514';
  end if;
  if exists (select 1 from unnest(p_vertragspartei_mitglied_ids) as ids(id)
    where not exists (select 1 from public.mitglied as m where m.id = ids.id)) then
    raise exception 'Eine Vertragspartei fehlt oder liegt außerhalb des Vertragsbereichs.' using errcode = '42501';
  end if;

  -- Lock selected relations in canonical order. Establish the predecessor context
  -- from the unchanged timeline, never from client-supplied member commands.
  for v_relation_id in select ids.id from unnest(p_pachtverhaeltnis_ids) as ids(id) order by ids.id loop
    select r.* into v_relation from public.pachtverhaeltnis as r where r.id = v_relation_id for update;
    if not found then
      raise exception 'Unbekannte Pachtbeziehung.' using errcode = 'P0002';
    end if;
    if (v_document.parzelle_id is not null and v_document.parzelle_id <> v_relation.parzelle_id)
       or not exists (select 1 from public.pachtverhaeltnis_beteiligung as b
         where b.pachtverhaeltnis_id = v_relation_id and b.mitglied_id = v_document.mitglied_id) then
      raise exception 'Die Pachtbeziehung gehört nicht zum Vertragskontext.' using errcode = '42501';
    end if;
    if v_relation.beendet_am < p_vertragsdatum then
      raise exception 'Die Pachtbeziehung war am Vertragsdatum bereits beendet.' using errcode = '23514';
    end if;
    if exists (select 1 from public.pachtvertragsfassung as f
      where f.pachtverhaeltnis_id = v_relation_id and f.wirksam_ab = p_vertragsdatum and f.dokument_id <> p_dokument_id) then
      raise exception 'Für dieses Datum besteht bereits ein anderer Vertragsstand.' using errcode = '23514';
    end if;
    select max(f.wirksam_ab) into v_predecessor_start from public.pachtvertragsfassung as f
      where f.pachtverhaeltnis_id = v_relation_id and f.wirksam_ab < p_vertragsdatum;
    select array(select distinct b.mitglied_id from public.pachtverhaeltnis_beteiligung as b
      where b.pachtverhaeltnis_id = v_relation_id and b.gueltig_ab <= v_predecessor_start
        and (b.gueltig_bis is null or b.gueltig_bis >= v_predecessor_start)
        and not (b.mitglied_id = any(p_vertragspartei_mitglied_ids))) || v_removed_ids into v_removed_ids;
  end loop;
  select array(select distinct ids.id from unnest(p_vertragspartei_mitglied_ids || v_removed_ids) as ids(id)) into v_context_ids;
  -- Also serialize membership consequences for persons present on other parcels.
  perform m.id from public.mitglied as m where m.id = any(v_context_ids) order by m.id for update;

  if p_mitglied_seit_korrektur_mitglied_id is not null or p_mitglied_seit_korrektur_datum is not null then
    if p_dokumenttyp_code <> 'historischer_ma_pv_erstvertrag'
       or p_mitglied_seit_korrektur_mitglied_id is null or p_mitglied_seit_korrektur_datum is null
       or not isfinite(p_mitglied_seit_korrektur_datum)
       or p_mitglied_seit_korrektur_datum > p_vertragsdatum
       or not (p_mitglied_seit_korrektur_mitglied_id = any(p_vertragspartei_mitglied_ids)) then
      raise exception 'Mitglied-seit-Korrekturen sind nur für eine Aufnahmeperson dieses MA+PV-Erstvertrags zulässig.' using errcode = '23514';
    end if;
  end if;
  if exists (select 1 from jsonb_array_elements(p_mitgliedschaftsenden) as cmd(value)
      group by cmd.value ->> 'mitglied_id' having count(*) > 1)
     or exists (select 1 from jsonb_array_elements(p_aktiv_status_aenderungen) as cmd(value)
      group by cmd.value ->> 'mitglied_id' having count(*) > 1) then
    raise exception 'Mitgliedsänderungen dürfen keine doppelten Befehle enthalten.' using errcode = '22023';
  end if;
  for v_command in select cmd.value from jsonb_array_elements(p_mitgliedschaftsenden) as cmd(value) loop
    v_command_member_id := (v_command ->> 'mitglied_id')::bigint;
    v_end_date := (v_command ->> 'mitglied_ende')::date;
    if p_dokumenttyp_code <> 'historischer_pv_folgevertrag'
       or v_command_member_id is null or v_end_date is null or not isfinite(v_end_date)
       or not (v_command_member_id = any(v_removed_ids))
       or v_command_member_id = any(p_vertragspartei_mitglied_ids) then
      raise exception 'Ein Mitgliedschaftsende ist nur für eine herausfallende Vorgängerpartei dieses Folgevertrags zulässig.' using errcode = '23514';
    end if;
  end loop;
  for v_command in select cmd.value from jsonb_array_elements(p_aktiv_status_aenderungen) as cmd(value) loop
    v_command_member_id := (v_command ->> 'mitglied_id')::bigint;
    if v_command_member_id is null or not (v_command_member_id = any(v_context_ids))
       or jsonb_typeof(v_command -> 'aktiv') is distinct from 'boolean' then
      raise exception 'Ein Aktiv-/Passiv-Befehl benötigt eine Person dieses Vertragsvorgangs und einen bestätigten Status.' using errcode = '23514';
    end if;
  end loop;

  foreach v_relation_id in array p_pachtverhaeltnis_ids loop
    select r.* into v_relation from public.pachtverhaeltnis as r where r.id = v_relation_id;
    select min(f.wirksam_ab) into v_successor_start from public.pachtvertragsfassung as f
      where f.pachtverhaeltnis_id = v_relation_id and f.wirksam_ab > p_vertragsdatum;
    -- A removed party loses only the selected relation, not another parcel or
    -- the state at an already existing successor. Split a spanning period.
    for v_current in select b.* from public.pachtverhaeltnis_beteiligung as b
      where b.pachtverhaeltnis_id = v_relation_id and b.gueltig_ab <= p_vertragsdatum
        and (b.gueltig_bis is null or b.gueltig_bis >= p_vertragsdatum)
        and not (b.mitglied_id = any(p_vertragspartei_mitglied_ids)) for update loop
      if v_current.gueltig_ab = p_vertragsdatum then
        raise exception 'Eine abweichende Beteiligung beginnt bereits am selben Vertragsdatum.' using errcode = '23514';
      end if;
      update public.pachtverhaeltnis_beteiligung as b set gueltig_bis = p_vertragsdatum - 1 where b.id = v_current.id;
      if v_successor_start is not null and (v_current.gueltig_bis is null or v_current.gueltig_bis >= v_successor_start) then
        insert into public.pachtverhaeltnis_beteiligung(pachtverhaeltnis_id, mitglied_id, rolle, gueltig_ab, gueltig_bis, created_by)
        values (v_relation_id, v_current.mitglied_id, v_current.rolle, v_successor_start, v_current.gueltig_bis, v_current.created_by);
      end if;
    end loop;
    foreach v_party_id in array p_vertragspartei_mitglied_ids loop
      if not exists (select 1 from public.pachtverhaeltnis_beteiligung as b
        where b.pachtverhaeltnis_id = v_relation_id and b.mitglied_id = v_party_id
          and b.gueltig_ab <= p_vertragsdatum and (b.gueltig_bis is null or b.gueltig_bis >= p_vertragsdatum)) then
        select min(b.gueltig_ab) into v_next_party_start from public.pachtverhaeltnis_beteiligung as b
          where b.pachtverhaeltnis_id = v_relation_id and b.mitglied_id = v_party_id and b.gueltig_ab > p_vertragsdatum;
        v_party_end := least(v_successor_start - 1, v_next_party_start - 1, v_relation.beendet_am);
        insert into public.pachtverhaeltnis_beteiligung(pachtverhaeltnis_id, mitglied_id, rolle, gueltig_ab, gueltig_bis, created_by)
        values (v_relation_id, v_party_id, case when v_party_id = v_document.mitglied_id then 'hauptpaechter' else 'mitpaechter' end,
          p_vertragsdatum, v_party_end, auth.uid());
      end if;
    end loop;
  end loop;

  -- The shared foundation still owns classification, version links, numbering,
  -- the audited membership-since correction and the return contract.
  return query select f.* from public.register_historischen_pachtvertrag_g6a(
    p_dokument_id, p_dokumenttyp_code, p_vertragsdatum, p_unterzeichnet, p_pachtverhaeltnis_ids,
    p_vorherige_enddaten, p_mitglied_seit_korrektur_mitglied_id, p_mitglied_seit_korrektur_datum) as f;

  for v_command in select cmd.value from jsonb_array_elements(p_mitgliedschaftsenden) as cmd(value) loop
    v_command_member_id := (v_command ->> 'mitglied_id')::bigint;
    v_end_date := (v_command ->> 'mitglied_ende')::date;
    select m.* into v_member from public.mitglied as m where m.id = v_command_member_id;
    if v_end_date < v_member.mitglied_seit or (v_member.mitglied_ende is not null and v_member.mitglied_ende <> v_end_date) then
      raise exception 'Das bestätigte Mitgliedschaftsende widerspricht dem vorhandenen Mitgliedschaftszeitraum.' using errcode = '23514';
    end if;
    if exists (select 1 from public.pachtverhaeltnis_beteiligung as b
      join public.pachtverhaeltnis as r on r.id = b.pachtverhaeltnis_id
      where b.mitglied_id = v_command_member_id
        and least(coalesce(b.gueltig_bis, 'infinity'::date), coalesce(r.beendet_am, 'infinity'::date)) > v_end_date
        and b.gueltig_ab <= coalesce(r.beendet_am, 'infinity'::date)) then
      raise exception 'Mitgliedschaftsende ist wegen weiterer wirksamer Pachtbeteiligung nicht zulässig.' using errcode = '23514';
    end if;
    update public.mitglied as m set mitglied_ende = v_end_date where m.id = v_command_member_id;
  end loop;
  for v_command in select cmd.value from jsonb_array_elements(p_aktiv_status_aenderungen) as cmd(value) loop
    v_command_member_id := (v_command ->> 'mitglied_id')::bigint;
    v_active := (v_command ->> 'aktiv')::boolean;
    select exists (select 1 from public.pachtverhaeltnis_beteiligung as b
      join public.pachtverhaeltnis as r on r.id = b.pachtverhaeltnis_id
      where b.mitglied_id = v_command_member_id and b.gueltig_ab <= v_today
        and (b.gueltig_bis is null or b.gueltig_bis >= v_today)
        and (r.beendet_am is null or r.beendet_am >= v_today)) into v_has_effective_lease;
    if v_active is distinct from v_has_effective_lease
       or (v_active and exists (select 1 from public.mitglied as m
         where m.id = v_command_member_id and m.mitglied_ende <= v_today)) then
      raise exception 'Der bestätigte Aktiv-/Passiv-Status widerspricht dem aktuellen Pachtbestand oder Mitgliedschaftsende.' using errcode = '23514';
    end if;
    update public.mitglied as m set aktiv = v_active where m.id = v_command_member_id;
  end loop;
end;
$function$;

CREATE OR REPLACE FUNCTION public.register_historischen_pachtvertrag_g6a(p_dokument_id bigint, p_dokumenttyp_code text, p_vertragsdatum date, p_unterzeichnet boolean, p_pachtverhaeltnis_ids bigint[], p_vorherige_enddaten jsonb DEFAULT '{}'::jsonb, p_mitglied_seit_korrektur_mitglied_id bigint DEFAULT NULL::bigint, p_mitglied_seit_korrektur_datum date DEFAULT NULL::date)
 RETURNS TABLE(dokument_id bigint, dokumenttyp_id bigint, pachtverhaeltnis_id bigint, pachtvertragsfassung_id bigint, historische_vertragsnummer integer)
 LANGUAGE plpgsql
 SECURITY DEFINER
 SET search_path TO 'public', 'pg_temp'
AS $function$
#variable_conflict error
declare
  v_dokument public.dokument%rowtype;
  v_dokumenttyp_id bigint;
  v_hat_ma_nachweis boolean;
  v_hat_pv_nachweis boolean;
  v_relation_id bigint;
  v_fassung_id bigint;
  v_vorgaenger_id bigint;
  v_nachfolger_id bigint;
  v_vorgaenger_ende date;
  v_nachfolger_beginn date;
  v_nummer integer;
  v_old_mitglied_seit date;
begin
  if not public.can_register_historical_contracts() then
    raise exception 'Für die Nacherfassung historischer Verträge fehlt die Berechtigung.' using errcode = '42501';
  end if;
  if p_vertragsdatum is null or p_unterzeichnet is null then
    raise exception 'Vertragsdatum und Signaturstatus sind erforderlich.' using errcode = '22023';
  end if;
  if coalesce(array_length(p_pachtverhaeltnis_ids, 1), 0) = 0
     or exists (select 1 from unnest(p_pachtverhaeltnis_ids) id group by id having count(*) > 1) then
    raise exception 'Mindestens eine eindeutige Pachtbeziehung ist erforderlich.' using errcode = '22023';
  end if;

  select d.* into v_dokument from public.dokument as d where d.id = p_dokument_id for update;
  if not found or v_dokument.archiviert_at is not null then
    raise exception 'Das Dokument existiert nicht oder ist bereits archiviert.' using errcode = 'P0002';
  end if;
  if v_dokument.source_kommunikationsvorgang_id is not null or v_dokument.source_wartungsvertrag_zuordnung_id is not null then
    raise exception 'Prepared-Dokumente dürfen nicht als historische Verträge registriert werden.' using errcode = '23514';
  end if;
  if v_dokument.mitglied_id is null then
    raise exception 'Ein historischer Vertrag benötigt ein zugeordnetes bestehendes Mitglied.' using errcode = '22023';
  end if;

  select dt.id,
      exists(select 1 from public.dokumenttyp_wirkung w where w.dokumenttyp_id = dt.id and w.wirkung = 'mitgliedsantrag_nachweis'),
      exists(select 1 from public.dokumenttyp_wirkung w where w.dokumenttyp_id = dt.id and w.wirkung = 'pachtvertrag_nachweis')
  into v_dokumenttyp_id, v_hat_ma_nachweis, v_hat_pv_nachweis
  from public.dokumenttyp dt where dt.code = p_dokumenttyp_code and dt.aktiv;
  if not found or p_dokumenttyp_code not in ('historischer_ma_pv_erstvertrag', 'historischer_pv_folgevertrag')
     or not v_hat_pv_nachweis then
    raise exception 'Der Dokumenttyp ist kein zulässiger historischer Pachtvertrag.' using errcode = '22023';
  end if;

  if v_dokument.dokumenttyp_id is not null
     and (v_dokument.dokumenttyp_id is distinct from v_dokumenttyp_id
       or v_dokument.dokumentdatum is distinct from p_vertragsdatum
       or v_dokument.unterzeichnet is distinct from p_unterzeichnet) then
    raise exception 'Das Dokument ist bereits abweichend klassifiziert.' using errcode = '23514';
  end if;

  foreach v_relation_id in array p_pachtverhaeltnis_ids loop
    if not exists (select 1 from public.pachtverhaeltnis as r where r.id = v_relation_id) then
      raise exception 'Unbekanntes Pachtverhältnis: %', v_relation_id using errcode = 'P0002';
    end if;
    if not exists (
      select 1 from public.pachtverhaeltnis_beteiligung b
      where b.pachtverhaeltnis_id = v_relation_id and b.mitglied_id = v_dokument.mitglied_id
        and b.gueltig_ab <= p_vertragsdatum and (b.gueltig_bis is null or b.gueltig_bis >= p_vertragsdatum)
    ) then
      raise exception 'Das Dokumentmitglied war am Vertragsdatum nicht an Pachtverhältnis % beteiligt.', v_relation_id using errcode = '23514';
    end if;
  end loop;

  update public.dokument as d set dokumenttyp_id = v_dokumenttyp_id, dokumentdatum = p_vertragsdatum,
    unterzeichnet = p_unterzeichnet, updated_at = now() where d.id = p_dokument_id;

  foreach v_relation_id in array p_pachtverhaeltnis_ids loop
    select f.id into v_fassung_id from public.pachtvertragsfassung as f
      where f.dokument_id = p_dokument_id and f.pachtverhaeltnis_id = v_relation_id for update;

    if found then
      if not exists (select 1 from public.pachtvertragsfassung f where f.id = v_fassung_id and f.fassungstyp = 'import' and f.wirksam_ab = p_vertragsdatum) then
        raise exception 'Das Dokument ist für Pachtverhältnis % bereits abweichend registriert.', v_relation_id using errcode = '23514';
      end if;
    else
      select f.id into v_vorgaenger_id from public.pachtvertragsfassung as f
        where f.pachtverhaeltnis_id = v_relation_id and f.wirksam_ab < p_vertragsdatum
        order by f.wirksam_ab desc, f.id desc limit 1 for update;
      select f.id, f.wirksam_ab into v_nachfolger_id, v_nachfolger_beginn from public.pachtvertragsfassung as f
        where f.pachtverhaeltnis_id = v_relation_id and f.wirksam_ab > p_vertragsdatum
        order by f.wirksam_ab, f.id limit 1 for update;

      insert into public.pachtvertragsfassung as f(pachtverhaeltnis_id, dokument_id, fassungstyp, vertragsdatum, wirksam_ab, gueltig_bis, vorgaenger_fassung_id)
      values (v_relation_id, p_dokument_id, 'import', p_vertragsdatum, p_vertragsdatum,
        case when v_nachfolger_beginn is null then null else v_nachfolger_beginn - 1 end, v_vorgaenger_id)
      returning f.id into v_fassung_id;

      if v_vorgaenger_id is not null then
        v_vorgaenger_ende := coalesce(nullif(p_vorherige_enddaten ->> v_relation_id::text, '')::date, p_vertragsdatum - 1);
        if v_vorgaenger_ende >= p_vertragsdatum then
          raise exception 'Das Vorgängerende muss vor dem neuen Vertragsbeginn liegen.' using errcode = '23514';
        end if;
        update public.pachtvertragsfassung as f set gueltig_bis = v_vorgaenger_ende where f.id = v_vorgaenger_id;
      end if;
      if v_nachfolger_id is not null then
        update public.pachtvertragsfassung as f set vorgaenger_fassung_id = v_fassung_id where f.id = v_nachfolger_id;
      end if;
    end if;

  end loop;

  -- Uses the established 80xxx reservation source and only broadens its permission gate.
  v_nummer := public.reserve_fachnummer('historischer_kombivertrag', 'historisches_dokument', p_dokument_id);
  update public.dokument as d set historische_vertragsnummer = v_nummer,
    titel = regexp_replace(coalesce(nullif(btrim(d.titel), ''), 'Historischer Vertrag'), '\s*·\s*Vertrags-Nr\.\s*\d{5}\s*$', '', 'i') || ' · Vertrags-Nr. ' || v_nummer,
    updated_at = now() where d.id = p_dokument_id;

  if p_mitglied_seit_korrektur_mitglied_id is not null or p_mitglied_seit_korrektur_datum is not null then
    if not v_hat_ma_nachweis or p_mitglied_seit_korrektur_mitglied_id is null or p_mitglied_seit_korrektur_datum is null then
      raise exception 'Mitglied-seit-Korrekturen sind nur bewusst beim MA+PV-Erstvertrag zulässig.' using errcode = '22023';
    end if;
    if not exists (select 1 from public.pachtverhaeltnis_beteiligung as b
      where b.pachtverhaeltnis_id = any(p_pachtverhaeltnis_ids)
        and b.mitglied_id = p_mitglied_seit_korrektur_mitglied_id
        and b.gueltig_ab <= p_vertragsdatum and (b.gueltig_bis is null or b.gueltig_bis >= p_vertragsdatum)) then
      raise exception 'Die Mitglied-seit-Korrektur gehört nicht zu den Vertragsparteien.' using errcode = '23514';
    end if;
    select m.mitglied_seit into v_old_mitglied_seit from public.mitglied as m where m.id = p_mitglied_seit_korrektur_mitglied_id for update;
    if not found then raise exception 'Mitglied für die Korrektur nicht gefunden.' using errcode = 'P0002'; end if;
    update public.mitglied as m set mitglied_seit = p_mitglied_seit_korrektur_datum where m.id = p_mitglied_seit_korrektur_mitglied_id;
    if not exists (select 1 from public.mitglied_historische_korrektur as k
      where k.mitglied_id = p_mitglied_seit_korrektur_mitglied_id
        and k.dokument_id = p_dokument_id and k.feld = 'mitglied_seit') then
      insert into public.mitglied_historische_korrektur(mitglied_id, dokument_id, feld, bisheriger_wert, korrigierter_wert, korrigiert_von)
      values (p_mitglied_seit_korrektur_mitglied_id, p_dokument_id, 'mitglied_seit', v_old_mitglied_seit, p_mitglied_seit_korrektur_datum, auth.uid());
    end if;
  end if;

  return query select p_dokument_id, v_dokumenttyp_id, f.pachtverhaeltnis_id, f.id, v_nummer
    from public.pachtvertragsfassung f where f.dokument_id = p_dokument_id order by f.pachtverhaeltnis_id;
end;
$function$;

CREATE OR REPLACE FUNCTION public.reserve_fachnummer(p_art text, p_quelle_art text, p_quelle_id bigint)
 RETURNS integer
 LANGUAGE plpgsql
 SECURITY DEFINER
 SET search_path TO 'public', 'pg_temp'
AS $function$
declare
  v_nummer integer;
  v_bestehende_art text;
  v_sequenz regclass;
begin
  if p_art = 'historischer_kombivertrag' and
     (p_quelle_art is distinct from 'historisches_dokument'
      or not public.is_registered_historical_contract_document(p_quelle_id)
      or (auth.role() is distinct from 'service_role'
          and not public.can_access_historical_contract_document(p_quelle_id, true))) then
    raise exception 'Historische Nummern benötigen einen berechtigten registrierten Vertragskontext.'
      using errcode = '42501';
  end if;

  if not public.is_admin_or_vorstand() and auth.role() <> 'service_role'
     and not (p_art = 'historischer_kombivertrag' and public.can_register_historical_contracts()) then
    raise exception 'Keine Berechtigung für Nummernvergabe' using errcode = '42501';
  end if;

  perform pg_advisory_xact_lock(
    hashtextextended(concat_ws(':', 'project', p_quelle_art, p_quelle_id::text), 0)
  );

  select nummer, art
    into v_nummer, v_bestehende_art
    from public.fachnummer_reservierung
   where quelle_art = p_quelle_art
     and quelle_id = p_quelle_id;

  if found then
    if v_bestehende_art <> p_art then
      raise exception 'Die Quelle besitzt bereits eine Reservierung der Art % und kann nicht als % verwendet werden.',
        v_bestehende_art, p_art using errcode = '22023';
    end if;
    return v_nummer;
  end if;

  v_sequenz := case p_art
    when 'mitgliedsantrag' then 'public.fachnummer_mitgliedsantrag_seq'::regclass
    when 'pachtvertrag' then 'public.fachnummer_pachtvertrag_seq'::regclass
    when 'historischer_kombivertrag' then 'public.fachnummer_historisch_seq'::regclass
    when 'wartungsvertrag' then 'public.fachnummer_wartungsvertrag_seq'::regclass
  end;
  if v_sequenz is null then
    raise exception 'Unbekannte Nummernart' using errcode = '22023';
  end if;

  v_nummer := nextval(v_sequenz);
  insert into public.fachnummer_reservierung(nummer, art, quelle_art, quelle_id)
  values (v_nummer, p_art, p_quelle_art, p_quelle_id);
  return v_nummer;
end;
$function$;

CREATE OR REPLACE FUNCTION public.start_wartungsvertrag_grundlegende_aenderung(p_vorgaenger_zuordnung_id bigint, p_titel text, p_beschreibung text, p_bereich text, p_max_aktive_zuordnungen integer, p_befreit_von_pflichtstunden boolean, p_arbeitsstunden_gutschrift numeric, p_gueltig_ab date, p_bemerkung text DEFAULT NULL::text)
 RETURNS TABLE(kommunikationsvorgang_id bigint, wartungsvertrag_id bigint, status text)
 LANGUAGE plpgsql
 SECURITY DEFINER
 SET search_path TO 'public', 'pg_temp'
AS $function$
declare
  v_old public.wartungsvertrag_zuordnungen%rowtype;
  v_old_contract public.wartungsvertraege%rowtype;
  v_change public.wartungsvertrag_grundlegende_aenderung%rowtype;
  v_new_contract public.wartungsvertraege%rowtype;
  v_communication public.kommunikationsvorgang%rowtype;
begin
  if auth.uid() is null or not public.is_admin_or_vorstand() then
    raise exception 'Nur Admin oder Vorstand darf eine grundlegende Wartungsvertragsänderung starten.'
      using errcode='42501';
  end if;

  select * into v_old
  from public.wartungsvertrag_zuordnungen
  where id=p_vorgaenger_zuordnung_id
  for update;
  if not found or (v_old.gueltig_bis is not null and v_old.gueltig_bis<current_date) then
    raise exception 'Die Ausgangszuordnung ist nicht mehr aktuell.' using errcode='23514';
  end if;
  if v_old.vertragspartei_mitglied_id is null then
    raise exception 'Die tatsächliche Vertragspartei des Altvertrags ist ungeklärt; es wird nicht geraten.'
      using errcode='23514';
  end if;
  if exists (
    select 1 from public.wartungsvertrag_zuordnungen successor
    where successor.vorgaenger_zuordnung_id=v_old.id
  ) then
    raise exception 'Für die Ausgangszuordnung besteht bereits eine Folgezuordnung.'
      using errcode='23514';
  end if;

  select * into v_change
  from public.wartungsvertrag_grundlegende_aenderung
  where vorgaenger_zuordnung_id=v_old.id
    and status='vorbereitet'
  for update;
  if found then
    select * into v_communication
    from public.kommunikationsvorgang
    where id=v_change.kommunikationsvorgang_id;
    if not found then
      raise exception 'Der vorgemerkte Signaturvorgang fehlt.' using errcode='23503';
    end if;
    return query select v_communication.id,v_change.wartungsvertrag_id,v_communication.status;
    return;
  end if;

  if nullif(btrim(p_titel),'') is null
     or p_max_aktive_zuordnungen<1
     or p_arbeitsstunden_gutschrift<0
     or (p_befreit_von_pflichtstunden and p_arbeitsstunden_gutschrift>0)
     or p_gueltig_ab is null
     or p_gueltig_ab<current_date
     or p_gueltig_ab<=v_old.gueltig_ab then
    raise exception 'Die Vertragsdaten oder der neue Gültigkeitsbeginn sind ungültig.'
      using errcode='22023';
  end if;

  select * into v_old_contract
  from public.wartungsvertraege
  where id=v_old.wartungsvertrag_id
  for key share;
  if not found then
    raise exception 'Die Vertragsdefinition des Altvertrags fehlt.' using errcode='23503';
  end if;

  insert into public.wartungsvertraege(
    titel,beschreibung,bereich,max_aktive_zuordnungen,
    befreit_von_pflichtstunden,arbeitsstunden_gutschrift,aktiv,bemerkung
  ) values (
    btrim(p_titel),nullif(btrim(p_beschreibung),''),nullif(btrim(p_bereich),''),
    p_max_aktive_zuordnungen,p_befreit_von_pflichtstunden,
    p_arbeitsstunden_gutschrift,false,nullif(btrim(p_bemerkung),'')
  ) returning * into v_new_contract;

  insert into public.wartungsvertrag_grundlegende_aenderung(
    vorgaenger_zuordnung_id,wartungsvertrag_id
  ) values (v_old.id,v_new_contract.id)
  returning * into v_change;

  insert into public.kommunikationsvorgang(
    vorgangstyp,dokumenttyp,status,mitglied_id,parzelle_id,
    wartungsvertrag_id,entwurf_daten,revision,zugewiesen_an
  ) values (
    'vertrag_signatur','wartungsvertrag','vorbereitet',
    v_old.vertragspartei_mitglied_id,null,v_new_contract.id,
    jsonb_build_object(
      'schema_version','kgv-prepared-contract/v1',
      'document_type','wartungsvertrag',
      'memberId',v_old.vertragspartei_mitglied_id,
      'wartungsvertrag_id',v_new_contract.id,
      'wartungsvertrag_zuordnung_id',null,
      'vorgaenger_zuordnung_id',v_old.id,
      'gueltig_ab',p_gueltig_ab::text,
      'gueltig_bis',null,
      'wartungsvertrag_titel',v_new_contract.titel,
      'wartungsvertrag_beschreibung',v_new_contract.beschreibung,
      'wartungsvertrag_bereich',v_new_contract.bereich,
      'befreit_von_pflichtstunden',v_new_contract.befreit_von_pflichtstunden,
      'arbeitsstunden_gutschrift',v_new_contract.arbeitsstunden_gutschrift,
      'bemerkung',v_new_contract.bemerkung
    ),1,auth.uid()
  ) returning * into v_communication;

  update public.wartungsvertrag_grundlegende_aenderung
  set kommunikationsvorgang_id=v_communication.id
  where id=v_change.id;
  return query select v_communication.id,v_new_contract.id,v_communication.status;
end;
$function$;

CREATE OR REPLACE FUNCTION public.rpc_export_arbeitsstunden_uebersicht(p_jahr integer DEFAULT NULL::integer, p_stunden_offen boolean DEFAULT false, p_stunden_fertig boolean DEFAULT false, p_wartungsvertraege boolean DEFAULT false, p_ansicht text DEFAULT 'zusammenfassung'::text)
 RETURNS TABLE(mitglied_id bigint, mitgliedsnummer bigint, garten_nr text, nachname text, vorname text, jahr integer, pflichtstunden_soll numeric, geleistete_stunden numeric, wartungsvertrag_gutschrift_stunden numeric, offene_stunden numeric, hat_wartungsvertrag boolean, wartungsvertraege text, status text, regelgrund text, zeilentyp text, leistendes_mitglied text, stunden_dieses_mitglieds numeric, datum date, taetigkeit text, sortierung integer)
 LANGUAGE plpgsql
 STABLE
 SET search_path TO 'public', 'pg_temp'
AS $function$
begin
  if exists (
    select 1 from public.v_pflichtstunden_uebersicht v
    join public.mitglied m on m.id = v.hauptmitglied_id
    where m.aktiv
      and (p_jahr is null or v.saison_jahr = p_jahr)
      and (m.mitgliedsnummer is null or m.mitgliedsnummer <= 0)
  ) then
    raise exception 'MEMBER_NUMBER_MISSING'
      using errcode = '23514', message = 'Für den Arbeitsstundenexport fehlt eine gültige Mitgliedsnummer.';
  end if;

  return query
  with basis as (
    select distinct on (v.hauptmitglied_id, v.saison_id)
      v.hauptmitglied_id as mitglied_id, m.mitgliedsnummer, m.name as nachname, m.vorname,
      v.saison_jahr as jahr, v.saison_id,
      coalesce(v.pflichtstunden_soll, 0) as pflichtstunden_soll,
      coalesce(v.geleistete_stunden, 0) as geleistete_stunden,
      coalesce(v.wartungsvertrag_gutschrift_stunden, 0) as wartungsvertrag_gutschrift_stunden,
      coalesce(v.offene_stunden, 0) as offene_stunden,
      coalesce(v.hat_wartungsvertrag, false) as hat_wartungsvertrag,
      coalesce(v.regelgrund, '') as regelgrund
    from public.v_pflichtstunden_uebersicht v
    join public.mitglied m on m.id = v.hauptmitglied_id
    where m.aktiv
      and (p_jahr is null or v.saison_jahr = p_jahr)
    order by v.hauptmitglied_id, v.saison_id desc
  ), gaerten as (
    select b.mitglied_id, b.jahr,
      coalesce(string_agg(distinct p.garten_nr, ', ' order by p.garten_nr), '') as garten_nr
    from basis b
    left join public.parzellen_belegung pb on pb.mitglied_id = b.mitglied_id
      and pb.von_datum <= make_date(b.jahr, 12, 31)
      and (pb.bis_datum is null or pb.bis_datum >= make_date(b.jahr, 1, 1))
    left join public.parzelle p on p.id = pb.parzelle_id
    group by b.mitglied_id, b.jahr
  ), summary as (
    select b.*, g.garten_nr, coalesce(string_agg(distinct w.titel, ', ' order by w.titel), '') as wartungsvertraege
    from basis b
    left join gaerten g on g.mitglied_id = b.mitglied_id and g.jahr = b.jahr
    left join public.wartungsvertrag_zuordnungen z on z.hauptmitglied_id = b.mitglied_id
      and z.gueltig_ab <= make_date(b.jahr, 12, 31)
      and (z.gueltig_bis is null or z.gueltig_bis >= make_date(b.jahr, 1, 1))
    left join public.wartungsvertraege w on w.id = z.wartungsvertrag_id
    group by b.mitglied_id, b.mitgliedsnummer, b.nachname, b.vorname, b.jahr, b.saison_id,
      b.pflichtstunden_soll, b.geleistete_stunden, b.wartungsvertrag_gutschrift_stunden,
      b.offene_stunden, b.hat_wartungsvertrag, b.regelgrund, g.garten_nr
  ), filtered as (
    select s.* from summary s
    where not (p_stunden_offen or p_stunden_fertig or p_wartungsvertraege)
       or (p_stunden_offen and s.offene_stunden > 0)
       or (p_stunden_fertig and s.pflichtstunden_soll > 0 and s.offene_stunden <= 0)
       or (p_wartungsvertraege and s.hat_wartungsvertrag)
       or s.regelgrund = 'altersbefreiung'
  ), detail as (
    select f.mitglied_id, a.mitglied_id as leistender_id, a.datum::date, a.art_der_arbeit, a.stunden,
      trim(concat_ws(' ', lm.vorname, lm.name)) as leistendes_mitglied
    from filtered f
    join public.arbeitsstunde a on a.saison_id = f.saison_id
    join public.mitglied lm on lm.id = a.mitglied_id
    where a.freigegeben and coalesce(lm.hauptmitglied_id, lm.id) = f.mitglied_id
  ), rows as (
    select f.*, 'Zusammenfassung'::text as zeilentyp, ''::text as leistendes_mitglied,
      null::numeric as stunden_dieses_mitglieds, null::date as datum, ''::text as taetigkeit, 0 as sortierung
    from filtered f
    union all
    select f.*, 'Davon Mitglied'::text, d.leistendes_mitglied, sum(d.stunden), null::date, ''::text, 1
    from filtered f join detail d on d.mitglied_id = f.mitglied_id
    where lower(coalesce(p_ansicht, 'zusammenfassung')) in ('pro_mitglied', 'einzeln')
    group by f.mitglied_id, f.mitgliedsnummer, f.garten_nr, f.nachname, f.vorname, f.jahr, f.saison_id,
      f.pflichtstunden_soll, f.geleistete_stunden, f.wartungsvertrag_gutschrift_stunden,
      f.offene_stunden, f.hat_wartungsvertrag, f.wartungsvertraege, f.regelgrund, d.leistendes_mitglied
    union all
    select f.*, 'Einzelbuchung'::text, d.leistendes_mitglied, d.stunden, d.datum, coalesce(d.art_der_arbeit, ''), 2
    from filtered f join detail d on d.mitglied_id = f.mitglied_id
    where lower(coalesce(p_ansicht, 'zusammenfassung')) = 'einzeln'
  )
  select r.mitglied_id, r.mitgliedsnummer, r.garten_nr, r.nachname, r.vorname, r.jahr, r.pflichtstunden_soll, r.geleistete_stunden,
    r.wartungsvertrag_gutschrift_stunden, r.offene_stunden, r.hat_wartungsvertrag, r.wartungsvertraege,
    case when r.regelgrund = 'wartungsvertrag' then 'Wartungsvertrag – befreit'
         when r.pflichtstunden_soll > 0 and r.offene_stunden > 0 then 'Stunden offen'
         when r.pflichtstunden_soll > 0 then 'Stunden fertig' else 'Befreit' end,
    r.regelgrund, r.zeilentyp, r.leistendes_mitglied, r.stunden_dieses_mitglieds, r.datum, r.taetigkeit, r.sortierung
  from rows r
  order by case when nullif(r.garten_nr, '') is null then 1 else 0 end,
    substring(r.garten_nr from '^[[:space:]]*([0-9]+)')::integer nulls last,
    r.garten_nr, r.nachname, r.vorname, r.sortierung, r.leistendes_mitglied, r.datum;
end;
$function$;

CREATE OR REPLACE FUNCTION public.rpc_export_datenschutz_aktive_mitglieder()
 RETURNS TABLE(garten_nr text, mitgliedsnummer bigint, name text, email text, email_info boolean, email_rechnung boolean, whatsapp boolean)
 LANGUAGE plpgsql
 STABLE
 SET search_path TO 'public', 'pg_temp'
AS $function$
begin
  if exists (
    select 1
    from public.mitglied m
    where m.aktiv = true
      and (m.mitglied_ende is null or m.mitglied_ende >= current_date)

      and (m.mitgliedsnummer is null or m.mitgliedsnummer <= 0)
  ) then
    raise exception 'MEMBER_NUMBER_MISSING'
      using errcode = '23514', message = 'Für den Datenschutzexport fehlt eine gültige Mitgliedsnummer.';
  end if;

  return query
  with aktive_gaerten as (
    select pb.mitglied_id, string_agg(distinct p.garten_nr, ', ' order by p.garten_nr) as garten_nr
    from public.parzellen_belegung pb
    join public.parzelle p on p.id = pb.parzelle_id
    where (pb.von_datum is null or pb.von_datum <= current_date)
      and (pb.bis_datum is null or pb.bis_datum >= current_date)
      and nullif(btrim(p.garten_nr), '') is not null
    group by pb.mitglied_id
  ), basis as (
    select m.id, m.hauptmitglied_id, m.mitgliedsnummer, coalesce(m.hauptmitglied_id, m.id) as gruppen_id,
      coalesce(eigener_garten.garten_nr, '') as garten_nr,
      coalesce(gruppen_garten.garten_nr, '') as gruppen_garten_nr,
      concat_ws(' ', m.vorname, m.name) as name, coalesce(m.email, '') as email,
      coalesce(m.email_info_einwilligung, false) as email_info,
      coalesce(m.email_rechnung_einwilligung, false) as email_rechnung,
      coalesce(m.whatsapp_einwilligung, false) as whatsapp
    from public.mitglied m
    left join aktive_gaerten eigener_garten on eigener_garten.mitglied_id = m.id
    left join aktive_gaerten gruppen_garten on gruppen_garten.mitglied_id = coalesce(m.hauptmitglied_id, m.id)
    where m.aktiv = true
      and (m.mitglied_ende is null or m.mitglied_ende >= current_date)

  )
  select b.garten_nr, b.mitgliedsnummer, b.name, b.email, b.email_info, b.email_rechnung, b.whatsapp
  from basis b
  order by case when nullif(b.gruppen_garten_nr, '') is null then 1 else 0 end,
    substring(b.gruppen_garten_nr from '^[[:space:]]*([0-9]+)')::integer nulls last,
    b.gruppen_garten_nr, b.gruppen_id,
    case when b.hauptmitglied_id is null then 0 else 1 end,
    b.name, b.email;
end;
$function$;

CREATE OR REPLACE FUNCTION public.rpc_export_mitgliederliste(p_aktiv_filter text, p_sortierung text)
 RETURNS SETOF jsonb
 LANGUAGE plpgsql
 STABLE
 SET search_path TO 'public', 'pg_temp'
AS $function$
begin
  if exists (
    select 1
    from public.mitglied m

    where (m.mitgliedsnummer is null or m.mitgliedsnummer <= 0)
  ) then
    raise exception 'MEMBER_NUMBER_MISSING'
      using errcode = '23514',
            message = 'Für den Mitgliederexport fehlt eine gültige Mitgliedsnummer.';
  end if;

  return query
  with settings as (
    select case lower(coalesce(nullif(btrim(p_aktiv_filter), ''), 'aktiv')) when 'alle' then 'alle' when 'inaktiv' then 'inaktiv' else 'aktiv' end as aktiv_filter,
           case lower(coalesce(nullif(btrim(p_sortierung), ''), 'name')) when 'mitgliedsnr' then 'mitgliedsnr' when 'garten' then 'garten' else 'name' end as sortierung
  ), base as (
    select m.*, coalesce(m.hauptmitglied_id, m.id) principal_member_id,
           m.aktiv and (m.mitglied_ende is null or m.mitglied_ende >= current_date) ist_aktiv,
           exists(select 1 from public.app_user au where au.mitglied_id = m.id) or m.auth_user_id is not null app_vorhanden
    from public.mitglied m
  ), grouped as (
    select b.*, hm.name hm_name, hm.vorname hm_vorname, hm.id hm_id,
           coalesce(g.gaerten,'') gaerten, coalesce(g.garten_sort_nr,2147483647) garten_sort_nr, coalesce(g.garten_sort_text,'') garten_sort_text
    from base b join base hm on hm.id=b.principal_member_id
    left join lateral (
      select string_agg(p.garten_nr, ', ' order by nullif(substring(p.garten_nr from '^[[:space:]]*([0-9]+)'), '')::integer nulls last, p.garten_nr) gaerten,
             min(nullif(substring(p.garten_nr from '^[[:space:]]*([0-9]+)'), '')::integer) garten_sort_nr, min(p.garten_nr) garten_sort_text
      from public.parzellen_belegung pb join public.parzelle p on p.id=pb.parzelle_id
      where pb.mitglied_id=hm.id and pb.von_datum<=current_date and (pb.bis_datum is null or pb.bis_datum>=current_date)
    ) g on true
  )
  select to_jsonb(g) || jsonb_build_object(
    'mitgliedsnr', g.mitgliedsnummer::text, 'mitgliedsnummer', g.mitgliedsnummer::text,
    'mitglied', concat_ws(', ', nullif(g.name,''), nullif(g.vorname,'')),
    'strasse_hsnr', coalesce(g.adresse,''), 'gartennummern', g.gaerten, 'pachtgaerten', g.gaerten,
    'adresse_block', concat_ws(E'\n',nullif(g.adresse,''),nullif(concat_ws(' ',nullif(g.plz,''),nullif(g.ort,'')),'')),
    'mobil', coalesce(g.handy,''), 'kontakt_block', concat_ws(E'\n',nullif(g.telefon,''),nullif(g.handy,'')),
    'wa', g.whatsapp_einwilligung, 're', g.email_rechnung_einwilligung, 'info', g.email_info_einwilligung,
    'app', g.app_vorhanden, 'app_vorhanden', g.app_vorhanden, 'aktiv_export', g.ist_aktiv
  )
  from grouped g cross join settings s
  where s.aktiv_filter='alle' or (s.aktiv_filter='aktiv' and g.ist_aktiv) or (s.aktiv_filter='inaktiv' and not g.ist_aktiv)
  order by
    case when s.sortierung='name' then lower(coalesce(g.hm_name,'')) end,
    case when s.sortierung='name' then lower(coalesce(g.hm_vorname,'')) end,
    case when s.sortierung='name' then g.hm_id end,
    case when s.sortierung='mitgliedsnr' then g.mitgliedsnummer end,
    case when s.sortierung='garten' then g.garten_sort_nr end,
    case when s.sortierung='garten' then g.garten_sort_text end,
    case when s.sortierung='garten' then lower(coalesce(g.hm_name,'')) end,
    g.principal_member_id,
    case when g.hauptmitglied_id is null then 0 else 1 end,
    lower(coalesce(g.name,'')), lower(coalesce(g.vorname,'')), g.mitgliedsnummer;
end;
$function$;

CREATE OR REPLACE FUNCTION public.rpc_export_parzellenliste(p_parameters jsonb DEFAULT '{}'::jsonb)
 RETURNS SETOF jsonb
 LANGUAGE sql
 STABLE
 SET search_path TO 'public', 'pg_temp'
AS $function$
  with settings as (
    select case lower(coalesce(nullif(btrim(p_parameters ->> 'belegung'), ''), 'alle'))
      when 'alle' then 'alle' when 'belegt' then 'belegt' when 'frei' then 'frei' else 'alle' end as belegung
  ), current_tenants as (
    select b.parzelle_id,
      string_agg(concat_ws(' ', m.vorname, m.name), ', ' order by m.name, m.vorname) as paechter
    from public.parzellen_belegung b
    join public.mitglied m on m.id = b.mitglied_id
    where b.von_datum <= current_date
      and (b.bis_datum is null or b.bis_datum >= current_date)
    group by b.parzelle_id
  )
  select to_jsonb(r)
  from (
    select p.garten_nr, p."Anlage" as anlage, coalesce(t.paechter, '') as paechter,
      p.flaeche_qm, (t.parzelle_id is not null) as belegt
    from public.parzelle p
    left join current_tenants t on t.parzelle_id = p.id
    cross join settings s
    where p.aktiv = true
      and (s.belegung = 'alle' or (s.belegung = 'belegt' and t.parzelle_id is not null) or (s.belegung = 'frei' and t.parzelle_id is null))
    order by
      case when nullif(btrim(p.garten_nr), '') is null then 1 else 0 end,
      nullif(substring(p.garten_nr from '^[[:space:]]*([0-9]+)'), '')::integer nulls last,
      p.garten_nr, p."Anlage", p.id
  ) r;
$function$;

-- The historical DemoDB still exposes two legacy export overloads.  They are
-- not used by the current generic export gateway, but remain a public API for
-- older clients.  Keep their result contracts and remove only the obsolete
-- row-level Demo predicates before the physical columns are dropped.
CREATE OR REPLACE FUNCTION public.rpc_export_mitgliederliste(
  p_sortierung text DEFAULT 'name'::text,
  p_aktiv_filter boolean DEFAULT NULL::boolean
) RETURNS TABLE(
  sortierung text, mitgliedsnr bigint, name text, vorname text,
  strasse_hsnr text, plz text, ort text, telefon text, mobil text, email text,
  geburtsdatum date, bemerkung text, whatsapp text, rechnung_per_mail text,
  info_per_mail text, auth_user text, aktiv text, mitglied_seit date,
  pachtgaerten text, sort_name text, sort_mitgliedsnr bigint,
  sort_gartennummer integer
) LANGUAGE sql STABLE SET search_path TO 'public', 'pg_temp' AS $function$
  with settings as (
    select case lower(coalesce(p_sortierung,'name'))
      when 'mitgliedsnr' then 'mitgliedsnr'
      when 'gartennummer' then 'garten'
      else 'name' end as modern_sortierung,
      case when p_aktiv_filter is null then 'alle'
        when p_aktiv_filter then 'aktiv' else 'inaktiv' end as modern_filter
  ), rows as (
    select e.value
    from settings s
    cross join lateral public.rpc_export_mitgliederliste(s.modern_filter,s.modern_sortierung) e(value)
  )
  select
    case lower(coalesce(p_sortierung,'name')) when 'mitgliedsnr' then 'mitgliedsnr'
      when 'gartennummer' then 'gartennummer' else 'name' end,
    (value->>'mitgliedsnummer')::bigint,
    value->>'name', value->>'vorname', value->>'adresse', value->>'plz',
    value->>'ort', value->>'telefon', value->>'handy', value->>'email',
    (value->>'geburtsdatum')::date, value->>'bemerkung',
    case when coalesce((value->>'whatsapp_einwilligung')::boolean,false) then 'Ja' else 'Nein' end,
    case when coalesce((value->>'email_rechnung_einwilligung')::boolean,false) then 'Ja' else 'Nein' end,
    case when coalesce((value->>'email_info_einwilligung')::boolean,false) then 'Ja' else 'Nein' end,
    case when coalesce((value->>'app_vorhanden')::boolean,false) then 'Ja' else 'Nein' end,
    case when coalesce((value->>'ist_aktiv')::boolean,false) then 'Ja' else 'Nein' end,
    (value->>'mitglied_seit')::date, coalesce(value->>'gaerten',''),
    lower(coalesce(value->>'name',''))||'|'||lower(coalesce(value->>'vorname','')),
    (value->>'mitgliedsnummer')::bigint,
    (value->>'garten_sort_nr')::integer
  from rows;
$function$;

CREATE OR REPLACE FUNCTION public.rpc_export_rfid_status(
  p_anlage_filter text DEFAULT NULL::text,
  p_status_filter text DEFAULT 'alle'::text
) RETURNS TABLE(
  anlage text, garten_nr text, mitgliedsnr bigint, name text, vorname text,
  wasser_rfid text, strom_rfid text, wasser_eingerichtet text,
  strom_eingerichtet text, beide_eingerichtet text, fehlt_irgendwas text,
  sort_anlage text, sort_gartennummer integer
) LANGUAGE sql STABLE SET search_path TO 'public', 'pg_temp' AS $function$
  with basis as (
    select p."Anlage" as anlage,p.garten_nr,m.id as mitgliedsnr,m.name,m.vorname,
      p.rfid_wasser as wasser_rfid,p.rfid_strom as strom_rfid,
      case when nullif(trim(coalesce(p.rfid_wasser,'')),'') is not null then 'Ja' else 'Nein' end wasser_eingerichtet,
      case when nullif(trim(coalesce(p.rfid_strom,'')),'') is not null then 'Ja' else 'Nein' end strom_eingerichtet,
      case when nullif(trim(coalesce(p.rfid_wasser,'')),'') is not null and nullif(trim(coalesce(p.rfid_strom,'')),'') is not null then 'Ja' else 'Nein' end beide_eingerichtet,
      case when nullif(trim(coalesce(p.rfid_wasser,'')),'') is null or nullif(trim(coalesce(p.rfid_strom,'')),'') is null then 'Ja' else 'Nein' end fehlt_irgendwas,
      nullif(regexp_replace(coalesce(p.garten_nr,''),'\D','','g'),'')::integer sort_gartennummer
    from public.parzelle p
    left join lateral (
      select pb.mitglied_id from public.parzellen_belegung pb
      where pb.parzelle_id=p.id and pb.von_datum<=current_date
        and (pb.bis_datum is null or pb.bis_datum>=current_date)
      order by pb.von_datum desc,pb.id desc limit 1
    ) belegung on true
    left join public.mitglied m on m.id=belegung.mitglied_id
    where coalesce(p.aktiv,true)
      and (p_anlage_filter is null or lower(trim(coalesce(p."Anlage",'')))=lower(trim(p_anlage_filter)))
  )
  select b.anlage,b.garten_nr,b.mitgliedsnr,b.name,b.vorname,b.wasser_rfid,b.strom_rfid,
    b.wasser_eingerichtet,b.strom_eingerichtet,b.beide_eingerichtet,b.fehlt_irgendwas,
    lower(coalesce(b.anlage,'')),b.sort_gartennummer
  from basis b
  where lower(coalesce(p_status_filter,'alle'))='alle'
    or (lower(p_status_filter)='fehlt_irgendwas' and b.fehlt_irgendwas='Ja')
    or (lower(p_status_filter)='beide_vorhanden' and b.beide_eingerichtet='Ja')
    or (lower(p_status_filter)='wasser_fehlt' and b.wasser_eingerichtet='Nein')
    or (lower(p_status_filter)='strom_fehlt' and b.strom_eingerichtet='Nein')
  order by lower(coalesce(b.anlage,'')),coalesce(b.sort_gartennummer,2147483647),b.garten_nr;
$function$;
-- current_app_user_context has a changed OUT contract.  Its three direct
-- Storage-policy dependencies are recreated explicitly without a column alias
-- list tied to the old return type.
drop policy vereinsunterlagen_cleanup_unregistered_admin_vorstand on storage.objects;
drop policy vereinsunterlagen_insert_admin_vorstand on storage.objects;
drop policy vereinsunterlagen_read_admin_vorstand on storage.objects;
drop function public.current_app_user_context();
create function public.current_app_user_context()
returns table(user_id uuid,mitglied_id bigint,role text,permission_grants bigint,permission_revocations bigint,is_protected boolean)
language sql stable security definer set search_path=public,pg_temp as $$
  select au.user_id,au.mitglied_id,au.role,coalesce(au.permission_grants,0),
    coalesce(au.permission_revocations,0),coalesce(au.is_protected,false)
  from public.app_user au where au.user_id=auth.uid();
$$;
revoke all on function public.current_app_user_context() from public,anon;
grant execute on function public.current_app_user_context() to authenticated,service_role;

create policy vereinsunterlagen_cleanup_unregistered_admin_vorstand on storage.objects
for delete to authenticated using(bucket_id='vereinsregelwerke' and (storage.foldername(name))[1]='vereinsunterlagen'
  and not exists(select 1 from public.vereinsdokument_fassung f where f.storage_path=objects.name)
  and exists(select 1 from public.current_app_user_context() c where c.role in ('admin','vorstand')));
create policy vereinsunterlagen_insert_admin_vorstand on storage.objects
for insert to authenticated with check(bucket_id='vereinsregelwerke' and (storage.foldername(name))[1]='vereinsunterlagen'
  and exists(select 1 from public.current_app_user_context() c where c.role in ('admin','vorstand')));
create policy vereinsunterlagen_read_admin_vorstand on storage.objects
for select to authenticated using(bucket_id='vereinsregelwerke' and (storage.foldername(name))[1]='vereinsunterlagen'
  and exists(select 1 from public.current_app_user_context() c where c.role in ('admin','vorstand')));

-- Remove D2 compatibility triggers and checks before dropping their columns.
drop trigger p11_d2_project_neutral_scope on public.app_user;
drop trigger p11_d2_project_neutral_scope on public.auth_allowlist;
drop trigger p11_d2_project_neutral_scope on public.arbeitseinsatz;
drop trigger p11_d2_project_neutral_scope on public.bekanntmachung;
drop trigger p11_d2_project_neutral_scope on public.historical_contract_operation;
drop trigger p11_d2_project_neutral_scope on public.mitglied;
drop trigger p11_d2_project_neutral_scope on public.parzelle;
drop trigger p11_d2_project_neutral_scope on public.saison;
drop trigger p11_d2_project_neutral_scope on public.termin;
drop trigger p11_d2_project_neutral_scope on public.wartungsvertraege;

alter table public.app_user drop constraint app_user_d2_project_neutral_demo_check;
alter table public.auth_allowlist drop constraint auth_allowlist_d2_project_neutral_demo_check;
alter table public.arbeitseinsatz drop constraint arbeitseinsatz_d2_project_neutral_demo_check;
alter table public.bekanntmachung drop constraint bekanntmachung_d2_project_neutral_demo_check;
alter table public.historical_contract_operation drop constraint historical_contract_operation_d2_project_neutral_demo_check;
alter table public.mitglied drop constraint mitglied_d2_project_neutral_demo_check;
alter table public.parzelle drop constraint parzelle_d2_project_neutral_demo_check;
alter table public.saison drop constraint saison_d2_project_neutral_demo_check;
alter table public.termin drop constraint termin_d2_project_neutral_demo_check;
alter table public.wartungsvertraege drop constraint wartungsvertraege_d2_project_neutral_demo_check;

drop trigger if exists trg_termin_bekanntmachung_demo_scope on public.termin;
drop trigger if exists trg_bekanntmachung_demo_scope on public.bekanntmachung;
drop function public.enforce_appointment_announcement_demo_scope();

alter table public.app_user drop column is_demo_account;
alter table public.auth_allowlist drop column allow_demo_access;
alter table public.arbeitseinsatz drop column is_demo;
alter table public.bekanntmachung drop column is_demo;
alter table public.fachnummer_reservierung drop column is_demo;
alter table public.historical_contract_operation drop column is_demo;
alter table public.mitglied drop column is_demo;
alter table public.parzelle drop column is_demo;
alter table public.saison drop column is_demo;
alter table public.termin drop column is_demo;
alter table public.wartungsvertraege drop column is_demo;

-- These columns exist in the historical DemoDB baseline but not in the
-- repository's synthetic/full-rebuild baseline.  IF EXISTS keeps D5 valid for
-- both schema histories; dropping a column never deletes the owning row.
alter table if exists public.arbeitseinsatz_anmeldung drop column if exists is_demo;
alter table if exists public.arbeitsstunde drop column if exists is_demo;
alter table if exists public.client_diagnostics_log drop column if exists is_demo;
alter table if exists public.dokument drop column if exists is_demo;
alter table if exists public.impressum_funktion_slot drop column if exists is_demo;
alter table if exists public.mitglied_gesetzlicher_vertreter drop column if exists is_demo;
alter table if exists public.mitglied_saison drop column if exists is_demo;
alter table if exists public.parzellen_belegung drop column if exists is_demo;
alter table if exists public.vereinskonfiguration drop column if exists is_demo;
alter table if exists public.wartungsvertrag_zuordnungen drop column if exists is_demo;
alter table if exists public.zaehler drop column if exists is_demo;
alter table if exists public.zaehler_ablesung drop column if exists is_demo;

drop function public.p11_d2_force_allow_demo_access_false();
drop function public.p11_d2_force_app_user_demo_false();
drop function public.p11_d2_force_is_demo_false();
drop function public.can_access_demo_scope();
drop function public.is_demo_app_user_row(bigint,boolean);
drop function public.is_demo_dokument_scope(bigint,bigint);
drop function public.is_demo_impressum_slot_scope(bigint);
drop function public.is_demo_member_arbeitseinsatz_scope(bigint,bigint);
drop function public.is_demo_member_parzelle_scope(bigint,bigint);
drop function public.is_demo_member_wartungsvertrag_scope(bigint,bigint);
drop function public.is_demo_mitglied_id(bigint);
drop function public.is_demo_or_reviewer();
drop function public.is_demo_parzelle_id(bigint);
drop function public.is_demo_user();
drop function public.is_demo_zaehler_id(bigint);
drop function public.is_restricted_demo_admin_or_vorstand();
drop function public.is_playstore_reviewer();
drop function public.assert_fachnummer_reservierung_project_conflict_free();

drop sequence public.fachnummer_mitgliedsantrag_demo_seq;
drop sequence public.fachnummer_pachtvertrag_demo_seq;
drop sequence public.fachnummer_historisch_demo_seq;
drop sequence public.fachnummer_wartungsvertrag_demo_seq;

-- Fail the migration if an active public-schema object still embeds a
-- row-level Demo contract.  Historical migration text is intentionally not
-- part of the live PostgreSQL catalog.
do $$
declare v_leftovers text;
begin
  select string_agg(kind||' '||object_name,E'\n' order by kind,object_name) into v_leftovers from (
    select 'column' kind,table_schema||'.'||table_name||'.'||column_name object_name
      from information_schema.columns where table_schema='public' and column_name ilike '%demo%'
    union all
    select 'function',p.oid::regprocedure::text from pg_proc p join pg_namespace n on n.oid=p.pronamespace
      where n.nspname='public' and p.prokind='f' and (p.proname ilike '%demo%' or pg_get_functiondef(p.oid) ilike '%demo%')
    union all
    select 'policy',schemaname||'.'||tablename||'.'||policyname from pg_policies where schemaname in ('public','storage')
      and (policyname ilike '%demo%' or coalesce(qual,'') ilike '%demo%' or coalesce(with_check,'') ilike '%demo%')
    union all
    select 'view',schemaname||'.'||viewname from pg_views where schemaname='public' and definition ilike '%demo%'
    union all
    select 'constraint',n.nspname||'.'||c.relname||'.'||con.conname from pg_constraint con join pg_class c on c.oid=con.conrelid join pg_namespace n on n.oid=c.relnamespace
      where n.nspname='public' and (con.conname ilike '%demo%' or pg_get_constraintdef(con.oid) ilike '%demo%')
    union all
    select 'index',schemaname||'.'||indexname from pg_indexes where schemaname='public' and (indexname ilike '%demo%' or indexdef ilike '%demo%')
  ) x;
  if v_leftovers is not null then raise exception 'D5 left active Demo schema objects:%',E'\n'||v_leftovers using errcode='2BP01'; end if;
end; $$;

commit;
