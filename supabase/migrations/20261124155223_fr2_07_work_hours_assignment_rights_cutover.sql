-- FR2-07 / Arbeitsstunden und Arbeitseinsaetze / Stand 24.11.2026
-- Ziel: ausschliesslich DemoDB nach getrenntem Migrationspreflight.
-- Basismigration: 20261122235600 (FR2-06). Keine Produktivfreigabe.
-- Weder bestehende Stunden noch Mitglieder, Anmeldungen oder Rechte werden geaendert.
BEGIN;

DO $fr207_guard$
DECLARE v_missing text;
BEGIN
  IF NOT EXISTS (
      SELECT 1 FROM supabase_migrations.schema_migrations
       WHERE version = '20261122235600'
  ) THEN
    RAISE EXCEPTION 'FR2-07: FR2-06-Migration fehlt; Cutover abgebrochen' USING ERRCODE='55000';
  END IF;
  SELECT string_agg(v.code, ', ' ORDER BY v.code) INTO v_missing
    FROM (VALUES ('arbeitsstunden.lesen'),('arbeitsstunden.bearbeiten'),
                 ('arbeitsstunden.freigeben'),('arbeitseinsatz.verwalten')) v(code)
   WHERE NOT EXISTS (SELECT 1 FROM public.fachrecht f WHERE f.code=v.code AND f.aktiv);
  IF v_missing IS NOT NULL THEN
    RAISE EXCEPTION 'FR2-07: Fachrechte fehlen oder sind nicht aktiv: %',v_missing USING ERRCODE='55000';
  END IF;
  IF (SELECT count(*) FROM pg_catalog.pg_proc p
       JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
      WHERE n.nspname='public' AND p.proname IN
       ('review_arbeitsstunde','enforce_own_open_arbeitsstunde_update',
        'acquire_browser_edit_lock_core','rpc_export_arbeitsstunden_uebersicht')) <> 4 THEN
    RAISE EXCEPTION 'FR2-07: erwartete RPC-/Trigger-Basis ist nicht vollstaendig' USING ERRCODE='55000';
  END IF;
  IF NOT EXISTS (
    SELECT 1 FROM pg_catalog.pg_trigger t
    WHERE t.tgrelid='public.arbeitsstunde'::regclass
      AND t.tgname='trg_sync_arbeitseinsatz_teilnahme_from_arbeitsstunde'
      AND t.tgenabled IN ('O','A')
  ) THEN
    RAISE EXCEPTION 'FR2-07: Teilnehmerstatus-Trigger fehlt' USING ERRCODE='55000';
  END IF;
END $fr207_guard$;

-- Eigenprozess und Verwaltungsprozess werden nur fuer *offene* Stunden erweitert.
-- LESEN bleibt ein eigenstaendiges Fachrecht; Bearbeiten oder Pruefen darf fuer
-- die jeweilige offene Arbeitsstunde die zur Bearbeitung noetige Sicht erhalten.
DROP POLICY IF EXISTS fr207_open_work_hours_edit_read ON public.arbeitsstunde;
CREATE POLICY fr207_open_work_hours_edit_read ON public.arbeitsstunde
FOR SELECT TO authenticated
USING (status='offen' AND freigegeben IS FALSE
  AND (public.ap02_has_effective_right('arbeitsstunden.bearbeiten')
       OR public.ap02_has_effective_right('arbeitsstunden.freigeben')));

DROP POLICY IF EXISTS fr207_open_work_hours_admin_insert ON public.arbeitsstunde;
CREATE POLICY fr207_open_work_hours_admin_insert ON public.arbeitsstunde
FOR INSERT TO authenticated
WITH CHECK (
  public.ap02_has_effective_right('arbeitsstunden.bearbeiten')
  AND public.current_mitglied_id() IS NOT NULL
  AND status='offen' AND freigegeben IS FALSE
  AND genehmigt_von IS NULL AND genehmigt_am IS NULL
  AND arbeitseinsatz_anmeldung_id IS NULL
);

DROP POLICY IF EXISTS fr207_open_work_hours_admin_update ON public.arbeitsstunde;
CREATE POLICY fr207_open_work_hours_admin_update ON public.arbeitsstunde
FOR UPDATE TO authenticated
USING (
  public.ap02_has_effective_right('arbeitsstunden.bearbeiten')
  AND public.current_mitglied_id() IS NOT NULL
  AND status='offen' AND freigegeben IS FALSE
)
WITH CHECK (
  public.ap02_has_effective_right('arbeitsstunden.bearbeiten')
  AND public.current_mitglied_id() IS NOT NULL
  AND status='offen' AND freigegeben IS FALSE
  AND genehmigt_von IS NULL AND genehmigt_am IS NULL
);

-- Gesperrte/abgelehnte/genehmigte Eintraege koennen nicht direkt zurueckgesetzt
-- werden. Auch bei einer UPDATE-Policy ist der Trigger zwingend (inkl. Lock).
CREATE OR REPLACE FUNCTION public.enforce_own_open_arbeitsstunde_update()
RETURNS trigger LANGUAGE plpgsql SECURITY DEFINER
SET search_path = 'pg_catalog', 'public', 'pg_temp' AS $fn$
DECLARE
  v_assignment_date date;
  v_is_own boolean := OLD.mitglied_id = public.current_mitglied_id();
  v_can_edit boolean := public.ap02_has_effective_right('arbeitsstunden.bearbeiten');
  v_can_review boolean := public.ap02_has_effective_right('arbeitsstunden.freigeben');
  v_has_lock boolean := false;
