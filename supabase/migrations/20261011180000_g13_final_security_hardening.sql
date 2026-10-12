-- G13.7: stale clients may modify known bits without destroying stored future bits.
create or replace function public.update_app_user_access(p_user_id uuid, p_role text, p_permission_grants bigint default 0, p_permission_revocations bigint default 0)
returns void language plpgsql security definer set search_path = public, pg_temp as $$
declare
  v_target public.app_user%rowtype;
  v_known_mask bigint := public.known_permission_mask();
  v_requested_grants bigint := coalesce(p_permission_grants, 0);
  v_requested_revocations bigint := coalesce(p_permission_revocations, 0);
  v_grants bigint;
  v_revocations bigint;
  v_effective bigint;
begin
  if not public.has_effective_permission(512) then raise exception 'CanManageRoles required' using errcode = '42501'; end if;
  select * into v_target from public.app_user where user_id = p_user_id for update;
  if not found then raise exception 'Target app_user not found' using errcode = 'P0002'; end if;
  if not exists (select 1 from public.mitglied m where m.id = v_target.mitglied_id and m.auth_user_id = v_target.user_id) then raise exception 'InvalidAppUserLink' using errcode = '23514'; end if;
  if p_role not in ('admin', 'vorstand', 'user') then raise exception 'Invalid role' using errcode = '22023'; end if;
  if v_requested_grants < 0 or v_requested_revocations < 0 then raise exception 'Invalid permission mask' using errcode = '22023'; end if;

  v_grants := (coalesce(v_target.permission_grants, 0) & ~v_known_mask) | (v_requested_grants & v_known_mask);
  v_revocations := (coalesce(v_target.permission_revocations, 0) & ~v_known_mask) | (v_requested_revocations & v_known_mask);
  v_effective := (public.permission_role_base(p_role) | (v_grants & v_known_mask)) & ~(v_revocations & v_known_mask);
  if v_target.is_protected and (p_role <> 'admin' or (v_effective & 512) <> 512) then raise exception 'Protected account must retain admin role management' using errcode = '42501'; end if;
  update public.app_user set role = p_role, permission_grants = v_grants, permission_revocations = v_revocations, updated_at = now() where user_id = p_user_id;
end;
$$;

revoke all on function public.update_app_user_access(uuid, text, bigint, bigint) from public;
revoke all on function public.unlink_app_user(uuid) from public;
revoke all on function public.provision_app_user_link(bigint, uuid, text) from public;
revoke all on function public.current_app_user_context() from public;
revoke all on function public.current_effective_permissions() from public;
revoke all on function public.current_mitglied_id() from public;
revoke all on function public.has_effective_permission(bigint) from public;
revoke all on function public.sync_current_member_email() from public;
grant execute on function public.update_app_user_access(uuid, text, bigint, bigint) to authenticated;
grant execute on function public.unlink_app_user(uuid) to authenticated;
grant execute on function public.provision_app_user_link(bigint, uuid, text) to service_role;
grant execute on function public.current_app_user_context() to authenticated;
grant execute on function public.current_effective_permissions() to authenticated;
grant execute on function public.current_mitglied_id() to authenticated;
grant execute on function public.has_effective_permission(bigint) to authenticated;
grant execute on function public.sync_current_member_email() to authenticated;
