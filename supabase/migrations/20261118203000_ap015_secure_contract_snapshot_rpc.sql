-- AP-01.5/F1: internal contract dispatch snapshot helper must not be
-- directly callable through the Data API by anonymous or signed-in users.
-- The legitimate trigger and protected dispatch wrappers run as their
-- postgres owner (SECURITY DEFINER) and retain owner EXECUTE.
-- No data or function definitions are changed.
begin;

revoke all on function public.ensure_vertragsversand_snapshot(bigint)
  from public, anon, authenticated;

grant execute on function public.ensure_vertragsversand_snapshot(bigint)
  to service_role;

-- The migration deliberately makes no RLS-policy or data changes.
commit;
