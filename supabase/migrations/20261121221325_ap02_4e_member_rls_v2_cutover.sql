-- AP-02.4E: Mitglied-RLS und formelle Mitgliedschaftsbeendigung auf Fachrechte 2.0.
-- Validiert gemeinsamen VDB-/DemoDB-Ausgangsstand; keine Demo-User-IDs.
-- Bewusster Fail-Closed-Cutover, Hauptrollen/individuelle Grants sind separat zu migrieren.
DO $pre$
DECLARE
 v_policies integer;
 v_old text := '  if not public.has_effective_permission(4) then';
 v_function text;
BEGIN
 IF to_regprocedure('public.ap02_can_read_mitglied(bigint)') IS NULL
    OR to_regprocedure('public.ap02_has_effective_right(text)') IS NULL THEN
   RAISE EXCEPTION 'AP-02.4D/AP-02.2 Rechtepruefung fehlt' USING ERRCODE='55000';
 END IF;
 SELECT count(*) INTO v_policies
 FROM pg_policies
 WHERE schemaname='public' AND tablename='mitglied'
   AND policyname IN (
    'mitglied_select_authorized_scope',
    'mitglied_insert_effective_member_creation',
    'mitglied_update_effective_edit_all_members',
    'mitglied_delete_effective_edit_all_members');
 IF v_policies <> 4 OR
    (SELECT count(*) FROM pg_policies WHERE schemaname='public' AND tablename='mitglied') <> 4
 THEN RAISE EXCEPTION 'Unerwartete Mitglied-RLS-Policies: %', v_policies USING ERRCODE='55000'; END IF;
 IF NOT EXISTS (
   SELECT 1 FROM pg_policies
   WHERE schemaname='public' AND tablename='mitglied'
     AND policyname='mitglied_select_authorized_scope'
     AND qual LIKE '%can_read_mitglied(id, auth_user_id)%'
 ) THEN RAISE EXCEPTION 'Mitglieds-Lesepolicy wurde gegenueber der Basis veraendert' USING ERRCODE='55000'; END IF;
 v_function := pg_get_functiondef('public.end_membership(bigint,text,text)'::regprocedure);
 IF (length(v_function)-length(replace(v_function,v_old,'')))/length(v_old) <> 1 THEN
   RAISE EXCEPTION 'Mitgliedschaftsbeendigungs-RPC entspricht nicht der erwarteten Basis' USING ERRCODE='55000';
 END IF;
END
$pre$;

-- Fachliche Berechtigungen und Zeilen-/Objektberechtigungen trennen.
ALTER POLICY mitglied_select_authorized_scope ON public.mitglied
  USING (public.ap02_can_read_mitglied(id));

ALTER POLICY mitglied_insert_effective_member_creation ON public.mitglied
  WITH CHECK (
    public.ap02_has_effective_right('mitglied.anlegen')
    AND erstellt_von_auth_user_id = auth.uid()
  );

ALTER POLICY mitglied_update_effective_edit_all_members ON public.mitglied
  USING (public.ap02_has_effective_right('mitglied.bearbeiten'))
  WITH CHECK (public.ap02_has_effective_right('mitglied.bearbeiten'));

-- Physische Mitgliederloeschung ist kein Fachrecht; formellen Prozess nutzen.
DROP POLICY mitglied_delete_effective_edit_all_members ON public.mitglied;
REVOKE DELETE ON TABLE public.mitglied FROM PUBLIC, anon, authenticated;

-- SECURITY DEFINER-Endpunkt sonst weiter mit altem Bit 4 aufrufbar:
-- gleicher Fachrechte-2.0-Gate statt des bisherigen Bitrechts.
DO $end_membership$
DECLARE
 v_def text := pg_get_functiondef('public.end_membership(bigint,text,text)'::regprocedure);
 v_old text := '  if not public.has_effective_permission(4) then';
 v_new text := '  if public.ap02_has_effective_right(''mitgliedschaft.verwalten'') is not true then';
BEGIN
 IF (length(v_def)-length(replace(v_def,v_old,'')))/length(v_old) <> 1 THEN
   RAISE EXCEPTION 'Mitgliedschaftsbeendigungs-Gate nicht eindeutig' USING ERRCODE='55000';
 END IF;
 EXECUTE replace(v_def,v_old,v_new);
END
$end_membership$;
