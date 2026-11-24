-- FR2-09: Vorgangs- und Versandrechte / Kandidat fuer DemoDB.
-- WICHTIG: noch NICHT angewandt. ProduktivDB ist strikt ausgeschlossen.
-- Fachrechtskatalog bleibt unveraendert: vorhandene Rechte wiederverwenden.

DO $fr209_preflight$
DECLARE v_count integer;
BEGIN
  SELECT count(*) INTO v_count FROM public.fachrecht
   WHERE aktiv AND code = ANY (ARRAY[
     'vorgang.lesen','vorgang.bearbeiten','vertrag.versenden',
     'druck.verwalten','dokument.lesen','vereinsdaten.alle_lesen']);
  IF v_count <> 6 THEN
    RAISE EXCEPTION 'FR2-09: Fachrechtskatalog unvollstaendig: % / 6', v_count
      USING ERRCODE = '55000';
  END IF;
  IF to_regclass('public.kommunikationsvorgang') IS NULL
      OR to_regclass('public.versandinstanz') IS NULL
      OR to_regclass('public.versandinstanz_dokument_snapshot') IS NULL
      OR to_regclass('public.versandinstanz_empfaenger_snapshot') IS NULL THEN
    RAISE EXCEPTION 'FR2-09: Pflicht-Tabellen fehlen' USING ERRCODE = '55000';
  END IF;
END;
$fr209_preflight$;

-- SELECT ist ausdruecklich vom Bearbeiten, Versenden und Loeschen getrennt.
DROP POLICY IF EXISTS ap02_kommunikation_lesen ON public.kommunikationsvorgang;
DROP POLICY IF EXISTS kommunikationsvorgang_effective ON public.kommunikationsvorgang;
DROP POLICY IF EXISTS fr209_kommunikation_read ON public.kommunikationsvorgang;
DROP POLICY IF EXISTS fr209_kommunikation_insert ON public.kommunikationsvorgang;
DROP POLICY IF EXISTS fr209_kommunikation_update ON public.kommunikationsvorgang;

CREATE POLICY fr209_kommunikation_read ON public.kommunikationsvorgang
 FOR SELECT TO authenticated USING (
   public.ap02_has_effective_right('vereinsdaten.alle_lesen')
   OR (public.ap02_has_effective_right('vorgang.lesen')
       AND (zugewiesen_an = (SELECT auth.uid()) OR erstellt_von = (SELECT auth.uid())))
   OR (public.ap02_has_effective_right('vorgang.bearbeiten')
       AND (zugewiesen_an = (SELECT auth.uid()) OR erstellt_von = (SELECT auth.uid())
            OR zugewiesen_an IS NULL))
 );
CREATE POLICY fr209_kommunikation_insert ON public.kommunikationsvorgang
 FOR INSERT TO authenticated WITH CHECK (
   public.ap02_has_effective_right('vorgang.bearbeiten')
   AND erstellt_von = (SELECT auth.uid())
   AND (zugewiesen_an = (SELECT auth.uid()) OR zugewiesen_an IS NULL)
 );
CREATE POLICY fr209_kommunikation_update ON public.kommunikationsvorgang
 FOR UPDATE TO authenticated
 USING (
   public.ap02_has_effective_right('vorgang.bearbeiten')
   AND (zugewiesen_an = (SELECT auth.uid()) OR erstellt_von = (SELECT auth.uid()))
 )
 WITH CHECK (
   public.ap02_has_effective_right('vorgang.bearbeiten')
   AND (zugewiesen_an = (SELECT auth.uid()) OR erstellt_von = (SELECT auth.uid()))
 );

-- Kein pauschales DELETE, TRUNCATE, REFERENCES oder TRIGGER durch App-Benutzer.
REVOKE DELETE, TRUNCATE, REFERENCES, TRIGGER ON TABLE public.kommunikationsvorgang
 FROM PUBLIC, anon, authenticated;
REVOKE ALL ON TABLE public.kommunikationsvorgang FROM anon;
GRANT SELECT, INSERT, UPDATE ON TABLE public.kommunikationsvorgang TO authenticated;

