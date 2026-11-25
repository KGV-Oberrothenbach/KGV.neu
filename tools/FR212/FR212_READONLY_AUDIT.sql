-- FR2-12: ausschliesslich SELECT. Kann in der DemoDB nach FR2-11 wiederholt werden.
-- Keine personenbezogenen Zeileninhalte exportieren.
SELECT 'schema_katalog' AS pruefung,
  (SELECT count(*) FROM public.fachrecht)::text AS wert,
  '55' AS soll;
SELECT 'hauptrollen' AS pruefung,(SELECT count(*) FROM public.hauptrolle)::text AS wert,'5' AS soll;
SELECT 'zusatzrollen' AS pruefung,(SELECT count(*) FROM public.zusatzrolle)::text AS wert,'2' AS soll;
SELECT 'app_user' AS pruefung,count(*) AS anzahl FROM public.app_user;
SELECT 'fehlende_hauptrolle' AS pruefung,count(*) AS anzahl
  FROM public.app_user u LEFT JOIN public.app_user_hauptrolle r USING(user_id)
  WHERE r.user_id IS NULL;
SELECT 'unplausibler_mitgliedsbezug' AS pruefung,count(*) AS anzahl
  FROM public.app_user u LEFT JOIN public.mitglied m ON m.id=u.mitglied_id
  WHERE m.id IS NULL OR m.auth_user_id IS DISTINCT FROM u.user_id;
SELECT c.relname AS tabelle,c.relrowsecurity AS rls,
       has_table_privilege('authenticated',c.oid,'SELECT') AS auth_select,
       has_table_privilege('authenticated',c.oid,'INSERT') AS auth_insert,
       has_table_privilege('authenticated',c.oid,'UPDATE') AS auth_update,
       has_table_privilege('authenticated',c.oid,'DELETE') AS auth_delete
FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
WHERE n.nspname='public' AND c.relkind='r'
 AND (c.relname LIKE 'app_user%' OR c.relname LIKE '%fachrecht%'
      OR c.relname LIKE '%rolle%' OR c.relname IN
      ('protected_admin_identity','berechtigungs_audit','auth_user_delete_lock'))
ORDER BY 1;
SELECT p.proname,pg_get_function_identity_arguments(p.oid) AS argumente,
       p.prosecdef AS security_definer,
       has_function_privilege('anon',p.oid,'EXECUTE') AS anon_execute,
       has_function_privilege('authenticated',p.oid,'EXECUTE') AS auth_execute
FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
WHERE n.nspname='public' AND
 (p.proname LIKE 'ap02_%' OR p.proname IN
 ('begin_auth_user_deletion','provision_app_user_link','update_app_user_access',
  'find_auth_user_id_by_email','fr212_user_account'))
ORDER BY 1,2;
-- Nicht automatisch ausfuehren: keine SELECTs auf auth.users oder Personendaten.
