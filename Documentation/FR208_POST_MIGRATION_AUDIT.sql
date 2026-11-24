-- FR2-08: READ-ONLY audit, DemoDB after independently approved migration.
-- Record results in external audit log and compare with signed pre-migration backup.
SELECT version,name FROM supabase_migrations.schema_migrations
 WHERE version >= '20261124155223' ORDER BY version;
SELECT COUNT(*) AS dokumente,
       COUNT(*) FILTER (WHERE mitglied_id IS NOT NULL) AS mitglied_dokumente,
       COUNT(*) FILTER (WHERE parzelle_id IS NOT NULL) AS parzellen_dokumente,
       COUNT(*) FILTER (WHERE source_kommunikationsvorgang_id IS NOT NULL) AS vertragssnapshots,
       COUNT(*) FILTER (WHERE archiviert_at IS NOT NULL) AS archivierte,
       COUNT(*) FILTER (WHERE historische_vertragsnummer IS NOT NULL) AS historische_vertragsnummern
FROM public.dokument;
SELECT COUNT(*) AS pachtvertragsfassungen FROM public.pachtvertragsfassung;
SELECT COUNT(*) AS vereinsdokumentfassungen FROM public.vereinsdokument_fassung;
SELECT COUNT(*) AS dokumentwirkungen FROM public.dokumenttyp_wirkung;
SELECT bucket_id,COUNT(*) AS objekte FROM storage.objects GROUP BY bucket_id ORDER BY bucket_id;
SELECT id,public FROM storage.buckets ORDER BY id;
SELECT schemaname,tablename,policyname,cmd,roles,qual,with_check
 FROM pg_policies WHERE (schemaname='public' AND tablename IN
 ('dokument','dokumenttyp','dokumenttyp_wirkung','vereinsdokument_typ','vereinsdokument_fassung',
  'serienbrief_vorlage','serienbrief_vorlage_exportfeld','serienbrief_vorlage_freifeld'))
 OR (schemaname='storage' AND tablename='objects')
 ORDER BY schemaname,tablename,cmd,policyname;
SELECT p.proname,pg_get_function_identity_arguments(p.oid) AS args,p.prosecdef AS security_definer,
       has_function_privilege('authenticated',p.oid,'EXECUTE') AS authenticated_execute,
       has_function_privilege('anon',p.oid,'EXECUTE') AS anon_execute
 FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
 WHERE n.nspname='public' AND p.proname LIKE 'fr208_%'
 ORDER BY p.proname;
SELECT table_name,privilege_type,grantee
 FROM information_schema.role_table_grants
 WHERE table_schema='public' AND grantee IN ('authenticated','anon')
 AND table_name IN ('dokument','dokumenttyp','dokumenttyp_wirkung')
 ORDER BY table_name,grantee,privilege_type;
SELECT event_object_table,trigger_name,action_timing,event_manipulation
 FROM information_schema.triggers
 WHERE event_object_schema='public'
 AND event_object_table IN ('dokument','dokumenttyp_wirkung')
 ORDER BY event_object_table,trigger_name;
-- IMPORTANT: This admin SQL inspection does NOT replace authent. HTTP-E2E as real users.
-- FR2-08 hardening: exact security predicates and service-only privilege checks.
SELECT policyname,cmd,qual FROM pg_policies
 WHERE schemaname='public' AND tablename='dokument_wartungsvertrag_zuordnung'
 ORDER BY policyname;
SELECT p.oid::regprocedure::text AS rpc,p.prosecdef,
       has_function_privilege('anon',p.oid,'EXECUTE') AS anon_exec,
       has_function_privilege('authenticated',p.oid,'EXECUTE') AS authenticated_exec,
       has_function_privilege('service_role',p.oid,'EXECUTE') AS service_exec
 FROM pg_proc p WHERE p.pronamespace='public'::regnamespace
 AND p.proname IN
 ('fr208_archive_dokument_service','fr208_dokumenttyp_ist_verwendet',
  'fr208_set_dokumenttyp_wirkungen_checked','fr208_save_dokumenttyp_wirkungen',
  'fr208_guard_historical_document_effect','fr208_block_document_delete')
 ORDER BY rpc;
SELECT tgrelid::regclass::text AS tbl,tgname,pg_get_triggerdef(oid) AS definition
 FROM pg_trigger WHERE NOT tgisinternal AND tgname LIKE 'fr208_%'
 ORDER BY tbl,tgname;
SELECT id,public FROM storage.buckets WHERE id IN
 ('dokumente','pdf-templates','prepared-contract-signatures','vereinsregelwerke') ORDER BY id;
-- WARNING: execute separate, real-user HTTP tests only after all FR2 work packages.
SELECT policyname,cmd,qual FROM pg_policies WHERE schemaname='storage' AND tablename='objects'
  AND policyname LIKE 'vereinsunterlagen_%' ORDER BY policyname;
