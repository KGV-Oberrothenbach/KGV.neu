DO $migration$
DECLARE f regprocedure; def text; patched text; anchor text; replacement text; expected_before text; expected_after text;
BEGIN
  FOR f,anchor,replacement,expected_before,expected_after IN SELECT * FROM (VALUES
    ('public.impressum_kontakte()'::regprocedure,$a$where auth.role() in ('authenticated', 'service_role')$a$,$b$where (current_setting('role', true) = 'service_role' OR (auth.role() = 'authenticated' AND EXISTS (SELECT 1 FROM public.current_app_user_context())))$b$,'413226b253bae36b9207dc00cd190628','0e5f5f9035c9018bfec8f31bb8d35bfb'),
    ('public.vertragsversand_mailtext_snapshot()'::regprocedure,$a$begin
  if to_regclass$a$,$b$begin
  IF current_setting('role', true) IS DISTINCT FROM 'service_role'
     AND NOT EXISTS (SELECT 1 FROM public.current_app_user_context()) THEN
    RAISE EXCEPTION 'Gueltige Sitzung erforderlich.' USING ERRCODE='42501';
  END IF;
  if to_regclass$b$,'98e2ad14450757f87c412ac10a8dd2ae','de997cbbd5b46eed7b08f6a5ee431417')
  ) v(f,anchor,replacement,expected_before,expected_after) LOOP
    SELECT pg_get_functiondef(f) INTO def;
    IF md5(def) IS DISTINCT FROM expected_before OR has_function_privilege('anon',f,'execute') OR NOT has_function_privilege('authenticated',f,'execute') OR NOT has_function_privilege('service_role',f,'execute') THEN
      RAISE EXCEPTION 'Unexpected contact RPC baseline: %',f;
    END IF;
    IF (length(def)-length(replace(def,anchor,'')))/length(anchor)<>1 THEN RAISE EXCEPTION 'Anchor mismatch: %',f; END IF;
    patched:=replace(def,anchor,replacement);
    IF md5(patched) IS DISTINCT FROM expected_after THEN RAISE EXCEPTION 'Patch fingerprint mismatch: %',f; END IF;
    EXECUTE patched;
    IF md5(pg_get_functiondef(f)) IS DISTINCT FROM expected_after OR has_function_privilege('anon',f,'execute') OR NOT has_function_privilege('authenticated',f,'execute') OR NOT has_function_privilege('service_role',f,'execute') THEN
      RAISE EXCEPTION 'Unexpected contact RPC postcondition: %',f;
    END IF;
  END LOOP;
END
$migration$;
