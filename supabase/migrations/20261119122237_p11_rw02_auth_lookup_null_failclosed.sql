-- P11 RW02: Fail-closed auth lookup, no production data changes.
do $guard$
declare
  v_definition text;
  v_old constant text := 'if auth.role() <> ''service_role'' and not public.has_effective_permission(512) then';
  v_new constant text := 'if auth.role() is distinct from ''service_role'' and public.has_effective_permission(512) is not true then';
begin
  select pg_get_functiondef('public.find_auth_user_id_by_email(text)'::regprocedure)
    into v_definition;

  if position(v_old in v_definition) = 0 then
    raise exception 'P11 RW02: unexpected auth lookup definition'
      using errcode='2BP01';
  end if;

  execute replace(v_definition, v_old, v_new);

  if position(
    v_new in pg_get_functiondef('public.find_auth_user_id_by_email(text)'::regprocedure)
  ) = 0 then
    raise exception 'P11 RW02: guard not applied'
      using errcode='2BP01';
  end if;
end
$guard$;

revoke execute on function public.find_auth_user_id_by_email(text)
  from public, anon;

grant execute on function public.find_auth_user_id_by_email(text)
  to authenticated, service_role;

do $acl$
begin
  if has_function_privilege(
       'anon', 'public.find_auth_user_id_by_email(text)', 'EXECUTE'
     )
     or not has_function_privilege(
       'authenticated', 'public.find_auth_user_id_by_email(text)', 'EXECUTE'
     )
     or not has_function_privilege(
       'service_role', 'public.find_auth_user_id_by_email(text)', 'EXECUTE'
     ) then
    raise exception 'P11 RW02: incorrect function ACL'
      using errcode='2BP01';
  end if;
end
$acl$;

comment on function public.find_auth_user_id_by_email(text) is
  'P11 RW02: authenticated with effective CanManageRoles (512) or service_role; missing JWT role fails closed.';