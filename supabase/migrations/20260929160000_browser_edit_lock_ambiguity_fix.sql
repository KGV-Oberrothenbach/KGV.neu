-- Behebt mehrdeutige Spaltennamen der Tabellen-Rueckgabeparameter in PL/pgSQL.
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
language plpgsql
security definer
set search_path = public
as $$
declare
  v_user_id uuid := auth.uid();
  v_now timestamptz := now();
  v_timeout integer := greatest(60, least(coalesce(p_timeout_seconds, 600), 1800));
  v_locked_by uuid;
  v_expires_at timestamptz;
  v_legacy_locked_by uuid;
  v_legacy_locked_at timestamp without time zone;
  v_rows integer;
begin
  if v_user_id is null then
    raise exception 'Anmeldung erforderlich.' using errcode = '42501';
  end if;
  if not exists (select 1 from public.app_user where user_id = v_user_id) then
    raise exception 'Keine KGV-Berechtigung vorhanden.' using errcode = '42501';
  end if;
  if p_entity_type is null or p_entity_type !~ '^[a-z0-9_]{1,64}$'
     or p_entity_id is null or char_length(p_entity_id) not between 1 and 128 then
    raise exception 'Ungueltiger Sperrschluessel.' using errcode = '22023';
  end if;

  delete from public.browser_edit_lock as l where l.expires_at <= v_now;

  insert into public.browser_edit_lock (
    entity_type, entity_id, locked_by_user_id, locked_at, expires_at
  ) values (
    p_entity_type, p_entity_id, v_user_id, v_now,
    v_now + make_interval(secs => v_timeout)
  )
  on conflict (entity_type, entity_id) do update
    set locked_by_user_id = excluded.locked_by_user_id,
        locked_at = case
          when browser_edit_lock.locked_by_user_id = excluded.locked_by_user_id
            then browser_edit_lock.locked_at
          else excluded.locked_at
        end,
        expires_at = excluded.expires_at
    where browser_edit_lock.locked_by_user_id = excluded.locked_by_user_id
       or browser_edit_lock.expires_at <= v_now
  returning browser_edit_lock.locked_by_user_id, browser_edit_lock.expires_at
    into v_locked_by, v_expires_at;

  if v_locked_by is null then
    select l.locked_by_user_id, l.expires_at
      into v_locked_by, v_expires_at
    from public.browser_edit_lock l
    where l.entity_type = p_entity_type and l.entity_id = p_entity_id;
  end if;

  if v_locked_by = v_user_id and p_entity_type = 'mitglied'
     and p_entity_id ~ '^[0-9]+$' then
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
      delete from public.browser_edit_lock as l
      where l.entity_type = p_entity_type and l.entity_id = p_entity_id
        and l.locked_by_user_id = v_user_id;
      v_locked_by := v_legacy_locked_by;
      v_expires_at := (v_legacy_locked_at at time zone 'UTC') + make_interval(secs => v_timeout);
    end if;
  elsif v_locked_by = v_user_id and p_entity_type = 'arbeitsstunde'
     and p_entity_id ~ '^[0-9]+$' then
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
      delete from public.browser_edit_lock as l
      where l.entity_type = p_entity_type and l.entity_id = p_entity_id
        and l.locked_by_user_id = v_user_id;
      v_locked_by := v_legacy_locked_by;
      v_expires_at := (v_legacy_locked_at at time zone 'UTC') + make_interval(secs => v_timeout);
    end if;
  end if;

  return query select
    v_locked_by = v_user_id,
    v_locked_by,
    coalesce(public.browser_edit_lock_display_name(v_locked_by), 'Ein anderer Benutzer'),
    v_expires_at;
end;
$$;

revoke all on function public.acquire_browser_edit_lock(text, text, integer) from public, anon;
grant execute on function public.acquire_browser_edit_lock(text, text, integer) to authenticated;