BEGIN
  IF auth.uid() IS NOT NULL THEN
    SELECT EXISTS (
      SELECT 1 FROM public.browser_edit_lock l
      WHERE l.entity_type='arbeitsstunde'
        AND l.entity_id=OLD.id::text
        AND l.locked_by_user_id=auth.uid()
        AND l.expires_at > now()
    ) INTO v_has_lock;
  END IF;

  -- Die Pruef-RPC setzt die transaktionslokale Markierung erst NACH einer
  -- eigenstaendigen Freigaberechtepruefung. Direktes UPDATE ist kein Review.
  IF current_setting('app.g7_work_hour_review', true)='true' AND v_can_review THEN
    IF OLD.arbeitseinsatz_anmeldung_id IS NOT NULL THEN
      IF NEW.arbeitseinsatz_anmeldung_id IS DISTINCT FROM OLD.arbeitseinsatz_anmeldung_id THEN
        RAISE EXCEPTION 'Arbeitseinsatzzuordnung ist unveraenderlich' USING ERRCODE='42501';
      END IF;
      SELECT a.datum INTO v_assignment_date
        FROM public.arbeitseinsatz_anmeldung aa
        JOIN public.arbeitseinsatz a ON a.id=aa.arbeitseinsatz_id
       WHERE aa.id=OLD.arbeitseinsatz_anmeldung_id;
      IF NEW.datum IS DISTINCT FROM v_assignment_date THEN
        RAISE EXCEPTION 'Einsatzdatum und Stundendatum muessen uebereinstimmen' USING ERRCODE='42501';
      END IF;
    END IF;
    RETURN NEW;
  END IF;

  IF NEW.lockedbyuserid IS DISTINCT FROM OLD.lockedbyuserid
     OR NEW.lockat IS DISTINCT FROM OLD.lockat THEN
    IF (to_jsonb(NEW)-'lockedbyuserid'-'lockat')
        IS DISTINCT FROM (to_jsonb(OLD)-'lockedbyuserid'-'lockat') THEN
      RAISE EXCEPTION 'Sperraenderungen duerfen keine Fachaenderung enthalten' USING ERRCODE='42501';
    END IF;
    IF NOT v_is_own AND NOT v_can_edit AND NOT v_can_review THEN
      RAISE EXCEPTION 'Fremde Arbeitsstundensperre ohne Fachrecht' USING ERRCODE='42501';
    END IF;
    IF NEW.lockedbyuserid=auth.uid() AND NEW.lockat IS NOT NULL AND v_has_lock THEN
      RETURN NEW;
    END IF;
    IF OLD.lockedbyuserid=auth.uid()
       AND NEW.lockedbyuserid IS NULL AND NEW.lockat IS NULL THEN
      RETURN NEW;
    END IF;
    RAISE EXCEPTION 'Sperrfelder duerfen nur durch den Lock-Dienst geaendert werden' USING ERRCODE='42501';
  END IF;

  -- Fremde offene Stunden nur für aktive Mitglieder bearbeiten.
  IF NOT v_is_own AND v_can_edit AND NOT EXISTS (
    SELECT 1 FROM public.mitglied m
    WHERE m.id=OLD.mitglied_id AND m.aktiv IS TRUE
  ) THEN
    RAISE EXCEPTION 'FR2-07: Zielmitglied ist nicht aktiv'
      USING ERRCODE='23514';
  END IF;

  IF (NOT v_is_own AND NOT v_can_edit)
     OR (NOT v_is_own AND NOT v_has_lock)
     OR OLD.status IS DISTINCT FROM 'offen' OR OLD.freigegeben IS DISTINCT FROM FALSE
     OR OLD.genehmigt_von IS NOT NULL OR OLD.genehmigt_am IS NOT NULL
     OR NEW.status IS DISTINCT FROM 'offen' OR NEW.freigegeben IS DISTINCT FROM FALSE
     OR NEW.genehmigt_von IS NOT NULL OR NEW.genehmigt_am IS NOT NULL
     OR NEW.id IS DISTINCT FROM OLD.id
     OR NEW.mitglied_id IS DISTINCT FROM OLD.mitglied_id
     OR NEW.saison_id IS DISTINCT FROM OLD.saison_id
     OR NEW.arbeitseinsatz_anmeldung_id IS DISTINCT FROM OLD.arbeitseinsatz_anmeldung_id THEN
    RAISE EXCEPTION 'Nur eigene oder berechtigt gesperrte, offene Arbeitsstunden duerfen fachlich geaendert werden' USING ERRCODE='42501';
  END IF;

  IF OLD.arbeitseinsatz_anmeldung_id IS NOT NULL THEN
    SELECT a.datum INTO v_assignment_date
      FROM public.arbeitseinsatz_anmeldung aa
      JOIN public.arbeitseinsatz a ON a.id=aa.arbeitseinsatz_id
     WHERE aa.id=OLD.arbeitseinsatz_anmeldung_id;
    IF NEW.datum IS DISTINCT FROM v_assignment_date THEN
      RAISE EXCEPTION 'Einsatz-Arbeitsstunden behalten das Einsatzdatum' USING ERRCODE='42501';
    END IF;
  END IF;
  RETURN NEW;
