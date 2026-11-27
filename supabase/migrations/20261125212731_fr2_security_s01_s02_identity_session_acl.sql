-- S01/S02: preserve existing predicates and server-only caller, bind end-user RPCs.
-- Exact source fingerprints were read from DemoDB migration level 292.
DO $migration$
DECLARE
  v_sig regprocedure;
  v_def text;
  v_old text;
  v_new text;
  v_guard constant text := $guard$case when p_auth_user_id is not null
    and (current_setting('role', true) = 'service_role'
      or (current_setting('role', true) = 'authenticated'
        and p_auth_user_id = auth.uid()
        and public.fr212_actor_session_valid()))
    and public.ap03_has_current_membership(p_auth_user_id)
    then ($guard$;
BEGIN
  v_sig := 'public.can_access_mitgliedsaufnahme_vorstand_document(uuid,bigint)'::regprocedure;
  v_def := pg_get_functiondef(v_sig);
  v_def := replace(v_def, E'\r\n', E'\n');
  IF md5(v_def) <> 'f8eb796ce2dff96930d911ffdb2a21da' THEN
    RAISE EXCEPTION 'S01: unexpected function definition; review migration baseline';
  END IF;
  v_old := ' select exists(';
  IF length(v_def) - length(replace(v_def,v_old,'')) <> length(v_old) THEN
    RAISE EXCEPTION 'S01: expected exactly one entry predicate';
  END IF;
  v_new := replace(v_def,v_old,' select ' || v_guard || 'exists(');
  v_old := E' );\n$function$';
  IF length(v_new) - length(replace(v_new,v_old,'')) <> length(v_old) THEN
    RAISE EXCEPTION 'S01: expected exactly one closing predicate';
  END IF;
  v_new := replace(v_new,v_old,E' )) else false end;\n$function$');
  EXECUTE v_new;

  v_sig := 'public.ist_wartungsvertrag_entscheidung_berechtigt(uuid,date)'::regprocedure;
  v_def := pg_get_functiondef(v_sig);
  v_def := replace(v_def, E'\r\n', E'\n');
  IF md5(v_def) <> '782e7c4459030ea2e9ff72274b994ece' THEN
    RAISE EXCEPTION 'S02: unexpected function definition; review migration baseline';
  END IF;
  v_old := '  select exists (';
  IF length(v_def) - length(replace(v_def,v_old,'')) <> length(v_old) THEN
    RAISE EXCEPTION 'S02: expected exactly one entry predicate';
  END IF;
  v_new := replace(v_def,v_old,'  select ' || v_guard || 'exists (');
  v_old := E'  );\n$function$';
  IF length(v_new) - length(replace(v_new,v_old,'')) <> length(v_old) THEN
    RAISE EXCEPTION 'S02: expected exactly one closing predicate';
  END IF;
  v_new := replace(v_new,v_old,E'  )) else false end;\n$function$');
  EXECUTE v_new;
END
$migration$;

REVOKE ALL ON FUNCTION public.can_access_mitgliedsaufnahme_vorstand_document(uuid,bigint) FROM PUBLIC, anon;
REVOKE ALL ON FUNCTION public.ist_wartungsvertrag_entscheidung_berechtigt(uuid,date) FROM PUBLIC, anon;
GRANT EXECUTE ON FUNCTION public.can_access_mitgliedsaufnahme_vorstand_document(uuid,bigint) TO authenticated, service_role;
GRANT EXECUTE ON FUNCTION public.ist_wartungsvertrag_entscheidung_berechtigt(uuid,date) TO authenticated, service_role;

DO $verify$
DECLARE
  v_sig regprocedure;
BEGIN
  FOREACH v_sig IN ARRAY ARRAY[
    'public.can_access_mitgliedsaufnahme_vorstand_document(uuid,bigint)'::regprocedure,
    'public.ist_wartungsvertrag_entscheidung_berechtigt(uuid,date)'::regprocedure
  ] LOOP
    IF has_function_privilege('anon', v_sig, 'EXECUTE')
       OR NOT has_function_privilege('authenticated', v_sig, 'EXECUTE')
       OR NOT has_function_privilege('service_role', v_sig, 'EXECUTE') THEN
      RAISE EXCEPTION 'Unexpected effective EXECUTE ACL: %', v_sig;
    END IF;
  END LOOP;
END
$verify$;
