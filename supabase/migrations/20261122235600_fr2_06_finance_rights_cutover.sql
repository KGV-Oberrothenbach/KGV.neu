-- FR2-06 / 23.11.2026 -- NUR DEMODB NACH GESONDERTER FREIGABE; VDB GESPERRT
-- Basis: Demo 285 Migrationen, bis 20261122190917; Fachrechte 2.0.
-- Bestehende deaktivierte Freigaben werden NICHT aktiviert.
-- Keine neuen Rollenprivilegien, keine Admin-Abkuerzung, keine personenbezogenen Daten.

DO $guard$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM public.fachrecht
                 WHERE code = 'jahresabschluss.finalisieren'
                   AND aktiv IS FALSE AND status = 'gesperrt_fachentscheidung')
     OR NOT EXISTS (SELECT 1 FROM public.fachrecht
                    WHERE code = 'jahresabschluss.korrigieren'
                      AND aktiv IS FALSE AND status = 'gesperrt_fachentscheidung') THEN
    RAISE EXCEPTION 'FR2-06 ABGEBROCHEN: Finale und Korrektur muessen deaktiviert bleiben' USING ERRCODE='42501';
  END IF;
  IF NOT EXISTS (SELECT 1 FROM public.fachrecht WHERE code='finanzen.lesen' AND aktiv)
     OR NOT EXISTS (SELECT 1 FROM public.fachrecht WHERE code='finanzen.rechnungen' AND aktiv)
     OR NOT EXISTS (SELECT 1 FROM public.fachrecht WHERE code='finanzen.kostenarten' AND aktiv)
     OR NOT EXISTS (SELECT 1 FROM public.fachrecht WHERE code='jahresabschluss.vorbereiten' AND aktiv) THEN
    RAISE EXCEPTION 'FR2-06 ABGEBROCHEN: aktiver Fachrechtekatalog unvollstaendig';
  END IF;
END
$guard$;

-- Keine ALL-Policies: Leserechte, Bearbeitung, Vorbereitung und Sperren trennen.
DROP POLICY IF EXISTS jahresabschluss_admin_full ON public.jahresabschluss;
CREATE POLICY fr206_ja_read ON public.jahresabschluss FOR SELECT TO authenticated
  USING (public.ap02_has_effective_right('finanzen.lesen')
      OR public.ap02_has_effective_right('finanzen.rechnungen')
      OR public.ap02_has_effective_right('jahresabschluss.vorbereiten')
      OR public.ap02_has_effective_right('jahresabschluss.finalisieren'));
CREATE POLICY fr206_ja_insert ON public.jahresabschluss FOR INSERT TO authenticated
  WITH CHECK (public.ap02_has_effective_right('jahresabschluss.vorbereiten')
     AND status <> 'abgeschlossen' AND abgeschlossen_am IS NULL AND abgeschlossen_von IS NULL);
CREATE POLICY fr206_ja_update ON public.jahresabschluss FOR UPDATE TO authenticated
  USING (public.ap02_has_effective_right('jahresabschluss.vorbereiten')
      OR public.ap02_has_effective_right('jahresabschluss.finalisieren'))
  WITH CHECK (public.ap02_has_effective_right('jahresabschluss.vorbereiten')
      OR public.ap02_has_effective_right('jahresabschluss.finalisieren'));
CREATE POLICY fr206_ja_delete ON public.jahresabschluss FOR DELETE TO authenticated
  USING (public.ap02_has_effective_right('jahresabschluss.vorbereiten')
     AND status <> 'abgeschlossen');

DROP POLICY IF EXISTS jahresabschluss_position_select_own_or_admin ON public.jahresabschluss_position;
DROP POLICY IF EXISTS jahresabschluss_position_write_admin ON public.jahresabschluss_position;
CREATE POLICY fr206_pos_read ON public.jahresabschluss_position FOR SELECT TO authenticated
  USING (mitglied_id = public.current_mitglied_id()
      OR public.ap02_has_effective_right('finanzen.lesen')
      OR public.ap02_has_effective_right('jahresabschluss.vorbereiten')
      OR public.ap02_has_effective_right('jahresabschluss.finalisieren'));
CREATE POLICY fr206_pos_insert ON public.jahresabschluss_position FOR INSERT TO authenticated
  WITH CHECK (public.ap02_has_effective_right('jahresabschluss.vorbereiten'));
