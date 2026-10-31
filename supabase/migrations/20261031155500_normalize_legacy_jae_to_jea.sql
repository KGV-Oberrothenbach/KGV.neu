-- Kompatibilitätsfix: historische JEA-Schreibweise 'jae' weiterhin akzeptieren,
-- aber kanonisch als 'jea' speichern.
-- Ursache:
--   20261030150200_meter_workflow_unification.sql validiert fälschlich auf 'jae'
--   und schließt 'einbau'/'ausbau' im direkten Capture-Flow aus.
--
-- Kanonische technische Werte:
--   normal, jea, einbau, ausbau
--
-- Fachliche Trennung:
--   submit_own_meter_reading: Mitglieder dürfen weiterhin nur normal/jea einreichen.
--   capture_meter_reading: direkte Erfassung durch Berechtigte unterstützt zusätzlich
--                          einbau/ausbau für die gemeinsamen Ein-/Ausbau-Ableseflows.

create or replace function public.submit_own_meter_reading(
  p_zaehler_id bigint,
  p_ablesedatum date,
  p_stand numeric,
  p_art public.ablesung_art,
  p_foto_pfad text default null,
  p_foto_dateiname text default null,
  p_foto_drive_file_id text default null
)
returns public.zaehler_ablesung
language plpgsql
security definer
set search_path to 'public', 'pg_temp'
as $$
declare
  v_meter public.zaehler%rowtype;
  v_reading public.zaehler_ablesung%rowtype;
  v_art public.ablesung_art;
begin
  if not coalesce(
    (select bool_value
       from public.app_setting
      where setting_key = 'allow_user_meter_reading_submissions'),
    false
  ) then
    raise exception 'Eigene Zählerablesungen sind aktuell nicht freigeschaltet.'
      using errcode = '42501';
  end if;

  if public.current_mitglied_id() is null then
    raise exception 'Mitgliedskontext fehlt.'
      using errcode = '42501';
  end if;

  if p_ablesedatum is null then
    raise exception 'Das Ablesedatum ist ungültig.'
      using errcode = '23514';
  end if;

  if p_stand is null or p_stand < 0 then
    raise exception 'Der Zählerstand ist ungültig.'
      using errcode = '23514';
  end if;

  v_art := case
    when p_art::text = 'jae' then 'jea'::public.ablesung_art
    else p_art
  end;

  if v_art not in ('normal', 'jea') then
    raise exception 'Die Ableseart ist für eine eigene Nutzerablesung ungültig.'
      using errcode = '23514';
  end if;

  select *
    into v_meter
    from public.zaehler
   where id = p_zaehler_id
   for share;

  if v_meter.id is null
     or v_meter.status <> 'aktiv'
     or v_meter.ausgebaut_am is not null then
    raise exception 'Der gewählte Zähler ist nicht aktiv.'
      using errcode = '23514';
  end if;

  if p_ablesedatum < v_meter.eingebaut_am then
    raise exception 'Das Ablesedatum liegt vor dem Einbau.'
      using errcode = '23514';
  end if;

  if not public.current_user_has_active_lease_for_parcel(
    v_meter.parzelle_id,
    p_ablesedatum
  ) then
    raise exception 'Die Parzelle ist dem aktuellen Mitglied am Ablesedatum nicht zugeordnet.'
      using errcode = '42501';
  end if;

  if exists (
    select 1
      from public.zaehler_ablesung a
     where a.zaehler_id = p_zaehler_id
       and a.ablesedatum::date = p_ablesedatum
       and a.art = v_art
       and coalesce(a.pruefstatus::text, '') <> 'abgelehnt'
  ) then
    raise exception 'Für Zähler, Datum und Ableseart existiert bereits eine Ablesung.'
      using errcode = '23505';
  end if;

  insert into public.zaehler_ablesung(
    zaehler_id,
    ablesedatum,
    stand,
    art,
    foto_pfad,
    foto_dateiname,
    foto_drive_file_id,
    freigegeben,
    pruefstatus
  )
  values (
    p_zaehler_id,
    p_ablesedatum,
    p_stand,
    v_art,
    nullif(trim(p_foto_pfad), ''),
    nullif(trim(p_foto_dateiname), ''),
    nullif(trim(p_foto_drive_file_id), ''),
    false,
    'eingereicht'
  )
  returning * into v_reading;

  return v_reading;
