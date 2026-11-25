-- FR2-12 SQL-Entwurf (NOCH KEINE MIGRATION): nicht remote anwenden.
-- Basis dd394206, DemoDB 291; erst nach SQL-Prüfung final freigeben,
-- zuerst transaktional/isoliert testen, DemoDB-Sicherung, dann nur mit Freigabe.
-- Schwerpunkt: Rechteaufteilung Benutzerverwaltung/Rechtevergabe und sichere
-- Standard-Hauptrolle neu eingeladener, bisher unzugeordneter Benutzer.
BEGIN;

DO $preflight$
BEGIN
  IF (SELECT count(*) FROM public.fachrecht) <> 55
     OR (SELECT count(*) FROM public.hauptrolle) <> 5
     OR NOT EXISTS (SELECT 1 FROM public.hauptrolle WHERE code='mitglied' AND aktiv)
     OR to_regprocedure('public.ap02_has_effective_right(text)') IS NULL
     OR to_regprocedure('public.ap02_can_manage_rights()') IS NULL
     OR to_regprocedure('public.begin_auth_user_deletion(uuid,bigint,uuid)') IS NULL
     OR to_regprocedure('public.provision_app_user_link(bigint,uuid,text)') IS NULL
  THEN RAISE EXCEPTION 'FR212: Fachrechte- oder Auth-Basis fehlt' USING ERRCODE='55000'; END IF;
END
$preflight$;

-- Bestehende Reservierungen ohne V2-Snapshot niemals stillschweigend übernehmen.
LOCK TABLE public.auth_user_delete_lock IN ACCESS EXCLUSIVE MODE;
DO $lock_preflight$
BEGIN
 IF EXISTS (SELECT 1 FROM public.auth_user_delete_lock) THEN
   RAISE EXCEPTION 'FR212: offene Kontolöschungen zuerst klären' USING ERRCODE='55000';
 END IF;
END $lock_preflight$;
ALTER TABLE public.auth_user_delete_lock ADD COLUMN fr212_rights_snapshot jsonb NOT NULL;
REVOKE ALL ON public.auth_user_delete_lock FROM PUBLIC, anon, authenticated;

-- Vorhandene, geschützte Löschreservierung beibehalten; allein den
-- bisherigen, fachlich falschen Berechtigungs-Gate ersetzen.
DO $cutover$
DECLARE v_definition text;
BEGIN
 SELECT pg_get_functiondef('public.begin_auth_user_deletion(uuid,bigint,uuid)'::regprocedure)
 INTO v_definition;
 IF (SELECT (length(v_definition)-length(replace(v_definition,
       'public.ap02_can_manage_rights() IS NOT TRUE','')))
       / length('public.ap02_can_manage_rights() IS NOT TRUE')) <> 1 THEN
   RAISE EXCEPTION 'FR212: Loeschreservierung weicht von geprüfter Basis ab' USING ERRCODE='55000';
 END IF;
 v_definition := replace(v_definition,
   'public.ap02_can_manage_rights() IS NOT TRUE',
   'public.ap02_has_effective_right(''benutzer.verwalten'') IS NOT TRUE');
 v_definition := replace(v_definition, 'CanManageRoles required', 'benutzer.verwalten required');
 IF strpos(v_definition, '  UPDATE public.mitglied' || chr(10) || '  SET auth_user_id = NULL') = 0 THEN
   RAISE EXCEPTION 'FR212: Reservierungsanker fehlt' USING ERRCODE='55000';
 END IF;
 IF (length(v_definition)-length(replace(v_definition,'permission_revocations, is_protected, app_user_created_at, app_user_updated_at)','')))/length('permission_revocations, is_protected, app_user_created_at, app_user_updated_at)') <> 1
 OR (length(v_definition)-length(replace(v_definition,'v_target.is_protected, v_target.created_at, v_target.updated_at);','')))/length('v_target.is_protected, v_target.created_at, v_target.updated_at);') <> 1 THEN
   RAISE EXCEPTION 'FR212: Snapshot-Anker nicht eindeutig' USING ERRCODE='55000';
 END IF;
 -- Snapshot in derselben INSERT-Anweisung, bevor der Trigger app_user und Rechte löscht.
 v_definition := replace(v_definition,
   'permission_revocations, is_protected, app_user_created_at, app_user_updated_at)',
   'permission_revocations, is_protected, app_user_created_at, app_user_updated_at, fr212_rights_snapshot)');
 v_definition := replace(v_definition,
   'v_target.is_protected, v_target.created_at, v_target.updated_at);',
   $snapshot$v_target.is_protected, v_target.created_at, v_target.updated_at,
     jsonb_build_object(
       'hauptrolle', (SELECT coalesce(jsonb_agg(to_jsonb(h)), '[]'::jsonb) FROM public.app_user_hauptrolle h WHERE h.user_id=p_user_id),
       'zusatzrollen', (SELECT coalesce(jsonb_agg(to_jsonb(z)), '[]'::jsonb) FROM public.app_user_zusatzrolle z WHERE z.user_id=p_user_id),
       'overrides', (SELECT coalesce(jsonb_agg(to_jsonb(o)), '[]'::jsonb) FROM public.app_user_fachrecht_override o WHERE o.user_id=p_user_id)));$snapshot$);
 IF strpos(v_definition, 'fr212_rights_snapshot)') = 0 OR strpos(v_definition, 'jsonb_build_object(') = 0 THEN
   RAISE EXCEPTION 'FR212: Snapshot-Anker fehlt' USING ERRCODE='55000';
 END IF;
 EXECUTE v_definition;
