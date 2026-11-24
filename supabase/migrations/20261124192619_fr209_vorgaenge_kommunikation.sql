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
-- FR2-09 Abschluss: direct client writes, immutable object links, mail fences.
-- Existing SECURITY DEFINER business routines retain their own checks.
DROP POLICY fr209_kommunikation_update ON public.kommunikationsvorgang;
CREATE POLICY fr209_kommunikation_update ON public.kommunikationsvorgang
 FOR UPDATE TO authenticated
 USING (public.ap02_has_effective_right('vorgang.bearbeiten') AND zugewiesen_an = (SELECT auth.uid()))
 WITH CHECK (public.ap02_has_effective_right('vorgang.bearbeiten') AND zugewiesen_an = (SELECT auth.uid()));
DROP POLICY fr209_kommunikation_insert ON public.kommunikationsvorgang;
CREATE POLICY fr209_kommunikation_insert ON public.kommunikationsvorgang FOR INSERT TO authenticated
 WITH CHECK (public.ap02_has_effective_right('vorgang.bearbeiten')
 AND erstellt_von = (SELECT auth.uid()) AND zugewiesen_an = (SELECT auth.uid())
 AND vorgangstyp = 'vertrag_signatur' AND status = 'vorbereitet'
 AND public.ap02_has_effective_right('vertrag.vorbereiten')
 AND (mitglied_id IS NOT NULL AND public.ap02_can_read_mitglied(mitglied_id))
 AND (parzelle_id IS NULL OR EXISTS (SELECT 1 FROM public.parzelle p WHERE p.id = parzelle_id))
 AND revision = 1 AND signatur_revision IS NULL AND signatur_gestartet_am IS NULL
 AND final_dokument_id IS NULL AND versendet_am IS NULL AND versendet_von IS NULL
 AND gedruckt_am IS NULL AND gedruckt_von IS NULL AND archiviert_am IS NULL AND archiviert_von IS NULL
 AND fehlertext IS NULL AND empfaenger_email IS NULL AND betreff IS NULL AND mailtext IS NULL
 AND vorschau_storage_bucket IS NULL AND vorschau_storage_path IS NULL
 AND final_storage_bucket IS NULL AND final_storage_path IS NULL);
-- A creator's history remains readable, not writable once reassigned.
-- The established user-facing claim pool is restricted to accessible member/parcel objects.
CREATE OR REPLACE FUNCTION public.fr209_object_scope(p_member bigint, p_parcel bigint)
 RETURNS boolean LANGUAGE sql STABLE SECURITY INVOKER SET search_path=pg_catalog,public
 AS $$ SELECT (p_member IS NULL OR public.ap02_can_read_mitglied(p_member))
 AND (p_parcel IS NULL OR EXISTS (SELECT 1 FROM public.parzelle p WHERE p.id=p_parcel)) $$;
REVOKE ALL ON FUNCTION public.fr209_object_scope(bigint,bigint) FROM PUBLIC,anon,authenticated;
GRANT EXECUTE ON FUNCTION public.fr209_object_scope(bigint,bigint) TO authenticated;
DROP POLICY fr209_kommunikation_read ON public.kommunikationsvorgang;
CREATE POLICY fr209_kommunikation_read ON public.kommunikationsvorgang FOR SELECT TO authenticated USING (
 public.ap02_has_effective_right('vereinsdaten.alle_lesen') OR
 (public.fr209_object_scope(mitglied_id,parzelle_id) AND (
  (public.ap02_has_effective_right('vorgang.lesen') AND (zugewiesen_an=(SELECT auth.uid()) OR erstellt_von=(SELECT auth.uid()))) OR
  (public.ap02_has_effective_right('vorgang.bearbeiten') AND (zugewiesen_an=(SELECT auth.uid()) OR (zugewiesen_an IS NULL AND status NOT IN ('erledigt','abgebrochen','verworfen')))) OR
  (public.ap02_has_effective_right('vertrag.versenden') AND zugewiesen_an=(SELECT auth.uid())) OR
  (public.ap02_has_effective_right('druck.verwalten') AND zugewiesen_an=(SELECT auth.uid()) AND vorgangstyp='druck')
 )));
