-- AP-02 Abschlussfix: alle alten und indirekten RLS-Berechtigungswege ablösen.
-- AUSSCHLIESSLICH DemoDB. Keine Datenänderungen. Vorstandsmatrix bleibt unberührt.
DO $pre$
DECLARE cnt integer;
BEGIN
 IF to_regprocedure('public.ap02_has_effective_right(text)') IS NULL OR
    (SELECT count(*) FROM public.fachrecht) <> 55 OR
    (SELECT count(*) FROM public.protected_admin_identity) < 1 THEN
   RAISE EXCEPTION 'AP-02 Basis unerwartet';
 END IF;
 IF EXISTS (
   SELECT 1 FROM public.fachrecht WHERE code IN (
     'vereinsfunktion.verwalten','mitglied.bearbeiten','mitglied.lesen',
     'begehung.verwalten','mitgliedschaft.verwalten','gesetzliche_vertreter.verwalten',
     'verein.konfiguration','ablesung.freigeben','zaehlerwechsel.verwalten'
   ) AND NOT aktiv
 ) THEN RAISE EXCEPTION 'Betroffene Fachrechte sind inaktiv'; END IF;
 SELECT count(*) INTO cnt FROM pg_policies WHERE schemaname='public'
 AND (coalesce(qual,'')||coalesce(with_check,'')) ~*
    'has_any_effective_permission[(]|is_kgv_admin[(]|is_productive_admin_or_vorstand[(]';
 IF cnt<>15 THEN RAISE EXCEPTION 'Erwartete 15 Alt-Policies, gefunden %',cnt; END IF;
END $pre$;

ALTER POLICY impressum_funktion_slot_admin_full ON public.impressum_funktion_slot
 USING (public.ap02_has_effective_right('vereinsfunktion.verwalten'))
 WITH CHECK (public.ap02_has_effective_right('vereinsfunktion.verwalten'));

ALTER POLICY mitglied_saison_admin_full ON public.mitglied_saison
 USING (public.ap02_has_effective_right('mitglied.bearbeiten'))
 WITH CHECK (public.ap02_has_effective_right('mitglied.bearbeiten'));
ALTER POLICY mitglied_saison_select_own_or_admin ON public.mitglied_saison
 USING ((mitglied_id = public.current_mitglied_id()) OR public.ap02_has_effective_right('mitglied.lesen'));

ALTER POLICY parzellen_begehung_feststellung_admin_full ON public.parzellen_begehung_feststellung
 USING (public.ap02_has_effective_right('begehung.verwalten'))
 WITH CHECK (public.ap02_has_effective_right('begehung.verwalten'));
ALTER POLICY parzellen_protokoll_admin_full ON public.parzellen_protokoll
 USING (public.ap02_has_effective_right('begehung.verwalten'))
 WITH CHECK (public.ap02_has_effective_right('begehung.verwalten'));
ALTER POLICY parzellen_protokoll_ablesung_admin_full ON public.parzellen_protokoll_ablesung
 USING (public.ap02_has_effective_right('begehung.verwalten'))
 WITH CHECK (public.ap02_has_effective_right('begehung.verwalten'));

ALTER POLICY wartungsvertraege_admin_full ON public.wartungsvertraege
 USING (public.ap02_has_effective_right('mitgliedschaft.verwalten'))
 WITH CHECK (public.ap02_has_effective_right('mitgliedschaft.verwalten'));
ALTER POLICY wartungsvertraege_select_assigned_or_admin ON public.wartungsvertraege
 USING (public.ap02_has_effective_right('mitgliedschaft.verwalten') OR
 EXISTS(SELECT 1 FROM public.wartungsvertrag_zuordnungen wz
 WHERE wz.wartungsvertrag_id = wartungsvertraege.id
 AND wz.hauptmitglied_id = public.current_mitglied_id()));

ALTER POLICY mitglied_gesetzlicher_vertreter_delete_admin ON public.mitglied_gesetzlicher_vertreter
 USING (public.ap02_has_effective_right('gesetzliche_vertreter.verwalten'));
ALTER POLICY mitglied_gesetzlicher_vertreter_insert_admin ON public.mitglied_gesetzlicher_vertreter
 WITH CHECK (public.ap02_has_effective_right('gesetzliche_vertreter.verwalten'));
ALTER POLICY mitglied_gesetzlicher_vertreter_update_admin ON public.mitglied_gesetzlicher_vertreter
 USING (public.ap02_has_effective_right('gesetzliche_vertreter.verwalten'))
 WITH CHECK (public.ap02_has_effective_right('gesetzliche_vertreter.verwalten'));

ALTER POLICY vereinskonfiguration_delete_admin ON public.vereinskonfiguration
 USING (public.ap02_has_effective_right('verein.konfiguration'));
ALTER POLICY vereinskonfiguration_insert_admin ON public.vereinskonfiguration
 WITH CHECK (public.ap02_has_effective_right('verein.konfiguration'));
ALTER POLICY vereinskonfiguration_update_admin ON public.vereinskonfiguration
 USING (public.ap02_has_effective_right('verein.konfiguration'))
 WITH CHECK (public.ap02_has_effective_right('verein.konfiguration'));

ALTER POLICY zaehler_ablesung_update_effective_meter_permission ON public.zaehler_ablesung
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
