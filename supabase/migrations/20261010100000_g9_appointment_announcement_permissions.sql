-- G9.2: appointment and announcement management uses the shared effective-permission matrix.
create or replace function public.has_effective_permission(p_permission bigint)
returns boolean language sql stable security definer set search_path to 'public', 'pg_temp' as $$
  select coalesce(((((case au.role when 'admin' then 8380407::bigint when 'vorstand' then 7855607::bigint else 10::bigint end | coalesce(au.permission_grants, 0)) & ~coalesce(au.permission_revocations, 0)) & p_permission) = p_permission), false)
  from public.app_user au where au.user_id = auth.uid() limit 1;
$$;

-- Replace legacy role-based and earlier management policies. There intentionally is no DELETE policy.
drop policy if exists termin_admin_full on public.termin;
drop policy if exists termin_demo_admin_full on public.termin;
drop policy if exists termin_select_visible_authenticated on public.termin;
drop policy if exists termin_manage_select on public.termin;
drop policy if exists termin_manage_insert on public.termin;
drop policy if exists termin_manage_update on public.termin;
drop policy if exists termin_manage_delete on public.termin;
create policy termin_manage_select on public.termin for select to authenticated using (
  public.has_effective_permission(2097152) and ((not public.is_demo_or_reviewer() and coalesce(is_demo, false) = false) or (public.is_demo_or_reviewer() and coalesce(is_demo, false) = true)));
create policy termin_manage_insert on public.termin for insert to authenticated with check (
  public.has_effective_permission(2097152) and ((not public.is_demo_or_reviewer() and coalesce(is_demo, false) = false) or (public.is_demo_or_reviewer() and coalesce(is_demo, false) = true)));
create policy termin_manage_update on public.termin for update to authenticated using (
  public.has_effective_permission(2097152) and ((not public.is_demo_or_reviewer() and coalesce(is_demo, false) = false) or (public.is_demo_or_reviewer() and coalesce(is_demo, false) = true))) with check (
  public.has_effective_permission(2097152) and ((not public.is_demo_or_reviewer() and coalesce(is_demo, false) = false) or (public.is_demo_or_reviewer() and coalesce(is_demo, false) = true)));
create policy termin_select_visible_authenticated on public.termin for select to authenticated using (
  aktiv = true and (sichtbar_ab is null or sichtbar_ab <= public.kgv_local_now()) and (sichtbar_bis is null or sichtbar_bis >= public.kgv_local_now()) and ((not public.is_demo_or_reviewer() and coalesce(is_demo, false) = false) or (public.is_demo_or_reviewer() and coalesce(is_demo, false) = true)));

drop policy if exists bekanntmachung_admin_full on public.bekanntmachung;
drop policy if exists bekanntmachung_demo_admin_full on public.bekanntmachung;
drop policy if exists bekanntmachung_select_visible_authenticated on public.bekanntmachung;
drop policy if exists bekanntmachung_manage_select on public.bekanntmachung;
drop policy if exists bekanntmachung_manage_insert on public.bekanntmachung;
drop policy if exists bekanntmachung_manage_update on public.bekanntmachung;
drop policy if exists bekanntmachung_manage_delete on public.bekanntmachung;
create policy bekanntmachung_manage_select on public.bekanntmachung for select to authenticated using (
  public.has_effective_permission(4194304) and ((not public.is_demo_or_reviewer() and coalesce(is_demo, false) = false) or (public.is_demo_or_reviewer() and coalesce(is_demo, false) = true)));
create policy bekanntmachung_manage_insert on public.bekanntmachung for insert to authenticated with check (
  public.has_effective_permission(4194304) and ((not public.is_demo_or_reviewer() and coalesce(is_demo, false) = false) or (public.is_demo_or_reviewer() and coalesce(is_demo, false) = true)));
create policy bekanntmachung_manage_update on public.bekanntmachung for update to authenticated using (
  public.has_effective_permission(4194304) and ((not public.is_demo_or_reviewer() and coalesce(is_demo, false) = false) or (public.is_demo_or_reviewer() and coalesce(is_demo, false) = true))) with check (
  public.has_effective_permission(4194304) and ((not public.is_demo_or_reviewer() and coalesce(is_demo, false) = false) or (public.is_demo_or_reviewer() and coalesce(is_demo, false) = true)));
create policy bekanntmachung_select_visible_authenticated on public.bekanntmachung for select to authenticated using (
  aktiv = true and (sichtbar_ab is null or sichtbar_ab <= public.kgv_local_now()) and (sichtbar_bis is null or sichtbar_bis >= public.kgv_local_now()) and ((not public.is_demo_or_reviewer() and coalesce(is_demo, false) = false) or (public.is_demo_or_reviewer() and coalesce(is_demo, false) = true)));