END
$cutover$;

-- Restore runs as service_role only; existing reservation and legacy safeguards stay intact.
DO $restore$
DECLARE v_definition text; v_anchor text := '  RETURN true;';
BEGIN
 SELECT pg_get_functiondef('public.restore_auth_user_deletion(uuid,uuid)'::regprocedure) INTO v_definition;
 IF (length(v_definition)-length(replace(v_definition,v_anchor,'')))/length(v_anchor) <> 1 THEN
   RAISE EXCEPTION 'FR212: Restore-Anker fehlt' USING ERRCODE='55000';
 END IF;
 v_definition := replace(v_definition,v_anchor,$rows$
  IF jsonb_typeof(v_lock.fr212_rights_snapshot->'hauptrolle') IS DISTINCT FROM 'array'
     OR jsonb_typeof(v_lock.fr212_rights_snapshot->'zusatzrollen') IS DISTINCT FROM 'array'
     OR jsonb_typeof(v_lock.fr212_rights_snapshot->'overrides') IS DISTINCT FROM 'array' THEN
    RAISE EXCEPTION 'FR212: invalid deletion snapshot' USING ERRCODE='55000';
  END IF;
  INSERT INTO public.app_user_hauptrolle
    SELECT * FROM jsonb_populate_recordset(NULL::public.app_user_hauptrolle,v_lock.fr212_rights_snapshot->'hauptrolle');
  INSERT INTO public.app_user_zusatzrolle
    SELECT * FROM jsonb_populate_recordset(NULL::public.app_user_zusatzrolle,v_lock.fr212_rights_snapshot->'zusatzrollen');
  INSERT INTO public.app_user_fachrecht_override
    SELECT * FROM jsonb_populate_recordset(NULL::public.app_user_fachrecht_override,v_lock.fr212_rights_snapshot->'overrides');
  RETURN true;$rows$);
 EXECUTE v_definition;
END $restore$;

-- Neu verknuepfte Benutzer erhalten minimal die Hauptrolle 'mitglied'.
-- Bei bestehenden Konten wird weder eine Haupt- noch eine Zusatzrolle
-- ueberschrieben. Admin-/Vorstandszuweisung bleibt AP02-only.
CREATE OR REPLACE FUNCTION public.provision_app_user_link(
  p_mitglied_id bigint, p_user_id uuid, p_role text)