-- SECURITY DEFINER claim cannot rely on the caller's RLS. Check parcel scope explicitly.
CREATE OR REPLACE FUNCTION public.fr209_claim_kommunikationsvorgang(p_id bigint)
 RETURNS SETOF public.kommunikationsvorgang LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,public
 AS $$ BEGIN
 IF auth.uid() IS NULL OR NOT public.ap02_has_effective_right('vorgang.bearbeiten') THEN
   RAISE EXCEPTION 'Vorgang uebernehmen nicht berechtigt' USING ERRCODE='42501'; END IF;
 RETURN QUERY UPDATE public.kommunikationsvorgang v SET zugewiesen_an=auth.uid()
 WHERE v.id=p_id AND v.zugewiesen_an IS NULL AND v.status NOT IN ('erledigt','abgebrochen','verworfen')
 AND (v.mitglied_id IS NULL OR public.ap02_can_read_mitglied(v.mitglied_id))
 AND (v.parzelle_id IS NULL OR public.ap02_has_effective_right('parzelle.lesen') OR public.ap02_has_effective_right('parzelle.bearbeiten') OR public.can_access_own_lease_parcel(v.parzelle_id))
 RETURNING v.*;
 IF NOT FOUND THEN RAISE EXCEPTION 'Vorgang nicht uebernehmbar' USING ERRCODE='PT409'; END IF;
 RETURN;
 END $$;
CREATE OR REPLACE FUNCTION public.fr209_release_kommunikationsvorgang(p_id bigint)
 RETURNS SETOF public.kommunikationsvorgang LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,public
 AS $$ BEGIN
 IF auth.uid() IS NULL OR NOT public.ap02_has_effective_right('vorgang.bearbeiten') THEN
   RAISE EXCEPTION 'Vorgang freigeben nicht berechtigt' USING ERRCODE='42501'; END IF;
 RETURN QUERY UPDATE public.kommunikationsvorgang v SET zugewiesen_an=NULL
 WHERE v.id=p_id AND v.zugewiesen_an=auth.uid() AND v.status NOT IN ('erledigt','abgebrochen','verworfen')
 RETURNING v.*;
 IF NOT FOUND THEN RAISE EXCEPTION 'Vorgang nicht freigebbar' USING ERRCODE='PT409'; END IF;
 RETURN;
 END $$;
