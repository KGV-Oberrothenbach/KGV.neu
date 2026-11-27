-- AP-02.1B: Rollen- und Fachrechteverwaltung im Schattenmodell.
-- Keine Umstellung der bisherigen Auth-, RLS-, Edge- und Client-Pfade.
-- Nur eine echte Admin-Zusatzrolle (inkl. protected Admin) darf zuordnen.
-- Mutationen ausschließlich über authentifizierte RPC, protokolliert.
-- VDB erst nach gesonderter Freigabe; DemoDB zuerst.

DO $check$
BEGIN
  IF (SELECT count(*) FROM public.fachrecht_gruppe) <> 10
    OR (SELECT count(*) FROM public.fachrecht) <> 55
    OR (SELECT count(*) FROM public.hauptrolle) <> 5
    OR (SELECT count(*) FROM public.zusatzrolle) <> 2
    OR ((EXISTS (SELECT 1 FROM auth.users) OR
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
      SELECT a.user_id
      FROM public.app_user a
      WHERE a.is_protected = true
      EXCEPT
      SELECT p.user_id
      FROM public.protected_admin_identity p
    )
    OR EXISTS (
      SELECT p.user_id
      FROM public.protected_admin_identity p
      EXCEPT
      SELECT a.user_id
      FROM public.app_user a
      WHERE a.is_protected = true
    )
    OR EXISTS (
      SELECT p.user_id
      FROM public.protected_admin_identity p
      WHERE NOT EXISTS (
        SELECT 1
        FROM public.app_user_zusatzrolle z
        WHERE z.user_id = p.user_id
          AND z.zusatzrolle_code = 'admin'
      )
    )
  THEN
    RAISE EXCEPTION 'AP-02.1A ist nicht vollstaendig installiert' USING ERRCODE = '55000';
  END IF;
END
$check$;

CREATE FUNCTION public.ap02_can_manage_rights()
RETURNS boolean
LANGUAGE sql STABLE SECURITY DEFINER
SET search_path = pg_catalog, public
AS $fn$
  SELECT EXISTS (
    SELECT 1
    FROM public.app_user a
    JOIN public.app_user_zusatzrolle z
      ON z.user_id = a.user_id AND z.zusatzrolle_code = 'admin'
    JOIN public.zusatzrolle r
      ON r.code = z.zusatzrolle_code AND r.aktiv
    JOIN public.zusatzrolle_fachrecht rf
      ON rf.zusatzrolle_code = r.code
     AND rf.fachrecht_code = 'berechtigungen.verwalten'
    JOIN public.fachrecht f
      ON f.code = rf.fachrecht_code AND f.aktiv
    LEFT JOIN public.app_user_fachrecht_override o
      ON o.user_id = a.user_id AND o.fachrecht_code = f.code
    WHERE a.user_id = auth.uid()
      AND a.mitglied_id IS NOT NULL
      AND o.entscheidung IS DISTINCT FROM 'deny'
  );
$fn$;

-- Admin-only Uebersicht fuer die kuenftige Rollenmaske; keine Personennamen.
CREATE FUNCTION public.ap02_list_user_access()
RETURNS TABLE (
  user_id uuid, mitglied_id bigint, legacy_role text,
  hauptrolle_code text, zusatzrollen text[], anzahl_ausnahmen integer,
  ist_geschuetzt boolean
)
LANGUAGE plpgsql STABLE SECURITY DEFINER
SET search_path = pg_catalog, public
AS $fn$
BEGIN
  IF NOT public.ap02_can_manage_rights() THEN
    RAISE EXCEPTION 'Keine Berechtigung zur Rollenverwaltung' USING ERRCODE = '42501';
  END IF;
  RETURN QUERY
    SELECT a.user_id, a.mitglied_id, a.role, h.hauptrolle_code,
      ARRAY(SELECT z.zusatzrolle_code
            FROM public.app_user_zusatzrolle z
            WHERE z.user_id = a.user_id ORDER BY z.zusatzrolle_code),
      (SELECT count(*)::integer FROM public.app_user_fachrecht_override o
       WHERE o.user_id = a.user_id),
      EXISTS(SELECT 1 FROM public.protected_admin_identity p
             WHERE p.user_id = a.user_id)
    FROM public.app_user a
    LEFT JOIN public.app_user_hauptrolle h ON h.user_id = a.user_id
    ORDER BY a.created_at, a.user_id;
END
$fn$;

CREATE FUNCTION public.ap02_assign_main_role(
  p_user_id uuid, p_hauptrolle text, p_grund text
)
RETURNS boolean
LANGUAGE plpgsql SECURITY DEFINER
SET search_path = pg_catalog, public
AS $fn$
DECLARE
  v_actor uuid := auth.uid();
  v_before text;
BEGIN
  IF v_actor IS NULL OR NOT public.ap02_can_manage_rights() THEN
    RAISE EXCEPTION 'Keine Berechtigung zur Rollenverwaltung' USING ERRCODE = '42501';
  END IF;
  IF length(btrim(coalesce(p_grund,''))) < 3 THEN
    RAISE EXCEPTION 'Ein nachvollziehbarer Grund ist erforderlich' USING ERRCODE = '22023';
  END IF;
  IF NOT EXISTS (SELECT 1 FROM public.hauptrolle r WHERE r.code=p_hauptrolle AND r.aktiv) THEN
    RAISE EXCEPTION 'Unbekannte oder inaktive Hauptrolle' USING ERRCODE = '22023';
  END IF;
  PERFORM 1 FROM public.app_user a WHERE a.user_id=p_user_id FOR UPDATE;
  IF NOT FOUND THEN
    RAISE EXCEPTION 'Zielbenutzer nicht gefunden' USING ERRCODE = '22023';
  END IF;
  SELECT h.hauptrolle_code INTO v_before
    FROM public.app_user_hauptrolle h WHERE h.user_id=p_user_id;
  IF v_before IS NOT DISTINCT FROM p_hauptrolle THEN
    RETURN false;
  END IF;
  INSERT INTO public.app_user_hauptrolle(user_id,hauptrolle_code,gesetzt_von)
    VALUES (p_user_id,p_hauptrolle,v_actor)
    ON CONFLICT (user_id) DO UPDATE
      SET hauptrolle_code=EXCLUDED.hauptrolle_code,
          gesetzt_von=EXCLUDED.gesetzt_von, gesetzt_am=now();
  INSERT INTO public.berechtigungs_audit
    (akteur,aktion,zieltyp,zielschluessel,vorher,nachher,herkunft)
    VALUES (v_actor,'hauptrolle_gesetzt','app_user',p_user_id::text,
      jsonb_build_object('hauptrolle',v_before),
      jsonb_build_object('hauptrolle',p_hauptrolle,'grund',btrim(p_grund)), 'ap02.1b');
  RETURN true;
END
$fn$;

CREATE FUNCTION public.ap02_set_additional_role(
  p_user_id uuid, p_zusatzrolle text, p_aktiv boolean, p_grund text
)
RETURNS boolean
LANGUAGE plpgsql SECURITY DEFINER
SET search_path = pg_catalog, public
AS $fn$
DECLARE
  v_actor uuid := auth.uid();
  v_before boolean;
BEGIN
  IF v_actor IS NULL OR NOT public.ap02_can_manage_rights() THEN
    RAISE EXCEPTION 'Keine Berechtigung zur Rollenverwaltung' USING ERRCODE = '42501';
  END IF;
  IF length(btrim(coalesce(p_grund,''))) < 3 OR p_aktiv IS NULL THEN
    RAISE EXCEPTION 'Aktivstatus und Grund sind erforderlich' USING ERRCODE='22023';
  END IF;
  IF NOT EXISTS (SELECT 1 FROM public.zusatzrolle r WHERE r.code=p_zusatzrolle AND r.aktiv) THEN
    RAISE EXCEPTION 'Unbekannte oder inaktive Zusatzrolle' USING ERRCODE='22023';
  END IF;
  PERFORM 1 FROM public.app_user a WHERE a.user_id=p_user_id FOR UPDATE;
  IF NOT FOUND THEN
    RAISE EXCEPTION 'Zielbenutzer nicht gefunden' USING ERRCODE='22023';
  END IF;
  SELECT EXISTS(SELECT 1 FROM public.app_user_zusatzrolle z
      WHERE z.user_id=p_user_id AND z.zusatzrolle_code=p_zusatzrolle)
    INTO v_before;
  IF v_before = p_aktiv THEN RETURN false; END IF;
  IF p_zusatzrolle='admin' AND p_aktiv AND
    NOT EXISTS(SELECT 1 FROM public.app_user_hauptrolle h WHERE h.user_id=p_user_id)
    AND NOT EXISTS(SELECT 1 FROM public.protected_admin_identity p WHERE p.user_id=p_user_id)
  THEN
    RAISE EXCEPTION 'Vor einer neuen Admin-Zusatzrolle muss eine Hauptrolle zugeteilt werden' USING ERRCODE='23514';
  END IF;
  IF p_zusatzrolle='admin' AND NOT p_aktiv AND
    EXISTS(SELECT 1 FROM public.protected_admin_identity p WHERE p.user_id=p_user_id)
  THEN
    RAISE EXCEPTION 'Die geschuetzte Admin-Zusatzrolle ist unverlierbar' USING ERRCODE='42501';
  END IF;
  IF p_aktiv THEN
    INSERT INTO public.app_user_zusatzrolle(user_id,zusatzrolle_code,gesetzt_von)
      VALUES (p_user_id,p_zusatzrolle,v_actor);
  ELSE
    DELETE FROM public.app_user_zusatzrolle z
      WHERE z.user_id=p_user_id AND z.zusatzrolle_code=p_zusatzrolle;
  END IF;
  INSERT INTO public.berechtigungs_audit
    (akteur,aktion,zieltyp,zielschluessel,vorher,nachher,herkunft)
    VALUES (v_actor,'zusatzrolle_geaendert','app_user',p_user_id::text,
      jsonb_build_object('zusatzrolle',p_zusatzrolle,'aktiv',v_before),
      jsonb_build_object('zusatzrolle',p_zusatzrolle,'aktiv',p_aktiv,'grund',btrim(p_grund)), 'ap02.1b');
  RETURN true;
END
$fn$;

CREATE FUNCTION public.ap02_set_permission_override(
  p_user_id uuid, p_fachrecht text, p_entscheidung text, p_grund text
)
RETURNS boolean
LANGUAGE plpgsql SECURITY DEFINER
SET search_path = pg_catalog, public
AS $fn$
DECLARE
  v_actor uuid := auth.uid();
  v_before text;
BEGIN
  IF v_actor IS NULL OR NOT public.ap02_can_manage_rights() THEN
    RAISE EXCEPTION 'Keine Berechtigung zur Rollenverwaltung' USING ERRCODE='42501';
  END IF;
  IF length(btrim(coalesce(p_grund,''))) < 3
     OR p_entscheidung IS NULL
     OR p_entscheidung NOT IN ('grant','deny','inherit') THEN
    RAISE EXCEPTION 'Gueltige Entscheidung und Grund erforderlich' USING ERRCODE='22023';
  END IF;
  IF NOT EXISTS(SELECT 1 FROM public.fachrecht f
                WHERE f.code=p_fachrecht AND f.aktiv) THEN
    RAISE EXCEPTION 'Unbekanntes oder gesperrtes Fachrecht' USING ERRCODE='22023';
  END IF;
  PERFORM 1 FROM public.app_user a WHERE a.user_id=p_user_id FOR UPDATE;
  IF NOT FOUND THEN
    RAISE EXCEPTION 'Zielbenutzer nicht gefunden' USING ERRCODE='22023';
  END IF;
  IF p_fachrecht IN ('benutzer.verwalten','berechtigungen.verwalten')
    AND p_entscheidung='grant' THEN
    RAISE EXCEPTION 'Systemverwaltungsrechte sind nur ueber die Admin-Zusatzrolle moeglich' USING ERRCODE='42501';
  END IF;
  IF p_fachrecht='berechtigungen.verwalten' AND p_entscheidung='deny'
     AND EXISTS (SELECT 1 FROM public.protected_admin_identity p
                 WHERE p.user_id=p_user_id) THEN
    RAISE EXCEPTION 'Geschuetzte Admin-Identitaet darf nicht ausgesperrt werden' USING ERRCODE='42501';
  END IF;
  SELECT o.entscheidung INTO v_before
    FROM public.app_user_fachrecht_override o
    WHERE o.user_id=p_user_id AND o.fachrecht_code=p_fachrecht;
  IF (p_entscheidung='inherit' AND v_before IS NULL)
     OR (v_before IS NOT DISTINCT FROM p_entscheidung) THEN
    RETURN false;
  END IF;
  IF p_entscheidung='inherit' THEN
    DELETE FROM public.app_user_fachrecht_override o
      WHERE o.user_id=p_user_id AND o.fachrecht_code=p_fachrecht;
  ELSE
    INSERT INTO public.app_user_fachrecht_override
      (user_id,fachrecht_code,entscheidung,grund,gesetzt_von,gesetzt_am)
      VALUES (p_user_id,p_fachrecht,p_entscheidung,btrim(p_grund),v_actor,now())
      ON CONFLICT (user_id,fachrecht_code) DO UPDATE
        SET entscheidung=EXCLUDED.entscheidung,grund=EXCLUDED.grund,
            gesetzt_von=EXCLUDED.gesetzt_von,gesetzt_am=now();
  END IF;
  INSERT INTO public.berechtigungs_audit
    (akteur,aktion,zieltyp,zielschluessel,vorher,nachher,herkunft)
    VALUES (v_actor,'fachrecht_ausnahme','app_user',p_user_id::text,
      jsonb_build_object('fachrecht',p_fachrecht,'entscheidung',v_before),
      jsonb_build_object('fachrecht',p_fachrecht,'entscheidung',p_entscheidung,'grund',btrim(p_grund)), 'ap02.1b');
  RETURN true;
END
$fn$;

-- Postgres vergibt EXECUTE standardmaessig an PUBLIC: alle Eintraege explizit beschraenken.
REVOKE ALL ON FUNCTION public.ap02_can_manage_rights() FROM PUBLIC, anon, authenticated;
REVOKE ALL ON FUNCTION public.ap02_list_user_access() FROM PUBLIC, anon, authenticated;
REVOKE ALL ON FUNCTION public.ap02_assign_main_role(uuid,text,text) FROM PUBLIC, anon, authenticated;
REVOKE ALL ON FUNCTION public.ap02_set_additional_role(uuid,text,boolean,text) FROM PUBLIC, anon, authenticated;
REVOKE ALL ON FUNCTION public.ap02_set_permission_override(uuid,text,text,text) FROM PUBLIC, anon, authenticated;
GRANT EXECUTE ON FUNCTION public.ap02_can_manage_rights() TO authenticated;
GRANT EXECUTE ON FUNCTION public.ap02_list_user_access() TO authenticated;
GRANT EXECUTE ON FUNCTION public.ap02_assign_main_role(uuid,text,text) TO authenticated;
GRANT EXECUTE ON FUNCTION public.ap02_set_additional_role(uuid,text,boolean,text) TO authenticated;
GRANT EXECUTE ON FUNCTION public.ap02_set_permission_override(uuid,text,text,text) TO authenticated;

COMMENT ON FUNCTION public.ap02_can_manage_rights() IS
  'AP02.1B admin-secondary-role-gated; effective right deny overrides role; no legacy fallback';
COMMENT ON FUNCTION public.ap02_list_user_access() IS
  'AP02.1B admin-only listing; does not alter existing authorization';
