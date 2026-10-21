-- Supabase role grants or default privileges may leave an explicit anon grant
-- after PUBLIC was revoked. Keep this privileged projection callable only by
-- application sessions and backend service calls.
revoke all on function public.impressum_kontakte() from public;
revoke all on function public.impressum_kontakte() from anon;
grant execute on function public.impressum_kontakte() to authenticated, service_role;
