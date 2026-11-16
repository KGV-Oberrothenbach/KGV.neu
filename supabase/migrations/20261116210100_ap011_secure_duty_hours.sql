-- KGV.neu / AP-01.1 / REVIDIERTER Entwurf B (NICHT ANGEWENDET)
-- Ziel: Pflichtstunden zentral wie bisher berechnen, aber nur eigene oder
-- explizit berechtigte fremde Mitgliederdaten ueber View/RPC ausgeben.
-- GitHub-main-Baseline: 348ede51753361a27ceb13d9de0f41ac82bc7c38
-- Live-Objektdefinitionsabgleich: 16.11.2026, Prod + Demo.
-- WICHTIG: Erst nach gesondertem SQL-Review und Testplanung anwenden.
-- Keine Veraenderung der bisherigen Fach-Berechnungsformel.
-- Die personenbezogene Mitgliederprojektion wird eigenstaendig autorisiert,
-- damit CanReadWorkHours auch ohne unnoetig breites SELECT auf `mitglied`
-- funktioniert. Saisonfilter bleiben auf Ebene der View (nicht in einer
-- parameterlosen Funktion ueber alle Saisons) fuer den Planner erreichbar.

-- Kontrollierter Aufruf der bestehenden Berechnungsfunktion.
-- SECURITY DEFINER ist hier nur deshalb noetig, damit die Summen aus der
-- bisherigen Datenbasis einschliesslich Nebenmitgliedern unveraendert bleiben;
-- VOR jeder Berechnung erfolgt die Zugriffspruefung. Die ungeschuetzte
-- Originalfunktion wird fuer externe Rollen anschliessend gesperrt.
BEGIN;

-- Ausschliesslich freigegebene Mitglieder-Metadaten fuer die zentrale
-- Stundenansicht. Diese Funktion gibt nie Adresse, E-Mail, Geburtsdatum etc. aus.
-- Direkte RPC-Nutzung bleibt denselben Scope-Regeln unterworfen.
CREATE OR REPLACE FUNCTION public.fn_pflichtstunden_sichtbare_mitglieder()
RETURNS TABLE (
  mitglied_id bigint,
  hauptmitglied_id bigint,
  name text,
  vorname text
)
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = public, pg_temp
AS $members$
  SELECT m.id, COALESCE(m.hauptmitglied_id, m.id), m.name, m.vorname
  FROM public.mitglied m
  WHERE
    auth.role() = 'service_role'
    OR (
      auth.uid() IS NOT NULL
      AND (
        m.id = public.current_mitglied_id()::bigint
        OR public.has_effective_permission(131072::bigint) -- CanReadWorkHours
        OR public.has_effective_permission(256::bigint)    -- CanManageWorkHours
      )
    );
$members$;

REVOKE ALL ON FUNCTION public.fn_pflichtstunden_sichtbare_mitglieder()
  FROM PUBLIC, anon, authenticated;
GRANT EXECUTE ON FUNCTION public.fn_pflichtstunden_sichtbare_mitglieder()
  TO authenticated, service_role;

CREATE OR REPLACE FUNCTION public.fn_berechne_pflichtstunden_status_authorized(
  p_mitglied_id bigint,
  p_saison_id bigint
)
RETURNS TABLE (
  hauptmitglied_id bigint,
  saison_id bigint,
  saison_jahr integer,
  regelgrund text,
  ist_befreit boolean,
  hat_wartungsvertrag boolean,
  wartungsvertrag_gutschrift_stunden numeric,
  altersbefreit boolean,
  eintritt_im_saisonjahr boolean,
  eintritt_zweites_halbjahr boolean,
  pflichtstunden_soll numeric,
  geleistete_stunden numeric,
  offene_stunden numeric,
  euro_pro_fehlstunde numeric,
  fehlbetrag numeric
)
LANGUAGE plpgsql
STABLE
SECURITY DEFINER
SET search_path = public, pg_temp
AS $function$
BEGIN
  IF auth.role() IS DISTINCT FROM 'service_role' THEN
    -- Ohne echte Anmeldung niemals personenbezogene Ergebnisse liefern.
    IF auth.uid() IS NULL THEN
      RETURN;
    END IF;

    -- CanReadWorkHours = 131072, CanManageWorkHours = 256.
    -- Eigene Pflichtstunden bleiben auch ohne Verwaltungsrecht sichtbar.
    IF p_mitglied_id IS DISTINCT FROM public.current_mitglied_id()::bigint
       AND NOT (
         public.has_effective_permission(131072::bigint)
         OR public.has_effective_permission(256::bigint)
       ) THEN
      RETURN;
    END IF;
  END IF;

  -- Alte Fachformel unveraendert, unter dem kontrollierten Funktionskontext.
  RETURN QUERY
    SELECT *
    FROM public.fn_berechne_pflichtstunden_status(p_mitglied_id, p_saison_id);
