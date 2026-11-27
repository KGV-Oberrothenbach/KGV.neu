-- FR2-RB04B: deny anonymous invocation of six internal/legacy helpers.
-- Preserve function bodies and the existing authenticated/service role grants.
DO $verify_before$
DECLARE
  f regprocedure;
  n integer;
  digest text;
BEGIN
  SELECT count(*),md5(string_agg(md5(replace(pg_get_functiondef(p.oid), E'\r\n', E'\n')),E'\n' ORDER BY p.proname)) INTO n,digest
  FROM pg_proc p JOIN pg_namespace ns ON ns.oid=p.pronamespace
  WHERE ns.nspname='public' AND p.proname IN ('can_access_live_internal_data','current_user_has_active_lease_for_parcel','is_admin','is_admin_or_vorstand','is_productive_admin_or_vorstand','vertragsversand_mailtext_snapshot');
  IF n<>6 OR digest IS DISTINCT FROM 'a8c914dbc3636a986f3b2bc279f47b50' THEN
    RAISE EXCEPTION 'Unexpected helper definitions';
  END IF;
  FOREACH f IN ARRAY ARRAY[
    'public.can_access_live_internal_data()'::regprocedure,
    'public.current_user_has_active_lease_for_parcel(bigint,date)'::regprocedure,
    'public.is_admin()'::regprocedure,
    'public.is_admin_or_vorstand()'::regprocedure,
    'public.is_productive_admin_or_vorstand()'::regprocedure,
    'public.vertragsversand_mailtext_snapshot()'::regprocedure
  ] LOOP
    IF NOT has_function_privilege('authenticated',f,'EXECUTE')
       OR NOT has_function_privilege('service_role',f,'EXECUTE') THEN
      RAISE EXCEPTION 'Unexpected existing ACL: %', f;
    END IF;
    EXECUTE format('REVOKE EXECUTE ON FUNCTION %s FROM PUBLIC, anon',f);
    IF has_function_privilege('anon',f,'EXECUTE')
       OR NOT has_function_privilege('authenticated',f,'EXECUTE')
       OR NOT has_function_privilege('service_role',f,'EXECUTE') THEN
      RAISE EXCEPTION 'Unexpected resulting ACL: %', f;
    END IF;
  END LOOP;
END
$verify_before$;
