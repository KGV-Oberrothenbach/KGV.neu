-- KGV.neu / AP-01.1 / REVIDIERTER Entwurf A (NICHT ANGEWENDET)
-- Ziel: Auslaufende Wartungsvertraege und ihre internen Folgeentscheidungen
-- nur mit wirksamer Verwaltungsberechtigung oder offizieller
-- Wartungsvertrags-Entscheidungsfunktion ausgeben.
-- GitHub-main-Baseline: 348ede51753361a27ceb13d9de0f41ac82bc7c38
-- Live-Objektdefinitionsabgleich: 16.11.2026, Prod + Demo.
-- WICHTIG: In das Repository erst mit `supabase migration new ...`
-- und nach expliziter SQL-Abnahme uebernehmen. Keine bisherigen Daten aendern.

BEGIN;

CREATE OR REPLACE FUNCTION public.get_auslaufende_wartungsvertrag_zuordnungen()
RETURNS TABLE (
  zuordnung_id bigint,
  hauptmitglied_id bigint,
  mitglied_name text,
  wartungsvertrag_id bigint,
  wartungsvertrag_titel text,
  gueltig_ab date,
  gueltig_bis date,
  entscheidung text,
  folgeentscheidung_id bigint,
  folgezuordnung_id bigint,
  zurueckgenommen_am timestamptz,
  can_decide boolean
)
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = public, pg_temp
AS $function$
  SELECT
    z.id,
    z.hauptmitglied_id,
    NULLIF(BTRIM(CONCAT_WS(' ', m.vorname, m.name)), ''),
    z.wartungsvertrag_id,
    w.titel,
    z.gueltig_ab,
    z.gueltig_bis,
    e.entscheidung,
    e.id,
    e.folgezuordnung_id,
    e.zurueckgenommen_am,
    (
      public.ist_wartungsvertrag_entscheidung_berechtigt()
      AND z.gueltig_bis > CURRENT_DATE
      AND (e.id IS NULL OR e.zurueckgenommen_am IS NOT NULL)
    )
  FROM public.wartungsvertrag_zuordnungen z
  JOIN public.mitglied m ON m.id = z.hauptmitglied_id
  JOIN public.wartungsvertraege w ON w.id = z.wartungsvertrag_id
  LEFT JOIN LATERAL (
    SELECT *
    FROM public.wartungsvertrag_folgeentscheidung x
    WHERE x.vorgaenger_zuordnung_id = z.id
    ORDER BY x.created_at DESC
    LIMIT 1
  ) e ON TRUE
  WHERE
    (
      auth.role() = 'service_role'
      OR (
        auth.uid() IS NOT NULL
        AND (
          public.has_effective_permission(4::bigint) -- CanEditAllMembers
          OR public.ist_wartungsvertrag_entscheidung_berechtigt()
        )
      )
    )
    AND z.gueltig_bis IS NOT NULL
    AND z.gueltig_bis >= CURRENT_DATE
    AND NOT EXISTS (
      SELECT 1
      FROM public.wartungsvertrag_zuordnungen f
      WHERE f.vorgaenger_zuordnung_id = z.id
    )
  ORDER BY z.gueltig_bis ASC, z.id ASC;
$function$;