-- Only explicitly supported direct client fields. Privileged RPCs can perform legitimate workflow transitions.
REVOKE UPDATE ON public.kommunikationsvorgang FROM PUBLIC,anon,authenticated;
GRANT UPDATE (entwurf_daten,revision,status,final_dokument_id,gedruckt_am) ON public.kommunikationsvorgang TO authenticated;
CREATE OR REPLACE FUNCTION public.fr209_guard_kommunikation_transport()
 RETURNS trigger LANGUAGE plpgsql SECURITY INVOKER SET search_path=pg_catalog,public AS $$
 BEGIN
 IF current_user NOT IN ('authenticated','anon') THEN RETURN NEW; END IF;
 IF auth.uid() IS NULL OR OLD.zugewiesen_an IS DISTINCT FROM auth.uid()
 OR NOT public.ap02_has_effective_right('vorgang.bearbeiten')
 OR NOT public.fr209_object_scope(OLD.mitglied_id,OLD.parzelle_id) THEN
   RAISE EXCEPTION 'Vorgang nicht zur Bearbeitung zugewiesen' USING ERRCODE='42501'; END IF;
 IF OLD.status IN ('erledigt','abgebrochen','verworfen') THEN RAISE EXCEPTION 'Abgeschlossener Vorgang' USING ERRCODE='55000'; END IF;
 IF NEW.zugewiesen_an IS DISTINCT FROM OLD.zugewiesen_an THEN RAISE EXCEPTION 'Zuweisung nur per RPC' USING ERRCODE='42501'; END IF;
 IF NEW.gedruckt_am IS DISTINCT FROM OLD.gedruckt_am OR (OLD.vorgangstyp='druck' AND NEW.status IS DISTINCT FROM OLD.status) THEN
  IF NOT (OLD.vorgangstyp='druck' AND OLD.status='druck_bereit' AND NEW.status='erledigt'
   AND NEW.gedruckt_am IS NOT NULL AND public.ap02_has_effective_right('druck.verwalten')) THEN
   RAISE EXCEPTION 'Druckabschluss nicht berechtigt' USING ERRCODE='42501'; END IF;
  NEW.gedruckt_am=now(); NEW.gedruckt_von=auth.uid();
 ELSIF OLD.vorgangstyp='vertrag_signatur' THEN
  IF NOT public.ap02_has_effective_right('vertrag.vorbereiten') THEN RAISE EXCEPTION 'Vertragsrecht fehlt' USING ERRCODE='42501'; END IF;
  IF NEW.entwurf_daten IS DISTINCT FROM OLD.entwurf_daten OR NEW.revision IS DISTINCT FROM OLD.revision THEN
    IF NOT (OLD.status='vorbereitet' AND NEW.status=OLD.status AND OLD.signatur_gestartet_am IS NULL AND NEW.revision=OLD.revision+1 AND NEW.final_dokument_id IS NOT DISTINCT FROM OLD.final_dokument_id) THEN
     RAISE EXCEPTION 'Ungueltige Entwurfsrevision' USING ERRCODE='55000'; END IF;
  ELSIF NEW.status IS DISTINCT FROM OLD.status THEN
    IF OLD.status='zur_signatur' AND NEW.status='letzte_pruefung' AND OLD.final_dokument_id IS NULL AND NEW.final_dokument_id IS NOT NULL THEN
      IF NOT EXISTS (SELECT 1 FROM public.dokument d WHERE d.id=NEW.final_dokument_id AND d.source_kommunikationsvorgang_id=OLD.id AND d.source_revision=OLD.signatur_revision AND d.archiviert_at IS NULL) THEN
       RAISE EXCEPTION 'Finaldokument passt nicht zum Vorgang' USING ERRCODE='42501'; END IF;
    ELSIF NOT (OLD.status='letzte_pruefung' AND NEW.status='erledigt' AND OLD.final_dokument_id IS NOT NULL AND NEW.final_dokument_id IS NOT DISTINCT FROM OLD.final_dokument_id AND OLD.dokumenttyp<>'mitgliedsantrag') THEN
      RAISE EXCEPTION 'Vertragsstatus nur per Fach-RPC' USING ERRCODE='42501'; END IF;
  ELSIF NEW.final_dokument_id IS DISTINCT FROM OLD.final_dokument_id THEN RAISE EXCEPTION 'Finaldokument nur im Statuswechsel' USING ERRCODE='42501'; END IF;
 ELSIF NEW IS DISTINCT FROM OLD THEN RAISE EXCEPTION 'Vorgangsstatus nur serverseitig' USING ERRCODE='42501'; END IF;
 RETURN NEW;
 END $$;
-- Existing global read is explicit; sensitive snapshots always require document read as well.
CREATE OR REPLACE FUNCTION public.fr209_can_read_dispatch_communication(p_kommunikationsvorgang_id bigint,p_sensitive boolean DEFAULT false)
 RETURNS boolean LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog,public AS $$
 SELECT auth.uid() IS NOT NULL
 AND (NOT p_sensitive OR public.ap02_has_effective_right('dokument.lesen'))
 AND (public.ap02_has_effective_right('vereinsdaten.alle_lesen') OR EXISTS (
 SELECT 1 FROM public.kommunikationsvorgang k WHERE k.id=p_kommunikationsvorgang_id
 AND (k.mitglied_id IS NULL OR public.ap02_can_read_mitglied(k.mitglied_id))
 AND (k.parzelle_id IS NULL OR public.ap02_has_effective_right('parzelle.lesen') OR public.can_access_own_lease_parcel(k.parzelle_id))
 AND ((public.ap02_has_effective_right('vorgang.lesen') AND (k.zugewiesen_an=auth.uid() OR k.erstellt_von=auth.uid()))
 OR (public.ap02_has_effective_right('vertrag.versenden') AND k.zugewiesen_an=auth.uid())))) $$;
