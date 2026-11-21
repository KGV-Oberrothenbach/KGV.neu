-- AP-02.2 | Zentraler benannter Fachrechte-Kern | zuerst DemoDB
-- Dieselben Definitionen sind in DemoDB-Migration 20261121204524 installiert.
-- KEINE Umstellung alter RLS, RPCs, Edges oder produktiver Berechtigungen.
-- Jede konkrete Fachoperation muss weiterhin Objekt-, Amts- und Prozessscope pruefen.

CREATE FUNCTION public.ap02_calculate_user_rights(p_user_id uuid)
RETURNS TABLE (code text, gruppe text, titel text, erlaubt boolean,
               individuell_entzogen boolean, quellen text[])
LANGUAGE sql STABLE SECURITY DEFINER
SET search_path = pg_catalog, public
AS $fn$
WITH kontext AS (
  SELECT u.user_id, u.mitglied_id, hr.code AS hauptrolle_code
  FROM public.app_user u
  LEFT JOIN public.app_user_hauptrolle h ON h.user_id = u.user_id
  LEFT JOIN public.hauptrolle hr ON hr.code = h.hauptrolle_code AND hr.aktiv
  WHERE u.user_id = p_user_id
    AND u.mitglied_id IS NOT NULL
    AND (
      hr.code IS NOT NULL
      OR EXISTS (
        SELECT 1 FROM public.protected_admin_identity p
        WHERE p.user_id = u.user_id
      )
    )
),
admin_kontext AS (
  SELECT EXISTS (
    SELECT 1 FROM kontext k
    JOIN public.app_user_zusatzrolle z
      ON z.user_id = k.user_id AND z.zusatzrolle_code = 'admin'
    JOIN public.zusatzrolle r
      ON r.code = z.zusatzrolle_code AND r.aktiv
  ) AS ist_admin
),
kandidaten AS (
  SELECT rf.fachrecht_code, 'hauptrolle:' || k.hauptrolle_code AS quelle
  FROM kontext k
  JOIN public.hauptrolle_fachrecht rf
    ON rf.hauptrolle_code = k.hauptrolle_code

  UNION ALL

  SELECT rf.fachrecht_code, 'zusatzrolle:' || z.zusatzrolle_code
  FROM kontext k
  JOIN public.app_user_zusatzrolle z ON z.user_id = k.user_id
  JOIN public.zusatzrolle r
    ON r.code = z.zusatzrolle_code AND r.aktiv
  JOIN public.zusatzrolle_fachrecht rf
    ON rf.zusatzrolle_code = z.zusatzrolle_code
  WHERE z.zusatzrolle_code <> 'wertermittlung'
     OR k.hauptrolle_code IN ('mitglied', 'bauausschuss')

  UNION ALL

  SELECT rf.fachrecht_code, 'vereinsfunktion:' || f.schluessel
  FROM kontext k
  JOIN public.vereinsfunktion_besetzung b
    ON b.mitglied_id = k.mitglied_id
  JOIN public.vereinsfunktion f
    ON f.id = b.vereinsfunktion_id AND f.aktiv
  JOIN public.vereinsfunktion_fachrecht rf
    ON rf.vereinsfunktion_schluessel = f.schluessel
  WHERE b.gueltig_ab <= current_date
    AND (b.gueltig_bis IS NULL OR b.gueltig_bis >= current_date)

  UNION ALL

  SELECT o.fachrecht_code, 'individuelle_gewaehrung'
  FROM kontext k
  JOIN public.app_user_fachrecht_override o ON o.user_id = k.user_id
  WHERE o.entscheidung = 'grant'
),
quellenliste AS (
  SELECT k.fachrecht_code,
    array_agg(DISTINCT k.quelle ORDER BY k.quelle) AS alle_quellen
  FROM kandidaten k
  GROUP BY k.fachrecht_code
)
SELECT f.code, f.gruppe_code, f.titel,
  (
    EXISTS (SELECT 1 FROM kontext)
    AND f.aktiv
    AND q.fachrecht_code IS NOT NULL
    AND o.entscheidung IS DISTINCT FROM 'deny'
    AND (
      f.code NOT IN (
        'benutzer.lesen',
        'benutzer.verwalten',
        'berechtigungen.lesen',
        'berechtigungen.verwalten',
        'vereinsdaten.alle_lesen'
      )
      OR (SELECT ist_admin FROM admin_kontext)
    )
  ) AS erlaubt,
  COALESCE(o.entscheidung = 'deny', false) AS individuell_entzogen,
  COALESCE(q.alle_quellen, ARRAY[]::text[]) AS quellen
