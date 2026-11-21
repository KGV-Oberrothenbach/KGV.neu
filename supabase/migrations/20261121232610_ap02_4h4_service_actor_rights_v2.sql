-- AP-02.4H4: Autoritative zielgebundene Rechte fuer service-only RPCs.
CREATE OR REPLACE FUNCTION public.ap02_user_has_effective_right(p_user_id uuid,p_code text)
RETURNS boolean
LANGUAGE sql STABLE SECURITY DEFINER
SET search_path TO pg_catalog,public
AS $fn$
 SELECT coalesce((SELECT p.erlaubt FROM public.ap02_calculate_user_rights(p_user_id) p
   WHERE p.code=p_code),false)
$fn$;
REVOKE ALL ON FUNCTION public.ap02_user_has_effective_right(uuid,text) FROM PUBLIC,anon,authenticated;
GRANT EXECUTE ON FUNCTION public.ap02_user_has_effective_right(uuid,text) TO service_role;

-- begin_auth_user_deletion
CREATE OR REPLACE FUNCTION public.begin_auth_user_deletion(p_user_id uuid, p_mitglied_id bigint, p_operation_id uuid)
 RETURNS boolean
 LANGUAGE plpgsql
 SECURITY DEFINER
 SET search_path TO 'public', 'pg_temp'
AS $function$
DECLARE
  v_link uuid;
  v_target public.app_user%rowtype;
  v_effective bigint;
BEGIN
  IF auth.uid() IS NULL OR public.ap02_can_manage_rights() IS NOT TRUE THEN
    RAISE EXCEPTION 'CanManageRoles required' USING ERRCODE = '42501';
  END IF;
  IF p_user_id IS NULL OR p_operation_id IS NULL OR p_mitglied_id IS NULL OR p_mitglied_id <= 0
     OR p_user_id = auth.uid() THEN
    RAISE EXCEPTION 'Invalid or self deletion target' USING ERRCODE = '42501';
  END IF;

  SELECT m.auth_user_id INTO v_link
  FROM public.mitglied m WHERE m.id = p_mitglied_id FOR UPDATE;
  IF NOT FOUND OR v_link IS DISTINCT FROM p_user_id THEN
    RAISE EXCEPTION 'Member/Auth relationship changed' USING ERRCODE = '42501';
  END IF;

  -- Serialize any concurrent linking operation, including a different member.
  PERFORM pg_advisory_xact_lock(hashtextextended(p_user_id::text, 0));
  IF EXISTS (
    SELECT 1 FROM public.mitglied m
    WHERE m.auth_user_id = p_user_id AND m.id <> p_mitglied_id
  ) THEN
    RAISE EXCEPTION 'Auth user linked to another member' USING ERRCODE = '42501';
  END IF;

  SELECT * INTO v_target
  FROM public.app_user WHERE user_id = p_user_id FOR UPDATE;
  IF NOT FOUND OR v_target.mitglied_id IS DISTINCT FROM p_mitglied_id THEN
    RAISE EXCEPTION 'App user/member relationship changed' USING ERRCODE = '42501';
  END IF;
  IF v_target.is_protected OR EXISTS (SELECT 1 FROM public.protected_admin_identity p WHERE p.user_id = p_user_id) OR EXISTS (SELECT 1 FROM public.app_user_zusatzrolle z WHERE z.user_id = p_user_id AND z.zusatzrolle_code = 'admin') THEN
    RAISE EXCEPTION 'Protected user cannot be deleted' USING ERRCODE = '42501';
  END IF;
  IF public.ap02_user_has_effective_right(p_user_id,'berechtigungen.verwalten') THEN
    RAISE EXCEPTION 'Role managers cannot be deleted with this endpoint'
      USING ERRCODE = '42501';
  END IF;

  INSERT INTO public.auth_user_delete_lock
    (user_id, mitglied_id, operation_id, requested_by, role, permission_grants,
     permission_revocations, is_protected, app_user_created_at, app_user_updated_at)
  VALUES
    (p_user_id, p_mitglied_id, p_operation_id, auth.uid(), v_target.role,
     v_target.permission_grants, v_target.permission_revocations,
     v_target.is_protected, v_target.created_at, v_target.updated_at);

  UPDATE public.mitglied
  SET auth_user_id = NULL
  WHERE id = p_mitglied_id AND auth_user_id = p_user_id;
  IF NOT FOUND THEN
    RAISE EXCEPTION 'Member link changed during deletion' USING ERRCODE = '42501';
  END IF;
  -- sync_app_user_from_mitglied() deletes the app_user row on unlink.
  IF EXISTS (SELECT 1 FROM public.app_user WHERE user_id = p_user_id) THEN
    RAISE EXCEPTION 'App user was not unlinked' USING ERRCODE = '42501';
  END IF;
  RETURN true;