END;
$function$;

REVOKE ALL ON FUNCTION public.fn_berechne_pflichtstunden_status_authorized(bigint,bigint)
  FROM PUBLIC, anon, authenticated;
GRANT EXECUTE ON FUNCTION public.fn_berechne_pflichtstunden_status_authorized(bigint,bigint)
  TO authenticated, service_role;

-- Bestehende Spalten, Reihenfolge und Namen unveraendert beibehalten.
-- security_invoker: View besitzt keine automatische OWNER-RLS-Umgehung;
-- privilegierte Basisdaten sind ausschliesslich in zwei serverseitig streng
-- autorisierten Funktionen erreichbar.
-- Die Berechnungsfunktion selbst erzwingt die Zugriffskontrolle.
-- Kein security_barrier-Flag ohne Performance-Nachweis.
-- Funktions- und View-Filter schuetzen auch dann, wenn ein Benutzer
-- zwar Mitglieder suchen darf, aber keine fremden Pflichtstunden lesen.
CREATE OR REPLACE VIEW public.v_pflichtstunden_uebersicht
WITH (security_invoker = true)
AS
SELECT
  s.id AS saison_id,
  s.jahr,
  s.jahr AS saison_jahr,
  m.mitglied_id AS mitglied_id,
  m.hauptmitglied_id AS hauptmitglied_id,
  m.name,
  m.vorname,
  x.regelgrund,
  x.ist_befreit,
  x.hat_wartungsvertrag,
  x.wartungsvertrag_gutschrift_stunden,
  x.altersbefreit,
  x.eintritt_im_saisonjahr,
  x.eintritt_zweites_halbjahr,
  x.pflichtstunden_soll,
  x.geleistete_stunden,
  x.offene_stunden,
  x.euro_pro_fehlstunde,
  x.fehlbetrag
FROM public.saison s
CROSS JOIN public.fn_pflichtstunden_sichtbare_mitglieder() m
CROSS JOIN LATERAL public.fn_berechne_pflichtstunden_status_authorized(
  m.mitglied_id, s.id::bigint
) x
-- Zusaetzliche Scope-Kontrolle auf View-Ebene (Defense in Depth).
WHERE
  auth.role() = 'service_role'
  OR (
    auth.uid() IS NOT NULL
    AND (
      m.mitglied_id = public.current_mitglied_id()::bigint
      OR public.has_effective_permission(131072::bigint)
      OR public.has_effective_permission(256::bigint)
    )
  );

-- View: nur explizit authentifizierte Nutzer / Service-Role duerfen lesen.
REVOKE ALL PRIVILEGES ON TABLE public.v_pflichtstunden_uebersicht
  FROM PUBLIC, anon, authenticated, service_role;
GRANT SELECT ON TABLE public.v_pflichtstunden_uebersicht
  TO authenticated, service_role;

-- Direkten ungeschuetzten Rechen-RPC fuer Endnutzer schliessen.
-- Bestehende Datenbank-/Service-Role-Funktionen koennen die Originalformel
-- weiter intern verwenden; die Clients nutzen die autorisierte View.
REVOKE ALL ON FUNCTION public.fn_berechne_pflichtstunden_status(bigint,bigint)
  FROM PUBLIC, anon, authenticated;
GRANT EXECUTE ON FUNCTION public.fn_berechne_pflichtstunden_status(bigint,bigint)
  TO service_role;

COMMIT;
