-- P11 D6: The verification status is an enum.  Coalescing that enum with an
-- empty string attempts to cast the empty string to the enum before the text
-- comparison and therefore fails for every invocation.  Keep the complete
-- function contract and change only the comparison operand to text.
begin;

do $$
declare
  v_definition text;
  v_old text := 'lower(coalesce(a.pruefstatus, ''''))';
  v_new text := 'lower(coalesce(a.pruefstatus::text, ''''))';
begin
  select pg_get_functiondef(
           'public.rpc_hauptzaehler_jahresverbrauch_segmente(integer)'::regprocedure
         )
    into v_definition;

  if position(v_old in v_definition) > 0 then
    v_definition := replace(v_definition, v_old, v_new);
    execute v_definition;
  elsif position(v_new in v_definition) = 0 then
    raise exception 'D6 expected verification-status expression was not found'
      using errcode = '2BP01';
  end if;

  if pg_get_functiondef(
       'public.rpc_hauptzaehler_jahresverbrauch_segmente(integer)'::regprocedure
     ) not like '%' || v_new || '%' then
    raise exception 'D6 verification-status correction was not applied'
      using errcode = '2BP01';
  end if;
end;
$$;

-- Supabase's API roles receive broad default function privileges.  Keep the
-- central account and effective-permission contract on its intended callers:
-- account changes require an authenticated actor, provisioning stays
-- service-only, and no anonymous caller can invoke the context helpers.
revoke all on function public.update_app_user_access(uuid, text, bigint, bigint)
  from public, anon;
revoke all on function public.unlink_app_user(uuid) from public, anon;
revoke all on function public.provision_app_user_link(bigint, uuid, text)
  from public, anon, authenticated;
revoke all on function public.current_app_user_context() from public, anon;
revoke all on function public.current_effective_permissions() from public, anon;
revoke all on function public.current_mitglied_id() from public, anon;
revoke all on function public.has_effective_permission(bigint) from public, anon;
revoke all on function public.has_any_effective_permission(bigint) from public, anon;
revoke all on function public.sync_current_member_email() from public, anon;

grant execute on function public.update_app_user_access(uuid, text, bigint, bigint)
  to authenticated, service_role;
grant execute on function public.unlink_app_user(uuid)
  to authenticated, service_role;
grant execute on function public.provision_app_user_link(bigint, uuid, text)
  to service_role;
grant execute on function public.current_app_user_context()
  to authenticated, service_role;
grant execute on function public.current_effective_permissions()
  to authenticated, service_role;
grant execute on function public.current_mitglied_id()
  to authenticated, service_role;
grant execute on function public.has_effective_permission(bigint)
  to authenticated, service_role;
grant execute on function public.has_any_effective_permission(bigint)
  to authenticated, service_role;
grant execute on function public.sync_current_member_email()
  to authenticated, service_role;

commit;
