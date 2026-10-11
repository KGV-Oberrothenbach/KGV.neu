-- G13.3a: app_user mutations are exclusively server-side RPC operations.
alter table public.app_user add column if not exists is_protected boolean not null default false;
comment on column public.app_user.is_protected is 'Prevents the designated emergency administrator from losing role-management access.';

update public.app_user set is_protected = true where mitglied_id = 7 and is_protected = false;
create unique index if not exists app_user_one_protected_account on public.app_user ((is_protected)) where is_protected;

create or replace function public.update_app_user_access(p_user_id uuid, p_role text, p_permission_grants bigint default 0, p_permission_revocations bigint default 0)
returns void language plpgsql security definer set search_path = public, pg_temp as $$
declare v_target public.app_user%rowtype; v_grants bigint := coalesce(p_permission_grants, 0); v_revocations bigint := coalesce(p_permission_revocations, 0); v_effective bigint;
begin
  if not public.has_effective_permission(512) then raise exception 'CanManageRoles required' using errcode = '42501'; end if;
  select * into v_target from public.app_user where user_id = p_user_id for update;
  if not found then raise exception 'Target app_user not found' using errcode = 'P0002'; end if;
  if p_role not in ('admin', 'vorstand', 'user') then raise exception 'Invalid role' using errcode = '22023'; end if;
  if v_grants < 0 or v_revocations < 0 or (v_grants & ~public.known_permission_mask()) <> 0 or (v_revocations & ~public.known_permission_mask()) <> 0 then raise exception 'Invalid permission mask' using errcode = '22023'; end if;
  v_effective := (public.permission_role_base(p_role) | v_grants) & ~v_revocations;
  if v_target.is_protected and (p_role <> 'admin' or (v_effective & 512) <> 512) then raise exception 'Protected account must retain admin role management' using errcode = '42501'; end if;
  update public.app_user set role = p_role, permission_grants = v_grants, permission_revocations = v_revocations, updated_at = now() where user_id = p_user_id;
end;
$$;

create or replace function public.unlink_app_user(p_user_id uuid)
returns void language plpgsql security definer set search_path = public, pg_temp as $$
declare v_target public.app_user%rowtype;
begin
  if not public.has_effective_permission(512) then raise exception 'CanManageRoles required' using errcode = '42501'; end if;
  select * into v_target from public.app_user where user_id = p_user_id for update;
  if not found then raise exception 'Target app_user not found' using errcode = 'P0002'; end if;
  if v_target.is_protected then raise exception 'Protected app_user cannot be unlinked' using errcode = '42501'; end if;
  update public.mitglied set auth_user_id = null where auth_user_id = v_target.user_id;
  delete from public.app_user where user_id = v_target.user_id;
end;
$$;

alter table public.app_user enable row level security;
drop policy if exists app_user_admin_full on public.app_user;
drop policy if exists app_user_select_own_or_admin on public.app_user;
drop policy if exists app_user_demo_admin_full on public.app_user;
drop policy if exists app_user_select_by_effective_roles on public.app_user;
create policy app_user_select_by_effective_roles on public.app_user for select to authenticated using (
  user_id = auth.uid() or public.has_effective_permission(262144) or public.has_effective_permission(512)
);
revoke insert, update, delete on table public.app_user from authenticated;
grant execute on function public.update_app_user_access(uuid, text, bigint, bigint) to authenticated;
grant execute on function public.unlink_app_user(uuid) to authenticated;