-- Unzugeordnete Vorgänge werden atomar übernommen. Kein Client darf dabei
-- fremde Objekt-IDs, Zuweisungen oder Fachrechtspruefungen manipulieren.
CREATE OR REPLACE FUNCTION public.fr209_claim_kommunikationsvorgang(p_id bigint)
 RETURNS SETOF public.kommunikationsvorgang
 LANGUAGE plpgsql SECURITY DEFINER SET search_path = pg_catalog, public
AS $fr209_claim$
BEGIN
  IF auth.uid() IS NULL OR NOT public.ap02_has_effective_right('vorgang.bearbeiten') THEN
    RAISE EXCEPTION 'Vorgang uebernehmen nicht berechtigt' USING ERRCODE = '42501';
  END IF;
  RETURN QUERY UPDATE public.kommunikationsvorgang v
    SET zugewiesen_an = auth.uid()
   WHERE v.id = p_id AND v.zugewiesen_an IS NULL
     AND v.status NOT IN ('erledigt','abgebrochen','verworfen')
   RETURNING v.*;
  IF NOT FOUND THEN
    RAISE EXCEPTION 'Vorgang ist nicht mehr uebernehmbar' USING ERRCODE = '55000';
  END IF;
  RETURN;
END;
$fr209_claim$;

CREATE OR REPLACE FUNCTION public.fr209_release_kommunikationsvorgang(p_id bigint)
 RETURNS SETOF public.kommunikationsvorgang
 LANGUAGE plpgsql SECURITY DEFINER SET search_path = pg_catalog, public
AS $fr209_release$
BEGIN
  IF auth.uid() IS NULL OR NOT public.ap02_has_effective_right('vorgang.bearbeiten') THEN
    RAISE EXCEPTION 'Vorgang freigeben nicht berechtigt' USING ERRCODE = '42501';
  END IF;
  RETURN QUERY UPDATE public.kommunikationsvorgang v
    SET zugewiesen_an = NULL
   WHERE v.id = p_id AND v.zugewiesen_an = auth.uid()
     AND v.status NOT IN ('erledigt','abgebrochen','verworfen')
   RETURNING v.*;
  IF NOT FOUND THEN
    RAISE EXCEPTION 'Vorgang ist nicht mehr freigebbar' USING ERRCODE = '55000';
  END IF;
  RETURN;
END;
$fr209_release$;

REVOKE ALL ON FUNCTION public.fr209_claim_kommunikationsvorgang(bigint)
  FROM PUBLIC, anon, authenticated;
REVOKE ALL ON FUNCTION public.fr209_release_kommunikationsvorgang(bigint)
  FROM PUBLIC, anon, authenticated;
GRANT EXECUTE ON FUNCTION public.fr209_claim_kommunikationsvorgang(bigint) TO authenticated;
GRANT EXECUTE ON FUNCTION public.fr209_release_kommunikationsvorgang(bigint) TO authenticated;

-- Backend-Haertung: E-Mail-Status und historische Vorgangsmetadaten duerfen
-- durch normale REST-Clients nicht frei gesetzt werden.
CREATE OR REPLACE FUNCTION public.fr209_guard_kommunikation_transport()
 RETURNS trigger LANGUAGE plpgsql SECURITY INVOKER
 SET search_path = pg_catalog, public