END;
$fn$;

-- Administrative Sofortgenehmigung ist ausschliesslich Bearbeiten+Freigeben.
-- Die eigenstaendige Teilnahmebestaetigung besitzt ihren eigenen legitimen
-- RPC-Pfad: Arbeitseinsatz verwalten+Arbeitsstunden freigeben, plus lokalen Marker.
CREATE OR REPLACE FUNCTION public.enforce_administrative_arbeitsstunde_insert()
RETURNS trigger LANGUAGE plpgsql SECURITY DEFINER
SET search_path = 'pg_catalog','public','pg_temp' AS $fn$
BEGIN
  -- FR2-07: Zielmitglied im SECURITY-DEFINER-Trigger prüfen.
  -- Ein Arbeitsstundenrecht setzt kein mitglied.lesen voraus.
  IF NEW.status='offen' AND NEW.freigegeben IS FALSE
     AND NEW.mitglied_id IS DISTINCT FROM public.current_mitglied_id()
     AND public.ap02_has_effective_right('arbeitsstunden.bearbeiten')
     AND NOT EXISTS (
       SELECT 1 FROM public.mitglied m
       WHERE m.id=NEW.mitglied_id AND m.aktiv IS TRUE
     ) THEN
    RAISE EXCEPTION 'FR2-07: Zielmitglied ist nicht aktiv'
      USING ERRCODE='23514';
  END IF;
  IF NEW.status='genehmigt' OR NEW.freigegeben IS TRUE
     OR NEW.genehmigt_von IS NOT NULL OR NEW.genehmigt_am IS NOT NULL THEN
    IF NEW.arbeitseinsatz_anmeldung_id IS NOT NULL THEN
      IF current_setting('app.fr207_assignment_confirm',true) IS DISTINCT FROM 'true'
         OR NOT public.ap02_has_effective_right('arbeitseinsatz.verwalten')
         OR NOT public.ap02_has_effective_right('arbeitsstunden.freigeben') THEN
        RAISE EXCEPTION 'Einsatzstundenbestaetigung nur ueber geschuetzte RPC'
          USING ERRCODE='42501';
      END IF;
    ELSIF NOT (
      public.ap02_has_effective_right('arbeitsstunden.bearbeiten')
      AND public.ap02_has_effective_right('arbeitsstunden.freigeben')
    ) THEN
      RAISE EXCEPTION 'Sofortgenehmigung benoetigt Bearbeiten und Freigeben'
        USING ERRCODE='42501';
    END IF;
    IF public.current_mitglied_id() IS NULL THEN
      RAISE EXCEPTION 'Genehmiger ist keinem Mitglied zugeordnet' USING ERRCODE='42501';
    END IF;
    NEW.status:='genehmigt'; NEW.freigegeben:=TRUE;
    NEW.genehmigt_von:=public.current_mitglied_id(); NEW.genehmigt_am:=now();
  END IF;
  RETURN NEW;
END;
$fn$;

-- Berechtigter Teilnehmer: Wiederholung mit identischem Inhalt gibt die
-- bereits offene Einreichung zurueck, ohne Audit- oder Stunden-Duplikat.
CREATE OR REPLACE FUNCTION public.submit_arbeitseinsatz_arbeitsstunde(
  p_arbeitseinsatz_anmeldung_id bigint,p_stunden numeric,
  p_art_der_arbeit text DEFAULT NULL
) RETURNS public.arbeitsstunde LANGUAGE plpgsql SECURITY DEFINER
SET search_path = 'pg_catalog','public','pg_temp' AS $fn$
DECLARE
 v_registration public.arbeitseinsatz_anmeldung;
 v_assignment public.arbeitseinsatz;
 v_saison_id integer;
 v_row public.arbeitsstunde;
 v_end timestamp without time zone;
 v_work_type text;
