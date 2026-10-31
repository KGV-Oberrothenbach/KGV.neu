-- G13.1c: Central permission contract shared with KGV.Core and KGV.Web.
-- Bit 23, CanPerformYearEndClosing, has the value 8388608.
create or replace function public.permission_role_base(p_role text)
returns bigint
language sql
immutable
set search_path to 'public', 'pg_temp'
as $$
  select case p_role
    when 'admin' then 16769015::bigint
    when 'vorstand' then 7855607::bigint
    else 10::bigint
  end;
$$;
create or replace function public.known_permission_mask()
returns bigint
language sql
immutable
set search_path to 'public', 'pg_temp'
as $$
  select 16777215::bigint;
$$;
create or replace function public.current_effective_permissions()
returns bigint
language sql
stable
security definer
set search_path to 'public', 'pg_temp'
as $$
  select coalesce((
    select (public.permission_role_base(au.role)
      | coalesce(au.permission_grants, 0))
      & ~coalesce(au.permission_revocations, 0)
    from public.app_user au
    where au.user_id = auth.uid()
    limit 1
  ), 0::bigint);
$$;
create or replace function public.has_effective_permission(p_permission bigint)
returns boolean
language sql
stable
security definer
set search_path to 'public', 'pg_temp'
as $$
  select coalesce(
    p_permission > 0
    and (p_permission & ~public.known_permission_mask()) = 0
    and (public.current_effective_permissions() & p_permission) = p_permission,
    false
  );
$$;
comment on function public.permission_role_base(text) is
  'G13 role base: user=10, vorstand=7855607, admin=16769015.';
comment on function public.known_permission_mask() is
  'G13 known permission mask for bits 0 through 23.';
comment on function public.current_effective_permissions() is
  'G13 effective permissions: (role base | grants) & ~revocations; missing app_user returns 0.';
comment on function public.has_effective_permission(bigint) is
  'G13 central permission check; unknown, null, and non-positive permission requests are denied.';