-- Der Detail-RPC zeigt u.a. interne Gruende und Ruecknahmebegruendungen.
-- Er muss dieselbe serverseitige Zugriffsgrenze besitzen.
CREATE OR REPLACE FUNCTION public.get_wartungsvertrag_folgeentscheidung_detail(
  p_vorgaenger_zuordnung_id bigint
)
RETURNS TABLE (
  vorgaenger_zuordnung_id bigint,
  hauptmitglied_id bigint,
  vertragspartei_mitglied_id bigint,
  wartungsvertrag_id bigint,
  gueltig_ab date,
  gueltig_bis date,
  folgeentscheidung_id bigint,
  entscheidung text,
  entscheidungsdatum date,
  vorstandsbeschlussdatum date,
  initiator text,
  begruendung text,
  folgezuordnung_id bigint,
  folge_wartungsvertrag_id bigint,
  folge_wartungsvertrag_titel text,
  folge_gueltig_ab date,
  folge_gueltig_bis date,
  kommunikationsvorgang_id bigint,
  workflow_id bigint,
  workflow_art text,
  workflow_status text,
  dokument_id bigint,
  zurueckgenommen_am timestamptz,
  ruecknahmebegruendung text,
  can_decide boolean,
  can_withdraw_nichtverlaengerung boolean
)
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = public, pg_temp
AS $function$
  SELECT
    z.id,
    z.hauptmitglied_id,
    z.vertragspartei_mitglied_id,
    z.wartungsvertrag_id,
    z.gueltig_ab,
    z.gueltig_bis,
    e.id,
    e.entscheidung,
    e.entscheidungsdatum,
    e.vorstandsbeschlussdatum,
    e.initiator,
    e.begruendung,
    e.folgezuordnung_id,
    f.wartungsvertrag_id,
    fw.titel,
    f.gueltig_ab,
    f.gueltig_bis,
    e.kommunikationsvorgang_id,
    wd.id,
    wd.workflow_art,
    wd.status,
    wd.dokument_id,
    e.zurueckgenommen_am,
    e.ruecknahmebegruendung,
    (
      public.ist_wartungsvertrag_entscheidung_berechtigt()
      AND z.gueltig_bis > CURRENT_DATE
      AND (e.id IS NULL OR e.zurueckgenommen_am IS NOT NULL)
    ),
    (
      public.ist_wartungsvertrag_entscheidung_berechtigt()
      AND e.entscheidung = 'nicht_verlaengern'
      AND e.zurueckgenommen_am IS NULL
      AND z.gueltig_bis > CURRENT_DATE
    )
  FROM public.wartungsvertrag_zuordnungen z
  LEFT JOIN public.wartungsvertrag_folgeentscheidung e
    ON e.vorgaenger_zuordnung_id = z.id
  LEFT JOIN public.wartungsvertrag_zuordnungen f
    ON f.id = e.folgezuordnung_id
  LEFT JOIN public.wartungsvertraege fw
    ON fw.id = f.wartungsvertrag_id
  LEFT JOIN public.wartungsvertrag_dokument_workflow wd
    ON wd.folgeentscheidung_id = e.id
  WHERE
    z.id = p_vorgaenger_zuordnung_id
    AND (
      auth.role() = 'service_role'
      OR (
        auth.uid() IS NOT NULL
        AND (
          public.has_effective_permission(4::bigint) -- CanEditAllMembers
          OR public.ist_wartungsvertrag_entscheidung_berechtigt()
        )
      )
    );
$function$;

-- PostgreSQL erlaubt EXECUTE auf Funktionen sonst oft implizit via PUBLIC.
REVOKE ALL ON FUNCTION public.get_auslaufende_wartungsvertrag_zuordnungen()
  FROM PUBLIC, anon, authenticated;
REVOKE ALL ON FUNCTION public.get_wartungsvertrag_folgeentscheidung_detail(bigint)
  FROM PUBLIC, anon, authenticated;
GRANT EXECUTE ON FUNCTION public.get_auslaufende_wartungsvertrag_zuordnungen()
  TO authenticated, service_role;
GRANT EXECUTE ON FUNCTION public.get_wartungsvertrag_folgeentscheidung_detail(bigint)
  TO authenticated, service_role;

-- AP-01.1: derselbe vertrauliche Entscheidungsverlauf war parallel zur RPC
-- direkt per REST-Tabellenzugriff fuer authenticated lesbar (ohne RLS).
-- Der aktuelle GitHub-Code verwendet die Lese-RPCs; die SQL-Schreibfunktionen
-- greifen als SECURITY DEFINER zu. Keine neue RLS-SELECT-Policy anfuegen.
ALTER TABLE public.wartungsvertrag_folgeentscheidung ENABLE ROW LEVEL SECURITY;
REVOKE SELECT ON TABLE public.wartungsvertrag_folgeentscheidung
  FROM PUBLIC, anon, authenticated;

COMMIT;