-- No automatic reclaim of in-flight SMTP calls. One reservation per dispatch recipient,
-- persisted BEFORE SMTP; unknown outcomes cannot be retried automatically.
CREATE TABLE public.fr209_mail_reservation (
  reservation_key text PRIMARY KEY CHECK (length(reservation_key) BETWEEN 1 AND 1024),
  kind text NOT NULL CHECK (kind IN ('contract','club')),
  created_at timestamptz NOT NULL DEFAULT now(),
  state text NOT NULL DEFAULT 'reserved' CHECK (state IN ('reserved','confirmed','unknown')),
  confirmed_at timestamptz
);
ALTER TABLE public.fr209_mail_reservation ENABLE ROW LEVEL SECURITY;
REVOKE ALL ON public.fr209_mail_reservation FROM PUBLIC,anon,authenticated;
GRANT SELECT,INSERT,UPDATE ON public.fr209_mail_reservation TO service_role;
CREATE OR REPLACE FUNCTION public.claim_contract_dispatch(p_versandinstanz_id bigint)
 RETURNS TABLE(id bigint,betreff text,mailtext text) LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,public AS $$
 BEGIN
 IF auth.uid() IS NULL OR NOT public.ap02_has_effective_right('vertrag.versenden') THEN RAISE EXCEPTION 'Versand nicht berechtigt' USING ERRCODE='42501'; END IF;
 UPDATE public.versandinstanz z SET status='wird_versendet',beansprucht_am=now(),beansprucht_von=auth.uid(),letzter_fehler=NULL
 FROM public.kommunikationsvorgang v WHERE z.id=p_versandinstanz_id AND v.id=z.kommunikationsvorgang_id
 AND v.zugewiesen_an=auth.uid() AND v.status='erledigt' AND z.revision=coalesce(v.signatur_revision,v.revision)
 AND (v.mitglied_id IS NULL OR public.ap02_can_read_mitglied(v.mitglied_id))
 AND (v.parzelle_id IS NULL OR public.ap02_has_effective_right('parzelle.lesen') OR public.can_access_own_lease_parcel(v.parzelle_id))
 AND z.status IN ('bereit','fehler','teilweise_fehlerhaft')
 AND EXISTS (SELECT 1 FROM public.versandinstanz_empfaenger_snapshot e WHERE e.versandinstanz_id=z.id AND e.status IN ('bereit','fehler') AND e.versand_erlaubt
  AND NOT EXISTS (SELECT 1 FROM public.fr209_mail_reservation f WHERE f.reservation_key='contract:'||z.id||':'||e.id))
 AND NOT EXISTS (SELECT 1 FROM public.fr209_mail_reservation f WHERE f.kind='contract' AND f.reservation_key LIKE 'contract:'||z.id||':%' AND f.state<>'confirmed')
 RETURNING z.id,z.betreff_snapshot,z.text_snapshot INTO id,betreff,mailtext;
 IF NOT FOUND THEN RAISE EXCEPTION 'Versandinstanz nicht beanspruchbar' USING ERRCODE='PT403'; END IF;
 RETURN NEXT;
 END $$;
REVOKE ALL ON FUNCTION public.claim_contract_dispatch(bigint) FROM PUBLIC,anon,authenticated;
GRANT EXECUTE ON FUNCTION public.claim_contract_dispatch(bigint) TO authenticated;
-- Fresh authorization and recipient/party binding immediately before the durable reservation.
CREATE OR REPLACE FUNCTION public.fr209_reserve_contract_mail(p_dispatch bigint,p_recipient bigint,p_actor uuid)
 RETURNS boolean LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,public AS $$
 BEGIN
 IF current_setting('request.jwt.claims',true) IS NULL OR coalesce(auth.jwt()->>'role','')<>'service_role' THEN RAISE EXCEPTION 'Nur Versandserver' USING ERRCODE='42501'; END IF;
 IF NOT public.ap02_user_has_effective_right(p_actor,'vertrag.versenden') OR NOT EXISTS (
 SELECT 1 FROM public.versandinstanz z JOIN public.kommunikationsvorgang v ON v.id=z.kommunikationsvorgang_id
 JOIN public.versandinstanz_empfaenger_snapshot e ON e.versandinstanz_id=z.id
 JOIN public.vertragsversand_vertragspartei_snapshot p ON p.kommunikationsvorgang_id=v.id AND p.revision=z.revision AND p.rolle=e.rolle AND p.mitglied_id=e.mitglied_id
 JOIN public.mitglied m ON m.id=e.mitglied_id
 WHERE z.id=p_dispatch AND z.status='wird_versendet' AND z.beansprucht_von=p_actor AND v.zugewiesen_an=p_actor AND v.status='erledigt'
 AND e.id=p_recipient AND e.status IN ('bereit','fehler') AND e.versand_erlaubt AND lower(btrim(m.email))=e.email_normalisiert
 AND (m.email_info_einwilligung OR m.email_rechnung_einwilligung)
 ) THEN RETURN false; END IF;
 INSERT INTO public.fr209_mail_reservation(reservation_key,kind) VALUES ('contract:'||p_dispatch||':'||p_recipient,'contract') ON CONFLICT DO NOTHING;
 RETURN FOUND;
 END $$;
