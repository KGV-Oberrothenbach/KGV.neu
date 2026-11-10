-- P11 AP-02A / A2.1: Ending a membership is member administration.
-- It must honour individual grants and revocations of CanEditAllMembers (4),
-- rather than treating the admin/vorstand role template as the authorization.
begin;

do $$
declare
  v_definition text;
  v_old text := 'v_is_productive_admin := public.is_productive_admin_or_vorstand();';
  v_new text := 'v_is_productive_admin := public.has_effective_permission(4);';
begin
  select pg_get_functiondef('public.end_membership(bigint,text)'::regprocedure)
    into v_definition;

  if position(v_old in v_definition) = 0 then
    raise exception 'P11 A2.1: expected role-template guard in end_membership was not found'
      using errcode = '2BP01';
  end if;

  v_definition := replace(v_definition, v_old, v_new);
  execute v_definition;

  if pg_get_functiondef('public.end_membership(bigint,text)'::regprocedure)
       not like '%' || v_new || '%' then
    raise exception 'P11 A2.1: effective CanEditAllMembers guard was not applied'
      using errcode = '2BP01';
  end if;
end;
$$;

comment on function public.end_membership(bigint,text) is
  'P11 AP-02A A2.1: membership ending requires effective CanEditAllMembers (4); locks and member-state checks remain mandatory.';

-- The lookup is used by the invitation/link-management path.  Service role is
-- a technical caller; every client caller must retain CanManageRoles (512).
do $$
declare
  v_definition text;
  v_old text := 'if auth.role() <> ''service_role'' and not public.is_admin_or_vorstand() then';
  v_new text := 'if auth.role() <> ''service_role'' and not public.has_effective_permission(512) then';
begin
  select pg_get_functiondef('public.find_auth_user_id_by_email(text)'::regprocedure)
    into v_definition;

  if position(v_old in v_definition) = 0 then
    raise exception 'P11 A2.1: expected role-template guard in find_auth_user_id_by_email was not found'
      using errcode = '2BP01';
  end if;

  v_definition := replace(v_definition, v_old, v_new);
  execute v_definition;

  if pg_get_functiondef('public.find_auth_user_id_by_email(text)'::regprocedure)
       not like '%' || v_new || '%' then
    raise exception 'P11 A2.1: effective CanManageRoles guard was not applied to auth lookup'
      using errcode = '2BP01';
  end if;
end;
$$;

comment on function public.find_auth_user_id_by_email(text) is
  'P11 AP-02A A2.1: client lookup of an auth user ID requires effective CanManageRoles (512); service_role remains a technical caller.';

commit;