END;
$function$
;

-- complete_prepared_mitgliedsantrag
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
      and (public.ap02_user_has_effective_right(p_created_by,'mitglied.anlegen'))
  ) then raise exception 'Keine Berechtigung' using errcode='42501'; end if;
  select * into v from public.kommunikationsvorgang where id=p_kommunikationsvorgang_id for update;
  if not found or v.vorgangstyp <> 'vertrag_signatur' or v.dokumenttyp <> 'mitgliedsantrag' then raise exception 'Der Vorgang ist kein Mitgliedsantrag' using errcode='23514'; end if;
  if v.status='erledigt' then
    select relation.vertreter_mitglied_id into representative_id from public.mitglied_gesetzlicher_vertreter relation where relation.minderjaehriges_mitglied_id=v.mitglied_id and relation.gueltig_bis is null order by relation.id desc limit 1;
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
end; $function$
;

-- complete_prepared_pachtvertrag_vor_b3
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
      and (public.ap02_user_has_effective_right(p_created_by,'pachtvertrag.verwalten'))
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
    select count(*),min(mitglied_id) into v_aktive_mitpaechter,v_aktiver_mitpaechter_id from public.pachtverhaeltnis_beteiligung pb where pb.pachtverhaeltnis_id=v_pachtverhaeltnis_id and rolle='mitpaechter' and gueltig_bis is null;
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
$function$
;

-- create_mitglied_vereinsdokument_bereitstellungen_digital
CREATE OR REPLACE FUNCTION public.create_mitglied_vereinsdokument_bereitstellungen_digital(p_mitglied_id bigint, p_vereinsdokument_fassung_ids bigint[], p_bereitgestellt_von uuid)
 RETURNS SETOF mitglied_vereinsdokument_bereitstellung
 LANGUAGE plpgsql
 SECURITY DEFINER
 SET search_path TO 'public', 'pg_temp'
AS $function$
declare v_count integer;
begin
 if auth.role()<>'service_role' then raise exception 'Die digitale Bereitstellung ist nur serverseitig zulässig' using errcode='42501'; end if;
 if p_mitglied_id is null or p_bereitgestellt_von is null or coalesce(array_length(p_vereinsdokument_fassung_ids,1),0)=0 then raise exception 'Mitglied, Fassungen und auslösender Benutzer sind erforderlich' using errcode='22023'; end if;
 if not exists(select 1 from public.app_user au join public.mitglied m on m.id=au.mitglied_id and m.auth_user_id=au.user_id where au.user_id=p_bereitgestellt_von and (public.ap02_user_has_effective_right(p_bereitgestellt_von,'vereinsunterlagen.verwalten'))) then raise exception 'Der auslösende Benutzer ist nicht berechtigt' using errcode='42501'; end if;
 if not exists(select 1 from public.mitglied where id=p_mitglied_id) then raise exception 'Unbekanntes Mitglied: %',p_mitglied_id using errcode='P0002'; end if;
 if cardinality(p_vereinsdokument_fassung_ids)<>cardinality(array(select distinct unnest(p_vereinsdokument_fassung_ids))) then raise exception 'Jede Dokumentfassung darf nur einmal übergeben werden' using errcode='22023'; end if;
 select count(*) into v_count from public.vereinsdokument_fassung where id=any(p_vereinsdokument_fassung_ids); if v_count<>cardinality(p_vereinsdokument_fassung_ids) then raise exception 'Mindestens eine Dokumentfassung ist unbekannt' using errcode='23503'; end if;
 return query insert into public.mitglied_vereinsdokument_bereitstellung(mitglied_id,vereinsdokument_fassung_id,bereitstellungsart,bereitgestellt_am,bereitgestellt_von) select p_mitglied_id,id,'digital',now(),p_bereitgestellt_von from unnest(p_vereinsdokument_fassung_ids) requested(id) returning *;