AS $fr209_transport$
BEGIN
  IF current_setting('request.jwt.claim.role', true) = 'service_role' THEN
    RETURN NEW;
  END IF;
  -- Nur deklarierte Selbstzuweisung bzw. Freigabe, keine fremden Ziel-IDs.
  IF NEW.zugewiesen_an IS DISTINCT FROM OLD.zugewiesen_an
     AND NOT (
       (OLD.zugewiesen_an IS NULL AND NEW.zugewiesen_an = auth.uid())
       OR (OLD.zugewiesen_an = auth.uid() AND NEW.zugewiesen_an IS NULL)
     ) THEN
    RAISE EXCEPTION 'Fremdzuweisung nicht berechtigt' USING ERRCODE = '42501';
  END IF;
  IF NEW.erstellt_von IS DISTINCT FROM OLD.erstellt_von
     OR NEW.erstellt_am IS DISTINCT FROM OLD.erstellt_am THEN
    RAISE EXCEPTION 'Vorgangsprovenienz ist unveraenderlich' USING ERRCODE = '42501';
  END IF;
  IF NEW.versendet_am IS DISTINCT FROM OLD.versendet_am
     OR NEW.versendet_von IS DISTINCT FROM OLD.versendet_von
     OR NEW.fehlertext IS DISTINCT FROM OLD.fehlertext
     OR NEW.gedruckt_von IS DISTINCT FROM OLD.gedruckt_von THEN
    RAISE EXCEPTION 'Versand- und Historienstatus sind serververwaltet'
      USING ERRCODE = '42501';
  END IF;
  IF NEW.gedruckt_am IS DISTINCT FROM OLD.gedruckt_am
     AND NOT (OLD.vorgangstyp = 'druck' AND OLD.status = 'druck_bereit'
       AND NEW.status = 'erledigt'
       AND OLD.zugewiesen_an = auth.uid()
       AND public.ap02_has_effective_right('druck.verwalten')) THEN
    RAISE EXCEPTION 'Druckabschluss nicht berechtigt' USING ERRCODE = '42501';
  END IF;
  IF OLD.status IN ('erledigt','abgebrochen','verworfen') AND NEW IS DISTINCT FROM OLD THEN
    RAISE EXCEPTION 'Abgeschlossener Vorgang ist unveraenderlich' USING ERRCODE = '55000';
  END IF;
  IF NEW.status IS DISTINCT FROM OLD.status
     AND NEW.status IN ('wird_versendet','fehler') THEN
    RAISE EXCEPTION 'Versandstatus darf nur der Server setzen' USING ERRCODE = '42501';
  END IF;
  RETURN NEW;
END;
$fr209_transport$;
DROP TRIGGER IF EXISTS trg_fr209_kommunikation_transport ON public.kommunikationsvorgang;
CREATE TRIGGER trg_fr209_kommunikation_transport
 BEFORE UPDATE ON public.kommunikationsvorgang
 FOR EACH ROW EXECUTE FUNCTION public.fr209_guard_kommunikation_transport();
REVOKE ALL ON FUNCTION public.fr209_guard_kommunikation_transport()
 FROM PUBLIC, anon, authenticated;

-- Lese-Scope fuer Versandhistorie: globale Administrationsleserechte oder
-- nachgewiesener Vorgangsbezug. Dokument-Snapshots brauchen weitere Rechte.
CREATE OR REPLACE FUNCTION public.fr209_can_read_dispatch_communication(
  p_kommunikationsvorgang_id bigint, p_sensitive boolean DEFAULT false)
 RETURNS boolean LANGUAGE sql STABLE SECURITY DEFINER
 SET search_path = pg_catalog, public
AS $fr209_read$
 SELECT (SELECT auth.uid()) IS NOT NULL AND (
   public.ap02_has_effective_right('vereinsdaten.alle_lesen')
   OR EXISTS (
     SELECT 1 FROM public.kommunikationsvorgang k
     WHERE k.id = p_kommunikationsvorgang_id
       AND (k.zugewiesen_an = (SELECT auth.uid()) OR k.erstellt_von = (SELECT auth.uid()))
       AND (
         public.ap02_has_effective_right('vertrag.versenden')
         OR (public.ap02_has_effective_right('vorgang.lesen')
             AND (NOT p_sensitive OR public.ap02_has_effective_right('dokument.lesen')))
       )
   )
 );
$fr209_read$;
CREATE OR REPLACE FUNCTION public.fr209_can_read_dispatch_instance(
  p_versandinstanz_id bigint, p_sensitive boolean DEFAULT false)
 RETURNS boolean LANGUAGE sql STABLE SECURITY DEFINER
 SET search_path = pg_catalog, public
AS $fr209_read_instance$
 SELECT EXISTS (
   SELECT 1 FROM public.versandinstanz d
   WHERE d.id = p_versandinstanz_id
     AND public.fr209_can_read_dispatch_communication(d.kommunikationsvorgang_id, p_sensitive)
 );
$fr209_read_instance$;
REVOKE ALL ON FUNCTION public.fr209_can_read_dispatch_communication(bigint,boolean)
 FROM PUBLIC, anon, authenticated;
