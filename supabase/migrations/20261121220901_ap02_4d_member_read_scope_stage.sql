-- AP-02.4D: vorbereiteter V2-Leseumfang fuer Mitglieder
-- Nur DemoDB; noch keine Umschaltung der bestehenden Mitglied-RLS-Policies.
-- Auth-/Mitglied-Bindung ist Pflicht. Kein Legacy-Rollenbit-Fallback.
CREATE FUNCTION public.ap02_can_read_mitglied(p_mitglied_id bigint)
RETURNS boolean
LANGUAGE sql STABLE SECURITY DEFINER
SET search_path = pg_catalog, public
AS $fn$
WITH actor AS (
  SELECT a.user_id, a.mitglied_id
  FROM public.app_user a
  JOIN public.mitglied own_member
    ON own_member.id = a.mitglied_id
   AND own_member.auth_user_id = a.user_id
  WHERE a.user_id = auth.uid()
),
target AS (
  SELECT m.id, m.auth_user_id, m.erstellt_von_auth_user_id
  FROM public.mitglied m
  WHERE m.id = p_mitglied_id
)
SELECT EXISTS (
  SELECT 1
  FROM actor a
  CROSS JOIN target m
  WHERE NOT EXISTS (
    SELECT 1 FROM public.app_user_fachrecht_override o
    WHERE o.user_id = a.user_id
      AND o.fachrecht_code = 'mitglied.lesen'
      AND o.entscheidung = 'deny'
  )
  AND (
    (a.mitglied_id = m.id AND m.auth_user_id = a.user_id)
    OR EXISTS (
      SELECT 1
      FROM public.mitglied_gesetzlicher_vertreter v
      WHERE v.vertreter_mitglied_id = a.mitglied_id
        AND v.minderjaehriges_mitglied_id = m.id
        AND v.gueltig_ab <= current_date
        AND (v.gueltig_bis IS NULL OR v.gueltig_bis >= current_date)
    )
    OR public.ap02_has_effective_right('mitglied.lesen')
    OR public.ap02_has_effective_right('mitglied.bearbeiten')
    OR public.ap02_has_effective_right('vereinsdaten.alle_lesen')
    OR (
      m.erstellt_von_auth_user_id = a.user_id
      AND public.ap02_has_effective_right('mitglied.anlegen')
    )
  )
)
$fn$;

REVOKE ALL ON FUNCTION public.ap02_can_read_mitglied(bigint)
  FROM PUBLIC, anon, authenticated;
GRANT EXECUTE ON FUNCTION public.ap02_can_read_mitglied(bigint)
  TO authenticated;

COMMENT ON FUNCTION public.ap02_can_read_mitglied(bigint) IS
  'AP-02.4D V2 Member-Read-Scope (own, active representative, explicit global, draft creator), deny wins; not yet wired into RLS';