end $function$
;

-- discard_prepared_contract
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
      and (public.ap02_user_has_effective_right(p_discarded_by,'mitglied.anlegen') AND public.ap02_user_has_effective_right(p_discarded_by,'dokument.verwalten'))
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
$function$
;

-- register_pachtverhaeltnis_erstvertrag
CREATE OR REPLACE FUNCTION public.register_pachtverhaeltnis_erstvertrag(p_parzelle_id bigint, p_hauptpaechter_mitglied_id bigint, p_urspruenglicher_pachtbeginn date, p_dokument_id bigint, p_vertragsdatum date, p_wirksam_ab date, p_created_by uuid, p_mitpaechter_mitglied_id bigint DEFAULT NULL::bigint)
 RETURNS TABLE(pachtverhaeltnis_id bigint, pachtvertragsfassung_id bigint)
 LANGUAGE plpgsql
 SECURITY DEFINER
 SET search_path TO 'public', 'pg_temp'
AS $function$
declare
  v_hauptpaechter public.mitglied%rowtype;
  v_mitpaechter public.mitglied%rowtype;
  v_dokument public.dokument%rowtype;
  v_pachtverhaeltnis_id bigint;
  v_pachtvertragsfassung_id bigint;
begin
  if auth.role() is distinct from 'service_role' then
    raise exception 'Die Vertragsregistrierung ist nur serverseitig zulässig' using errcode = '42501';
  end if;
  if p_parzelle_id is null or p_hauptpaechter_mitglied_id is null
     or p_urspruenglicher_pachtbeginn is null or p_dokument_id is null
     or p_vertragsdatum is null or p_wirksam_ab is null or p_created_by is null then
    raise exception 'Parzelle, Hauptpächter, Pachtbeginn, Dokument, Vertragsdaten und auslösender Benutzer sind erforderlich' using errcode = '22023';
  end if;
  if p_wirksam_ab <> p_urspruenglicher_pachtbeginn then
    raise exception 'Beim Erstvertrag muss wirksam_ab dem ursprünglichen Pachtbeginn entsprechen' using errcode = '23514';
  end if;
  if not exists (
    select 1 from public.app_user au
    join public.mitglied m on m.id = au.mitglied_id and m.auth_user_id = au.user_id
    where au.user_id = p_created_by and au.mitglied_id is not null and au.mitglied_id > 0
      and (public.ap02_user_has_effective_right(p_created_by,'pachtvertrag.verwalten'))
  ) then
    raise exception 'Der auslösende Benutzer ist nicht als Admin oder Vorstand berechtigt' using errcode = '42501';
  end if;
  select * into v_hauptpaechter from public.mitglied where id = p_hauptpaechter_mitglied_id;
  if not found then raise exception 'Unbekannter Hauptpächter: %', p_hauptpaechter_mitglied_id using errcode = 'P0002'; end if;
  if p_mitpaechter_mitglied_id is not null then
    if p_mitpaechter_mitglied_id = p_hauptpaechter_mitglied_id then
      raise exception 'Hauptpächter und Mitpächter müssen verschieden sein' using errcode = '23514';
    end if;
    select * into v_mitpaechter from public.mitglied where id = p_mitpaechter_mitglied_id;
    if not found then raise exception 'Unbekannter Mitpächter: %', p_mitpaechter_mitglied_id using errcode = 'P0002'; end if;
    if v_hauptpaechter.geburtsdatum is not null
       and (v_hauptpaechter.geburtsdatum + interval '18 years')::date > p_urspruenglicher_pachtbeginn then
      raise exception 'Minderjährige Hauptpächter dürfen keinen Mitpächter haben' using errcode = '23514';
    end if;
  end if;
  perform 1 from public.parzelle where id = p_parzelle_id for key share;
  if not found then raise exception 'Unbekannte Parzelle: %', p_parzelle_id using errcode = 'P0002'; end if;
  perform pg_advisory_xact_lock(hashtextextended('pachtverhaeltnis:parzelle:' || p_parzelle_id::text, 0));
  if exists (select 1 from public.pachtverhaeltnis where parzelle_id = p_parzelle_id and beendet_am is null for update) then
    raise exception 'Für die Parzelle besteht bereits ein aktives Pachtverhältnis' using errcode = '23505';
  end if;
  select * into v_dokument from public.dokument where id = p_dokument_id and archiviert_at is null for update;
  if not found then raise exception 'Unbekanntes oder archiviertes Vertragsdokument: %', p_dokument_id using errcode = 'P0002'; end if;
  if v_dokument.parzelle_id is distinct from p_parzelle_id then
    raise exception 'Das Vertragsdokument gehört nicht zur angegebenen Parzelle' using errcode = '23514';
  end if;
  if v_dokument.mitglied_id is not null and v_dokument.mitglied_id is distinct from p_hauptpaechter_mitglied_id then
    raise exception 'Das Vertragsdokument gehört nicht zum Hauptpächter' using errcode = '23514';
  end if;
  if exists (select 1 from public.pachtvertragsfassung where dokument_id = p_dokument_id) then
    raise exception 'Das Vertragsdokument ist bereits einer Vertragsfassung zugeordnet' using errcode = '23505';
  end if;
  if not exists (
    select 1 from public.kommunikationsvorgang kv
    where kv.final_dokument_id = p_dokument_id and kv.vorgangstyp = 'vertrag_signatur'
      and kv.dokumenttyp = 'pachtvertrag' and kv.parzelle_id = p_parzelle_id
      and kv.mitglied_id = p_hauptpaechter_mitglied_id and kv.status in ('letzte_pruefung', 'erledigt')
  ) then
    raise exception 'Das Dokument ist kein finales Pachtvertragsdokument des angegebenen Vertragsvorgangs' using errcode = '23514';
  end if;
  insert into public.pachtverhaeltnis (parzelle_id, urspruenglicher_pachtbeginn, created_by)
  values (p_parzelle_id, p_urspruenglicher_pachtbeginn, p_created_by) returning id into v_pachtverhaeltnis_id;
  insert into public.pachtverhaeltnis_beteiligung (pachtverhaeltnis_id, mitglied_id, rolle, gueltig_ab, created_by)
  values (v_pachtverhaeltnis_id, p_hauptpaechter_mitglied_id, 'hauptpaechter', p_urspruenglicher_pachtbeginn, p_created_by);
  if p_mitpaechter_mitglied_id is not null then
    insert into public.pachtverhaeltnis_beteiligung (pachtverhaeltnis_id, mitglied_id, rolle, gueltig_ab, created_by)
    values (v_pachtverhaeltnis_id, p_mitpaechter_mitglied_id, 'mitpaechter', p_urspruenglicher_pachtbeginn, p_created_by);
  end if;
  insert into public.pachtvertragsfassung
    (pachtverhaeltnis_id, dokument_id, fassungstyp, vertragsdatum, wirksam_ab, vorheriges_vertragsdatum, vorgaenger_fassung_id, created_by)
  values (v_pachtverhaeltnis_id, p_dokument_id, 'erstvertrag', p_vertragsdatum, p_wirksam_ab, null, null, p_created_by)
  returning id into v_pachtvertragsfassung_id;
  return query select v_pachtverhaeltnis_id, v_pachtvertragsfassung_id;
