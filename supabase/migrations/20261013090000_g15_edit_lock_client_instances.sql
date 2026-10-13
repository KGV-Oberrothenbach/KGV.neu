-- G15.3a: A lock lease belongs to a user and a concrete client instance.
-- Existing rows intentionally remain nullable until their current lease expires.

alter table public.browser_edit_lock
  add column if not exists client_instance_id uuid;

create index if not exists ix_browser_edit_lock_owner_client
  on public.browser_edit_lock (locked_by_user_id, client_instance_id);

create or replace function public.acquire_browser_edit_lock_core(
  p_entity_type text,
  p_entity_id text,
  p_client_instance_id uuid,
  p_timeout_seconds integer default 600
)
returns table (
  acquired boolean,
  locked_by_user_id uuid,
  locked_by_display_name text,
  expires_at timestamptz
)
language plpgsql
security definer
set search_path = public
as $$
declare
  v_user_id uuid := auth.uid();
  v_now timestamptz := now();
  v_timeout integer := greatest(60, least(coalesce(p_timeout_seconds, 600), 1800));
  v_locked_by uuid;
  v_locked_client_instance_id uuid;
  v_expires_at timestamptz;
  v_legacy_locked_by uuid;
  v_legacy_locked_at timestamp without time zone;
  v_rows integer;
  v_acquired boolean := false;