RETURNS void
LANGUAGE plpgsql SECURITY DEFINER
SET search_path TO public, pg_temp
AS $fn$
DECLARE
  v_member public.mitglied%rowtype;
  v_app_user public.app_user%rowtype;
BEGIN
  IF p_mitglied_id IS NULL OR p_mitglied_id <= 0 OR p_user_id IS NULL THEN
    RAISE EXCEPTION 'Invalid member or auth user id' USING ERRCODE='22023';
  END IF;
  IF p_role IS DISTINCT FROM 'user' THEN
    RAISE EXCEPTION 'Invalid role' USING ERRCODE='22023';
  END IF;
  SELECT * INTO v_member FROM public.mitglied WHERE id=p_mitglied_id FOR UPDATE;
  IF NOT FOUND THEN RAISE EXCEPTION 'Member not found' USING ERRCODE='P0002'; END IF;
  PERFORM pg_advisory_xact_lock(hashtextextended(p_user_id::text,0));
  IF EXISTS(SELECT 1 FROM public.mitglied WHERE auth_user_id=p_user_id AND id<>p_mitglied_id) THEN
    RAISE EXCEPTION 'Auth user is already linked to another member' USING ERRCODE='23505';
  END IF;
  SELECT * INTO v_app_user FROM public.app_user
    WHERE user_id=p_user_id OR mitglied_id=p_mitglied_id FOR UPDATE;
  IF FOUND THEN
    IF v_app_user.user_id=p_user_id AND v_app_user.mitglied_id=p_mitglied_id
       AND v_member.auth_user_id=p_user_id THEN
       RAISE EXCEPTION 'Member is already linked' USING ERRCODE='23505';
    END IF;
    RAISE EXCEPTION 'Conflicting app_user linkage' USING ERRCODE='23505';
  END IF;
  IF v_member.auth_user_id IS NOT NULL AND v_member.auth_user_id<>p_user_id THEN
    RAISE EXCEPTION 'Member is already linked to another auth user' USING ERRCODE='23505';
  END IF;
  UPDATE public.mitglied SET auth_user_id=p_user_id WHERE id=p_mitglied_id;
  UPDATE public.app_user SET mitglied_id=p_mitglied_id,role='user',
    permission_grants=0, permission_revocations=0, is_protected=false,
    updated_at=now() WHERE user_id=p_user_id;
  IF NOT FOUND THEN
    RAISE EXCEPTION 'App user was not created by member-link trigger' USING ERRCODE='P0001';
  END IF;
  INSERT INTO public.app_user_hauptrolle(user_id,hauptrolle_code,gesetzt_von)
  VALUES(p_user_id,'mitglied',NULL)
  ON CONFLICT (user_id) DO NOTHING;
END;
$fn$;
REVOKE ALL ON FUNCTION public.provision_app_user_link(bigint,uuid,text) FROM PUBLIC,anon,authenticated;
GRANT EXECUTE ON FUNCTION public.provision_app_user_link(bigint,uuid,text) TO service_role;

-- Das Lesen der Berechtigungen ist eine gesonderte Befugnis.
-- Vorhandene Mutations-RPCs bleiben strikt bei berechtigungen.verwalten.
DO $separate_read$
DECLARE v_name text; v_signature regprocedure; v_definition text;
BEGIN
 FOREACH v_name IN ARRAY ARRAY['ap02_list_user_access','ap02_admin_preview_user_rights'] LOOP
   IF v_name='ap02_list_user_access' THEN
      v_signature:='public.ap02_list_user_access()'::regprocedure;
   ELSE v_signature:='public.ap02_admin_preview_user_rights(uuid)'::regprocedure; END IF;
   SELECT pg_get_functiondef(v_signature) INTO v_definition;
   IF v_definition IS NULL OR
      (length(v_definition)-length(replace(v_definition,'IF NOT public.ap02_can_manage_rights() THEN','')))
         / length('IF NOT public.ap02_can_manage_rights() THEN') <> 1 THEN
      RAISE EXCEPTION 'FR212: Rechte-Lesefunktion % weicht von Basis ab',v_name USING ERRCODE='55000';
   END IF;
   v_definition:=replace(v_definition,
      'IF NOT public.ap02_can_manage_rights() THEN',
      'IF NOT public.ap02_has_effective_right(''berechtigungen.lesen'') THEN');
   EXECUTE v_definition;
 END LOOP;
