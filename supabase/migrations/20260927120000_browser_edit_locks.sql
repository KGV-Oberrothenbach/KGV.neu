-- Zentrale Bearbeitungssperren fuer Browser, MAUI und WPF.
-- Die Sperre ist eine zeitlich begrenzte Lease und wird serverseitig atomar vergeben.

create table if not exists public.browser_edit_lock (
  entity_type text not null,
  entity_id text not null,
  locked_by_user_id uuid not null,
  locked_at timestamptz not null default now(),
  expires_at timestamptz not null,
  primary key (entity_type, entity_id),
  constraint browser_edit_lock_entity_type_check
    check (entity_type ~ '^[a-z0-9_]{1,64}$'),
  constraint browser_edit_lock_entity_id_check
    check (char_length(entity_id) between 1 and 128)
);

create index if not exists ix_browser_edit_lock_expires_at
  on public.browser_edit_lock (expires_at);

alter table public.browser_edit_lock enable row level security;
revoke all on table public.browser_edit_lock from anon, authenticated;

create or replace function public.browser_edit_lock_display_name(p_user_id uuid)
returns text
language sql
stable
security definer
set search_path = public
as $$
  select coalesce(
    nullif(btrim(concat_ws(' ', m.vorname, m.name)), ''),
    'Ein anderer Benutzer'
  )
  from public.app_user au
  left join public.mitglied m on m.id = au.mitglied_id
  where au.user_id = p_user_id
  limit 1;
$$;

revoke all on function public.browser_edit_lock_display_name(uuid) from public, anon, authenticated;

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

create or replace function public.release_browser_edit_lock(
  p_entity_type text,
  p_entity_id text
)
returns boolean
language plpgsql
security definer
set search_path = public
as $$
declare
  v_user_id uuid := auth.uid();
begin
  if v_user_id is null then return false; end if;

  delete from public.browser_edit_lock
  where entity_type = p_entity_type and entity_id = p_entity_id
    and locked_by_user_id = v_user_id;

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

create or replace function public.release_all_browser_edit_locks()
returns boolean
language plpgsql
security definer
set search_path = public
as $$
declare
  v_user_id uuid := auth.uid();
begin
  if v_user_id is null then return false; end if;
  delete from public.browser_edit_lock where locked_by_user_id = v_user_id;
  update public.mitglied set lockedbyuserid = null, lockat = null
    where lockedbyuserid = v_user_id;
  update public.arbeitsstunde set lockedbyuserid = null, lockat = null
    where lockedbyuserid = v_user_id;
  return true;
end;
$$;

revoke all on function public.acquire_browser_edit_lock(text, text, integer) from public, anon;
revoke all on function public.release_browser_edit_lock(text, text) from public, anon;
revoke all on function public.release_all_browser_edit_locks() from public, anon;
grant execute on function public.acquire_browser_edit_lock(text, text, integer) to authenticated;
grant execute on function public.release_browser_edit_lock(text, text) to authenticated;
grant execute on function public.release_all_browser_edit_locks() to authenticated;

create or replace function public.enforce_browser_edit_lock()
returns trigger
language plpgsql
security definer
set search_path = public
as $$
declare
  v_entity_id text := to_jsonb(old) ->> tg_argv[1];
  v_locked_by uuid;
begin
  if auth.role() = 'service_role' then return new; end if;
  select locked_by_user_id into v_locked_by
  from public.browser_edit_lock
  where entity_type = tg_argv[0]
    and entity_id = v_entity_id
    and expires_at > now();

  if v_locked_by is not null and v_locked_by <> auth.uid() then
    raise exception 'Datensatz wird derzeit von % bearbeitet.',
      coalesce(public.browser_edit_lock_display_name(v_locked_by), 'einem anderen Benutzer')
      using errcode = '55P03';
  end if;
  return new;
end;
$$;

revoke all on function public.enforce_browser_edit_lock() from public, anon, authenticated;

do $$
declare
  v_item record;
begin
  for v_item in
    select * from (values
      ('mitglied', 'id'),
      ('parzelle', 'id'),
      ('parzellen_belegung', 'id'),
      ('zaehler_ablesung', 'id'),
      ('arbeitsstunde', 'id'),
      ('arbeitseinsatz', 'id'),
      ('arbeitseinsatz_anmeldung', 'id'),
      ('termin', 'id'),
      ('bekanntmachung', 'id'),
      ('wartungsvertraege', 'id'),
      ('wartungsvertrag_zuordnungen', 'id'),
      ('app_user', 'user_id'),
      ('saison', 'id'),
      ('vereinskonfiguration', 'id')
    ) as x(table_name, id_column)
  loop
    if to_regclass(format('public.%I', v_item.table_name)) is not null then
      execute format('drop trigger if exists trg_browser_edit_lock on public.%I', v_item.table_name);
      execute format(
        'create trigger trg_browser_edit_lock before update on public.%I for each row execute function public.enforce_browser_edit_lock(%L, %L)',
        v_item.table_name, v_item.table_name, v_item.id_column
      );
    end if;
  end loop;
end;
$$;
