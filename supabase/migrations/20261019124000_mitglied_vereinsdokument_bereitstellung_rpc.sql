-- DOC-UI1b1.1: Papier- und digitalen Bereitstellungsnachweis strikt trennen.
create or replace function public.create_mitglied_vereinsdokument_bereitstellungen_papier(
  p_mitglied_id bigint,
  p_vereinsdokument_fassung_ids bigint[],
  p_bereitgestellt_am timestamptz
)
returns setof public.mitglied_vereinsdokument_bereitstellung
language plpgsql
security definer
set search_path = public, pg_temp
as $$
declare
  v_count integer;
  v_actor uuid;
begin
  if p_mitglied_id is null or p_bereitgestellt_am is null
     or coalesce(array_length(p_vereinsdokument_fassung_ids, 1), 0) = 0 then
    raise exception 'Mitglied, Fassungen und Zeitpunkt sind erforderlich' using errcode = '22023';
  end if;
  if auth.role() <> 'authenticated'
     or not exists (select 1 from public.current_app_user_context() c where c.role in ('admin', 'vorstand')) then
    raise exception 'Nur Admin oder Vorstand dürfen Papierübergaben dokumentieren' using errcode = '42501';
  end if;
  v_actor := auth.uid();
  if v_actor is null then
    raise exception 'Ein auslösender Benutzer ist erforderlich' using errcode = '42501';
  end if;
  if p_bereitgestellt_am > now() then
    raise exception 'Eine Papierübergabe darf nicht in der Zukunft liegen' using errcode = '22023';
  end if;
  if not exists (select 1 from public.mitglied where id = p_mitglied_id) then
    raise exception 'Unbekanntes Mitglied: %', p_mitglied_id using errcode = 'P0002';
  end if;
  if cardinality(p_vereinsdokument_fassung_ids) <> cardinality(array(select distinct unnest(p_vereinsdokument_fassung_ids))) then
    raise exception 'Jede Dokumentfassung darf nur einmal übergeben werden' using errcode = '22023';
  end if;
  select count(*) into v_count from public.vereinsdokument_fassung where id = any(p_vereinsdokument_fassung_ids);
  if v_count <> cardinality(p_vereinsdokument_fassung_ids) then
    raise exception 'Mindestens eine Dokumentfassung ist unbekannt' using errcode = '23503';
  end if;

  return query
  insert into public.mitglied_vereinsdokument_bereitstellung
    (mitglied_id, vereinsdokument_fassung_id, bereitstellungsart, bereitgestellt_am, bereitgestellt_von)
  select p_mitglied_id, id, 'papier', p_bereitgestellt_am, v_actor
  from unnest(p_vereinsdokument_fassung_ids) as requested(id)
  returning *;
end;
$$;

create or replace function public.create_mitglied_vereinsdokument_bereitstellungen_digital(
  p_mitglied_id bigint,
  p_vereinsdokument_fassung_ids bigint[],
  p_bereitgestellt_von uuid
)
returns setof public.mitglied_vereinsdokument_bereitstellung
language plpgsql
security definer
set search_path = public, pg_temp
as $$
declare
  v_count integer;
begin
  if auth.role() <> 'service_role' then
    raise exception 'Die digitale Bereitstellung ist nur serverseitig zulässig' using errcode = '42501';
  end if;
  if p_mitglied_id is null or p_bereitgestellt_von is null
     or coalesce(array_length(p_vereinsdokument_fassung_ids, 1), 0) = 0 then
    raise exception 'Mitglied, Fassungen und auslösender Benutzer sind erforderlich' using errcode = '22023';
  end if;
  if not exists (
    select 1
    from public.app_user au
    join public.mitglied m on m.id = au.mitglied_id and m.auth_user_id = au.user_id
    where au.user_id = p_bereitgestellt_von
      and au.mitglied_id is not null
      and au.mitglied_id > 0
      and au.role in ('admin', 'vorstand')
  ) then
    raise exception 'Der auslösende Benutzer ist nicht als Admin oder Vorstand berechtigt' using errcode = '42501';
  end if;
  if not exists (select 1 from public.mitglied where id = p_mitglied_id) then
    raise exception 'Unbekanntes Mitglied: %', p_mitglied_id using errcode = 'P0002';
  end if;
  if cardinality(p_vereinsdokument_fassung_ids) <> cardinality(array(select distinct unnest(p_vereinsdokument_fassung_ids))) then
    raise exception 'Jede Dokumentfassung darf nur einmal übergeben werden' using errcode = '22023';
  end if;
  select count(*) into v_count from public.vereinsdokument_fassung where id = any(p_vereinsdokument_fassung_ids);
  if v_count <> cardinality(p_vereinsdokument_fassung_ids) then
    raise exception 'Mindestens eine Dokumentfassung ist unbekannt' using errcode = '23503';
  end if;

  return query
  insert into public.mitglied_vereinsdokument_bereitstellung
    (mitglied_id, vereinsdokument_fassung_id, bereitstellungsart, bereitgestellt_am, bereitgestellt_von)
  select p_mitglied_id, id, 'digital', now(), p_bereitgestellt_von
  from unnest(p_vereinsdokument_fassung_ids) as requested(id)
  returning *;
end;
$$;

drop policy if exists mitglied_vereinsdokument_bereitstellung_insert_admin_vorstand
  on public.mitglied_vereinsdokument_bereitstellung;
revoke insert, delete on table public.mitglied_vereinsdokument_bereitstellung from authenticated;

revoke all on function public.create_mitglied_vereinsdokument_bereitstellungen_papier(bigint, bigint[], timestamptz) from public, anon, authenticated, service_role;
grant execute on function public.create_mitglied_vereinsdokument_bereitstellungen_papier(bigint, bigint[], timestamptz) to authenticated;

revoke all on function public.create_mitglied_vereinsdokument_bereitstellungen_digital(bigint, bigint[], uuid) from public, anon, authenticated;
grant execute on function public.create_mitglied_vereinsdokument_bereitstellungen_digital(bigint, bigint[], uuid) to service_role;
