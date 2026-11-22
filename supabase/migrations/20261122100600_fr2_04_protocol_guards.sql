-- FR2-04.4b -- DEMODB ONLY
-- Preserve drafts, signed protocol records, aftercare and document references.
-- Client role may edit drafts; completion is performed by the controlled RPC.
-- Trusted service-role PDF finalization and definer-based aftercare links remain available.

DROP POLICY IF EXISTS parzellen_protokoll_admin_full ON public.parzellen_protokoll;
DROP POLICY IF EXISTS parzellen_protokoll_ablesung_admin_full ON public.parzellen_protokoll_ablesung;
DROP POLICY IF EXISTS parzellen_begehung_feststellung_admin_full ON public.parzellen_begehung_feststellung;

DO $policies$
DECLARE item text;
BEGIN
  FOREACH item IN ARRAY ARRAY['parzellen_protokoll','parzellen_protokoll_ablesung','parzellen_begehung_feststellung'] LOOP
    EXECUTE format('CREATE POLICY %I ON public.%I FOR SELECT TO authenticated USING (public.ap02_has_effective_right(''begehung.verwalten''))', item||'_fr2_select',item);
    EXECUTE format('CREATE POLICY %I ON public.%I FOR INSERT TO authenticated WITH CHECK (public.ap02_has_effective_right(''begehung.verwalten''))', item||'_fr2_insert',item);
    EXECUTE format('CREATE POLICY %I ON public.%I FOR UPDATE TO authenticated USING (public.ap02_has_effective_right(''begehung.verwalten'')) WITH CHECK (public.ap02_has_effective_right(''begehung.verwalten''))', item||'_fr2_update',item);
  END LOOP;
END $policies$;

-- TRUNCATE bypasses RLS; DELETE must not erase signed history or readings.
REVOKE DELETE, TRUNCATE, REFERENCES, TRIGGER ON TABLE
  public.parzellen_protokoll, public.parzellen_protokoll_ablesung, public.parzellen_begehung_feststellung
  FROM PUBLIC, anon, authenticated;

CREATE FUNCTION public.fr2_04_guard_direct_protokoll_mutation() RETURNS trigger
LANGUAGE plpgsql SECURITY INVOKER SET search_path TO 'pg_catalog','public'
AS $guard$
BEGIN
  IF current_user <> 'authenticated' THEN RETURN NEW; END IF;
  IF auth.uid() IS NULL THEN
    RAISE EXCEPTION 'Benutzeridentität fehlt.' USING ERRCODE='42501';
  END IF;
  IF TG_OP='INSERT' THEN
    IF NEW.status IS DISTINCT FROM 'entwurf' OR NEW.dokument_id IS NOT NULL
       OR NEW.pachtverhaeltnis_id IS NOT NULL OR NEW.abgeschlossen_am IS NOT NULL
       OR NEW.abgeschlossen_von IS NOT NULL THEN
      RAISE EXCEPTION 'Abgeschlossene oder vertragsgebundene Protokolle nur über den Fachprozess.' USING ERRCODE='42501';
    END IF;
    IF NEW.erstellt_von IS NOT NULL AND NEW.erstellt_von <> auth.uid() THEN
      RAISE EXCEPTION 'Erstelleridentität stimmt nicht überein.' USING ERRCODE='42501';
    END IF;
    NEW.erstellt_von := auth.uid();
  ELSE
    IF OLD.status IS DISTINCT FROM 'entwurf' OR NEW.status IS DISTINCT FROM 'entwurf'
       OR NEW.dokument_id IS DISTINCT FROM OLD.dokument_id
       OR NEW.pachtverhaeltnis_id IS DISTINCT FROM OLD.pachtverhaeltnis_id
       OR NEW.abgeschlossen_am IS DISTINCT FROM OLD.abgeschlossen_am
       OR NEW.abgeschlossen_von IS DISTINCT FROM OLD.abgeschlossen_von
       OR NEW.erstellt_von IS DISTINCT FROM OLD.erstellt_von
       OR NEW.parzelle_id IS DISTINCT FROM OLD.parzelle_id
       OR NEW.mitglied_id IS DISTINCT FROM OLD.mitglied_id THEN
      RAISE EXCEPTION 'Status, Nachsorgebindung und abgeschlossene Protokolle sind direkt unveränderlich.' USING ERRCODE='42501';
    END IF;
  END IF;
  RETURN NEW;
END $guard$;
REVOKE ALL ON FUNCTION public.fr2_04_guard_direct_protokoll_mutation() FROM PUBLIC, anon, authenticated;
CREATE TRIGGER trg_fr2_04_guard_direct_protokoll_mutation
BEFORE INSERT OR UPDATE ON public.parzellen_protokoll
FOR EACH ROW EXECUTE FUNCTION public.fr2_04_guard_direct_protokoll_mutation();