BEGIN
 IF auth.uid() IS NULL OR public.current_mitglied_id() IS NULL THEN
   RAISE EXCEPTION 'Angemeldetes Mitglied erforderlich' USING ERRCODE='42501';
 END IF;
 IF p_stunden IS NULL OR p_stunden<=0 THEN
   RAISE EXCEPTION 'Stunden muessen positiv sein' USING ERRCODE='22023';
 END IF;
 SELECT * INTO v_registration FROM public.arbeitseinsatz_anmeldung
  WHERE id=p_arbeitseinsatz_anmeldung_id FOR UPDATE;
 IF NOT FOUND OR v_registration.mitglied_id<>public.current_mitglied_id() THEN
   RAISE EXCEPTION 'Nur eigene Anmeldungen koennen eingereicht werden' USING ERRCODE='42501';
 END IF;
 SELECT * INTO v_assignment FROM public.arbeitseinsatz
  WHERE id=v_registration.arbeitseinsatz_id;
 IF NOT FOUND OR v_assignment.aktiv IS DISTINCT FROM TRUE THEN
   RAISE EXCEPTION 'Arbeitseinsatz ist nicht aktiv' USING ERRCODE='42501';
 END IF;
 v_work_type:=coalesce(nullif(btrim(p_art_der_arbeit),''),v_assignment.titel);
 v_end:=v_assignment.datum+coalesce(v_assignment.end_uhrzeit,v_assignment.start_uhrzeit,time '23:59');
 IF public.kgv_local_now()<v_end THEN
   RAISE EXCEPTION 'Erfassung erst nach Einsatzende' USING ERRCODE='42501';
 END IF;
 SELECT * INTO v_row FROM public.arbeitsstunde
  WHERE arbeitseinsatz_anmeldung_id=v_registration.id FOR UPDATE;
 IF FOUND THEN
   IF v_row.mitglied_id=v_registration.mitglied_id
     AND v_row.status='offen' AND v_row.freigegeben=FALSE
     AND v_row.stunden=p_stunden AND v_row.art_der_arbeit=v_work_type THEN
       RETURN v_row;
   END IF;
   RAISE EXCEPTION 'Einsatzstunden bereits abweichend erfasst' USING ERRCODE='23505';
 END IF;
 IF v_registration.status<>'angemeldet' THEN
   RAISE EXCEPTION 'Nur angemeldete Teilnehmer duerfen Stunden einreichen'
     USING ERRCODE='42501';
 END IF;
 SELECT id INTO v_saison_id FROM public.saison
  WHERE jahr=extract(year from v_assignment.datum)::integer LIMIT 1;
 IF v_saison_id IS NULL THEN
   RAISE EXCEPTION 'Saison fuer Einsatzjahr fehlt' USING ERRCODE='P0002';
 END IF;
 INSERT INTO public.arbeitsstunde(
   mitglied_id,saison_id,datum,stunden,art_der_arbeit,status,freigegeben,
   genehmigt_von,genehmigt_am,arbeitseinsatz_anmeldung_id
 ) VALUES (
   v_registration.mitglied_id,v_saison_id,v_assignment.datum,p_stunden,
   v_work_type,'offen',FALSE,NULL,NULL,v_registration.id
 ) RETURNING * INTO v_row;
 RETURN v_row;
END;
$fn$;

-- Bestätigung durch verantwortliche Einsatzverwaltung. Keine Anforderung
-- von arbeitsstunden.bearbeiten; Voraussetzung ist die eigene Freigabebefugnis.
CREATE OR REPLACE FUNCTION public.confirm_arbeitseinsatz_arbeitsstunde(
  p_arbeitseinsatz_anmeldung_id bigint,p_stunden numeric,
  p_art_der_arbeit text DEFAULT NULL
) RETURNS public.arbeitsstunde LANGUAGE plpgsql SECURITY DEFINER
SET search_path = 'pg_catalog','public','pg_temp' AS $fn$
DECLARE
 v_registration public.arbeitseinsatz_anmeldung;
 v_assignment public.arbeitseinsatz;
 v_saison_id integer;
 v_reviewer integer;
 v_row public.arbeitsstunde;
 v_end timestamp without time zone;
 v_work_type text;
