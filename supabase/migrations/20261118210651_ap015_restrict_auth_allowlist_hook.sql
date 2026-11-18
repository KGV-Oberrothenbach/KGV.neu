BEGIN;

REVOKE EXECUTE
ON FUNCTION public.before_user_created_allowlist(jsonb)
FROM PUBLIC, anon, authenticated;

GRANT EXECUTE
ON FUNCTION public.before_user_created_allowlist(jsonb)
TO supabase_auth_admin, service_role;

DO $verify$
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
        RAISE EXCEPTION 'Auth-Hook weiterhin öffentlich';
    END IF;

    IF NOT has_function_privilege(
        'supabase_auth_admin',
        'public.before_user_created_allowlist(jsonb)',
        'EXECUTE'
    ) OR NOT has_function_privilege(
        'service_role',
        'public.before_user_created_allowlist(jsonb)',
        'EXECUTE'
    ) THEN
        RAISE EXCEPTION 'Interne Auth-Hook-Rechte fehlen';
    END IF;
END
$verify$;

COMMIT;