CREATE POLICY fr206_pos_update ON public.jahresabschluss_position FOR UPDATE TO authenticated
  USING (public.ap02_has_effective_right('jahresabschluss.vorbereiten'))
  WITH CHECK (public.ap02_has_effective_right('jahresabschluss.vorbereiten'));
CREATE POLICY fr206_pos_delete ON public.jahresabschluss_position FOR DELETE TO authenticated
  USING (public.ap02_has_effective_right('jahresabschluss.vorbereiten'));

DROP POLICY IF EXISTS jahresabschluss_verbrauch_select_own_or_admin ON public.jahresabschluss_verbrauch;
DROP POLICY IF EXISTS jahresabschluss_verbrauch_write_admin ON public.jahresabschluss_verbrauch;
CREATE POLICY fr206_consumption_read ON public.jahresabschluss_verbrauch FOR SELECT TO authenticated
  USING (mitglied_id = public.current_mitglied_id()
      OR public.ap02_has_effective_right('finanzen.lesen')
      OR public.ap02_has_effective_right('jahresabschluss.vorbereiten')
      OR public.ap02_has_effective_right('jahresabschluss.finalisieren'));
CREATE POLICY fr206_consumption_insert ON public.jahresabschluss_verbrauch FOR INSERT TO authenticated
  WITH CHECK (public.ap02_has_effective_right('jahresabschluss.vorbereiten'));
CREATE POLICY fr206_consumption_update ON public.jahresabschluss_verbrauch FOR UPDATE TO authenticated
  USING (public.ap02_has_effective_right('jahresabschluss.vorbereiten'))
  WITH CHECK (public.ap02_has_effective_right('jahresabschluss.vorbereiten'));
CREATE POLICY fr206_consumption_delete ON public.jahresabschluss_verbrauch FOR DELETE TO authenticated
  USING (public.ap02_has_effective_right('jahresabschluss.vorbereiten'));