BEGIN
 IF NOT public.ap02_has_effective_right('arbeitseinsatz.verwalten')
    OR NOT public.ap02_has_effective_right('arbeitsstunden.freigeben') THEN
   RAISE EXCEPTION 'Verwaltungs- und Freigaberecht erforderlich' USING ERRCODE='42501';
 END IF;
 IF p_stunden IS NULL OR p_stunden<=0 THEN
   RAISE EXCEPTION 'Stunden muessen positiv sein' USING ERRCODE='22023';
 END IF;
 v_reviewer:=public.current_mitglied_id();
 IF v_reviewer IS NULL THEN
   RAISE EXCEPTION 'Bearbeiter hat keinen Mitgliedskontext' USING ERRCODE='42501';
 END IF;
 SELECT * INTO v_registration FROM public.arbeitseinsatz_anmeldung
  WHERE id=p_arbeitseinsatz_anmeldung_id FOR UPDATE;
 IF NOT FOUND THEN
   RAISE EXCEPTION 'Anmeldung fehlt' USING ERRCODE='P0002';
 END IF;
 SELECT * INTO v_assignment FROM public.arbeitseinsatz
  WHERE id=v_registration.arbeitseinsatz_id;
 IF NOT FOUND THEN
   RAISE EXCEPTION 'Arbeitseinsatz fehlt' USING ERRCODE='P0002';
 END IF;
 v_work_type:=coalesce(nullif(btrim(p_art_der_arbeit),''),v_assignment.titel);
 v_end:=v_assignment.datum+coalesce(v_assignment.end_uhrzeit,v_assignment.start_uhrzeit,time '23:59');
 IF public.kgv_local_now()<v_end THEN
   RAISE EXCEPTION 'Bestätigung erst nach Einsatzende' USING ERRCODE='42501';
 END IF;
 SELECT * INTO v_row FROM public.arbeitsstunde
  WHERE arbeitseinsatz_anmeldung_id=v_registration.id FOR UPDATE;
 IF FOUND THEN
   IF v_row.mitglied_id=v_registration.mitglied_id
      AND v_row.status='genehmigt' AND v_row.freigegeben=TRUE
      AND v_registration.status='teilgenommen'
      AND v_row.stunden=p_stunden AND v_row.art_der_arbeit=v_work_type THEN
        RETURN v_row;
   END IF;
   RAISE EXCEPTION 'Arbeitsstunden fuer Anmeldung bereits vorhanden' USING ERRCODE='23505';
 END IF;
 IF v_registration.status='teilgenommen' THEN
   RAISE EXCEPTION 'Teilnahmestatus ohne passenden Stundenbeleg' USING ERRCODE='23514';
 END IF;
 IF v_registration.status NOT IN ('angemeldet','nicht_erschienen') THEN
   RAISE EXCEPTION 'Ungueltiger Teilnehmerstatus fuer Bestaetigung' USING ERRCODE='23514';
 END IF;
 SELECT id INTO v_saison_id FROM public.saison
  WHERE jahr=extract(year from v_assignment.datum)::integer LIMIT 1;
 IF v_saison_id IS NULL THEN
   RAISE EXCEPTION 'Saison fuer Einsatzjahr fehlt' USING ERRCODE='P0002';
 END IF;
 PERFORM set_config('app.fr207_assignment_confirm','true',true);
 INSERT INTO public.arbeitsstunde(
   mitglied_id,saison_id,datum,stunden,art_der_arbeit,status,freigegeben,
   genehmigt_von,genehmigt_am,arbeitseinsatz_anmeldung_id
 ) VALUES (
   v_registration.mitglied_id,v_saison_id,v_assignment.datum,p_stunden,v_work_type,
   'genehmigt',TRUE,v_reviewer,now(),v_registration.id
 ) RETURNING * INTO v_row;
 RETURN v_row;
END;
$fn$;

REVOKE EXECUTE ON FUNCTION public.submit_arbeitseinsatz_arbeitsstunde(bigint,numeric,text) FROM PUBLIC,anon;
REVOKE EXECUTE ON FUNCTION public.confirm_arbeitseinsatz_arbeitsstunde(bigint,numeric,text) FROM PUBLIC,anon;
GRANT EXECUTE ON FUNCTION public.submit_arbeitseinsatz_arbeitsstunde(bigint,numeric,text) TO authenticated,service_role;
GRANT EXECUTE ON FUNCTION public.confirm_arbeitseinsatz_arbeitsstunde(bigint,numeric,text) TO authenticated,service_role;

-- Freigabe und Editieren sind getrennte Rechte. Korrigieren/Loeschen benoetigt
-- beide Rechte; reine Freigabe/Ablehnung nur arbeitsstunden.freigeben.
CREATE OR REPLACE FUNCTION public.review_arbeitsstunde(
  p_arbeitsstunde_id integer, p_aktion text, p_begruendung text,
  p_datum date DEFAULT NULL, p_stunden numeric DEFAULT NULL,
  p_art_der_arbeit text DEFAULT NULL
) RETURNS jsonb LANGUAGE plpgsql SECURITY DEFINER
SET search_path = 'pg_catalog', 'public', 'pg_temp' AS $fn$
DECLARE
  v_entry public.arbeitsstunde%rowtype;
  v_before jsonb;
  v_after jsonb;
  v_reviewer_id integer;
  v_action text := lower(btrim(coalesce(p_aktion,'')));
  v_comment text := btrim(coalesce(p_begruendung,''));
  v_audit_action text;
