-- AP-02 Abschluss-Nachtrag: letzte direkte Legacy-Vereinsrollenprüfung beseitigen.
-- Kein automatischer Rechtetransfer. Geschuetzte DemoDB-Adminidentität bleibt erhalten.
DO $pre$ BEGIN
 IF (SELECT count(*) FROM pg_policies WHERE schemaname='public'
  AND (coalesce(qual,'')||coalesce(with_check,'')) ~* 'is_kgv_admin_or_vorstand[(]')<>2
 THEN RAISE EXCEPTION 'Unerwarteter Ausgangsstand der Rollen-Policies'; END IF;
 IF (SELECT count(*) FROM public.fachrecht WHERE code IN ('gesetzliche_vertreter.verwalten',
  'verein.konfiguration','vereinsdaten.alle_lesen') AND aktiv)<>3
 THEN RAISE EXCEPTION 'Fachrechte fehlen oder sind gesperrt'; END IF;
END $pre$;

-- Andere gesetzliche Vertreter bleiben durch die allgemeine mgv_read-Policy
-- weiterhin nur mit mitglied.lesen oder eigener berechtigter Beziehung lesbar.
ALTER POLICY mitglied_gesetzlicher_vertreter_select_admin_vorstand
 ON public.mitglied_gesetzlicher_vertreter
 USING (public.ap02_has_effective_right('gesetzliche_vertreter.verwalten'));

ALTER POLICY vereinskonfiguration_select_admin_vorstand
 ON public.vereinskonfiguration
 USING (public.ap02_has_effective_right('verein.konfiguration')
        OR public.ap02_has_effective_right('vereinsdaten.alle_lesen'));

CREATE OR REPLACE FUNCTION public.is_kgv_admin_or_vorstand()
RETURNS boolean LANGUAGE sql STABLE SECURITY DEFINER
SET search_path TO pg_catalog,public
AS $fn$ SELECT false $fn$;
REVOKE ALL ON FUNCTION public.is_kgv_admin_or_vorstand() FROM PUBLIC,anon,authenticated;

DO $post$ BEGIN
 IF EXISTS(SELECT 1 FROM pg_policies WHERE schemaname='public'
  AND (coalesce(qual,'')||coalesce(with_check,'')) ~*
  'has_any_effective_permission[(]|has_effective_permission[(]|is_kgv_admin[(]|is_kgv_admin_or_vorstand[(]|is_productive_admin_or_vorstand[(]|is_admin[(]|is_admin_or_vorstand[(]') THEN
 RAISE EXCEPTION 'Legacy-RLS-Berechtigung verblieben'; END IF;
 IF has_function_privilege('authenticated','public.is_kgv_admin_or_vorstand()','EXECUTE')
    OR has_function_privilege('anon','public.is_kgv_admin_or_vorstand()','EXECUTE') THEN
 RAISE EXCEPTION 'Altrollen-Hilfe ist noch aufrufbar'; END IF;
END $post$;;