REVOKE ALL ON FUNCTION public.fr209_reserve_contract_mail(bigint,bigint,uuid) FROM PUBLIC,anon,authenticated;
GRANT EXECUTE ON FUNCTION public.fr209_reserve_contract_mail(bigint,bigint,uuid) TO service_role;
-- Template creation permission is independent of template maintenance.
CREATE POLICY fr209_serial_template_read ON public.serienbrief_vorlage FOR SELECT TO authenticated USING (public.ap02_has_effective_right('serienbrief.erstellen'));
REVOKE TRUNCATE,TRIGGER,REFERENCES ON public.serienbrief_vorlage FROM PUBLIC,anon,authenticated;
-- Keep the old endpoint fail-closed during the Edge rollout; only the versioned Edge uses the new claim.
ALTER FUNCTION public.claim_contract_dispatch(bigint) RENAME TO fr209_claim_contract_dispatch;
REVOKE ALL ON FUNCTION public.fr209_claim_contract_dispatch(bigint) FROM PUBLIC,anon,authenticated;
GRANT EXECUTE ON FUNCTION public.fr209_claim_contract_dispatch(bigint) TO authenticated;
CREATE FUNCTION public.claim_contract_dispatch(p_versandinstanz_id bigint)
 RETURNS TABLE(id bigint,betreff text,mailtext text) LANGUAGE plpgsql SECURITY INVOKER SET search_path=pg_catalog,public AS $$
 BEGIN RAISE EXCEPTION 'Versandclient aktualisieren' USING ERRCODE='PT503'; END $$;
REVOKE ALL ON FUNCTION public.claim_contract_dispatch(bigint) FROM PUBLIC,anon,authenticated;
GRANT EXECUTE ON FUNCTION public.claim_contract_dispatch(bigint) TO authenticated;
-- One intentional redispatch per parent. A further intentional resend must refer to the newer completed instance.
CREATE OR REPLACE FUNCTION public.prepare_contract_dispatch(p_kommunikationsvorgang_id bigint,p_erneuter_versand_von_id bigint DEFAULT NULL::bigint)
 RETURNS bigint LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,public AS $$
 DECLARE v public.kommunikationsvorgang%ROWTYPE; result_id bigint;
 BEGIN
 IF auth.uid() IS NULL OR NOT public.ap02_has_effective_right('vertrag.versenden') THEN RAISE EXCEPTION 'Versandrecht fehlt' USING ERRCODE='42501'; END IF;
 SELECT * INTO v FROM public.kommunikationsvorgang WHERE id=p_kommunikationsvorgang_id FOR UPDATE;
 IF NOT FOUND OR v.zugewiesen_an IS DISTINCT FROM auth.uid() OR v.status<>'erledigt' OR v.final_dokument_id IS NULL
 OR (v.mitglied_id IS NOT NULL AND NOT public.ap02_can_read_mitglied(v.mitglied_id))
 OR (v.parzelle_id IS NOT NULL AND NOT (public.ap02_has_effective_right('parzelle.lesen') OR public.can_access_own_lease_parcel(v.parzelle_id))) THEN
  RAISE EXCEPTION 'Vertragsvorgang nicht versandberechtigt' USING ERRCODE='42501'; END IF;
 IF p_erneuter_versand_von_id IS NOT NULL AND NOT EXISTS (
  SELECT 1 FROM public.versandinstanz s WHERE s.id=p_erneuter_versand_von_id AND s.kommunikationsvorgang_id=v.id AND s.revision=coalesce(v.signatur_revision,v.revision) AND s.status='versendet') THEN
  RAISE EXCEPTION 'Fremder oder nicht abgeschlossener Vorversand' USING ERRCODE='23514'; END IF;
 SELECT z.id INTO result_id FROM public.versandinstanz z WHERE z.kommunikationsvorgang_id=v.id AND z.revision=coalesce(v.signatur_revision,v.revision)
 AND z.erneuter_versand_von_id IS NOT DISTINCT FROM p_erneuter_versand_von_id ORDER BY z.id DESC LIMIT 1;
 IF FOUND THEN RETURN result_id; END IF;
 RETURN public.prepare_contract_dispatch_vor_phase12(v.id,p_erneuter_versand_von_id);
 END $$;
