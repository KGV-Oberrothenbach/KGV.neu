BEGIN READ ONLY;

DO $test$
BEGIN
    IF has_function_privilege(
        'anon',
        'public.before_user_created_allowlist(jsonb)',
        'EXECUTE'
    ) OR has_function_privilege(
        'authenticated',
        'public.before_user_created_allowlist(jsonb)',
        'EXECUTE'
    ) THEN
        RAISE EXCEPTION 'Auth-Hook-Berechtigung fehlerhaft';
    END IF;

    IF NOT has_function_privilege(
        'supabase_auth_admin',
        'public.before_user_created_allowlist(jsonb)',
        'EXECUTE'
    ) THEN
        RAISE EXCEPTION 'Supabase Auth darf Hook nicht ausführen';
    END IF;
END
$test$;

SET LOCAL ROLE anon;

DO $test$
BEGIN
    BEGIN
        PERFORM public.before_user_created_allowlist(
            '{"user":{"email":"ap015@example.test",
              "app_metadata":{"provider":"email"}}}'::jsonb
        );
        RAISE EXCEPTION 'Anonymer Aufruf unerwartet erlaubt';
    EXCEPTION WHEN insufficient_privilege THEN
        NULL;
    END;
END
$test$;

RESET ROLE;
ROLLBACK;