BEGIN
  IF p_arbeitsstunde_id IS NULL OR p_arbeitsstunde_id<=0 THEN
    RAISE EXCEPTION 'Gueltige Arbeitsstunden-ID erforderlich' USING ERRCODE='22023';
  END IF;
  IF v_action NOT IN ('freigeben','ablehnen','korrigieren','loeschen') THEN
    RAISE EXCEPTION 'Ungueltige Pruefaktion' USING ERRCODE='22023';
  END IF;
  IF v_comment='' THEN
    RAISE EXCEPTION 'Jede Pruefaktion benoetigt eine Begruendung' USING ERRCODE='22023';
  END IF;
  IF NOT public.ap02_has_effective_right('arbeitsstunden.freigeben') THEN
    RAISE EXCEPTION 'Arbeitsstundenfreigabe nicht erlaubt' USING ERRCODE='42501';
  END IF;
  IF v_action IN ('korrigieren','loeschen')
     AND NOT public.ap02_has_effective_right('arbeitsstunden.bearbeiten') THEN
    RAISE EXCEPTION 'Korrektur/Loeschen benoetigt zusaetzlich Bearbeitungsrecht' USING ERRCODE='42501';
  END IF;
  v_reviewer_id := public.current_mitglied_id();
  IF v_reviewer_id IS NULL THEN
    RAISE EXCEPTION 'Pruefer ist keinem Mitglied zugeordnet' USING ERRCODE='42501';
  END IF;
  SELECT * INTO v_entry FROM public.arbeitsstunde
   WHERE id=p_arbeitsstunde_id FOR UPDATE;
  IF NOT FOUND THEN
    RAISE EXCEPTION 'Arbeitsstunde existiert nicht' USING ERRCODE='P0002';
  END IF;
  IF v_entry.status IS DISTINCT FROM 'offen' OR v_entry.freigegeben IS DISTINCT FROM FALSE THEN
    RAISE EXCEPTION 'Arbeitsstunde ist bereits abschliessend geprueft'
      USING ERRCODE='23514'; -- fachlich final, KEIN retrybarer 40001
  END IF;
  IF v_action='korrigieren' AND
       (p_datum IS NULL OR p_stunden IS NULL OR p_stunden<=0
        OR nullif(btrim(p_art_der_arbeit),'') IS NULL) THEN
    RAISE EXCEPTION 'Korrektur benoetigt Datum, positive Stunden und Taetigkeit'
      USING ERRCODE='22023';
  END IF;

  v_before := to_jsonb(v_entry);
  PERFORM set_config('app.g7_work_hour_review','true',true);
  IF v_action='loeschen' THEN
    INSERT INTO public.arbeitsstunde_pruefverlauf(
      arbeitsstunde_id,aktion,begruendung,geprueft_von,geprueft_am,vorher_snapshot,nachher_snapshot
    ) VALUES (v_entry.id,'geloescht',v_comment,v_reviewer_id,now(),v_before,NULL);
    DELETE FROM public.arbeitsstunde WHERE id=v_entry.id;
    RETURN jsonb_build_object('arbeitsstunde_id',p_arbeitsstunde_id,'aktion','geloescht');
  END IF;

  IF v_action='freigeben' THEN
    UPDATE public.arbeitsstunde
       SET status='genehmigt',freigegeben=TRUE,
           genehmigt_von=v_reviewer_id,genehmigt_am=now(),
           lockedbyuserid=NULL,lockat=NULL
     WHERE id=v_entry.id RETURNING to_jsonb(arbeitsstunde) INTO v_after;
    v_audit_action:='freigegeben';
  ELSIF v_action='ablehnen' THEN
    UPDATE public.arbeitsstunde
       SET status='abgelehnt',freigegeben=FALSE,
           genehmigt_von=NULL,genehmigt_am=NULL,
           lockedbyuserid=NULL,lockat=NULL
     WHERE id=v_entry.id RETURNING to_jsonb(arbeitsstunde) INTO v_after;
    v_audit_action:='abgelehnt';
  ELSE
    UPDATE public.arbeitsstunde
       SET datum=p_datum,stunden=p_stunden,
           art_der_arbeit=btrim(p_art_der_arbeit),
           status='genehmigt',freigegeben=TRUE,
           genehmigt_von=v_reviewer_id,genehmigt_am=now(),
           lockedbyuserid=NULL,lockat=NULL
     WHERE id=v_entry.id RETURNING to_jsonb(arbeitsstunde) INTO v_after;
    v_audit_action:='korrigiert';
  END IF;
  INSERT INTO public.arbeitsstunde_pruefverlauf(
    arbeitsstunde_id,aktion,begruendung,geprueft_von,geprueft_am,vorher_snapshot,nachher_snapshot
  ) VALUES (v_entry.id,v_audit_action,v_comment,v_reviewer_id,now(),v_before,v_after);
  RETURN jsonb_build_object('arbeitsstunde_id',p_arbeitsstunde_id,
    'aktion',v_audit_action,'nachher_snapshot',v_after);
END;
$fn$;
REVOKE EXECUTE ON FUNCTION public.review_arbeitsstunde(integer,text,text,date,numeric,text)
  FROM PUBLIC, anon;
GRANT EXECUTE ON FUNCTION public.review_arbeitsstunde(integer,text,text,date,numeric,text)
  TO authenticated,service_role;

-- Bisher konnte jede KGV-Identitaet Arbeitsstunden-Locks versuchen.
-- Lockcore wird nur gezielt erweitert; bei abweichender Funktionsdefinition
-- bricht die Migration geschlossen ab (kein pauschaler Re-Write).
DO $fr207_patch_lock$
DECLARE v_src text; v_anchor text := '  if p_entity_type = ''termin'' and (';
        v_add text;
BEGIN
  SELECT pg_get_functiondef(p.oid) INTO v_src FROM pg_catalog.pg_proc p
    JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
   WHERE n.nspname='public' AND p.proname='acquire_browser_edit_lock_core';
  IF v_src IS NULL OR
     (length(v_src)-length(replace(v_src,v_anchor,'')))/length(v_anchor) <> 1 THEN
    RAISE EXCEPTION 'FR2-07: Browser-Lock-Quellanker abweichend' USING ERRCODE='55000';
  END IF;
  v_add := $piece$
  -- FR2-07: fachlich und objektbezogen abgesicherter Lock fuer Arbeitsstunden.
  if p_entity_type = 'arbeitsstunde' then
    if p_entity_id !~ '^[0-9]+$' or not exists (
      select 1 from public.arbeitsstunde h
       where h.id=p_entity_id::integer and h.status='offen' and h.freigegeben=false
         and (h.mitglied_id=public.current_mitglied_id()
           or public.ap02_has_effective_right('arbeitsstunden.bearbeiten')
           or public.ap02_has_effective_right('arbeitsstunden.freigeben'))
    ) then
      raise exception 'Arbeitsstunde liegt ausserhalb des berechtigten Lock-Scopes'
        using errcode='42501';
    end if;
  end if;