end;
$function$
;

-- register_pachtvertrags_neufassung
CREATE OR REPLACE FUNCTION public.register_pachtvertrags_neufassung(p_pachtverhaeltnis_id bigint, p_vorgaenger_fassung_id bigint, p_dokument_id bigint, p_vertragsdatum date, p_wirksam_ab date, p_created_by uuid)
 RETURNS TABLE(pachtvertragsfassung_id bigint)
 LANGUAGE plpgsql
 SECURITY DEFINER
 SET search_path TO 'public', 'pg_temp'
AS $function$
declare
  v_pachtverhaeltnis public.pachtverhaeltnis%rowtype;
  v_vorgaenger public.pachtvertragsfassung%rowtype;
  v_dokument public.dokument%rowtype;
  v_hauptpaechter_mitglied_id bigint;
  v_aktive_hauptpaechter_anzahl integer;
  v_pachtvertragsfassung_id bigint;
begin
  if auth.role() is distinct from 'service_role' then
    raise exception 'Die Vertragsregistrierung ist nur serverseitig zulässig' using errcode = '42501';
  end if;
  if p_pachtverhaeltnis_id is null or p_vorgaenger_fassung_id is null
     or p_dokument_id is null or p_vertragsdatum is null or p_wirksam_ab is null or p_created_by is null then
    raise exception 'Pachtverhältnis, Vorgängerfassung, Dokument, Vertragsdaten und auslösender Benutzer sind erforderlich' using errcode = '22023';
  end if;
  if not exists (
    select 1 from public.app_user au
    join public.mitglied m on m.id = au.mitglied_id and m.auth_user_id = au.user_id
    where au.user_id = p_created_by and au.mitglied_id is not null and au.mitglied_id > 0
      and (public.ap02_user_has_effective_right(p_created_by,'pachtvertrag.verwalten'))
  ) then
    raise exception 'Der auslösende Benutzer ist nicht als Admin oder Vorstand berechtigt' using errcode = '42501';
  end if;
  select * into v_pachtverhaeltnis from public.pachtverhaeltnis
  where id = p_pachtverhaeltnis_id and beendet_am is null for update;
  if not found then raise exception 'Unbekanntes oder bereits beendetes Pachtverhältnis: %', p_pachtverhaeltnis_id using errcode = 'P0002'; end if;
  if v_pachtverhaeltnis.urspruenglicher_pachtbeginn is not null
     and p_wirksam_ab < v_pachtverhaeltnis.urspruenglicher_pachtbeginn then
    raise exception 'wirksam_ab darf nicht vor dem ursprünglichen Pachtbeginn liegen' using errcode = '23514';
  end if;
  select count(*), min(mitglied_id) into v_aktive_hauptpaechter_anzahl, v_hauptpaechter_mitglied_id
  from public.pachtverhaeltnis_beteiligung
  where pachtverhaeltnis_id = p_pachtverhaeltnis_id and rolle = 'hauptpaechter' and gueltig_bis is null;
  if v_aktive_hauptpaechter_anzahl <> 1 then
    raise exception 'Das Pachtverhältnis muss genau einen aktiven Hauptpächter haben' using errcode = '23514';
  end if;
  select * into v_vorgaenger from public.pachtvertragsfassung where id = p_vorgaenger_fassung_id for key share;
  if not found then raise exception 'Unbekannte Vorgängerfassung: %', p_vorgaenger_fassung_id using errcode = 'P0002'; end if;
  if v_vorgaenger.pachtverhaeltnis_id <> p_pachtverhaeltnis_id then
    raise exception 'Die Vorgängerfassung gehört nicht zum angegebenen Pachtverhältnis' using errcode = '23514';
  end if;
  if v_vorgaenger.vertragsdatum is not null and p_vertragsdatum < v_vorgaenger.vertragsdatum then
    raise exception 'Das Vertragsdatum der Neufassung darf nicht vor dem Vertragsdatum der Vorgängerfassung liegen' using errcode = '23514';
  end if;
  select * into v_dokument from public.dokument where id = p_dokument_id and archiviert_at is null for update;
  if not found then raise exception 'Unbekanntes oder archiviertes Vertragsdokument: %', p_dokument_id using errcode = 'P0002'; end if;
  if v_dokument.parzelle_id is distinct from v_pachtverhaeltnis.parzelle_id then
    raise exception 'Das Vertragsdokument gehört nicht zur Parzelle des Pachtverhältnisses' using errcode = '23514';
  end if;
  if v_dokument.mitglied_id is not null and v_dokument.mitglied_id is distinct from v_hauptpaechter_mitglied_id then
    raise exception 'Das Vertragsdokument gehört nicht zum aktiven Hauptpächter' using errcode = '23514';
  end if;
  if exists (select 1 from public.pachtvertragsfassung where dokument_id = p_dokument_id) then
    raise exception 'Das Vertragsdokument ist bereits einer Vertragsfassung zugeordnet' using errcode = '23505';
  end if;
  if not exists (
    select 1 from public.kommunikationsvorgang kv
    where kv.final_dokument_id = p_dokument_id and kv.vorgangstyp = 'vertrag_signatur'
      and kv.dokumenttyp = 'pachtvertrag' and kv.parzelle_id = v_pachtverhaeltnis.parzelle_id
      and kv.mitglied_id = v_hauptpaechter_mitglied_id and kv.status in ('letzte_pruefung', 'erledigt')
  ) then
    raise exception 'Das Dokument ist kein finales Pachtvertragsdokument des angegebenen Vertragsvorgangs' using errcode = '23514';
  end if;
  if exists (select 1 from public.pachtvertragsfassung where vorgaenger_fassung_id = p_vorgaenger_fassung_id) then
    raise exception 'Vorgängerfassung besitzt bereits einen Nachfolger' using errcode = '23505';
  end if;
  insert into public.pachtvertragsfassung
    (pachtverhaeltnis_id, dokument_id, fassungstyp, vertragsdatum, wirksam_ab, vorheriges_vertragsdatum, vorgaenger_fassung_id, created_by)
  values (p_pachtverhaeltnis_id, p_dokument_id, 'neufassung', p_vertragsdatum, p_wirksam_ab, v_vorgaenger.vertragsdatum, p_vorgaenger_fassung_id, p_created_by)
  returning id into v_pachtvertragsfassung_id;
  return query select v_pachtvertragsfassung_id;