FROM public.fachrecht f
LEFT JOIN quellenliste q ON q.fachrecht_code = f.code
LEFT JOIN public.app_user_fachrecht_override o
  ON o.fachrecht_code = f.code
 AND o.user_id = (SELECT user_id FROM kontext)
ORDER BY f.gruppe_code, f.code
$fn$;

CREATE OR REPLACE FUNCTION public.ap02_preview_effective_rights()
RETURNS TABLE (code text, gruppe text, titel text, erlaubt boolean,
               individuell_entzogen boolean, quellen text[])
LANGUAGE sql STABLE SECURITY DEFINER
SET search_path = pg_catalog, public
AS $fn$
  SELECT * FROM public.ap02_calculate_user_rights(auth.uid())
$fn$;

CREATE FUNCTION public.ap02_has_effective_right(p_fachrecht text)
RETURNS boolean
LANGUAGE sql STABLE SECURITY DEFINER
SET search_path = pg_catalog, public
AS $fn$
  SELECT COALESCE(
    (
      SELECT p.erlaubt
      FROM public.ap02_calculate_user_rights(auth.uid()) p
      WHERE p.code = p_fachrecht
    ), false
  )
$fn$;

CREATE FUNCTION public.ap02_admin_preview_user_rights(p_user_id uuid)
RETURNS TABLE (code text, gruppe text, titel text, erlaubt boolean,
               individuell_entzogen boolean, quellen text[])
LANGUAGE plpgsql STABLE SECURITY DEFINER
SET search_path = pg_catalog, public
AS $fn$
BEGIN
  IF NOT public.ap02_can_manage_rights() THEN
    RAISE EXCEPTION 'Keine Berechtigung zur Vorschau anderer Benutzer'
      USING ERRCODE = '42501';
  END IF;
  IF p_user_id IS NULL
     OR NOT EXISTS(SELECT 1 FROM public.app_user u WHERE u.user_id=p_user_id)
  THEN
    RAISE EXCEPTION 'Benutzer nicht gefunden' USING ERRCODE = '22023';
  END IF;
  RETURN QUERY
    SELECT p.code,p.gruppe,p.titel,p.erlaubt,p.individuell_entzogen,p.quellen
    FROM public.ap02_calculate_user_rights(p_user_id) p;
END
$fn$;

CREATE OR REPLACE FUNCTION public.ap02_can_manage_rights()
RETURNS boolean
LANGUAGE sql STABLE SECURITY DEFINER
SET search_path = pg_catalog, public
AS $fn$
  SELECT public.ap02_has_effective_right('berechtigungen.verwalten')
$fn$;

REVOKE ALL ON FUNCTION public.ap02_calculate_user_rights(uuid) FROM PUBLIC, anon, authenticated;
REVOKE ALL ON FUNCTION public.ap02_has_effective_right(text) FROM PUBLIC, anon, authenticated;
REVOKE ALL ON FUNCTION public.ap02_admin_preview_user_rights(uuid) FROM PUBLIC, anon, authenticated;
REVOKE ALL ON FUNCTION public.ap02_preview_effective_rights() FROM PUBLIC, anon, authenticated;
GRANT EXECUTE ON FUNCTION public.ap02_has_effective_right(text) TO authenticated;
GRANT EXECUTE ON FUNCTION public.ap02_admin_preview_user_rights(uuid) TO authenticated;
GRANT EXECUTE ON FUNCTION public.ap02_preview_effective_rights() TO authenticated;

COMMENT ON FUNCTION public.ap02_calculate_user_rights(uuid) IS
  'AP02.2 interner autoritativer Schatten-Rechtekern; kein direkter REST-Zugriff; kein Fachobjekt-Scope';
COMMENT ON FUNCTION public.ap02_has_effective_right(text) IS
  'AP02.2 effektives Fachrecht des angemeldeten Benutzers; Fachoperationen pruefen weiteren Objekt-/Prozessscope';
COMMENT ON FUNCTION public.ap02_admin_preview_user_rights(uuid) IS
  'AP02.2 Admin-only Fremdbenutzervorschau';
