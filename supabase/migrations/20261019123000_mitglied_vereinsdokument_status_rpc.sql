-- DOC-UI1a: Stichtagsbezogener Lesestatus der aktuellen Pflichtunterlagen je Mitglied.

create or replace function public.get_mitglied_vereinsdokument_status(
  p_mitglied_id bigint,
  p_stichtag date
)
returns table (
  dokumenttyp_code text,
  bezeichnung text,
  pflicht boolean,
  vereinsdokument_fassung_id bigint,
  fassung text,
  gueltig_ab date,
  gueltig_bis date,
  storage_path text,
  dateiname text,
  bereitstellung_id bigint,
  bereitstellungsart text,
  bereitgestellt_am timestamptz,
  bereitgestellt_von uuid,
  status text
)
language plpgsql
security definer
set search_path = public, pg_temp
as $$
begin
  if p_mitglied_id is null or p_stichtag is null then
    raise exception 'Mitglied und Stichtag sind erforderlich' using errcode = '22023';
  end if;

  if not (
    auth.role() = 'service_role'
    or exists (
      select 1 from public.current_app_user_context() c
      where c.role in ('admin', 'vorstand')
    )
  ) then
    raise exception 'Nur Admin oder Vorstand dürfen den Pflichtunterlagenstatus prüfen' using errcode = '42501';
  end if;

  if not exists (select 1 from public.mitglied where id = p_mitglied_id) then
    raise exception 'Unbekanntes Mitglied: %', p_mitglied_id using errcode = 'P0002';
  end if;

  return query
  select
    typ.code,
    typ.bezeichnung,
    typ.pflicht,
    fassung.id,
    fassung.fassung,
    fassung.gueltig_ab,
    fassung.gueltig_bis,
    fassung.storage_path,
    fassung.dateiname,
    bereitstellung.id,
    bereitstellung.bereitstellungsart,
    bereitstellung.bereitgestellt_am,
    bereitstellung.bereitgestellt_von,
    case
      when fassung.id is null then 'keine_aktuelle_fassung'
      when bereitstellung.id is null then 'fehlt'
      else 'bereitgestellt'
    end
  from public.vereinsdokument_typ typ
  left join lateral (
    select f.*
    from public.vereinsdokument_fassung f
    where f.dokumenttyp_code = typ.code
      and f.gueltig_ab <= p_stichtag
      and (f.gueltig_bis is null or f.gueltig_bis >= p_stichtag)
    order by f.gueltig_ab desc, f.id desc
    limit 1
  ) fassung on true
  left join lateral (
    select b.*
    from public.mitglied_vereinsdokument_bereitstellung b
    where b.mitglied_id = p_mitglied_id
      and b.vereinsdokument_fassung_id = fassung.id
      and b.bereitgestellt_am::date <= p_stichtag
      and (b.storniert_am is null or b.storniert_am::date > p_stichtag)
    order by b.bereitgestellt_am desc, b.id desc
    limit 1
  ) bereitstellung on true
  where typ.pflicht = true
  order by typ.code;
end;
$$;
create or replace function public.get_mitglied_vereinsdokument_bereitstellung_historie(
  p_mitglied_id bigint
)
returns table (
  bereitstellung_id bigint,
  mitglied_id bigint,
  vereinsdokument_fassung_id bigint,
  dokumenttyp_code text,
  bezeichnung text,
  fassung text,
  bereitstellungsart text,
  bereitgestellt_am timestamptz,
  bereitgestellt_von uuid,
  storniert_am timestamptz,
  storniert_von uuid,
  storno_grund text
)
language plpgsql
security definer
set search_path = public, pg_temp
as $$
begin
  if p_mitglied_id is null then
    raise exception 'Mitglied ist erforderlich' using errcode = '22023';
  end if;

  if not (
    auth.role() = 'service_role'
    or exists (
      select 1 from public.current_app_user_context() c
      where c.role in ('admin', 'vorstand')
    )
  ) then
    raise exception 'Nur Admin oder Vorstand dürfen die Bereitstellungshistorie prüfen' using errcode = '42501';
  end if;

  if not exists (select 1 from public.mitglied where id = p_mitglied_id) then
    raise exception 'Unbekanntes Mitglied: %', p_mitglied_id using errcode = 'P0002';
  end if;

  return query
  select
    b.id,
    b.mitglied_id,
    b.vereinsdokument_fassung_id,
    f.dokumenttyp_code,
    typ.bezeichnung,
    f.fassung,
    b.bereitstellungsart,
    b.bereitgestellt_am,
    b.bereitgestellt_von,
    b.storniert_am,
    b.storniert_von,
    b.storno_grund
  from public.mitglied_vereinsdokument_bereitstellung b
  join public.vereinsdokument_fassung f on f.id = b.vereinsdokument_fassung_id
  join public.vereinsdokument_typ typ on typ.code = f.dokumenttyp_code
  where b.mitglied_id = p_mitglied_id
  order by b.bereitgestellt_am desc, b.id desc;
end;
$$;
revoke all on function public.get_mitglied_vereinsdokument_status(bigint, date) from public, anon, authenticated;
grant execute on function public.get_mitglied_vereinsdokument_status(bigint, date) to authenticated, service_role;
revoke all on function public.get_mitglied_vereinsdokument_bereitstellung_historie(bigint) from public, anon, authenticated;
grant execute on function public.get_mitglied_vereinsdokument_bereitstellung_historie(bigint) to authenticated, service_role;
