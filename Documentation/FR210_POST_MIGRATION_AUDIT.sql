-- FR2-10: Nur lesende, unabhängige Nachkontrolle (DemoDB, nach Freigabe).
-- Vergleich der Zeilen- und Versionszahlen mit separaten Vorher-Fingerabdrücken.
SELECT COUNT(*) AS migration_count, MAX(version) AS latest_migration,
       md5(string_agg(version, ',' ORDER BY version)) AS migration_fingerprint
FROM supabase_migrations.schema_migrations;

SELECT c.relname AS table_name, c.relrowsecurity AS rls_active
FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
WHERE n.nspname='public' AND c.relname IN
  ('termin','bekanntmachung','vereinsfunktion','vereinsfunktion_besetzung',
   'vereinsfunktion_vertretung','vereinsfunktion_verwendung','vereinskonfiguration','saison')
ORDER BY c.relname;

SELECT tablename, policyname, cmd,
       roles::text AS roles, qual AS using_expression, with_check AS check_expression
FROM pg_policies WHERE schemaname='public' AND tablename IN
  ('termin','bekanntmachung','vereinsfunktion','vereinsfunktion_besetzung',
   'vereinsfunktion_vertretung','vereinsfunktion_verwendung','vereinskonfiguration','saison')
ORDER BY tablename, cmd, policyname;

SELECT t.tablename,
       has_table_privilege('anon', format('public.%I',t.tablename), 'INSERT') AS anon_insert,
       has_table_privilege('anon', format('public.%I',t.tablename), 'UPDATE') AS anon_update,
       has_table_privilege('anon', format('public.%I',t.tablename), 'DELETE') AS anon_delete,
       has_table_privilege('authenticated', format('public.%I',t.tablename), 'TRUNCATE') AS authenticated_truncate,
       has_table_privilege('authenticated', format('public.%I',t.tablename), 'TRIGGER') AS authenticated_trigger,
       has_table_privilege('authenticated', format('public.%I',t.tablename), 'REFERENCES') AS authenticated_references,
       has_table_privilege('authenticated', format('public.%I',t.tablename), 'DELETE') AS authenticated_delete
FROM (VALUES ('termin'),('bekanntmachung'),('vereinsfunktion'),('vereinsfunktion_besetzung'),
             ('vereinsfunktion_vertretung'),('vereinsfunktion_verwendung'),('vereinskonfiguration'),('saison')) t(tablename)
ORDER BY t.tablename;

SELECT tgrelid::regclass::text AS table_name, tgname AS trigger_name,
       pg_get_triggerdef(oid) AS definition
FROM pg_trigger WHERE NOT tgisinternal AND tgname LIKE 'trg_fr210_%'
ORDER BY table_name, trigger_name;

SELECT 'saison' AS table_name, count(*) AS rows FROM public.saison
UNION ALL SELECT 'termin',count(*) FROM public.termin
UNION ALL SELECT 'bekanntmachung',count(*) FROM public.bekanntmachung
UNION ALL SELECT 'vereinsfunktion',count(*) FROM public.vereinsfunktion
UNION ALL SELECT 'vereinsfunktion_besetzung',count(*) FROM public.vereinsfunktion_besetzung
UNION ALL SELECT 'vereinsfunktion_vertretung',count(*) FROM public.vereinsfunktion_vertretung
UNION ALL SELECT 'vereinsfunktion_verwendung',count(*) FROM public.vereinsfunktion_verwendung
UNION ALL SELECT 'vereinskonfiguration',count(*) FROM public.vereinskonfiguration
ORDER BY table_name;
