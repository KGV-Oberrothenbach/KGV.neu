-- AP-02.4C: Fachrechte 2.0 duerfen nur bei konsistenter Auth-/Mitglied-Verknuepfung gelten.
-- Nur DemoDB getestet; produktive Datenbank erst nach eigener Freigabe.
-- Aendert nur den internen Rechtekern. Bestehende Mitglied-RLS bleibt unveraendert.
DO $ap024c$
DECLARE
  v_definition text;
  v_old text := E'  FROM public.app_user u\n  LEFT JOIN public.app_user_hauptrolle h ON h.user_id = u.user_id';
  v_new text := E'  FROM public.app_user u\n  JOIN public.mitglied m ON m.id = u.mitglied_id AND m.auth_user_id = u.user_id\n  LEFT JOIN public.app_user_hauptrolle h ON h.user_id = u.user_id';
  v_hits integer;
BEGIN
  IF to_regprocedure('public.ap02_calculate_user_rights(uuid)') IS NULL THEN
    RAISE EXCEPTION 'AP-02.2-Funktion fehlt' USING ERRCODE = '55000';
  END IF;
  v_definition := pg_get_functiondef('public.ap02_calculate_user_rights(uuid)'::regprocedure);
  v_hits := (length(v_definition) - length(replace(v_definition, v_old, ''))) / length(v_old);
  IF v_hits <> 1 THEN
    RAISE EXCEPTION 'Unerwarteter Rechtekern: erwartete Stelle % mal vorhanden', v_hits
      USING ERRCODE = '55000';
  END IF;
  EXECUTE replace(v_definition, v_old, v_new);
END
$ap024c$;