$piece$;
  EXECUTE replace(v_src,v_anchor,v_add||v_anchor);
END;
$fr207_patch_lock$;

-- Arbeitsstundenexporte enthalten Mitglieder- und Pflichtstundendaten.
-- Der vorhandene SQL-Text wird mit einem eindeutigen, geprueften Anker
-- abgesichert, ohne Auswertung/Pflichtstundenberechnung zu ersetzen.
DO $fr207_patch_export$
DECLARE v_src text; v_anchor text := '  if exists ('; v_gate text;
BEGIN
  SELECT pg_get_functiondef(p.oid) INTO v_src FROM pg_catalog.pg_proc p
    JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
   WHERE n.nspname='public' AND p.proname='rpc_export_arbeitsstunden_uebersicht';
  IF v_src IS NULL OR
     (length(v_src)-length(replace(v_src,v_anchor,'')))/length(v_anchor) <> 1 THEN
    RAISE EXCEPTION 'FR2-07: Export-RPC-Quellanker abweichend' USING ERRCODE='55000';
  END IF;
  v_gate := $piece$  -- FR2-07: kein pauschaler Export ueber Einzelfall-Selbstservice.
  if not public.ap02_has_effective_right('arbeitsstunden.lesen')
     or not public.ap02_has_effective_right('export.ausfuehren') then
    raise exception 'Fachrecht fuer Arbeitsstundenexport fehlt' using errcode='42501';
  end if;
$piece$;
  EXECUTE replace(v_src,v_anchor,v_gate||v_anchor);
END;
$fr207_patch_export$;

CREATE OR REPLACE FUNCTION public.rpc_export_arbeitsstunden_jahre()
RETURNS TABLE(label text,value text) LANGUAGE sql STABLE
SET search_path = 'pg_catalog','public' AS $fn$
  SELECT DISTINCT s.jahr::text AS label,s.jahr::text AS value
    FROM public.saison s
   WHERE public.ap02_has_effective_right('arbeitsstunden.lesen')
     AND public.ap02_has_effective_right('export.ausfuehren')
   ORDER BY 1 DESC;
$fn$;
REVOKE EXECUTE ON FUNCTION public.rpc_export_arbeitsstunden_jahre() FROM PUBLIC,anon;
REVOKE EXECUTE ON FUNCTION public.rpc_export_arbeitsstunden_uebersicht(integer,boolean,boolean,boolean,text) FROM PUBLIC,anon;
REVOKE EXECUTE ON FUNCTION public.rpc_export_arbeitsstunden_uebersicht_json(jsonb) FROM PUBLIC,anon;
GRANT EXECUTE ON FUNCTION public.rpc_export_arbeitsstunden_jahre() TO authenticated,service_role;
GRANT EXECUTE ON FUNCTION public.rpc_export_arbeitsstunden_uebersicht(integer,boolean,boolean,boolean,text) TO authenticated,service_role;
GRANT EXECUTE ON FUNCTION public.rpc_export_arbeitsstunden_uebersicht_json(jsonb) TO authenticated,service_role;

-- Genehmigungs-/Korrekturverlauf: Pruefberechtigte duerfen nur die offen
-- vorliegenden Datensaetze anschauen; bestaetigte fremde Historien bleiben
-- ohne explizites arbeitsstunden.lesen gesperrt.
DROP POLICY IF EXISTS fr207_pruefverlauf_open_reviewer ON public.arbeitsstunde_pruefverlauf;
CREATE POLICY fr207_pruefverlauf_open_reviewer ON public.arbeitsstunde_pruefverlauf
FOR SELECT TO authenticated USING (
  public.ap02_has_effective_right('arbeitsstunden.freigeben')
  AND EXISTS (SELECT 1 FROM public.arbeitsstunde a
               WHERE a.id=arbeitsstunde_id AND a.status='offen')
);

-- Nachvollziehbarkeit aller fachlichen Aenderungen, auch administrativer
-- Neuanlagen und Einsaetze. Keine direkten Client-Zugriffe auf die Audit-Tabelle.
CREATE TABLE IF NOT EXISTS public.fr207_arbeitsaudit (
  id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  geaendert_am timestamptz NOT NULL DEFAULT now(),
  akteur uuid NULL,
  objekt_tabelle text NOT NULL,
  objekt_id text NOT NULL,
  operation text NOT NULL,
  vor_daten jsonb NULL,
  nach_daten jsonb NULL,
  transaktions_id bigint NOT NULL DEFAULT txid_current()
);
CREATE INDEX IF NOT EXISTS ix_fr207_arbeitsaudit_objekt
 ON public.fr207_arbeitsaudit(objekt_tabelle,objekt_id,geaendert_am DESC);
