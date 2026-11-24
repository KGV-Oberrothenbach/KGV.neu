-- Independent catalog assertion; no data mutations.
DO $fr211_audit$
DECLARE v_bad text;
BEGIN
  IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'authenticated' AND (rolsuper OR rolbypassrls)) THEN
    RAISE EXCEPTION 'FR211 authenticated can bypass RLS';
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_proc WHERE oid='public.rpc_execute_export(text,jsonb)'::regprocedure AND NOT prosecdef)
     OR NOT EXISTS (SELECT 1 FROM pg_proc WHERE oid='public.rpc_fr211_jae_export(integer)'::regprocedure AND NOT prosecdef)
     OR NOT has_function_privilege('authenticated','public.rpc_execute_export(text,jsonb)','EXECUTE')
     OR has_function_privilege('anon','public.rpc_execute_export(text,jsonb)','EXECUTE')
     OR has_function_privilege('anon','public.rpc_fr211_jae_export(integer)','EXECUTE') THEN
    RAISE EXCEPTION 'FR211 gateway mode/ACL audit failed';
  END IF;
  SELECT p.oid::regprocedure::text INTO v_bad FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
    WHERE n.nspname='public' AND p.proname LIKE 'rpc_export_%'
      AND p.proname NOT IN ('rpc_export_arbeitsstunden_jahre','rpc_export_jahresablesung_jahre')
      AND (has_function_privilege('anon',p.oid,'EXECUTE') OR has_function_privilege('authenticated',p.oid,'EXECUTE')) LIMIT 1;
  IF v_bad IS NOT NULL THEN RAISE EXCEPTION 'FR211 raw public source still executable: %',v_bad; END IF;
  IF has_function_privilege('authenticated','public.rpc_execute_table_or_view_export(text)','EXECUTE')
     OR has_function_privilege('anon','public.rpc_execute_table_or_view_export(text)','EXECUTE') THEN
    RAISE EXCEPTION 'FR211 legacy gateway still executable';
  END IF;
  IF (SELECT count(*) FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
      WHERE n.nspname='fr211_export_private' AND NOT p.prosecdef AND NOT has_function_privilege('anon',p.oid,'EXECUTE')) <> 11 THEN
    RAISE EXCEPTION 'FR211 private source count/security mismatch';
  END IF;
  IF has_schema_privilege('anon','fr211_export_private','USAGE')
     OR has_schema_privilege('authenticated','fr211_export_private','CREATE') THEN RAISE EXCEPTION 'FR211 private schema ACL unsafe'; END IF;
  IF EXISTS (SELECT 1 FROM pg_db_role_setting r, unnest(r.setconfig) cfg
      WHERE cfg LIKE 'pgrst.db_schemas=%' AND cfg LIKE '%fr211_export_private%') THEN
    RAISE EXCEPTION 'FR211 private schema included in exposed schemas';
  END IF;
  IF EXISTS (SELECT 1 FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
      WHERE n.nspname='public' AND c.relname IN ('app_export_definition','app_export_column_definition','app_export_filter_definition','mitglied','app_user','parzelle','parzellen_belegung','pachtverhaeltnis','pachtverhaeltnis_beteiligung','arbeitsstunde','wartungsvertraege','wartungsvertrag_zuordnungen','saison','zaehler','zaehler_ablesung') AND NOT c.relrowsecurity) THEN
    RAISE EXCEPTION 'FR211 RLS disabled on a source table';
  END IF;
  IF EXISTS (SELECT 1 FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
      WHERE n.nspname='public' AND c.relname IN ('v_pflichtstunden_uebersicht','v_zaehler_eichstatus') AND NOT coalesce(c.reloptions @> ARRAY['security_invoker=true'],false)) THEN
    RAISE EXCEPTION 'FR211 export view bypasses caller';
  END IF;
  IF EXISTS (SELECT 1 FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace WHERE n.nspname='public' AND p.proname IN ('rpc_hauptzaehler_jahresverbrauch_segmente','rpc_jahresverbrauch_haupt_und_unterzaehler_g6_basis','rpc_jahresverbrauch_haupt_und_unterzaehler','rpc_jahresverbrauch_haupt_unter_differenz') AND p.prosecdef) THEN
    RAISE EXCEPTION 'FR211 JAE source still definer';
  END IF;
END;
$fr211_audit$;
SELECT 'FR211_CATALOG_PASS' AS result;