create or replace function public.acquire_browser_edit_lock(p_entity_type text, p_entity_id text, p_timeout_seconds integer default 600)
returns table (acquired boolean, locked_by_user_id uuid, locked_by_display_name text, expires_at timestamptz)
language plpgsql security definer set search_path = public as $$
declare v_user_id uuid := auth.uid(); v_now timestamptz := now(); v_timeout integer := greatest(60, least(coalesce(p_timeout_seconds, 600), 1800)); v_locked_by uuid; v_expires_at timestamptz;
begin
  if v_user_id is null or not exists (select 1 from public.app_user where user_id = v_user_id) then raise exception 'Keine KGV-Berechtigung vorhanden.' using errcode = '42501'; end if;
  if p_entity_type is null or p_entity_type !~ '^[a-z0-9_]{1,64}$' or p_entity_id is null or char_length(p_entity_id) not between 1 and 128 then raise exception 'Ungueltiger Sperrschluessel.' using errcode = '22023'; end if;
  if p_entity_type in ('arbeitseinsatz', 'arbeitseinsatz_anmeldung') then
    if not public.has_effective_permission(1048576) or p_entity_id !~ '^[0-9]+$' then raise exception 'Für Arbeitseinsätze fehlt ManageWorkAssignments oder der Schlüssel ist ungültig.' using errcode = '42501'; end if;
    if p_entity_type = 'arbeitseinsatz' and not exists (select 1 from public.arbeitseinsatz a where a.id = p_entity_id::bigint and ((not public.is_demo_or_reviewer() and coalesce(a.is_demo, false) = false) or (public.is_demo_or_reviewer() and coalesce(a.is_demo, false) = true))) then raise exception 'Arbeitseinsatz liegt außerhalb des zulässigen Scopes.' using errcode = '42501'; end if;
    if p_entity_type = 'arbeitseinsatz_anmeldung' and not exists (select 1 from public.arbeitseinsatz_anmeldung aa join public.arbeitseinsatz a on a.id = aa.arbeitseinsatz_id where aa.id = p_entity_id::bigint and ((not public.is_demo_or_reviewer() and coalesce(a.is_demo, false) = false) or (public.is_demo_or_reviewer() and public.is_demo_member_arbeitseinsatz_scope(aa.mitglied_id, aa.arbeitseinsatz_id)))) then raise exception 'Anmeldung liegt außerhalb des zulässigen Scopes.' using errcode = '42501'; end if;
  end if;
  if p_entity_type = 'termin' and (not public.has_effective_permission(2097152) or not exists (select 1 from public.termin t where t.id = p_entity_id::bigint and ((not public.is_demo_or_reviewer() and coalesce(t.is_demo, false) = false) or (public.is_demo_or_reviewer() and coalesce(t.is_demo, false) = true)))) then raise exception 'Termin liegt außerhalb des zulässigen Scopes.' using errcode = '42501'; end if;
  if p_entity_type = 'bekanntmachung' and (not public.has_effective_permission(4194304) or not exists (select 1 from public.bekanntmachung b where b.id = p_entity_id::bigint and ((not public.is_demo_or_reviewer() and coalesce(b.is_demo, false) = false) or (public.is_demo_or_reviewer() and coalesce(b.is_demo, false) = true)))) then raise exception 'Bekanntmachung liegt außerhalb des zulässigen Scopes.' using errcode = '42501'; end if;
  delete from public.browser_edit_lock l where l.expires_at <= v_now;
  insert into public.browser_edit_lock(entity_type, entity_id, locked_by_user_id, locked_at, expires_at) values (p_entity_type, p_entity_id, v_user_id, v_now, v_now + make_interval(secs => v_timeout)) on conflict (entity_type, entity_id) do update set locked_by_user_id = excluded.locked_by_user_id, locked_at = case when browser_edit_lock.locked_by_user_id = excluded.locked_by_user_id then browser_edit_lock.locked_at else excluded.locked_at end, expires_at = excluded.expires_at where browser_edit_lock.locked_by_user_id = excluded.locked_by_user_id or browser_edit_lock.expires_at <= v_now returning browser_edit_lock.locked_by_user_id, browser_edit_lock.expires_at into v_locked_by, v_expires_at;
  if v_locked_by is null then select l.locked_by_user_id, l.expires_at into v_locked_by, v_expires_at from public.browser_edit_lock l where l.entity_type = p_entity_type and l.entity_id = p_entity_id; end if;
  return query select v_locked_by = v_user_id, v_locked_by, coalesce(public.browser_edit_lock_display_name(v_locked_by), 'Ein anderer Benutzer'), v_expires_at;
end;
$$;

-- Scope comes from the authenticated session, never from a client-provided is_demo value.
create or replace function public.enforce_appointment_announcement_demo_scope()
returns trigger language plpgsql security definer set search_path = public as $$
begin
  if tg_op = 'INSERT' then new.is_demo := public.is_demo_or_reviewer();
  elsif new.is_demo is distinct from old.is_demo then raise exception 'Der Demo-Scope darf nicht geändert werden.' using errcode = '42501'; end if;
  return new;
end;
$$;
drop trigger if exists termin_enforce_demo_scope on public.termin;
create trigger termin_enforce_demo_scope before insert or update on public.termin for each row execute function public.enforce_appointment_announcement_demo_scope();
drop trigger if exists bekanntmachung_enforce_demo_scope on public.bekanntmachung;
create trigger bekanntmachung_enforce_demo_scope before insert or update on public.bekanntmachung for each row execute function public.enforce_appointment_announcement_demo_scope();