begin
  if v_user_id is null then
    raise exception 'Anmeldung erforderlich.' using errcode = '42501';
  end if;
  if p_client_instance_id is null then
    raise exception 'Client-Instanz erforderlich.' using errcode = '22023';
  end if;
  if not exists (select 1 from public.app_user where user_id = v_user_id) then
    raise exception 'Keine KGV-Berechtigung vorhanden.' using errcode = '42501';
  end if;
  if p_entity_type is null or p_entity_type !~ '^[a-z0-9_]{1,64}$'
     or p_entity_id is null or char_length(p_entity_id) not between 1 and 128 then
    raise exception 'Ungueltiger Sperrschluessel.' using errcode = '22023';
  end if;

  -- Keep the final G8/G9 permission and demo-scope checks intact.
  if p_entity_type in ('arbeitseinsatz', 'arbeitseinsatz_anmeldung') then
    if not public.has_effective_permission(1048576) or p_entity_id !~ '^[0-9]+$' then
      raise exception 'Für Arbeitseinsätze fehlt ManageWorkAssignments oder der Schlüssel ist ungültig.' using errcode = '42501';
    end if;
    if p_entity_type = 'arbeitseinsatz' and not exists (
      select 1 from public.arbeitseinsatz a
      where a.id = p_entity_id::bigint
        and ((not public.is_demo_or_reviewer() and coalesce(a.is_demo, false) = false)
          or (public.is_demo_or_reviewer() and coalesce(a.is_demo, false) = true))
    ) then
      raise exception 'Arbeitseinsatz liegt außerhalb des zulässigen Scopes.' using errcode = '42501';
    end if;
    if p_entity_type = 'arbeitseinsatz_anmeldung' and not exists (
      select 1
      from public.arbeitseinsatz_anmeldung aa
      join public.arbeitseinsatz a on a.id = aa.arbeitseinsatz_id
      where aa.id = p_entity_id::bigint
        and ((not public.is_demo_or_reviewer() and coalesce(a.is_demo, false) = false)
          or (public.is_demo_or_reviewer() and public.is_demo_member_arbeitseinsatz_scope(aa.mitglied_id, aa.arbeitseinsatz_id)))
    ) then
      raise exception 'Anmeldung liegt außerhalb des zulässigen Scopes.' using errcode = '42501';
    end if;
  end if;
  if p_entity_type = 'termin' and (
    not public.has_effective_permission(2097152)
    or not exists (
      select 1 from public.termin t
      where t.id = p_entity_id::bigint
        and ((not public.is_demo_or_reviewer() and coalesce(t.is_demo, false) = false)
          or (public.is_demo_or_reviewer() and coalesce(t.is_demo, false) = true))
    )
  ) then
    raise exception 'Termin liegt außerhalb des zulässigen Scopes.' using errcode = '42501';
  end if;
  if p_entity_type = 'bekanntmachung' and (
    not public.has_effective_permission(4194304)
    or not exists (
      select 1 from public.bekanntmachung b
      where b.id = p_entity_id::bigint
        and ((not public.is_demo_or_reviewer() and coalesce(b.is_demo, false) = false)
          or (public.is_demo_or_reviewer() and coalesce(b.is_demo, false) = true))
    )
  ) then
    raise exception 'Bekanntmachung liegt außerhalb des zulässigen Scopes.' using errcode = '42501';
  end if;

  delete from public.browser_edit_lock l where l.expires_at <= v_now;

  insert into public.browser_edit_lock (
    entity_type, entity_id, locked_by_user_id, client_instance_id, locked_at, expires_at
  ) values (
    p_entity_type, p_entity_id, v_user_id, p_client_instance_id, v_now,
    v_now + make_interval(secs => v_timeout)
  )
  on conflict (entity_type, entity_id) do update
    set expires_at = excluded.expires_at
  where browser_edit_lock.expires_at <= v_now
     or (
       browser_edit_lock.locked_by_user_id = v_user_id
       and browser_edit_lock.client_instance_id is not distinct from p_client_instance_id
     )
  returning browser_edit_lock.locked_by_user_id, browser_edit_lock.client_instance_id, browser_edit_lock.expires_at
    into v_locked_by, v_locked_client_instance_id, v_expires_at;

  if v_locked_by is null then
    select l.locked_by_user_id, l.client_instance_id, l.expires_at
      into v_locked_by, v_locked_client_instance_id, v_expires_at
    from public.browser_edit_lock l
    where l.entity_type = p_entity_type and l.entity_id = p_entity_id;
  end if;

  v_acquired := v_locked_by = v_user_id
    and v_locked_client_instance_id is not distinct from p_client_instance_id;

  -- Preserve the legacy lock fields required by older RPCs and triggers.
  if v_acquired and p_entity_type = 'mitglied' and p_entity_id ~ '^[0-9]+$' then
    update public.mitglied
       set lockedbyuserid = v_user_id,
           lockat = v_now at time zone 'UTC'
     where id = p_entity_id::bigint
       and (
         lockedbyuserid is null
         or lockedbyuserid = v_user_id
         or (lockat is not null and lockat < (v_now at time zone 'UTC') - make_interval(secs => v_timeout))
       );
    get diagnostics v_rows = row_count;
    if v_rows = 0 then
      select lockedbyuserid, lockat into v_legacy_locked_by, v_legacy_locked_at
      from public.mitglied where id = p_entity_id::bigint;
      delete from public.browser_edit_lock l
      where l.entity_type = p_entity_type and l.entity_id = p_entity_id
        and l.locked_by_user_id = v_user_id
        and l.client_instance_id is not distinct from p_client_instance_id;
      v_locked_by := v_legacy_locked_by;
      v_locked_client_instance_id := null;
      v_expires_at := (v_legacy_locked_at at time zone 'UTC') + make_interval(secs => v_timeout);
      v_acquired := false;
    end if;
  elsif v_acquired and p_entity_type = 'arbeitsstunde' and p_entity_id ~ '^[0-9]+$' then
    update public.arbeitsstunde
       set lockedbyuserid = v_user_id,
           lockat = v_now at time zone 'UTC'
     where id = p_entity_id::integer
       and (
         lockedbyuserid is null
         or lockedbyuserid = v_user_id
         or (lockat is not null and lockat < (v_now at time zone 'UTC') - make_interval(secs => v_timeout))
       );
    get diagnostics v_rows = row_count;
    if v_rows = 0 then
      select lockedbyuserid, lockat into v_legacy_locked_by, v_legacy_locked_at
      from public.arbeitsstunde where id = p_entity_id::integer;
      delete from public.browser_edit_lock l
      where l.entity_type = p_entity_type and l.entity_id = p_entity_id
        and l.locked_by_user_id = v_user_id
        and l.client_instance_id is not distinct from p_client_instance_id;
      v_locked_by := v_legacy_locked_by;
      v_locked_client_instance_id := null;
      v_expires_at := (v_legacy_locked_at at time zone 'UTC') + make_interval(secs => v_timeout);
      v_acquired := false;
    end if;
  end if;

  return query select
    v_acquired,
    v_locked_by,
    coalesce(public.browser_edit_lock_display_name(v_locked_by), 'Ein anderer Benutzer'),
    v_expires_at;
end;
$$;

create or replace function public.release_browser_edit_lock_core(
  p_entity_type text,
  p_entity_id text,
  p_client_instance_id uuid
)
returns boolean
language plpgsql
security definer
set search_path = public
as $$
declare
  v_user_id uuid := auth.uid();
  v_released boolean := false;
begin
  if v_user_id is null or p_client_instance_id is null then return false; end if;

  delete from public.browser_edit_lock l
  where l.entity_type = p_entity_type and l.entity_id = p_entity_id
    and l.locked_by_user_id = v_user_id
    and l.client_instance_id is not distinct from p_client_instance_id
  returning true into v_released;

  if not coalesce(v_released, false) then return false; end if;

  if p_entity_type = 'mitglied' and p_entity_id ~ '^[0-9]+$' then
    update public.mitglied set lockedbyuserid = null, lockat = null
    where id = p_entity_id::bigint and lockedbyuserid = v_user_id;
  elsif p_entity_type = 'arbeitsstunde' and p_entity_id ~ '^[0-9]+$' then
    update public.arbeitsstunde set lockedbyuserid = null, lockat = null
    where id = p_entity_id::integer and lockedbyuserid = v_user_id;
  end if;
  return true;
