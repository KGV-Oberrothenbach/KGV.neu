-- G13.3b: link a newly prepared auth user to a member atomically.
create or replace function public.provision_app_user_link(
  p_mitglied_id bigint,
  p_user_id uuid,
  p_role text
)
returns void
language plpgsql
security definer
set search_path = public, pg_temp
as $$
declare
  v_member public.mitglied%rowtype;
  v_app_user public.app_user%rowtype;
begin
  if p_mitglied_id is null or p_mitglied_id <= 0 or p_user_id is null then
    raise exception 'Invalid member or auth user id' using errcode = '22023';
  end if;
  if p_role not in ('admin', 'vorstand', 'user') then
    raise exception 'Invalid role' using errcode = '22023';
  end if;

  select * into v_member from public.mitglied where id = p_mitglied_id for update;
  if not found then
    raise exception 'Member not found' using errcode = 'P0002';
  end if;

  if exists (select 1 from public.mitglied where auth_user_id = p_user_id and id <> p_mitglied_id) then
    raise exception 'Auth user is already linked to another member' using errcode = '23505';
  end if;

  select * into v_app_user from public.app_user where user_id = p_user_id or mitglied_id = p_mitglied_id for update;
  if found then
    if v_app_user.user_id = p_user_id and v_app_user.mitglied_id = p_mitglied_id and v_member.auth_user_id = p_user_id then
      raise exception 'Member is already linked' using errcode = '23505';
    end if;
    raise exception 'Conflicting app_user linkage' using errcode = '23505';
  end if;

  if v_member.auth_user_id is not null and v_member.auth_user_id <> p_user_id then
    raise exception 'Member is already linked to another auth user' using errcode = '23505';
  end if;

  update public.mitglied set auth_user_id = p_user_id where id = p_mitglied_id;
  insert into public.app_user (user_id, mitglied_id, role, permission_grants, permission_revocations, is_protected, updated_at)
  values (p_user_id, p_mitglied_id, p_role, 0, 0, false, now());
end;
$$;

revoke all on function public.provision_app_user_link(bigint, uuid, text) from public;
revoke all on function public.provision_app_user_link(bigint, uuid, text) from authenticated;
grant execute on function public.provision_app_user_link(bigint, uuid, text) to service_role;