-- Rechnung und Vorauszahlung: Lesender Finanzzugriff ist nicht gleich Schreibzugriff.
DROP POLICY IF EXISTS jahresabschluss_rechnung_admin_full ON public.jahresabschluss_rechnung;
DROP POLICY IF EXISTS jahresabschluss_rechnung_zuordnung_admin_full ON public.jahresabschluss_rechnung_zuordnung;
DROP POLICY IF EXISTS jahresabschluss_vorauszahlung_admin_full ON public.jahresabschluss_vorauszahlung;
DO $policies$
DECLARE r record;
BEGIN
  FOR r IN SELECT unnest(ARRAY['jahresabschluss_rechnung',
                                'jahresabschluss_rechnung_zuordnung',
                                'jahresabschluss_vorauszahlung']) AS t LOOP
    EXECUTE format('CREATE POLICY fr206_read ON public.%I FOR SELECT TO authenticated USING (
      public.ap02_has_effective_right(''finanzen.lesen'')
      OR public.ap02_has_effective_right(''finanzen.rechnungen'')
      OR public.ap02_has_effective_right(''jahresabschluss.vorbereiten'')
      OR public.ap02_has_effective_right(''jahresabschluss.finalisieren''))', r.t);
    EXECUTE format('CREATE POLICY fr206_insert ON public.%I FOR INSERT TO authenticated
      WITH CHECK (public.ap02_has_effective_right(''finanzen.rechnungen''))', r.t);
    EXECUTE format('CREATE POLICY fr206_update ON public.%I FOR UPDATE TO authenticated
      USING (public.ap02_has_effective_right(''finanzen.rechnungen''))
      WITH CHECK (public.ap02_has_effective_right(''finanzen.rechnungen''))', r.t);
    EXECUTE format('CREATE POLICY fr206_delete ON public.%I FOR DELETE TO authenticated
      USING (public.ap02_has_effective_right(''finanzen.rechnungen''))', r.t);
  END LOOP;
END
$policies$;

-- Gemeinsame, authentifiziert lesbare Kostenart-/Umlageart-Kataloge bleiben lesbar,
-- weil auch nichtfinanzielle Workflows dieselben Optionen verwenden.
DROP POLICY IF EXISTS kostenart_write_admin ON public.kostenart;
DROP POLICY IF EXISTS umlageart_write_admin ON public.umlageart;
DO $policies$
DECLARE r record;
BEGIN
  FOR r IN SELECT unnest(ARRAY['kostenart', 'umlageart']) AS t LOOP
    EXECUTE format('CREATE POLICY fr206_insert ON public.%I FOR INSERT TO authenticated
      WITH CHECK (public.ap02_has_effective_right(''finanzen.kostenarten''))', r.t);
    EXECUTE format('CREATE POLICY fr206_update ON public.%I FOR UPDATE TO authenticated
      USING (public.ap02_has_effective_right(''finanzen.kostenarten''))
      WITH CHECK (public.ap02_has_effective_right(''finanzen.kostenarten''))', r.t);
    EXECUTE format('CREATE POLICY fr206_delete ON public.%I FOR DELETE TO authenticated
      USING (public.ap02_has_effective_right(''finanzen.kostenarten''))', r.t);
  END LOOP;
END
$policies$;

-- Korrektur-Tabellen bleiben ausschliesslich bei explizitem, noch deaktiviertem Recht beschreibbar.
DROP POLICY IF EXISTS jahresabschluss_korrektur_admin_full ON public.jahresabschluss_korrektur;
DROP POLICY IF EXISTS jahresabschluss_korrektur_position_admin_full ON public.jahresabschluss_korrektur_position;
DO $policies$
DECLARE r record;
BEGIN
  FOR r IN SELECT unnest(ARRAY['jahresabschluss_korrektur',
                                'jahresabschluss_korrektur_position']) AS t LOOP
    EXECUTE format('CREATE POLICY fr206_read ON public.%I FOR SELECT TO authenticated
      USING (public.ap02_has_effective_right(''finanzen.lesen'')
      OR public.ap02_has_effective_right(''jahresabschluss.korrigieren''))',r.t);
    EXECUTE format('CREATE POLICY fr206_insert ON public.%I FOR INSERT TO authenticated
      WITH CHECK (public.ap02_has_effective_right(''jahresabschluss.korrigieren''))',r.t);
    EXECUTE format('CREATE POLICY fr206_update ON public.%I FOR UPDATE TO authenticated
      USING (public.ap02_has_effective_right(''jahresabschluss.korrigieren''))
      WITH CHECK (public.ap02_has_effective_right(''jahresabschluss.korrigieren''))',r.t);
    EXECUTE format('CREATE POLICY fr206_delete ON public.%I FOR DELETE TO authenticated
      USING (public.ap02_has_effective_right(''jahresabschluss.korrigieren''))',r.t);
  END LOOP;
END
$policies$;

-- Finale Zustandswechsel: neben RLS stets nach Aktion und Ausgangszustand pruefen.
-- Inaktiv bedeutet weiterhin: auch Admin und individuell Berechtigte kommen nicht durch.
CREATE OR REPLACE FUNCTION public.ap02_guard_jahresabschluss_status()
RETURNS trigger LANGUAGE plpgsql
SET search_path = pg_catalog, public
AS $fn$
BEGIN
  IF TG_TABLE_NAME = 'jahresabschluss_korrektur' THEN
    IF NOT public.ap02_has_effective_right('jahresabschluss.korrigieren') THEN
      RAISE EXCEPTION 'Korrekturabschluss weiterhin gesperrt' USING ERRCODE='42501';
    END IF;
    RETURN NEW;
  END IF;

  IF TG_OP = 'INSERT' THEN
    IF NOT public.ap02_has_effective_right('jahresabschluss.vorbereiten') THEN
      RAISE EXCEPTION 'Jahresabschluss vorbereiten nicht erlaubt' USING ERRCODE='42501';
    END IF;
    IF NEW.status NOT IN ('in_bearbeitung', 'berechnet', 'geprueft')
       OR NEW.abgeschlossen_am IS NOT NULL OR NEW.abgeschlossen_von IS NOT NULL THEN
      RAISE EXCEPTION 'Ungueltiger erster Abschlusszustand' USING ERRCODE='23514';
    END IF;
    RETURN NEW;
  END IF;

  IF NEW.id IS DISTINCT FROM OLD.id OR NEW.saison_id IS DISTINCT FROM OLD.saison_id THEN
    RAISE EXCEPTION 'Abschlussidentitaet und Saison sind unveraenderlich' USING ERRCODE='23514';
  END IF;
  IF OLD.status = 'abgeschlossen' THEN
    RAISE EXCEPTION 'Abgeschlossener Snapshot ist unveraenderlich' USING ERRCODE='23514';
  END IF;
  IF NEW.status = 'abgeschlossen' THEN
    IF NOT public.ap02_has_effective_right('jahresabschluss.finalisieren') THEN
      RAISE EXCEPTION 'Finalisierung derzeit nicht freigegeben' USING ERRCODE='42501';
    END IF;
    IF OLD.status <> 'berechnet'
       OR (NEW.berechnung_version, NEW.bemerkung, NEW.erstellt_am, NEW.erstellt_von)
          IS DISTINCT FROM (OLD.berechnung_version, OLD.bemerkung, OLD.erstellt_am, OLD.erstellt_von)
       OR NEW.abgeschlossen_am IS NULL
       OR NEW.abgeschlossen_von IS DISTINCT FROM public.current_mitglied_id() THEN
      RAISE EXCEPTION 'Ungueltiger finaler Zustandswechsel' USING ERRCODE='23514';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM public.jahresabschluss_position p
                   WHERE p.jahresabschluss_id = OLD.id) THEN
      RAISE EXCEPTION 'Finalisierung ohne Ergebnispositionen nicht zulaessig' USING ERRCODE='23514';
    END IF;
    IF EXISTS (SELECT 1 FROM public.jahresabschluss_rechnung r
      LEFT JOIN LATERAL (
        SELECT COALESCE(sum(z.betrag),0) AS zugeordnet
        FROM public.jahresabschluss_rechnung_zuordnung z WHERE z.rechnung_id=r.id
      ) x ON true
      WHERE r.saison_id = OLD.saison_id AND x.zugeordnet <> r.gesamtbetrag) THEN
      RAISE EXCEPTION 'Nicht vollstaendig zugeordnete Rechnungen' USING ERRCODE='23514';
    END IF;
  ELSE
    IF NOT public.ap02_has_effective_right('jahresabschluss.vorbereiten') THEN
      RAISE EXCEPTION 'Vorbereitung ohne Fachrecht' USING ERRCODE='42501';
    END IF;
    IF NEW.status NOT IN ('in_bearbeitung', 'berechnet', 'geprueft')
       OR NEW.abgeschlossen_am IS NOT NULL OR NEW.abgeschlossen_von IS NOT NULL THEN
      RAISE EXCEPTION 'Ungueltiger Entwurfszustand' USING ERRCODE='23514';
    END IF;
  END IF;
  RETURN NEW;
END
$fn$;

-- Auch Verbrauchergebnisse sind Snapshots; bisher fehlte dafuer eine Abschlusssperre.
-- FOR SHARE sperrt den Elternabschluss unabhaengig vom aktuellen Status.
CREATE OR REPLACE FUNCTION public.fr206_guard_snapshot_child()
RETURNS trigger LANGUAGE plpgsql
SET search_path = pg_catalog, public
AS $fn$
DECLARE v_old_ja bigint; v_new_ja bigint; v_state text;
BEGIN
  IF TG_TABLE_NAME = 'jahresabschluss_position' THEN
    IF TG_OP IN ('UPDATE','DELETE') THEN
      v_old_ja := OLD.jahresabschluss_id;
    END IF;
    IF TG_OP IN ('UPDATE','INSERT') THEN
      v_new_ja := NEW.jahresabschluss_id;
    END IF;
  ELSE
    IF TG_OP IN ('UPDATE','DELETE') THEN
      SELECT p.jahresabschluss_id INTO v_old_ja
        FROM public.jahresabschluss_position p WHERE p.id = OLD.jahresabschluss_position_id;
    END IF;
    IF TG_OP IN ('UPDATE','INSERT') THEN
      SELECT p.jahresabschluss_id INTO v_new_ja
        FROM public.jahresabschluss_position p WHERE p.id = NEW.jahresabschluss_position_id;
    END IF;
  END IF;
  IF v_old_ja IS NOT NULL THEN
    -- Lock even a non-finalized row: otherwise status could turn final concurrently.
    SELECT j.status INTO v_state FROM public.jahresabschluss j
    WHERE j.id=v_old_ja FOR SHARE;
    IF v_state = 'abgeschlossen' THEN
      RAISE EXCEPTION 'Abgeschlossener Snapshot darf nicht geaendert werden' USING ERRCODE='23514';
    END IF;
  END IF;
  IF v_new_ja IS NOT NULL THEN
    v_state := NULL;
    SELECT j.status INTO v_state FROM public.jahresabschluss j
    WHERE j.id=v_new_ja FOR SHARE;
    IF v_state = 'abgeschlossen' THEN
      RAISE EXCEPTION 'Abgeschlossener Snapshot darf nicht geaendert werden' USING ERRCODE='23514';
    END IF;
  END IF;
  IF TG_OP = 'DELETE' THEN RETURN OLD; END IF;
  RETURN NEW;
END
$fn$;
DROP TRIGGER IF EXISTS fr206_guard_position ON public.jahresabschluss_position;
CREATE TRIGGER fr206_guard_position BEFORE INSERT OR UPDATE OR DELETE
ON public.jahresabschluss_position FOR EACH ROW EXECUTE FUNCTION public.fr206_guard_snapshot_child();
DROP TRIGGER IF EXISTS fr206_guard_consumption ON public.jahresabschluss_verbrauch;
CREATE TRIGGER fr206_guard_consumption BEFORE INSERT OR UPDATE OR DELETE
ON public.jahresabschluss_verbrauch FOR EACH ROW EXECUTE FUNCTION public.fr206_guard_snapshot_child();

-- Nach finalem Saisonabschluss koennen weder Rechnungsquellen noch Vorauszahlungen
-- nachtraeglich geaendert werden. (Wird erst nach spaeterer Freigabe relevant.)
CREATE OR REPLACE FUNCTION public.fr206_guard_finalized_source()
RETURNS trigger LANGUAGE plpgsql
SET search_path = pg_catalog, public
AS $fn$
DECLARE v_old_saison integer; v_new_saison integer; v_state text;
BEGIN
  IF TG_TABLE_NAME = 'jahresabschluss_rechnung_zuordnung' THEN
    IF TG_OP IN ('UPDATE','DELETE') THEN
      SELECT saison_id INTO v_old_saison FROM public.jahresabschluss_rechnung
      WHERE id=OLD.rechnung_id;
    END IF;
    IF TG_OP IN ('UPDATE','INSERT') THEN
      SELECT saison_id INTO v_new_saison FROM public.jahresabschluss_rechnung
      WHERE id=NEW.rechnung_id;
    END IF;
  ELSE
    IF TG_OP IN ('UPDATE','DELETE') THEN v_old_saison := OLD.saison_id; END IF;
    IF TG_OP IN ('UPDATE','INSERT') THEN v_new_saison := NEW.saison_id; END IF;
  END IF;
  IF v_old_saison IS NOT NULL THEN
    SELECT j.status INTO v_state FROM public.jahresabschluss j
    WHERE j.saison_id=v_old_saison FOR SHARE;
    IF v_state = 'abgeschlossen' THEN
      RAISE EXCEPTION 'Saison bereits abgeschlossen' USING ERRCODE='23514';
    END IF;
  END IF;
  IF v_new_saison IS NOT NULL THEN
    v_state := NULL;
    SELECT j.status INTO v_state FROM public.jahresabschluss j
    WHERE j.saison_id=v_new_saison FOR SHARE;
    IF v_state = 'abgeschlossen' THEN
      RAISE EXCEPTION 'Saison bereits abgeschlossen' USING ERRCODE='23514';
    END IF;
  END IF;
  IF TG_OP='DELETE' THEN RETURN OLD; END IF;
  RETURN NEW;
END
$fn$;
DO $triggers$
DECLARE r record;
BEGIN
  FOR r IN SELECT unnest(ARRAY['jahresabschluss_rechnung',
     'jahresabschluss_rechnung_zuordnung', 'jahresabschluss_vorauszahlung']) AS t LOOP
    EXECUTE format('DROP TRIGGER IF EXISTS fr206_guard_finalized_source ON public.%I', r.t);
    EXECUTE format('CREATE TRIGGER fr206_guard_finalized_source BEFORE INSERT OR UPDATE OR DELETE
      ON public.%I FOR EACH ROW EXECUTE FUNCTION public.fr206_guard_finalized_source()', r.t);
  END LOOP;
END
$triggers$;

-- Atomares Ersetzen von Entwurfspositionen. Ein HTTP-Aufruf statt DELETE+INSERT:
-- ACID, FOR UPDATE auf dem Elternabschluss und inhaltsgleiche Retries ohne neue IDs.
CREATE OR REPLACE FUNCTION public.fr206_replace_jahresabschluss_positionen(
  p_jahresabschluss_id bigint, p_positionen jsonb)
RETURNS integer LANGUAGE plpgsql SECURITY INVOKER
SET search_path = pg_catalog, public
AS $fn$
DECLARE
  v_abschluss public.jahresabschluss%ROWTYPE;
  v_current jsonb;
  v_wanted jsonb;
  v_count integer;
BEGIN
  IF NOT public.ap02_has_effective_right('jahresabschluss.vorbereiten') THEN
    RAISE EXCEPTION 'Keine Berechtigung fuer Jahresabschluss-Entwurf' USING ERRCODE='42501';
  END IF;
  IF p_jahresabschluss_id IS NULL OR p_jahresabschluss_id <= 0
     OR p_positionen IS NULL OR jsonb_typeof(p_positionen) <> 'array'
     OR jsonb_array_length(p_positionen) = 0
     OR jsonb_array_length(p_positionen) > 20000 THEN
    RAISE EXCEPTION 'Ungueltige Positionenliste' USING ERRCODE='22023';
  END IF;
  SELECT * INTO v_abschluss FROM public.jahresabschluss
   WHERE id=p_jahresabschluss_id FOR UPDATE;
  IF NOT FOUND OR v_abschluss.status <> 'berechnet' THEN
    RAISE EXCEPTION 'Entwurf existiert nicht oder ist nicht berechnet' USING ERRCODE='23514';
  END IF;
  -- Keine Fremdschluessel-, ID- oder Eltern-Metadaten aus der Nutzlast uebernehmen.
  SELECT jsonb_agg(jsonb_build_object('parzelle_id',r.parzelle_id,
    'mitglied_id',r.mitglied_id,'position_typ',r.position_typ,'menge',r.menge,
    'einheit',r.einheit,'preis',r.preis,'betrag',r.betrag,'bemerkung',r.bemerkung)
    ORDER BY e.ord) INTO v_wanted
  FROM jsonb_array_elements(p_positionen) WITH ORDINALITY AS e(item, ord)
  CROSS JOIN LATERAL jsonb_to_record(e.item) AS r(
    parzelle_id bigint, mitglied_id bigint, position_typ text,
    menge numeric, einheit text, preis numeric, betrag numeric,
    bemerkung text);
  IF v_wanted IS NULL THEN
    RAISE EXCEPTION 'Keine gueltigen Positionen' USING ERRCODE='22023';
  END IF;
  SELECT COALESCE(jsonb_agg(jsonb_build_object('parzelle_id',p.parzelle_id,
    'mitglied_id',p.mitglied_id,'position_typ',p.position_typ,'menge',p.menge,
    'einheit',p.einheit,'preis',p.preis,'betrag',p.betrag,'bemerkung',p.bemerkung)
    ORDER BY p.id),'[]'::jsonb) INTO v_current
  FROM public.jahresabschluss_position p
  WHERE p.jahresabschluss_id=p_jahresabschluss_id;
  IF v_current = v_wanted THEN
    RETURN jsonb_array_length(v_wanted);
  END IF;
  DELETE FROM public.jahresabschluss_position
    WHERE jahresabschluss_id=p_jahresabschluss_id;
  INSERT INTO public.jahresabschluss_position
    (jahresabschluss_id,parzelle_id,mitglied_id,position_typ,menge,einheit,
     preis,betrag,bemerkung)
  SELECT p_jahresabschluss_id, r.parzelle_id,r.mitglied_id,r.position_typ,
     r.menge,r.einheit,r.preis,r.betrag,r.bemerkung
  FROM jsonb_array_elements(p_positionen) WITH ORDINALITY AS e(item, ord)
  CROSS JOIN LATERAL jsonb_to_record(e.item) AS r(
    parzelle_id bigint, mitglied_id bigint, position_typ text,
    menge numeric, einheit text, preis numeric, betrag numeric,
    bemerkung text)
  ORDER BY e.ord;
  GET DIAGNOSTICS v_count = ROW_COUNT;
  RETURN v_count;
END
$fn$;
REVOKE ALL ON FUNCTION public.fr206_replace_jahresabschluss_positionen(bigint,jsonb)
  FROM PUBLIC, anon;
GRANT EXECUTE ON FUNCTION public.fr206_replace_jahresabschluss_positionen(bigint,jsonb)
  TO authenticated;

-- Neue Triggerfunktionen sind keine oeffentlichen RPC-Endpunkte.
REVOKE ALL ON FUNCTION public.fr206_guard_snapshot_child() FROM PUBLIC, anon, authenticated;
REVOKE ALL ON FUNCTION public.fr206_guard_finalized_source() FROM PUBLIC, anon, authenticated;

-- WICHTIG: keine UPDATEs auf fachrecht, hauptrolle, zusatzrolle oder Testkonten.