end;
$$;

create or replace function public.release_all_browser_edit_locks_core(
  p_client_instance_id uuid
)
returns boolean
language plpgsql
security definer
set search_path = public
as $$
declare
  v_user_id uuid := auth.uid();
  v_lock record;
begin
  if v_user_id is null or p_client_instance_id is null then return false; end if;

  for v_lock in
    delete from public.browser_edit_lock l
    where l.locked_by_user_id = v_user_id
      and l.client_instance_id is not distinct from p_client_instance_id
    returning l.entity_type, l.entity_id
  loop
    if v_lock.entity_type = 'mitglied' and v_lock.entity_id ~ '^[0-9]+$' then
      update public.mitglied set lockedbyuserid = null, lockat = null
      where id = v_lock.entity_id::bigint and lockedbyuserid = v_user_id;
    elsif v_lock.entity_type = 'arbeitsstunde' and v_lock.entity_id ~ '^[0-9]+$' then
      update public.arbeitsstunde set lockedbyuserid = null, lockat = null
      where id = v_lock.entity_id::integer and lockedbyuserid = v_user_id;
    end if;
  end loop;
  return true;
end;
$$;

-- Legacy callers share one deterministic per-user client identity during rollout.
create or replace function public.acquire_browser_edit_lock(
  p_entity_type text,
  p_entity_id text,
  p_timeout_seconds integer default 600
)
returns table (
  acquired boolean,
  locked_by_user_id uuid,
  locked_by_display_name text,
  expires_at timestamptz
)
language sql
security definer
set search_path = public
as $$
  select * from public.acquire_browser_edit_lock_core(
    p_entity_type, p_entity_id, auth.uid(), p_timeout_seconds
  );
$$;

create or replace function public.release_browser_edit_lock(
  p_entity_type text,
  p_entity_id text
)
returns boolean
language sql
security definer
set search_path = public
as $$
  select public.release_browser_edit_lock_core(p_entity_type, p_entity_id, auth.uid());
$$;

create or replace function public.release_all_browser_edit_locks()
returns boolean
language sql
security definer
set search_path = public
as $$
  select public.release_all_browser_edit_locks_core(auth.uid());
$$;

create or replace function public.acquire_browser_edit_lock_for_client(
  p_entity_type text,
  p_entity_id text,
  p_client_instance_id uuid,
  p_timeout_seconds integer default 600
)
returns table (
  acquired boolean,
  locked_by_user_id uuid,
  locked_by_display_name text,
  expires_at timestamptz
)
language sql
security definer
set search_path = public
as $$
  select * from public.acquire_browser_edit_lock_core(
    p_entity_type, p_entity_id, p_client_instance_id, p_timeout_seconds
  );
$$;

create or replace function public.release_browser_edit_lock_for_client(
  p_entity_type text,
  p_entity_id text,
  p_client_instance_id uuid
)
returns boolean
language sql
security definer
set search_path = public
as $$
  select public.release_browser_edit_lock_core(p_entity_type, p_entity_id, p_client_instance_id);
$$;

create or replace function public.release_all_browser_edit_locks_for_client(
  p_client_instance_id uuid
)
returns boolean
language sql
security definer
set search_path = public
as $$
  select public.release_all_browser_edit_locks_core(p_client_instance_id);
$$;

revoke all on function public.acquire_browser_edit_lock_core(text, text, uuid, integer) from public, anon, authenticated;
revoke all on function public.release_browser_edit_lock_core(text, text, uuid) from public, anon, authenticated;
revoke all on function public.release_all_browser_edit_locks_core(uuid) from public, anon, authenticated;

revoke all on function public.acquire_browser_edit_lock(text, text, integer) from public, anon;
revoke all on function public.release_browser_edit_lock(text, text) from public, anon;
revoke all on function public.release_all_browser_edit_locks() from public, anon;
grant execute on function public.acquire_browser_edit_lock(text, text, integer) to authenticated;
grant execute on function public.release_browser_edit_lock(text, text) to authenticated;
grant execute on function public.release_all_browser_edit_locks() to authenticated;

revoke all on function public.acquire_browser_edit_lock_for_client(text, text, uuid, integer) from public, anon;
revoke all on function public.release_browser_edit_lock_for_client(text, text, uuid) from public, anon;
revoke all on function public.release_all_browser_edit_locks_for_client(uuid) from public, anon;
grant execute on function public.acquire_browser_edit_lock_for_client(text, text, uuid, integer) to authenticated;
grant execute on function public.release_browser_edit_lock_for_client(text, text, uuid) to authenticated;
grant execute on function public.release_all_browser_edit_locks_for_client(uuid) to authenticated;
