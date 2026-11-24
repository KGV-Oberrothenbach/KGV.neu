-- FR2-09: nur lesende Kontrollabfragen für DemoDB; keine ProduktivDB-Abfragen mit Mutationen.
SELECT count(*) AS migrations, max(version) AS letzte_migration
 FROM supabase_migrations.schema_migrations;
SELECT code, aktiv, operation, objekt_scope FROM public.fachrecht
 WHERE code IN ('vorgang.lesen','vorgang.bearbeiten','vertrag.vorbereiten','vertrag.versenden',
 'druck.verwalten','serienbrief.erstellen','vorlage.verwalten','vereinsdaten.alle_lesen') ORDER BY code;
SELECT tablename, policyname, cmd, qual, with_check FROM pg_policies
 WHERE schemaname='public'
 AND tablename IN ('kommunikationsvorgang','versandinstanz','versandinstanz_dokument_snapshot',
 'versandinstanz_empfaenger_snapshot','versandinstanz_versuch',
 'vertragsversand_anlage_snapshot','vertragsversand_vertragspartei_snapshot')
 ORDER BY tablename,policyname;
SELECT table_name,grantee,privilege_type FROM information_schema.role_table_grants
 WHERE table_schema='public'
 AND table_name IN ('kommunikationsvorgang','versandinstanz',
 'versandinstanz_dokument_snapshot','versandinstanz_empfaenger_snapshot',
 'versandinstanz_versuch','vertragsversand_anlage_snapshot',
 'vertragsversand_vertragspartei_snapshot')
 AND grantee IN ('anon','authenticated','PUBLIC') ORDER BY table_name,grantee,privilege_type;
SELECT p.proname,pg_get_function_identity_arguments(p.oid) AS args,
 p.prosecdef AS security_definer,
 has_function_privilege('anon',p.oid,'EXECUTE') AS anon_execute,
 has_function_privilege('authenticated',p.oid,'EXECUTE') AS authenticated_execute
 FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
 WHERE n.nspname='public' AND p.proname LIKE 'fr209_%' ORDER BY p.proname;
SELECT 'kommunikationsvorgang' AS t,count(*) AS n FROM public.kommunikationsvorgang
 UNION ALL SELECT 'versandinstanz',count(*) FROM public.versandinstanz
 UNION ALL SELECT 'versandinstanz_dokument_snapshot',count(*) FROM public.versandinstanz_dokument_snapshot
 UNION ALL SELECT 'versandinstanz_empfaenger_snapshot',count(*) FROM public.versandinstanz_empfaenger_snapshot
 UNION ALL SELECT 'versandinstanz_versuch',count(*) FROM public.versandinstanz_versuch;
