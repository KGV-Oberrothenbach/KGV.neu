-- AP-02 Abschlussfix: alle alten und indirekten RLS-Berechtigungswege ablösen.
-- AUSSCHLIESSLICH DemoDB. Keine Datenänderungen. Vorstandsmatrix bleibt unberührt.
-- Die Relation war vor diesem Reparaturpunkt nur remote vorhanden und fehlte
-- in der Repository-Historie. IF NOT EXISTS wahrt vorhandene Installationen.
CREATE TABLE IF NOT EXISTS public.vereinskonfiguration (
  id bigserial PRIMARY KEY,
  aktiv boolean NOT NULL DEFAULT true,
  vereinsname text NOT NULL,
  kurzname text,
  registerangabe text,
  strasse text,
  plz text,
  ort text,
  standard_email text,
  standard_telefon text,
  website text,
  kontoinhaber text,
  iban text,
  bic text,
  bankname text,
  verwendungszweck_mitgliedsantrag text,
  verwendungszweck_pachtvertrag text,
  dokument_ort text,
  standard_hinweistext text,
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now(),
  datenschutz_text text,
  datenschutz_version text,
  datenschutz_stand date
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_vereinskonfiguration_nur_eine_aktive
  ON public.vereinskonfiguration(aktiv) WHERE aktiv = true;
ALTER TABLE public.vereinskonfiguration ENABLE ROW LEVEL SECURITY;

CREATE OR REPLACE FUNCTION public.set_updated_at_vereinskonfiguration()
RETURNS trigger LANGUAGE plpgsql SET search_path = pg_catalog, public AS $fn$
BEGIN
  NEW.updated_at = now();
  RETURN NEW;
END
$fn$;
REVOKE ALL ON FUNCTION public.set_updated_at_vereinskonfiguration()
  FROM PUBLIC, anon, authenticated;

DROP TRIGGER IF EXISTS trg_vereinskonfiguration_updated_at
  ON public.vereinskonfiguration;
CREATE TRIGGER trg_vereinskonfiguration_updated_at
BEFORE UPDATE ON public.vereinskonfiguration
FOR EACH ROW EXECUTE FUNCTION public.set_updated_at_vereinskonfiguration();
DROP TRIGGER IF EXISTS trg_browser_edit_lock ON public.vereinskonfiguration;
CREATE TRIGGER trg_browser_edit_lock
BEFORE UPDATE ON public.vereinskonfiguration
FOR EACH ROW EXECUTE FUNCTION public.enforce_browser_edit_lock('vereinskonfiguration','id');

GRANT SELECT ON public.vereinskonfiguration TO anon;
GRANT SELECT, INSERT, UPDATE ON public.vereinskonfiguration TO authenticated;
GRANT ALL ON public.vereinskonfiguration TO service_role;
GRANT USAGE ON SEQUENCE public.vereinskonfiguration_id_seq
  TO anon, authenticated, service_role;

CREATE OR REPLACE FUNCTION public.is_kgv_admin_or_vorstand()
RETURNS boolean LANGUAGE sql STABLE SECURITY DEFINER
SET search_path = pg_catalog, public
AS $fn$ SELECT public.is_admin_or_vorstand() $fn$;
REVOKE ALL ON FUNCTION public.is_kgv_admin_or_vorstand()
  FROM PUBLIC, anon;
GRANT EXECUTE ON FUNCTION public.is_kgv_admin_or_vorstand()
  TO authenticated;

DROP POLICY IF EXISTS mitglied_gesetzlicher_vertreter_select_admin_vorstand
  ON public.mitglied_gesetzlicher_vertreter;
CREATE POLICY mitglied_gesetzlicher_vertreter_select_admin_vorstand
ON public.mitglied_gesetzlicher_vertreter FOR SELECT TO authenticated
USING (public.is_kgv_admin_or_vorstand());

DROP POLICY IF EXISTS vereinskonfiguration_select_admin_vorstand
  ON public.vereinskonfiguration;
CREATE POLICY vereinskonfiguration_select_admin_vorstand
ON public.vereinskonfiguration FOR SELECT TO authenticated
USING (public.is_kgv_admin_or_vorstand());

DO $pre$
DECLARE cnt integer;
BEGIN
 IF to_regprocedure('public.ap02_has_effective_right(text)') IS NULL OR
    (SELECT count(*) FROM public.fachrecht) <> 55 OR
    ((EXISTS (SELECT 1 FROM auth.users) OR
      EXISTS (SELECT 1 FROM public.app_user) OR
      EXISTS (SELECT 1 FROM public.mitglied) OR
      EXISTS (SELECT 1 FROM public.parzelle) OR
      EXISTS (SELECT 1 FROM public.pachtverhaeltnis) OR
      EXISTS (SELECT 1 FROM public.dokument) OR
      EXISTS (SELECT 1 FROM public.kommunikationsvorgang) OR
      EXISTS (SELECT 1 FROM public.zaehler) OR
      EXISTS (SELECT 1 FROM public.arbeitsstunde) OR
      EXISTS (SELECT 1 FROM public.wartungsvertraege) OR
      EXISTS (SELECT 1 FROM public.vereinsfunktion) OR
      EXISTS (SELECT 1 FROM public.vereinsfunktion_besetzung) OR
      EXISTS (SELECT 1 FROM public.vereinsfunktion_verwendung) OR
      EXISTS (SELECT 1 FROM public.vereinsfunktion_vertretung)) AND NOT EXISTS (SELECT 1 FROM public.protected_admin_identity)) OR
    EXISTS (
      SELECT 1 FROM public.protected_admin_identity p
      WHERE NOT EXISTS (SELECT 1 FROM public.app_user_zusatzrolle z
        WHERE z.user_id=p.user_id AND z.zusatzrolle_code='admin')
    ) OR
    EXISTS (
      SELECT a.user_id FROM public.app_user a WHERE a.is_protected = true
      EXCEPT
      SELECT p.user_id FROM public.protected_admin_identity p
    ) OR
    EXISTS (
      SELECT p.user_id FROM public.protected_admin_identity p
      EXCEPT
      SELECT a.user_id FROM public.app_user a WHERE a.is_protected = true
    ) THEN
   RAISE EXCEPTION 'AP-02 Basis unerwartet';
 END IF;
 IF EXISTS (
   SELECT 1 FROM public.fachrecht WHERE code IN (
     'vereinsfunktion.verwalten','mitglied.bearbeiten','mitglied.lesen',
     'begehung.verwalten','mitgliedschaft.verwalten','gesetzliche_vertreter.verwalten',
     'verein.konfiguration','ablesung.freigeben','zaehlerwechsel.verwalten'
   ) AND NOT aktiv
 ) THEN RAISE EXCEPTION 'Betroffene Fachrechte sind inaktiv'; END IF;
 WITH expected(tablename) AS (VALUES
   ('impressum_funktion_slot'),
   ('mitglied_saison'),
   ('parzellen_begehung_feststellung'),
   ('parzellen_protokoll'),
   ('parzellen_protokoll_ablesung'),
   ('wartungsvertraege'),
   ('mitglied_gesetzlicher_vertreter'),
   ('vereinskonfiguration'),
   ('zaehler_ablesung')
 )
 SELECT count(*) INTO cnt
 FROM expected e
 JOIN pg_class c ON c.relname=e.tablename
 JOIN pg_namespace n ON n.oid=c.relnamespace AND n.nspname='public'
 WHERE c.relkind IN ('r','p');
 IF cnt<>9 THEN RAISE EXCEPTION 'Erwartete 9 Policy-Zieltabellen, gefunden %',cnt; END IF;

 IF EXISTS (
   WITH expected(tablename,policyname) AS (VALUES
     ('impressum_funktion_slot','impressum_funktion_slot_admin_full'),
     ('mitglied_saison','mitglied_saison_admin_full'),
     ('mitglied_saison','mitglied_saison_select_own_or_admin'),
     ('parzellen_begehung_feststellung','parzellen_begehung_feststellung_admin_full'),
     ('parzellen_protokoll','parzellen_protokoll_admin_full'),
     ('parzellen_protokoll_ablesung','parzellen_protokoll_ablesung_admin_full'),
     ('wartungsvertraege','wartungsvertraege_admin_full'),
     ('wartungsvertraege','wartungsvertraege_select_assigned_or_admin'),
     ('mitglied_gesetzlicher_vertreter','mitglied_gesetzlicher_vertreter_delete_admin'),
     ('mitglied_gesetzlicher_vertreter','mitglied_gesetzlicher_vertreter_insert_admin'),
     ('mitglied_gesetzlicher_vertreter','mitglied_gesetzlicher_vertreter_update_admin'),
     ('vereinskonfiguration','vereinskonfiguration_delete_admin'),
     ('vereinskonfiguration','vereinskonfiguration_insert_admin'),
     ('vereinskonfiguration','vereinskonfiguration_update_admin'),
     ('zaehler_ablesung','zaehler_ablesung_update_effective_meter_permission')
   )
   SELECT 1 FROM pg_policies p
   WHERE p.schemaname='public'
     AND (coalesce(p.qual,'')||coalesce(p.with_check,'')) ~*
       'has_any_effective_permission[(]|is_kgv_admin[(]|is_productive_admin_or_vorstand[(]'
     AND NOT EXISTS (
       SELECT 1 FROM expected e
       WHERE e.tablename=p.tablename AND e.policyname=p.policyname
     )
 ) THEN RAISE EXCEPTION 'Unerwartete weitere Legacy-Policy vorhanden'; END IF;
END $pre$;

DROP POLICY IF EXISTS impressum_funktion_slot_admin_full ON public.impressum_funktion_slot;
CREATE POLICY impressum_funktion_slot_admin_full ON public.impressum_funktion_slot
 FOR ALL TO authenticated
 USING (public.ap02_has_effective_right('vereinsfunktion.verwalten'))
 WITH CHECK (public.ap02_has_effective_right('vereinsfunktion.verwalten'));

DROP POLICY IF EXISTS mitglied_saison_admin_full ON public.mitglied_saison;
CREATE POLICY mitglied_saison_admin_full ON public.mitglied_saison
 FOR ALL TO authenticated
 USING (public.ap02_has_effective_right('mitglied.bearbeiten'))
 WITH CHECK (public.ap02_has_effective_right('mitglied.bearbeiten'));
DROP POLICY IF EXISTS mitglied_saison_select_own_or_admin ON public.mitglied_saison;
CREATE POLICY mitglied_saison_select_own_or_admin ON public.mitglied_saison
 FOR SELECT TO authenticated
 USING ((mitglied_id = public.current_mitglied_id()) OR public.ap02_has_effective_right('mitglied.lesen'));

DROP POLICY IF EXISTS parzellen_begehung_feststellung_admin_full ON public.parzellen_begehung_feststellung;
CREATE POLICY parzellen_begehung_feststellung_admin_full ON public.parzellen_begehung_feststellung
 FOR ALL TO authenticated
 USING (public.ap02_has_effective_right('begehung.verwalten'))
 WITH CHECK (public.ap02_has_effective_right('begehung.verwalten'));
DROP POLICY IF EXISTS parzellen_protokoll_admin_full ON public.parzellen_protokoll;
CREATE POLICY parzellen_protokoll_admin_full ON public.parzellen_protokoll
 FOR ALL TO authenticated
 USING (public.ap02_has_effective_right('begehung.verwalten'))
 WITH CHECK (public.ap02_has_effective_right('begehung.verwalten'));
DROP POLICY IF EXISTS parzellen_protokoll_ablesung_admin_full ON public.parzellen_protokoll_ablesung;
CREATE POLICY parzellen_protokoll_ablesung_admin_full ON public.parzellen_protokoll_ablesung
 FOR ALL TO authenticated
 USING (public.ap02_has_effective_right('begehung.verwalten'))
 WITH CHECK (public.ap02_has_effective_right('begehung.verwalten'));

DROP POLICY IF EXISTS wartungsvertraege_admin_full ON public.wartungsvertraege;
CREATE POLICY wartungsvertraege_admin_full ON public.wartungsvertraege
 FOR ALL TO authenticated
 USING (public.ap02_has_effective_right('mitgliedschaft.verwalten'))
 WITH CHECK (public.ap02_has_effective_right('mitgliedschaft.verwalten'));
DROP POLICY IF EXISTS wartungsvertraege_select_assigned_or_admin ON public.wartungsvertraege;
CREATE POLICY wartungsvertraege_select_assigned_or_admin ON public.wartungsvertraege
 FOR SELECT TO authenticated
 USING (public.ap02_has_effective_right('mitgliedschaft.verwalten') OR
 EXISTS(SELECT 1 FROM public.wartungsvertrag_zuordnungen wz
 WHERE wz.wartungsvertrag_id = wartungsvertraege.id
 AND wz.hauptmitglied_id = public.current_mitglied_id()));

DROP POLICY IF EXISTS mitglied_gesetzlicher_vertreter_delete_admin ON public.mitglied_gesetzlicher_vertreter;
CREATE POLICY mitglied_gesetzlicher_vertreter_delete_admin ON public.mitglied_gesetzlicher_vertreter
 FOR DELETE TO authenticated
 USING (public.ap02_has_effective_right('gesetzliche_vertreter.verwalten'));
DROP POLICY IF EXISTS mitglied_gesetzlicher_vertreter_insert_admin ON public.mitglied_gesetzlicher_vertreter;
CREATE POLICY mitglied_gesetzlicher_vertreter_insert_admin ON public.mitglied_gesetzlicher_vertreter
 FOR INSERT TO authenticated
 WITH CHECK (public.ap02_has_effective_right('gesetzliche_vertreter.verwalten'));
DROP POLICY IF EXISTS mitglied_gesetzlicher_vertreter_update_admin ON public.mitglied_gesetzlicher_vertreter;
CREATE POLICY mitglied_gesetzlicher_vertreter_update_admin ON public.mitglied_gesetzlicher_vertreter
 FOR UPDATE TO authenticated
 USING (public.ap02_has_effective_right('gesetzliche_vertreter.verwalten'))
 WITH CHECK (public.ap02_has_effective_right('gesetzliche_vertreter.verwalten'));

DROP POLICY IF EXISTS vereinskonfiguration_delete_admin ON public.vereinskonfiguration;
CREATE POLICY vereinskonfiguration_delete_admin ON public.vereinskonfiguration
 FOR DELETE TO authenticated
 USING (public.ap02_has_effective_right('verein.konfiguration'));
DROP POLICY IF EXISTS vereinskonfiguration_insert_admin ON public.vereinskonfiguration;
CREATE POLICY vereinskonfiguration_insert_admin ON public.vereinskonfiguration
 FOR INSERT TO authenticated
 WITH CHECK (public.ap02_has_effective_right('verein.konfiguration'));
DROP POLICY IF EXISTS vereinskonfiguration_update_admin ON public.vereinskonfiguration;
CREATE POLICY vereinskonfiguration_update_admin ON public.vereinskonfiguration
 FOR UPDATE TO authenticated
 USING (public.ap02_has_effective_right('verein.konfiguration'))
 WITH CHECK (public.ap02_has_effective_right('verein.konfiguration'));

DROP POLICY IF EXISTS zaehler_ablesung_update_effective_meter_permission ON public.zaehler_ablesung;
CREATE POLICY zaehler_ablesung_update_effective_meter_permission ON public.zaehler_ablesung
 FOR UPDATE TO authenticated
 USING (public.ap02_has_effective_right('ablesung.freigeben') OR
        public.ap02_has_effective_right('zaehlerwechsel.verwalten'))
 WITH CHECK (public.ap02_has_effective_right('ablesung.freigeben') OR
             public.ap02_has_effective_right('zaehlerwechsel.verwalten'));

-- Althilfe nicht mehr als administrative Autorisierungsabkürzung nutzbar.
CREATE OR REPLACE FUNCTION public.is_kgv_admin()
 RETURNS boolean LANGUAGE sql STABLE SECURITY DEFINER SET search_path TO pg_catalog,public
 AS $fn$ SELECT false $fn$;
REVOKE ALL ON FUNCTION public.is_kgv_admin() FROM PUBLIC,anon,authenticated;

DO $post$
DECLARE cnt integer;
BEGIN
 SELECT count(*) INTO cnt FROM pg_policies WHERE schemaname='public'
 AND (coalesce(qual,'')||coalesce(with_check,'')) ~*
    'has_any_effective_permission[(]|has_effective_permission[(]|is_kgv_admin[(]|is_productive_admin_or_vorstand[(]|is_admin[(]|is_admin_or_vorstand[(]';
 IF cnt<>0 THEN RAISE EXCEPTION 'Noch % alte RLS-Prüfungen',cnt; END IF;
 IF has_function_privilege('authenticated','public.is_kgv_admin()','EXECUTE')
 OR has_function_privilege('anon','public.is_kgv_admin()','EXECUTE') THEN
 RAISE EXCEPTION 'Legacy administrative helper still exposed'; END IF;
END $post$;;