REVOKE ALL ON FUNCTION public.fr209_can_read_dispatch_instance(bigint,boolean)
 FROM PUBLIC, anon, authenticated;
GRANT EXECUTE ON FUNCTION public.fr209_can_read_dispatch_communication(bigint,boolean)
 TO authenticated;
GRANT EXECUTE ON FUNCTION public.fr209_can_read_dispatch_instance(bigint,boolean)
 TO authenticated;

-- Alle Snapshot-/Dispatch-Tabellen: Client nur SELECT, kein direkter Write.
REVOKE ALL ON TABLE public.versandinstanz,
 public.versandinstanz_dokument_snapshot, public.versandinstanz_empfaenger_snapshot,
 public.versandinstanz_versuch, public.vertragsversand_anlage_snapshot,
 public.vertragsversand_vertragspartei_snapshot
 FROM PUBLIC, anon, authenticated;
GRANT SELECT ON TABLE public.versandinstanz,
 public.versandinstanz_dokument_snapshot, public.versandinstanz_empfaenger_snapshot,
 public.versandinstanz_versuch, public.vertragsversand_anlage_snapshot,
 public.vertragsversand_vertragspartei_snapshot
 TO authenticated;

DROP POLICY IF EXISTS versandinstanz_read ON public.versandinstanz;
DROP POLICY IF EXISTS fr209_dispatch_read ON public.versandinstanz;
CREATE POLICY fr209_dispatch_read ON public.versandinstanz
 FOR SELECT TO authenticated USING (
   public.fr209_can_read_dispatch_communication(kommunikationsvorgang_id, false));
DROP POLICY IF EXISTS versandinstanz_dokument_read ON public.versandinstanz_dokument_snapshot;
DROP POLICY IF EXISTS fr209_dispatch_document_read ON public.versandinstanz_dokument_snapshot;
CREATE POLICY fr209_dispatch_document_read ON public.versandinstanz_dokument_snapshot
 FOR SELECT TO authenticated USING (
   public.fr209_can_read_dispatch_instance(versandinstanz_id, true));
DROP POLICY IF EXISTS versandinstanz_empfaenger_read ON public.versandinstanz_empfaenger_snapshot;
DROP POLICY IF EXISTS fr209_dispatch_recipient_read ON public.versandinstanz_empfaenger_snapshot;
CREATE POLICY fr209_dispatch_recipient_read ON public.versandinstanz_empfaenger_snapshot
 FOR SELECT TO authenticated USING (
   public.fr209_can_read_dispatch_instance(versandinstanz_id, false));
DROP POLICY IF EXISTS versandinstanz_versuch_read ON public.versandinstanz_versuch;
DROP POLICY IF EXISTS fr209_dispatch_attempt_read ON public.versandinstanz_versuch;
CREATE POLICY fr209_dispatch_attempt_read ON public.versandinstanz_versuch
 FOR SELECT TO authenticated USING (
   public.fr209_can_read_dispatch_instance(versandinstanz_id, false));
DROP POLICY IF EXISTS vertragsversand_anlage_read ON public.vertragsversand_anlage_snapshot;
DROP POLICY IF EXISTS fr209_dispatch_attachment_read ON public.vertragsversand_anlage_snapshot;
CREATE POLICY fr209_dispatch_attachment_read ON public.vertragsversand_anlage_snapshot
 FOR SELECT TO authenticated USING (
   public.fr209_can_read_dispatch_communication(kommunikationsvorgang_id, true));
DROP POLICY IF EXISTS vertragsversand_partei_read ON public.vertragsversand_vertragspartei_snapshot;
DROP POLICY IF EXISTS fr209_dispatch_party_read ON public.vertragsversand_vertragspartei_snapshot;
CREATE POLICY fr209_dispatch_party_read ON public.vertragsversand_vertragspartei_snapshot
 FOR SELECT TO authenticated USING (
   public.fr209_can_read_dispatch_communication(kommunikationsvorgang_id, false));

-- Kein Daten-DELETE, kein UPDATE vorhandener Versand-Snapshots und keine
-- Aenderung an Signatur-/Vertragsdokumenten in dieser Migration.