CREATE FUNCTION public.fr2_04_guard_direct_protocol_child() RETURNS trigger
LANGUAGE plpgsql SECURITY INVOKER SET search_path TO 'pg_catalog','public'
AS $guard$
DECLARE pstatus text;
BEGIN
  IF current_user <> 'authenticated' THEN RETURN NEW; END IF;
  IF TG_OP='UPDATE' AND NEW.protokoll_id IS DISTINCT FROM OLD.protokoll_id THEN
    RAISE EXCEPTION 'Protokollbezug ist unveränderlich.' USING ERRCODE='42501';
  END IF;
  SELECT status INTO pstatus FROM public.parzellen_protokoll WHERE id=NEW.protokoll_id;
  IF pstatus IS NULL THEN RAISE EXCEPTION 'Protokoll nicht sichtbar.' USING ERRCODE='42501'; END IF;
  IF pstatus <> 'entwurf' THEN
    -- Meter snapshots are immutable; findings may still be updated as part of
    -- a later inspection/repair status workflow, but never inserted retrospectively.
    IF TG_TABLE_NAME='parzellen_protokoll_ablesung' OR TG_OP='INSERT' THEN
      RAISE EXCEPTION 'Abgeschlossene Protokollanlagen können nicht direkt geändert werden.' USING ERRCODE='42501';
    END IF;
  END IF;
  RETURN NEW;
END $guard$;
REVOKE ALL ON FUNCTION public.fr2_04_guard_direct_protocol_child() FROM PUBLIC, anon, authenticated;
CREATE TRIGGER trg_fr2_04_guard_protocol_reading
BEFORE INSERT OR UPDATE ON public.parzellen_protokoll_ablesung
FOR EACH ROW EXECUTE FUNCTION public.fr2_04_guard_direct_protocol_child();
CREATE TRIGGER trg_fr2_04_guard_protocol_finding
BEFORE INSERT OR UPDATE ON public.parzellen_begehung_feststellung
FOR EACH ROW EXECUTE FUNCTION public.fr2_04_guard_direct_protocol_child();

-- MAUI's former unrestricted UPDATE is replaced by this checked RPC.
-- This RPC is not a substitute for verifying the signed PDF bytes in HTTP E2E.
CREATE FUNCTION public.complete_parzellen_protokoll(p_protokoll_id bigint, p_dokument_id bigint)
RETURNS boolean LANGUAGE plpgsql SECURITY DEFINER SET search_path TO 'pg_catalog','public'
AS $completion$
DECLARE p public.parzellen_protokoll%ROWTYPE;
        d public.dokument%ROWTYPE;
        actor uuid:=auth.uid();
BEGIN
  IF actor IS NULL OR NOT public.ap02_has_effective_right('begehung.verwalten') THEN
    RAISE EXCEPTION 'Fachrecht begehung.verwalten fehlt.' USING ERRCODE='42501';
  END IF;
  IF p_protokoll_id IS NULL OR p_dokument_id IS NULL OR p_protokoll_id<=0 OR p_dokument_id<=0 THEN
    RAISE EXCEPTION 'Protokoll und Dokument sind erforderlich.' USING ERRCODE='22023';
  END IF;
  SELECT * INTO p FROM public.parzellen_protokoll WHERE id=p_protokoll_id FOR UPDATE;
  IF NOT FOUND THEN RAISE EXCEPTION 'Protokoll nicht gefunden.' USING ERRCODE='P0002'; END IF;
  IF p.status='abgeschlossen' THEN
    IF p.dokument_id=p_dokument_id THEN RETURN TRUE; END IF;
    RAISE EXCEPTION 'Protokoll mit anderer Dokumentreferenz bereits abgeschlossen.' USING ERRCODE='23505';
  END IF;
  IF p.status<>'entwurf' OR p.dokument_id IS NOT NULL OR p.abgeschlossen_am IS NOT NULL
     OR p.abgeschlossen_von IS NOT NULL THEN
    RAISE EXCEPTION 'Protokoll ist nicht abschließbar.' USING ERRCODE='23514';
  END IF;
  IF p.paechter_signiert_am IS NULL OR p.vorstand1_signiert_am IS NULL OR p.vorstand2_signiert_am IS NULL
     OR ((p.begleitperson_mitglied_id IS NOT NULL OR NULLIF(BTRIM(p.begleitperson_name),'') IS NOT NULL)
         AND p.begleitperson_signiert_am IS NULL) THEN
    RAISE EXCEPTION 'Erforderliche Signaturen fehlen.' USING ERRCODE='23514';
  END IF;
  SELECT * INTO d FROM public.dokument WHERE id=p_dokument_id FOR KEY SHARE;
  IF NOT FOUND OR d.archiviert_at IS NOT NULL OR d.mitglied_id IS DISTINCT FROM p.mitglied_id
     OR d.created_by IS DISTINCT FROM actor OR LOWER(COALESCE(d.mime_type,''))<>'application/pdf'
     OR COALESCE(d.titel,'') !~* 'protokoll' OR d.parzelle_id IS NOT NULL THEN
    RAISE EXCEPTION 'Dokument gehört nicht zum signierten Mitgliedsprotokoll.' USING ERRCODE='23514';
  END IF;
  UPDATE public.parzellen_protokoll SET status='abgeschlossen', dokument_id=d.id,
     abgeschlossen_von=actor, abgeschlossen_am=now(), updated_at=now() WHERE id=p.id;
  RETURN TRUE;