REVOKE ALL ON FUNCTION public.prepare_contract_dispatch(bigint,bigint) FROM PUBLIC,anon,authenticated;
GRANT EXECUTE ON FUNCTION public.prepare_contract_dispatch(bigint,bigint) TO authenticated;
-- Explicit server-only reservation for club documents; exact selection is idempotent.
CREATE FUNCTION public.fr209_reserve_club_mail(p_member bigint,p_versions bigint[],p_actor uuid,p_email text)
 RETURNS text LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,public AS $$
 DECLARE k text; canonical bigint[];
 BEGIN
 IF coalesce(auth.jwt()->>'role','')<>'service_role' THEN RAISE EXCEPTION 'Nur Versandserver' USING ERRCODE='42501'; END IF;
 IF NOT public.ap02_user_has_effective_right(p_actor,'vereinsunterlagen.verwalten')
 OR NOT EXISTS (SELECT 1 FROM public.app_user a JOIN public.mitglied own ON own.id=a.mitglied_id AND own.auth_user_id=a.user_id WHERE a.user_id=p_actor)
 OR EXISTS (SELECT 1 FROM public.app_user_fachrecht_override o WHERE o.user_id=p_actor AND o.fachrecht_code='mitglied.lesen' AND o.entscheidung='deny')
 OR NOT EXISTS (SELECT 1 FROM public.mitglied m WHERE m.id=p_member AND lower(btrim(m.email))=lower(btrim(p_email))
 AND (m.auth_user_id=p_actor OR public.ap02_user_has_effective_right(p_actor,'mitglied.lesen') OR public.ap02_user_has_effective_right(p_actor,'vereinsdaten.alle_lesen') OR public.ap02_user_has_effective_right(p_actor,'mitglied.bearbeiten'))) THEN
  RAISE EXCEPTION 'Mitglied oder Versandrecht nicht berechtigt' USING ERRCODE='42501'; END IF;
 IF p_versions IS NULL OR cardinality(p_versions) NOT BETWEEN 1 AND 50 THEN RAISE EXCEPTION 'Ungueltige Fassungsmenge' USING ERRCODE='22023'; END IF;
 SELECT array_agg(DISTINCT x ORDER BY x) INTO canonical FROM unnest(p_versions) x;
 IF cardinality(canonical)<>cardinality(p_versions) OR array_position(canonical,NULL) IS NOT NULL OR EXISTS (SELECT 1 FROM unnest(canonical) x WHERE x<=0)
 OR (SELECT count(*) FROM public.vereinsdokument_fassung f WHERE f.id=ANY(canonical) AND f.storage_path IS NOT NULL AND f.dateiname IS NOT NULL)=0
 OR (SELECT count(*) FROM public.vereinsdokument_fassung f WHERE f.id=ANY(canonical) AND f.storage_path IS NOT NULL AND f.dateiname IS NOT NULL)<>cardinality(canonical) THEN
  RAISE EXCEPTION 'Unbekannte oder unvollstaendige Fassungen' USING ERRCODE='22023'; END IF;
 k:='club:'||p_member||':'||md5(array_to_string(canonical,','));
 INSERT INTO public.fr209_mail_reservation(reservation_key,kind) VALUES(k,'club') ON CONFLICT DO NOTHING;
 IF NOT FOUND THEN RETURN NULL; END IF;
 RETURN k;
 END $$;
REVOKE ALL ON FUNCTION public.fr209_reserve_club_mail(bigint,bigint[],uuid,text) FROM PUBLIC,anon,authenticated;
GRANT EXECUTE ON FUNCTION public.fr209_reserve_club_mail(bigint,bigint[],uuid,text) TO service_role;
