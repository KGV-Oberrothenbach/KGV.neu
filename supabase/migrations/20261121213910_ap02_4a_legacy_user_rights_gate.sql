-- AP-02.4A: Alte Benutzer-/Rechteverwaltungs-Einstiege an Fachrechte 2.0 binden.
-- Ausschliesslich DemoDB bis zur gesonderten Produktiv-Freigabe.
-- Bewusst keine globale Umschaltung aller Fach-RPCs oder Edge-Functions.
DO $ap024a$
DECLARE v record; v_definition text; v_count integer;
BEGIN
  IF to_regprocedure('public.ap02_can_manage_rights()') IS NULL
     OR to_regprocedure('public.ap02_has_effective_right(text)') IS NULL
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
      SELECT 1 FROM public.protected_admin_identity p
      WHERE NOT EXISTS (SELECT 1 FROM public.app_user_zusatzrolle z
        WHERE z.user_id=p.user_id AND z.zusatzrolle_code='admin')
    ) OR
    EXISTS (
       SELECT a.user_id FROM public.app_user a WHERE a.is_protected = true
       EXCEPT
       SELECT p.user_id FROM public.protected_admin_identity p
     )
     OR EXISTS (
       SELECT p.user_id FROM public.protected_admin_identity p
       EXCEPT
       SELECT a.user_id FROM public.app_user a WHERE a.is_protected = true
     )
  THEN
    RAISE EXCEPTION 'AP-02-Schattenmodell fehlt' USING ERRCODE='55000';
  END IF;
  FOR v IN
    SELECT * FROM (VALUES
      ('public.update_app_user_access(uuid,text,bigint,bigint)',
       'if not public.has_effective_permission(512) then',
       'if public.ap02_can_manage_rights() is not true then'),
      ('public.begin_auth_user_deletion(uuid,bigint,uuid)',
       'IF auth.uid() IS NULL OR public.has_effective_permission(512) IS NOT TRUE THEN',
       'IF auth.uid() IS NULL OR public.ap02_can_manage_rights() IS NOT TRUE THEN'),
      ('public.find_auth_user_id_by_email(text)',
       'if auth.role() is distinct from ''service_role'' and public.has_effective_permission(512) is not true then',
       'if auth.role() is distinct from ''service_role'' and public.ap02_can_manage_rights() is not true then'),
      ('public.unlink_app_user(uuid)',
       'IF auth.uid() IS NULL OR NOT public.has_effective_permission(512) THEN',
       'IF auth.uid() IS NULL OR public.ap02_can_manage_rights() IS NOT TRUE THEN'),
      ('public.begin_auth_user_deletion(uuid,bigint,uuid)',
       'IF v_target.is_protected THEN',
       'IF v_target.is_protected OR EXISTS (SELECT 1 FROM public.protected_admin_identity p WHERE p.user_id = p_user_id) OR EXISTS (SELECT 1 FROM public.app_user_zusatzrolle z WHERE z.user_id = p_user_id AND z.zusatzrolle_code = ''admin'') THEN'),
      ('public.unlink_app_user(uuid)',
       'IF v_target.is_protected THEN',
       'IF v_target.is_protected OR EXISTS (SELECT 1 FROM public.protected_admin_identity p WHERE p.user_id = p_user_id) OR EXISTS (SELECT 1 FROM public.app_user_zusatzrolle z WHERE z.user_id = p_user_id AND z.zusatzrolle_code = ''admin'') THEN')
    ) AS patches(signature,old_fragment,new_fragment)
  LOOP
    IF to_regprocedure(v.signature) IS NULL THEN
      RAISE EXCEPTION 'Erwartete Funktion fehlt: %', v.signature;
    END IF;
    v_definition := pg_get_functiondef(to_regprocedure(v.signature));
    v_count := (length(v_definition)-length(replace(v_definition,v.old_fragment,''))) / length(v.old_fragment);
    IF v_count <> 1 THEN
      RAISE EXCEPTION 'Abbruch: % hat % Fundstellen statt genau einer: %',v.signature,v_count,v.old_fragment;
    END IF;
    EXECUTE replace(v_definition,v.old_fragment,v.new_fragment);
  END LOOP;
END
$ap024a$;

-- Das reine Benutzerlisten-Leserecht wird ebenfalls nach Fachrechte 2.0
-- beurteilt. Eigene app_user-Zeilen bleiben fuer angemeldete Benutzer lesbar.
ALTER POLICY app_user_select_by_effective_roles ON public.app_user
USING (user_id = auth.uid() OR public.ap02_has_effective_right('benutzer.lesen'));