END
$separate_read$;

-- Eigenständige, eng begrenzte Leseschnittstelle fuer die Benutzerkonto-Maske.
CREATE OR REPLACE FUNCTION public.fr212_user_account(p_mitglied_id bigint)
RETURNS TABLE(user_id uuid,mitglied_id bigint,email text,legacy_role text,
  ist_geschuetzt boolean,hat_admin_zusatzrolle boolean)
LANGUAGE plpgsql STABLE SECURITY DEFINER
SET search_path TO pg_catalog,public
AS $fn$
BEGIN
  IF auth.uid() IS NULL OR NOT
    public.ap02_has_effective_right('benutzer.lesen') THEN
    RAISE EXCEPTION 'benutzer.lesen required' USING ERRCODE='42501';
  END IF;
  IF p_mitglied_id IS NULL OR p_mitglied_id<=0 THEN
    RAISE EXCEPTION 'Invalid member id' USING ERRCODE='22023';
  END IF;
  IF NOT EXISTS(SELECT 1 FROM public.mitglied m WHERE m.id=p_mitglied_id) THEN
    RAISE EXCEPTION 'Member not found' USING ERRCODE='P0002';
  END IF;
  IF EXISTS (SELECT 1 FROM public.mitglied m LEFT JOIN public.app_user a ON a.mitglied_id=m.id
       WHERE m.id=p_mitglied_id AND m.auth_user_id IS DISTINCT FROM a.user_id) THEN
    RAISE EXCEPTION 'Inconsistent account link' USING ERRCODE='55000';
  END IF;
  RETURN QUERY
  SELECT a.user_id,m.id,m.email::text,a.role,
    COALESCE(a.is_protected,false)
      OR EXISTS(SELECT 1 FROM public.protected_admin_identity p WHERE p.user_id=a.user_id),
    EXISTS(SELECT 1 FROM public.app_user_zusatzrolle z
      WHERE z.user_id=a.user_id AND z.zusatzrolle_code='admin')
  FROM public.mitglied m
  LEFT JOIN public.app_user a ON a.mitglied_id=m.id
  WHERE m.id=p_mitglied_id;
  RETURN;
END;
$fn$;
REVOKE ALL ON FUNCTION public.fr212_user_account(bigint) FROM PUBLIC,anon,authenticated;
GRANT EXECUTE ON FUNCTION public.fr212_user_account(bigint) TO authenticated;

-- Auth-Konto und Mitgliedszuordnung müssen tatsächlich zusammenpassen.
-- Ehemalige Mitglieder behalten ihren eng begrenzten historischen Eigenkontext.
CREATE OR REPLACE FUNCTION public.current_app_user_context()
RETURNS TABLE(user_id uuid, mitglied_id bigint, role text, permission_grants bigint,
  permission_revocations bigint, is_protected boolean)
LANGUAGE sql STABLE SECURITY DEFINER
SET search_path TO pg_catalog,public
AS $context$
 SELECT au.user_id,au.mitglied_id,au.role,coalesce(au.permission_grants,0),
   coalesce(au.permission_revocations,0),coalesce(au.is_protected,false)
 FROM public.app_user au
 JOIN public.mitglied m ON m.id=au.mitglied_id AND m.auth_user_id=au.user_id
 JOIN auth.users u ON u.id=au.user_id AND u.deleted_at IS NULL
   AND (u.banned_until IS NULL OR u.banned_until<=now())
 WHERE au.user_id=auth.uid();
$context$;
REVOKE ALL ON FUNCTION public.current_app_user_context() FROM PUBLIC,anon;
GRANT EXECUTE ON FUNCTION public.current_app_user_context() TO authenticated,service_role;