end;
$$;


create or replace function public.capture_meter_reading(
  p_zaehler_id bigint,
  p_ablesedatum date,
  p_stand numeric,
  p_art public.ablesung_art,
  p_foto_pfad text default null,
  p_foto_dateiname text default null,
  p_foto_drive_file_id text default null
)
returns public.zaehler_ablesung
language plpgsql
security definer
set search_path to 'public', 'pg_temp'
as $$
declare
  v_meter public.zaehler%rowtype;
  v_reading public.zaehler_ablesung%rowtype;
  v_art public.ablesung_art;
begin
  if not public.has_effective_permission(32) then
    raise exception 'Für die direkte Ablesung besteht keine Berechtigung.'
      using errcode = '42501';
  end if;

  if p_ablesedatum is null then
    raise exception 'Das Ablesedatum ist ungültig.'
      using errcode = '23514';
  end if;

  if p_stand is null or p_stand < 0 then
    raise exception 'Der Zählerstand ist ungültig.'
      using errcode = '23514';
  end if;

  v_art := case
    when p_art::text = 'jae' then 'jea'::public.ablesung_art
    else p_art
  end;

  if v_art not in ('normal', 'jea', 'einbau', 'ausbau') then
    raise exception 'Die Ableseart ist ungültig.'
      using errcode = '23514';
  end if;

  select *
    into v_meter
    from public.zaehler
   where id = p_zaehler_id
   for share;

  if v_meter.id is null
     or v_meter.status <> 'aktiv'
     or v_meter.ausgebaut_am is not null
     or p_ablesedatum < v_meter.eingebaut_am then
    raise exception 'Der gewählte Zähler ist für dieses Datum nicht aktiv.'
      using errcode = '23514';
  end if;

  if exists (
    select 1
      from public.zaehler_ablesung a
     where a.zaehler_id = p_zaehler_id
       and a.ablesedatum::date = p_ablesedatum
       and a.art = v_art
       and coalesce(a.pruefstatus::text, '') <> 'abgelehnt'
  ) then
    raise exception 'Für Zähler, Datum und Ableseart existiert bereits eine Ablesung.'
      using errcode = '23505';
  end if;

  insert into public.zaehler_ablesung(
    zaehler_id,
    ablesedatum,
    stand,
    art,
    foto_pfad,
    foto_dateiname,
    foto_drive_file_id,
    freigegeben,
    pruefstatus
  )
  values (
    p_zaehler_id,
    p_ablesedatum,
    p_stand,
    v_art,
    nullif(trim(p_foto_pfad), ''),
    nullif(trim(p_foto_dateiname), ''),
    nullif(trim(p_foto_drive_file_id), ''),
    true,
    'freigegeben'
  )
  returning * into v_reading;

  return v_reading;
end;
$$;

comment on function public.submit_own_meter_reading(
  bigint, date, numeric, public.ablesung_art, text, text, text
) is
'Mitgliedereinreichung für normale Ablesung oder Jahresendablesung; historisches jae wird kanonisch als jea gespeichert.';

comment on function public.capture_meter_reading(
  bigint, date, numeric, public.ablesung_art, text, text, text
) is
'Direkte Ablesung durch Berechtigte; unterstützt normal, jea, einbau und ausbau; historisches jae wird als jea normalisiert.';

-- Sicherheits-/Ausführungsrechte sicherstellen.
grant execute on function public.submit_own_meter_reading(
  bigint, date, numeric, public.ablesung_art, text, text, text
) to authenticated;

grant execute on function public.capture_meter_reading(
  bigint, date, numeric, public.ablesung_art, text, text, text
) to authenticated;
