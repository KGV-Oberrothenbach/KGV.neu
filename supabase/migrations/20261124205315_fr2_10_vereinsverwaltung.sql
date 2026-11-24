-- FR2-10 Vereinsverwaltung / Fachrechte 2.0
-- CANDIDATE ONLY: DO NOT APPLY WITHOUT FULL DEMODB BACKUP, ROLLBACK TEST,
-- DRY RUN AND AUTHORISED LOCAL EXECUTION. NEVER APPLY TO PRODUCTION.
-- Base: main 77357c8209c08b9de5b980bfc6401f49dfcbf198; Demo 289 migrations.
-- The migration deliberately leaves all existing business records unchanged.

DO $fr210$
BEGIN
  IF (SELECT count(*) FROM public.fachrecht
      WHERE code IN ('termin.verwalten','bekanntmachung.verwalten',
        'vereinsfunktion.verwalten','verein.konfiguration','saison.verwalten')
        AND aktiv IS TRUE) <> 5 THEN
    RAISE EXCEPTION 'FR2-10: Fachrechtekatalog unvollstaendig' USING ERRCODE = '23514';
  END IF;
END
$fr210$;

-- TRUNCATE is not covered by row-level security. Protect every FR2-10 table.
REVOKE TRUNCATE, TRIGGER, REFERENCES ON TABLE
  public.termin, public.bekanntmachung,
  public.vereinsfunktion, public.vereinsfunktion_besetzung,
  public.vereinsfunktion_vertretung, public.vereinsfunktion_verwendung,
  public.vereinskonfiguration, public.saison
  FROM PUBLIC, anon, authenticated;

-- No write capability for anonymous clients, even if future policies change.
REVOKE INSERT, UPDATE, DELETE ON TABLE
  public.termin, public.bekanntmachung,
  public.vereinsfunktion, public.vereinsfunktion_besetzung,
  public.vereinsfunktion_vertretung, public.vereinsfunktion_verwendung,
  public.vereinskonfiguration, public.saison
  FROM PUBLIC, anon;

-- Hard delete is not part of appointment / announcement lifecycle, nor the
-- historical occupancy / representative / season / club configuration model.
REVOKE DELETE ON TABLE
  public.termin, public.bekanntmachung,
  public.vereinsfunktion, public.vereinsfunktion_besetzung,
  public.vereinsfunktion_vertretung,
  public.vereinskonfiguration, public.saison
  FROM authenticated;

-- Existing MAUI workflow intentionally supports removing an *assignment of
-- a usage purpose*; its permission is independent from deleting an office.
-- Keep DELETE for vereinsfunktion_verwendung behind an explicit DELETE policy.
DROP POLICY IF EXISTS vereinsfunktion_effective ON public.vereinsfunktion;
CREATE POLICY fr210_vf_select ON public.vereinsfunktion FOR SELECT TO authenticated
  USING (public.ap02_has_effective_right('vereinsfunktion.verwalten'));
CREATE POLICY fr210_vf_insert ON public.vereinsfunktion FOR INSERT TO authenticated
  WITH CHECK (public.ap02_has_effective_right('vereinsfunktion.verwalten'));
CREATE POLICY fr210_vf_update ON public.vereinsfunktion FOR UPDATE TO authenticated
  USING (public.ap02_has_effective_right('vereinsfunktion.verwalten'))
  WITH CHECK (public.ap02_has_effective_right('vereinsfunktion.verwalten'));

DROP POLICY IF EXISTS vereinsfunktion_besetzung_effective ON public.vereinsfunktion_besetzung;
CREATE POLICY fr210_vf_besetzung_select ON public.vereinsfunktion_besetzung FOR SELECT TO authenticated
  USING (public.ap02_has_effective_right('vereinsfunktion.verwalten'));
CREATE POLICY fr210_vf_besetzung_insert ON public.vereinsfunktion_besetzung FOR INSERT TO authenticated
  WITH CHECK (public.ap02_has_effective_right('vereinsfunktion.verwalten'));
CREATE POLICY fr210_vf_besetzung_update ON public.vereinsfunktion_besetzung FOR UPDATE TO authenticated
  USING (public.ap02_has_effective_right('vereinsfunktion.verwalten'))
  WITH CHECK (public.ap02_has_effective_right('vereinsfunktion.verwalten'));

DROP POLICY IF EXISTS vereinsfunktion_vertretung_effective ON public.vereinsfunktion_vertretung;
CREATE POLICY fr210_vf_vertretung_select ON public.vereinsfunktion_vertretung FOR SELECT TO authenticated
  USING (public.ap02_has_effective_right('vereinsfunktion.verwalten'));
CREATE POLICY fr210_vf_vertretung_insert ON public.vereinsfunktion_vertretung FOR INSERT TO authenticated
  WITH CHECK (public.ap02_has_effective_right('vereinsfunktion.verwalten'));
CREATE POLICY fr210_vf_vertretung_update ON public.vereinsfunktion_vertretung FOR UPDATE TO authenticated
  USING (public.ap02_has_effective_right('vereinsfunktion.verwalten'))
  WITH CHECK (public.ap02_has_effective_right('vereinsfunktion.verwalten'));

DROP POLICY IF EXISTS vereinsfunktion_verwendung_effective ON public.vereinsfunktion_verwendung;
CREATE POLICY fr210_vf_verwendung_select ON public.vereinsfunktion_verwendung FOR SELECT TO authenticated
  USING (public.ap02_has_effective_right('vereinsfunktion.verwalten'));
CREATE POLICY fr210_vf_verwendung_insert ON public.vereinsfunktion_verwendung FOR INSERT TO authenticated
  WITH CHECK (public.ap02_has_effective_right('vereinsfunktion.verwalten'));