ALTER TABLE public.fr207_arbeitsaudit ENABLE ROW LEVEL SECURITY;
REVOKE ALL ON public.fr207_arbeitsaudit FROM PUBLIC,anon,authenticated;
GRANT SELECT ON public.fr207_arbeitsaudit TO service_role;

CREATE OR REPLACE FUNCTION public.fr207_write_arbeitsaudit()
RETURNS trigger LANGUAGE plpgsql SECURITY DEFINER
SET search_path = 'pg_catalog','public','pg_temp' AS $fn$
DECLARE v_old jsonb; v_new jsonb; v_object text;
BEGIN
  IF TG_OP='INSERT' THEN
    v_old:=NULL; v_new:=to_jsonb(NEW); v_object:=v_new->>'id';
  ELSIF TG_OP='UPDATE' THEN
    v_old:=to_jsonb(OLD); v_new:=to_jsonb(NEW); v_object:=v_new->>'id';
    IF (v_old-'lockat'-'lockedbyuserid')
       IS NOT DISTINCT FROM (v_new-'lockat'-'lockedbyuserid') THEN
      RETURN NEW;
    END IF;
  ELSE
    v_old:=to_jsonb(OLD); v_new:=NULL; v_object:=v_old->>'id';
  END IF;
  INSERT INTO public.fr207_arbeitsaudit
    (akteur,objekt_tabelle,objekt_id,operation,vor_daten,nach_daten)
  VALUES (auth.uid(),TG_TABLE_NAME,v_object,TG_OP,v_old,v_new);
  IF TG_OP='DELETE' THEN RETURN OLD; END IF;
  RETURN NEW;
END;
$fn$;
REVOKE ALL ON FUNCTION public.fr207_write_arbeitsaudit() FROM PUBLIC,anon,authenticated;

DROP TRIGGER IF EXISTS trg_fr207_audit_arbeitsstunde ON public.arbeitsstunde;
CREATE TRIGGER trg_fr207_audit_arbeitsstunde
AFTER INSERT OR UPDATE OR DELETE ON public.arbeitsstunde
FOR EACH ROW EXECUTE FUNCTION public.fr207_write_arbeitsaudit();
DROP TRIGGER IF EXISTS trg_fr207_audit_arbeitseinsatz ON public.arbeitseinsatz;
CREATE TRIGGER trg_fr207_audit_arbeitseinsatz
AFTER INSERT OR UPDATE OR DELETE ON public.arbeitseinsatz
FOR EACH ROW EXECUTE FUNCTION public.fr207_write_arbeitsaudit();
DROP TRIGGER IF EXISTS trg_fr207_audit_anmeldung ON public.arbeitseinsatz_anmeldung;
CREATE TRIGGER trg_fr207_audit_anmeldung
AFTER INSERT OR UPDATE OR DELETE ON public.arbeitseinsatz_anmeldung
FOR EACH ROW EXECUTE FUNCTION public.fr207_write_arbeitsaudit();

-- Beteiligungen und historische Stunden duerfen nicht durch Entfernen eines
-- Arbeitseinsatzes kaskadierend ihren historischen Objektbezug verlieren.
CREATE OR REPLACE FUNCTION public.fr207_guard_arbeitseinsatz_delete()
RETURNS trigger LANGUAGE plpgsql SECURITY DEFINER
SET search_path = 'pg_catalog','public','pg_temp' AS $fn$
BEGIN
  IF NOT public.ap02_has_effective_right('arbeitseinsatz.verwalten') THEN
    RAISE EXCEPTION 'Fachrecht Arbeitseinsatzverwaltung fehlt' USING ERRCODE='42501';
  END IF;
  IF OLD.datum <= current_date OR EXISTS (
       SELECT 1 FROM public.arbeitseinsatz_anmeldung a
        WHERE a.arbeitseinsatz_id=OLD.id
     ) THEN
    RAISE EXCEPTION 'Einsatz mit Historie/Anmeldungen darf nicht geloescht werden'
      USING ERRCODE='23514';
  END IF;
  RETURN OLD;
END;
$fn$;
REVOKE ALL ON FUNCTION public.fr207_guard_arbeitseinsatz_delete() FROM PUBLIC,anon,authenticated;
DROP TRIGGER IF EXISTS trg_fr207_einsatz_delete_guard ON public.arbeitseinsatz;
CREATE TRIGGER trg_fr207_einsatz_delete_guard BEFORE DELETE ON public.arbeitseinsatz
FOR EACH ROW EXECUTE FUNCTION public.fr207_guard_arbeitseinsatz_delete();

-- Exzessive DDL-/Datenloeschprivilegien nicht durch RLS absicherbar (TRUNCATE).
REVOKE ALL ON public.arbeitsstunde_pruefverlauf FROM anon;
REVOKE TRUNCATE,TRIGGER,REFERENCES ON public.arbeitsstunde,
  public.arbeitseinsatz,public.arbeitseinsatz_anmeldung,
  public.arbeitsstunde_pruefverlauf FROM authenticated;
REVOKE ALL ON public.arbeitsstunde,public.arbeitseinsatz,
  public.arbeitseinsatz_anmeldung FROM anon;

COMMIT;
