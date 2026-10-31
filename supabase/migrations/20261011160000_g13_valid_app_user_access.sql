-- G13.4: Supabase authentication alone never establishes KGV application access.
create or replace function public.current_app_user_context()
returns table(user_id uuid, mitglied_id bigint, role text, permission_grants bigint, permission_revocations bigint, is_protected boolean, is_demo_account boolean)
language sql stable security definer set search_path = public, pg_temp as $$
  select au.user_id, au.mitglied_id, au.role, coalesce(au.permission_grants, 0),
         coalesce(au.permission_revocations, 0), coalesce(au.is_protected, false),
         coalesce(au.is_demo_account, false)
  from public.app_user au
  join public.mitglied m on m.id = au.mitglied_id and m.auth_user_id = au.user_id
  where au.user_id = auth.uid()
    and au.mitglied_id is not null and au.mitglied_id > 0
    and au.role in ('admin', 'vorstand', 'user');
$$;
create or replace function public.current_effective_permissions()
returns bigint language sql stable security definer set search_path = public, pg_temp as $$
  select coalesce((select (public.permission_role_base(c.role) | c.permission_grants) & ~c.permission_revocations
    from public.current_app_user_context() c limit 1), 0::bigint);
$$;
create or replace function public.current_mitglied_id()
returns integer language sql stable security definer set search_path = public, pg_temp as $$
  select (select c.mitglied_id::integer from public.current_app_user_context() c limit 1);
$$;
create or replace function public.update_app_user_access(p_user_id uuid, p_role text, p_permission_grants bigint default 0, p_permission_revocations bigint default 0)
returns void language plpgsql security definer set search_path = public, pg_temp as $$
declare v_target public.app_user%rowtype; v_grants bigint := coalesce(p_permission_grants, 0); v_revocations bigint := coalesce(p_permission_revocations, 0); v_effective bigint;
begin
  if not public.has_effective_permission(512) then raise exception 'CanManageRoles required' using errcode = '42501'; end if;
  select * into v_target from public.app_user where user_id = p_user_id for update;
  if not found then raise exception 'Target app_user not found' using errcode = 'P0002'; end if;
  if not exists (select 1 from public.mitglied m where m.id = v_target.mitglied_id and m.auth_user_id = v_target.user_id) then
    raise exception 'InvalidAppUserLink' using errcode = '23514';
  end if;
  if p_role not in ('admin', 'vorstand', 'user') then raise exception 'Invalid role' using errcode = '22023'; end if;
  if v_grants < 0 or v_revocations < 0 or (v_grants & ~public.known_permission_mask()) <> 0 or (v_revocations & ~public.known_permission_mask()) <> 0 then raise exception 'Invalid permission mask' using errcode = '22023'; end if;
  v_effective := (public.permission_role_base(p_role) | v_grants) & ~v_revocations;
  if v_target.is_protected and (p_role <> 'admin' or (v_effective & 512) <> 512) then raise exception 'Protected account must retain admin role management' using errcode = '42501'; end if;
  update public.app_user set role = p_role, permission_grants = v_grants, permission_revocations = v_revocations, updated_at = now() where user_id = p_user_id;
end;
$$;
grant execute on function public.current_app_user_context() to authenticated;