end;
$function$
;

-- unlink_app_user
CREATE OR REPLACE FUNCTION public.unlink_app_user(p_user_id uuid)
 RETURNS void
 LANGUAGE plpgsql
 SECURITY DEFINER
 SET search_path TO 'public', 'pg_temp'
AS $function$
DECLARE
  v_target public.app_user%rowtype;
  v_effective bigint;
BEGIN
  IF auth.uid() IS NULL OR public.ap02_can_manage_rights() IS NOT TRUE THEN
    RAISE EXCEPTION 'CanManageRoles required' USING ERRCODE = '42501';
  END IF;
  IF p_user_id IS NULL OR p_user_id = auth.uid() THEN
    RAISE EXCEPTION 'Own account cannot be unlinked here' USING ERRCODE = '42501';
  END IF;

  SELECT * INTO v_target
  FROM public.app_user
  WHERE user_id = p_user_id
  FOR UPDATE;
  IF NOT FOUND THEN
    RAISE EXCEPTION 'Target app_user not found' USING ERRCODE = 'P0002';
  END IF;
  IF v_target.is_protected OR EXISTS (SELECT 1 FROM public.protected_admin_identity p WHERE p.user_id = p_user_id) OR EXISTS (SELECT 1 FROM public.app_user_zusatzrolle z WHERE z.user_id = p_user_id AND z.zusatzrolle_code = 'admin') THEN
    RAISE EXCEPTION 'Protected app_user cannot be unlinked' USING ERRCODE = '42501';
  END IF;

  -- Fail closed: privileged accounts cannot be deleted by this endpoint.
  -- It guarantees that the last managing user cannot be removed here.
  IF public.ap02_user_has_effective_right(p_user_id,'berechtigungen.verwalten') THEN
    RAISE EXCEPTION 'Accounts with CanManageRoles require separate administration'
      USING ERRCODE = '42501';
  END IF;

  UPDATE public.mitglied
  SET auth_user_id = NULL
  WHERE auth_user_id = v_target.user_id;

  DELETE FROM public.app_user WHERE user_id = v_target.user_id;
END;
$function$
;
DO $verify$ BEGIN
 IF EXISTS(SELECT 1 FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
 WHERE n.nspname='public' AND p.proname=ANY(ARRAY['complete_prepared_mitgliedsantrag','complete_prepared_pachtvertrag_vor_b3','create_mitglied_vereinsdokument_bereitstellungen_digital','discard_prepared_contract','register_pachtverhaeltnis_erstvertrag','register_pachtvertrags_neufassung','begin_auth_user_deletion','unlink_app_user'])
 AND pg_get_functiondef(p.oid) LIKE '%public.permission_role_base(%') THEN
 RAISE EXCEPTION 'Legacy authorization remains'; END IF;
 IF has_function_privilege('authenticated','public.ap02_user_has_effective_right(uuid,text)','EXECUTE') THEN
 RAISE EXCEPTION 'Target-user evaluation unexpectedly public'; END IF;
END $verify$;;
