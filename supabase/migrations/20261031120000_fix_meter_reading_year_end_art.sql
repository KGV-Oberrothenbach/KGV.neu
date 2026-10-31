-- App und Bestandsdaten verwenden JEA; die Workflow-Migration erlaubte irrtuemlich jae.
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
begin
  -- Legacy-Alias auf den bestehenden kanonischen Jahresendtyp abbilden.
  if p_art::text = 'jae' then p_art := 'JEA'::public.ablesung_art; end if;
  if not coalesce((select bool_value from public.app_setting where setting_key = 'allow_user_meter_reading_submissions'), false) then
    raise exception 'Eigene Zählerablesungen sind aktuell nicht freigeschaltet.' using errcode = '42501';
  end if;
  if public.current_mitglied_id() is null then
    raise exception 'Mitgliedskontext fehlt.' using errcode = '42501';
  end if;
  if p_ablesedatum is null or p_stand is null or p_stand < 0 or p_art is null or p_art not in ('normal', 'JEA') then
    raise exception 'Ablesedatum, Stand oder Ableseart sind ungültig.' using errcode = '23514';
  end if;

  select * into v_meter from public.zaehler where id = p_zaehler_id for share;
  if v_meter.id is null or v_meter.status <> 'aktiv' or v_meter.ausgebaut_am is not null then
    raise exception 'Der gewählte Zähler ist nicht aktiv.' using errcode = '23514';
  end if;
  if p_ablesedatum < v_meter.eingebaut_am then
    raise exception 'Das Ablesedatum liegt vor dem Einbau.' using errcode = '23514';
  end if;
  if not public.current_user_has_active_lease_for_parcel(v_meter.parzelle_id, p_ablesedatum) then
    raise exception 'Die Parzelle ist dem aktuellen Mitglied am Ablesedatum nicht zugeordnet.' using errcode = '42501';
  end if;
  if exists (select 1 from public.zaehler_ablesung a where a.zaehler_id = p_zaehler_id and a.ablesedatum::date = p_ablesedatum and a.art = p_art and coalesce(a.pruefstatus::text, '') <> 'abgelehnt') then
    raise exception 'Für Zähler, Datum und Ableseart existiert bereits eine Ablesung.' using errcode = '23505';
  end if;

  insert into public.zaehler_ablesung(zaehler_id, ablesedatum, stand, art, foto_pfad, foto_dateiname, foto_drive_file_id, freigegeben, pruefstatus)
  values (p_zaehler_id, p_ablesedatum, p_stand, p_art, nullif(trim(p_foto_pfad), ''), nullif(trim(p_foto_dateiname), ''), nullif(trim(p_foto_drive_file_id), ''), false, 'eingereicht')
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
declare v_meter public.zaehler%rowtype; v_reading public.zaehler_ablesung%rowtype;
begin
  -- Legacy-Alias auf den bestehenden kanonischen Jahresendtyp abbilden.
  if p_art::text = 'jae' then p_art := 'JEA'::public.ablesung_art; end if;
  if not public.has_effective_permission(32) then
    raise exception 'Für die direkte Ablesung besteht keine Berechtigung.' using errcode = '42501';
  end if;
  if p_ablesedatum is null or p_stand is null or p_stand < 0 or p_art is null or p_art not in ('normal', 'JEA') then
    raise exception 'Ablesedatum, Stand oder Ableseart sind ungültig.' using errcode = '23514';
  end if;
  select * into v_meter from public.zaehler where id = p_zaehler_id for share;
  if v_meter.id is null or v_meter.status <> 'aktiv' or v_meter.ausgebaut_am is not null or p_ablesedatum < v_meter.eingebaut_am then
    raise exception 'Der gewählte Zähler ist für dieses Datum nicht aktiv.' using errcode = '23514';
  end if;
  if exists (select 1 from public.zaehler_ablesung a where a.zaehler_id = p_zaehler_id and a.ablesedatum::date = p_ablesedatum and a.art = p_art and coalesce(a.pruefstatus::text, '') <> 'abgelehnt') then
    raise exception 'Für Zähler, Datum und Ableseart existiert bereits eine Ablesung.' using errcode = '23505';
  end if;
  insert into public.zaehler_ablesung(zaehler_id, ablesedatum, stand, art, foto_pfad, foto_dateiname, foto_drive_file_id, freigegeben, pruefstatus)
  values (p_zaehler_id, p_ablesedatum, p_stand, p_art, nullif(trim(p_foto_pfad), ''), nullif(trim(p_foto_dateiname), ''), nullif(trim(p_foto_drive_file_id), ''), true, 'freigegeben') returning * into v_reading;
  return v_reading;
end;
$$;
NOTIFY pgrst, 'reload schema';