DO $auth_context$
DECLARE d text; a text := '  WHERE u.user_id = p_user_id';
BEGIN
 SELECT pg_get_functiondef('public.ap02_calculate_user_rights(uuid)'::regprocedure) INTO d;
 IF (length(d)-length(replace(d,a,'')))/length(a)<>1 THEN
   RAISE EXCEPTION 'FR212: Kontextanker fehlt' USING ERRCODE='55000'; END IF;
 d:=replace(d,a,$guard$  WHERE u.user_id = p_user_id
    AND EXISTS (SELECT 1 FROM auth.users au WHERE au.id=u.user_id
      AND au.deleted_at IS NULL AND (au.banned_until IS NULL OR au.banned_until<=now()))$guard$);
 EXECUTE d;
END $auth_context$;

-- E-Mail-Lookup ist Teil der Kontoanlage, keine Rollenvergabe.
DO $lookup$
DECLARE d text; a text := 'public.ap02_can_manage_rights() is not true';
BEGIN
 SELECT pg_get_functiondef('public.find_auth_user_id_by_email(text)'::regprocedure) INTO d;
 IF (length(d)-length(replace(d,a,'')))/length(a)<>1 THEN
   RAISE EXCEPTION 'FR212: Auth-Lookup-Anker fehlt' USING ERRCODE='55000'; END IF;
 d:=replace(d,a,'public.ap02_has_effective_right(''benutzer.verwalten'') is not true');
 d:=replace(d,'raise exception ''not authorized'';','raise exception ''not authorized'' using errcode=''42501'';');
 EXECUTE d;
END $lookup$;

-- Systemrechte werden ausschließlich über die Admin-Zusatzrolle gewährt.
-- Geschützte Identität darf auch nicht aus der Leseansicht ausgesperrt werden.
DO $system_rights$
DECLARE d text; a text := 'p_fachrecht IN (''benutzer.verwalten'',''berechtigungen.verwalten'')';
        b text := 'p_fachrecht=''berechtigungen.verwalten'' AND p_entscheidung=''deny''';
BEGIN
 IF EXISTS(SELECT 1 FROM public.app_user_fachrecht_override WHERE entscheidung='grant'
   AND fachrecht_code IN ('benutzer.lesen','benutzer.verwalten','berechtigungen.lesen','berechtigungen.verwalten','vereinsdaten.alle_lesen')) THEN
   RAISE EXCEPTION 'FR212: vorhandene System-Gewährung zuerst klären' USING ERRCODE='55000'; END IF;
 SELECT pg_get_functiondef('public.ap02_set_permission_override(uuid,text,text,text)'::regprocedure) INTO d;
 IF (length(d)-length(replace(d,a,'')))/length(a)<>1 OR (length(d)-length(replace(d,b,'')))/length(b)<>1 THEN
   RAISE EXCEPTION 'FR212: Systemrechte-Anker fehlt' USING ERRCODE='55000'; END IF;
 d:=replace(d,a,'p_fachrecht IN (''benutzer.lesen'',''benutzer.verwalten'',''berechtigungen.lesen'',''berechtigungen.verwalten'',''vereinsdaten.alle_lesen'')');
 d:=replace(d,b,'p_fachrecht IN (''berechtigungen.lesen'',''berechtigungen.verwalten'') AND p_entscheidung=''deny''');
 EXECUTE d;
END $system_rights$;

-- Grants bleiben bewusst explizit; keine Schreibfreigabe fuer Tabellen.
DO $post$
BEGIN
 IF has_function_privilege('anon','public.fr212_user_account(bigint)','EXECUTE')
 OR NOT has_function_privilege('authenticated','public.fr212_user_account(bigint)','EXECUTE')
 OR has_function_privilege('authenticated','public.provision_app_user_link(bigint,uuid,text)','EXECUTE')
 THEN RAISE EXCEPTION 'FR212: EXECUTE-Pruefung fehlgeschlagen' USING ERRCODE='42501'; END IF;
END $post$;
COMMIT;