END $completion$;
REVOKE ALL ON FUNCTION public.complete_parzellen_protokoll(bigint,bigint) FROM PUBLIC, anon, authenticated;
GRANT EXECUTE ON FUNCTION public.complete_parzellen_protokoll(bigint,bigint) TO authenticated;

-- Restore the preexisting aftercare workflow, correcting the missing-nachsorge
-- check. The protocol/parcel/reason/lease checks remain in place.
CREATE OR REPLACE FUNCTION public.ordne_nachsorge_protokoll_zu(p_nachsorge_id bigint, p_protokoll_id bigint, p_begruendung text)
RETURNS boolean LANGUAGE plpgsql SECURITY DEFINER SET search_path TO 'pg_catalog','public'
AS $aftercare$
DECLARE n public.pacht_nachsorge%ROWTYPE;
        p public.parzellen_protokoll%ROWTYPE;
        caller uuid:=auth.uid();
        reason text:=NULLIF(BTRIM(p_begruendung),'');
BEGIN
  IF caller IS NULL OR NOT public.ap02_has_effective_right('pachtvertrag.verwalten') THEN
    RAISE EXCEPTION 'Keine Berechtigung für Übergabeprotokolle.' USING ERRCODE='42501';
  END IF;
  IF reason IS NULL THEN RAISE EXCEPTION 'Begründung erforderlich.' USING ERRCODE='22023'; END IF;
  SELECT * INTO n FROM public.pacht_nachsorge WHERE id=p_nachsorge_id FOR UPDATE;
  IF NOT FOUND THEN RAISE EXCEPTION 'Nachsorge nicht gefunden.' USING ERRCODE='P0002'; END IF;
  SELECT * INTO p FROM public.parzellen_protokoll WHERE id=p_protokoll_id FOR UPDATE;
  IF NOT FOUND OR p.parzelle_id<>n.parzelle_id OR p.protokoll_typ NOT IN ('rueckgabe','uebernahme')
    OR p.status<>'abgeschlossen' OR p.dokument_id IS NULL THEN
    RAISE EXCEPTION 'Abgeschlossenes passendes Übergabeprotokoll erforderlich.' USING ERRCODE='23514';
  END IF;
  IF p.pachtverhaeltnis_id IS NOT NULL AND p.pachtverhaeltnis_id<>n.pachtverhaeltnis_id THEN
    RAISE EXCEPTION 'Protokoll gehört zu einem anderen Pachtverhältnis.' USING ERRCODE='23514';
  END IF;
  UPDATE public.parzellen_protokoll SET pachtverhaeltnis_id=n.pachtverhaeltnis_id WHERE id=p.id;
  INSERT INTO public.pacht_nachsorge_ereignis(nachsorge_id,art,begruendung,akteur_user_id,protokoll_id)
    VALUES(n.id,'protokoll_zugeordnet',reason,caller,p.id);
  RETURN TRUE;
END $aftercare$;

DO $permissions$
BEGIN
  IF has_table_privilege('authenticated','public.parzellen_protokoll','DELETE')
     OR has_table_privilege('authenticated','public.parzellen_protokoll','TRUNCATE')
     OR has_table_privilege('authenticated','public.parzellen_protokoll_ablesung','DELETE')
     OR has_table_privilege('authenticated','public.parzellen_protokoll_ablesung','TRUNCATE')
     OR has_table_privilege('authenticated','public.parzellen_begehung_feststellung','DELETE')
     OR has_table_privilege('authenticated','public.parzellen_begehung_feststellung','TRUNCATE') THEN
    RAISE EXCEPTION 'FR2-04.4b: Unsichere Protokollberechtigungen vorhanden.' USING ERRCODE='42501';
  END IF;
END $permissions$;
