-- FR2-07: LESENDE, unabhaengige SQL-/Persistenzpruefung auf DEMODB.
-- Auszufuehren nach migriertem Demo-Cutover; KEINE ProduktivDB.
-- Keine Geheimnisse/Passwoerter/JWTs in den Bericht uebernehmen.
WITH policies AS (
 SELECT tablename,policyname,cmd,qual,with_check
 FROM pg_catalog.pg_policies WHERE schemaname='public'
   AND tablename IN ('arbeitsstunde','arbeitseinsatz','arbeitseinsatz_anmeldung','arbeitsstunde_pruefverlauf')
),functions AS (
 SELECT p.proname,pg_get_function_identity_arguments(p.oid) AS args,
        has_function_privilege('anon',p.oid,'EXECUTE') AS anon_exec,
        has_function_privilege('authenticated',p.oid,'EXECUTE') AS auth_exec
 FROM pg_catalog.pg_proc p JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
 WHERE n.nspname='public' AND p.proname IN (
 'review_arbeitsstunde','submit_arbeitseinsatz_arbeitsstunde',
 'confirm_arbeitseinsatz_arbeitsstunde','rpc_export_arbeitsstunden_jahre',
 'rpc_export_arbeitsstunden_uebersicht','rpc_export_arbeitsstunden_uebersicht_json')
)
SELECT jsonb_pretty(jsonb_build_object(
  'migrations', (SELECT count(*) FROM supabase_migrations.schema_migrations),
  'fr06_present', (SELECT EXISTS(SELECT 1 FROM supabase_migrations.schema_migrations WHERE version='20261122235600')),
  'fr07_versions',(SELECT coalesce(jsonb_agg(version ORDER BY version),'[]'::jsonb)
     FROM supabase_migrations.schema_migrations WHERE name='fr2_07_work_hours_assignment_rights_cutover'),
  'right_grants',(SELECT coalesce(jsonb_agg(to_jsonb(x)),'[]'::jsonb) FROM (
    SELECT f.code,count(*) FILTER(WHERE u.erlaubt) AS users_allowed
    FROM public.fachrecht f
    LEFT JOIN LATERAL (
      SELECT rights.erlaubt FROM public.app_user app
      CROSS JOIN LATERAL public.ap02_calculate_user_rights(app.user_id) rights
      WHERE rights.code=f.code
    ) u ON true
    WHERE f.code IN ('arbeitsstunden.lesen','arbeitsstunden.bearbeiten','arbeitsstunden.freigeben','arbeitseinsatz.verwalten')
    GROUP BY f.code
  ) x),
  'policies',(SELECT coalesce(jsonb_agg(to_jsonb(p)),'[]'::jsonb) FROM policies p),
  'exec_rights',(SELECT coalesce(jsonb_agg(to_jsonb(f)),'[]'::jsonb) FROM functions f),
  'audit_rls',(SELECT c.relrowsecurity FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
     WHERE n.nspname='public' AND c.relname='fr207_arbeitsaudit'),
  'audit_exists',(SELECT to_regclass('public.fr207_arbeitsaudit') IS NOT NULL),
  'hour_counts',(SELECT coalesce(jsonb_agg(to_jsonb(x)),'[]'::jsonb)
      FROM (SELECT status,freigegeben,count(*) AS anzahl FROM public.arbeitsstunde GROUP BY 1,2) x),
  'assignment_count',(SELECT count(*) FROM public.arbeitseinsatz),
  'registration_count',(SELECT count(*) FROM public.arbeitseinsatz_anmeldung)
)) AS fr207_verification;

-- Nach HTTP-E2E mit positiver synthetischer Datensatz-ID gesondert pruefen:
-- SELECT id,mitglied_id,saison_id,datum,stunden,status,freigegeben,
--        genehmigt_von,genehmigt_am,arbeitseinsatz_anmeldung_id
-- FROM public.arbeitsstunde WHERE id = <FR207_HTTP_TEST_ID>;
-- SELECT id,arbeitsstunde_id,aktion,geprueft_von,geprueft_am
-- FROM public.arbeitsstunde_pruefverlauf
-- WHERE arbeitsstunde_id = <FR207_HTTP_TEST_ID> ORDER BY id;
-- SELECT id,akteur,objekt_tabelle,objekt_id,operation,geaendert_am
-- FROM public.fr207_arbeitsaudit
-- WHERE objekt_tabelle='arbeitsstunde' AND objekt_id='<FR207_HTTP_TEST_ID>'
-- ORDER BY id;
-- Bei Wiederholungen: Kein zusaetzlicher Stundenbeleg, kein zweiter Audit-Eintrag.