CREATE POLICY fr210_vf_verwendung_update ON public.vereinsfunktion_verwendung FOR UPDATE TO authenticated
  USING (public.ap02_has_effective_right('vereinsfunktion.verwalten'))
  WITH CHECK (public.ap02_has_effective_right('vereinsfunktion.verwalten'));
CREATE POLICY fr210_vf_verwendung_delete ON public.vereinsfunktion_verwendung FOR DELETE TO authenticated
  USING (public.ap02_has_effective_right('vereinsfunktion.verwalten'));

-- Independent seasonal authority. Existing public saison_select_authenticated
-- remains: viewing prices / season labels does not imply edit permission.
DROP POLICY IF EXISTS saison_effective_configuration ON public.saison;
CREATE POLICY fr210_saison_insert ON public.saison FOR INSERT TO authenticated
  WITH CHECK (public.ap02_has_effective_right('saison.verwalten'));
CREATE POLICY fr210_saison_update ON public.saison FOR UPDATE TO authenticated
  USING (public.ap02_has_effective_right('saison.verwalten'))
  WITH CHECK (public.ap02_has_effective_right('saison.verwalten'));

-- Club configuration: no physical delete; distinct right already governs
-- SELECT (plus global read), INSERT and UPDATE. Preserve these policies.
DROP POLICY IF EXISTS vereinskonfiguration_delete_admin ON public.vereinskonfiguration;

-- A separate trigger protects historic seasonal calculation inputs even if a
-- privileged future RPC accidentally bypasses RLS. The guard is independent
-- of browser-side validation and avoids incorrectly using SQLSTATE 40001.
CREATE OR REPLACE FUNCTION public.fr210_guard_saison_history()
RETURNS trigger
LANGUAGE plpgsql SECURITY DEFINER SET search_path = ''
AS $fr210$
DECLARE
  v_current_year integer := EXTRACT(YEAR FROM (now() AT TIME ZONE 'Europe/Berlin'))::integer;
  v_closing_status text;
BEGIN
  IF TG_OP = 'DELETE' THEN
    RAISE EXCEPTION 'FR2-10: Saisons duerfen nicht geloescht werden'
      USING ERRCODE='42501';
  END IF;

  IF TG_OP = 'UPDATE' THEN
    IF NEW.id IS DISTINCT FROM OLD.id OR NEW.jahr IS DISTINCT FROM OLD.jahr THEN
      RAISE EXCEPTION 'FR2-10: Saisonidentitaet und Jahr sind unveraenderlich'
        USING ERRCODE='23514';
    END IF;
    IF OLD.jahr < v_current_year THEN
      RAISE EXCEPTION 'FR2-10: Vergangene Saison bleibt unveraenderlich'
        USING ERRCODE='23514';
    END IF;
    -- Protect inputs once the closing reached a calculated or final state.
    -- Lock the current closing row even while still in progress; this
    -- serializes season changes against updates to an existing closing.
    -- A concurrent *new* closing still needs a separate concurrency E2E test.
    SELECT ja.status INTO v_closing_status FROM public.jahresabschluss ja
      WHERE ja.saison_id=OLD.id FOR SHARE;
    IF v_closing_status IN ('berechnet','geprueft','abgeschlossen') THEN
      RAISE EXCEPTION 'FR2-10: Saison mit berechnetem/abgeschlossenem Jahresabschluss gesperrt'
        USING ERRCODE='23514';
    END IF;
  END IF;
  RETURN NEW;
END
$fr210$;
REVOKE ALL ON FUNCTION public.fr210_guard_saison_history() FROM PUBLIC, anon, authenticated;
DROP TRIGGER IF EXISTS trg_fr210_guard_saison_history ON public.saison;
CREATE TRIGGER trg_fr210_guard_saison_history
BEFORE UPDATE OR DELETE ON public.saison
FOR EACH ROW EXECUTE FUNCTION public.fr210_guard_saison_history();

-- Historical functions / occupancies / representations must not vanish under
-- a direct SQL call. A usage-purpose assignment is NOT a historical occupancy
-- and can still be removed by authorized admins as intended.
CREATE OR REPLACE FUNCTION public.fr210_block_function_history_delete()
RETURNS trigger
LANGUAGE plpgsql SECURITY DEFINER SET search_path = ''
AS $fr210$
BEGIN
  RAISE EXCEPTION 'FR2-10: Historische Vereinsfunktionen und Besetzungen nicht physisch loeschen'
    USING ERRCODE='42501';
END
$fr210$;
REVOKE ALL ON FUNCTION public.fr210_block_function_history_delete() FROM PUBLIC, anon, authenticated;
DROP TRIGGER IF EXISTS trg_fr210_vf_block_delete ON public.vereinsfunktion;
CREATE TRIGGER trg_fr210_vf_block_delete BEFORE DELETE ON public.vereinsfunktion
  FOR EACH ROW EXECUTE FUNCTION public.fr210_block_function_history_delete();
DROP TRIGGER IF EXISTS trg_fr210_vf_besetzung_block_delete ON public.vereinsfunktion_besetzung;
CREATE TRIGGER trg_fr210_vf_besetzung_block_delete BEFORE DELETE ON public.vereinsfunktion_besetzung
  FOR EACH ROW EXECUTE FUNCTION public.fr210_block_function_history_delete();
DROP TRIGGER IF EXISTS trg_fr210_vf_vertretung_block_delete ON public.vereinsfunktion_vertretung;
CREATE TRIGGER trg_fr210_vf_vertretung_block_delete BEFORE DELETE ON public.vereinsfunktion_vertretung
  FOR EACH ROW EXECUTE FUNCTION public.fr210_block_function_history_delete();

-- Every operation above is DDL-only. No UPDATE/DELETE/INSERT to business data.
