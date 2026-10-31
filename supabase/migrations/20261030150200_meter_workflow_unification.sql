-- Einheitliche, serverseitig abgesicherte Zählerworkflows.
-- RFID bleibt nur ein Auflösungsweg; die fachliche Identität ist Parzelle + Medium + Zähler.

create or replace function public.current_user_has_active_lease_for_parcel(p_parzelle_id bigint, p_on_date date default current_date)
returns boolean
language sql
stable
security definer
set search_path to 'public', 'pg_temp'
as $$
  select exists (
    select 1
    from public.pachtverhaeltnis pv
    join public.pachtverhaeltnis_beteiligung pb on pb.pachtverhaeltnis_id = pv.id
    where pv.parzelle_id = p_parzelle_id
      and (pv.beendet_am is null or pv.beendet_am >= p_on_date)
      and pb.mitglied_id = public.current_mitglied_id()
      and pb.gueltig_ab <= p_on_date
      and (pb.gueltig_bis is null or pb.gueltig_bis >= p_on_date)
  );
$$;
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
  if not coalesce((select bool_value from public.app_setting where setting_key = 'allow_user_meter_reading_submissions'), false) then
    raise exception 'Eigene Zählerablesungen sind aktuell nicht freigeschaltet.' using errcode = '42501';
  end if;
  if public.current_mitglied_id() is null then
    raise exception 'Mitgliedskontext fehlt.' using errcode = '42501';
  end if;
  if p_ablesedatum is null or p_stand is null or p_stand < 0 or p_art not in ('normal', 'jae') then
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
  if not public.has_effective_permission(32) then
    raise exception 'Für die direkte Ablesung besteht keine Berechtigung.' using errcode = '42501';
  end if;
  if p_ablesedatum is null or p_stand is null or p_stand < 0 or p_art not in ('normal', 'jae') then
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
create table if not exists public.zaehlerwechsel_antrag (
  id bigint generated by default as identity primary key,
  antragsteller_mitglied_id bigint not null references public.mitglied(id) on delete restrict,
  parzelle_id bigint not null references public.parzelle(id) on delete restrict,
  medium public.zaehler_medium not null,
  alter_zaehler_id bigint not null references public.zaehler(id) on delete restrict,
  ausbaudatum date not null,
  endstand numeric not null check (endstand >= 0),
  ausbau_foto_pfad text,
  ausbau_foto_dateiname text,
  ausbau_foto_drive_file_id text,
  neue_zaehlernummer text not null check (length(trim(neue_zaehlernummer)) > 0),
  einbaudatum date not null,
  eichdatum date not null,
  anfangsstand numeric not null check (anfangsstand >= 0),
  einbau_foto_pfad text,
  einbau_foto_dateiname text,
  einbau_foto_drive_file_id text,
  status text not null default 'eingereicht' check (status in ('eingereicht', 'abgelehnt', 'ausgefuehrt')),
  eingereicht_am timestamptz not null default now(),
  geprueft_von bigint references public.mitglied(id) on delete set null,
  geprueft_am timestamptz,
  pruefkommentar text,
  ausgefuehrter_neuer_zaehler_id bigint references public.zaehler(id) on delete restrict,
  constraint zaehlerwechsel_antrag_daten_chk check (einbaudatum >= ausbaudatum)
);
create index if not exists ix_zaehlerwechsel_antrag_status on public.zaehlerwechsel_antrag(status, eingereicht_am);
create index if not exists ix_zaehlerwechsel_antrag_parzelle on public.zaehlerwechsel_antrag(parzelle_id, medium);
create or replace function public.execute_meter_change(
  p_alter_zaehler_id bigint,
  p_parzelle_id bigint,
  p_medium public.zaehler_medium,
  p_ausbaudatum date,
  p_endstand numeric,
  p_ausbau_foto_pfad text,
  p_neue_zaehlernummer text,
  p_eichdatum date,
  p_einbaudatum date,
  p_anfangsstand numeric,
  p_einbau_foto_pfad text
)
returns table(alter_zaehler_id bigint, neuer_zaehler_id bigint)
language plpgsql
security definer
set search_path to 'public', 'pg_temp'
as $$
declare
  v_old public.zaehler%rowtype;
  v_new public.zaehler%rowtype;
  v_last_stand numeric;
begin
  if not public.has_effective_permission(64) then
    raise exception 'Für Zählerwechsel besteht keine Berechtigung.' using errcode = '42501';
  end if;
  if p_ausbaudatum is null or p_einbaudatum is null or p_eichdatum is null or p_endstand is null or p_endstand < 0 or p_anfangsstand is null or p_anfangsstand < 0 or nullif(trim(p_neue_zaehlernummer), '') is null then
    raise exception 'Die Wechselangaben sind unvollständig oder ungültig.' using errcode = '23514';
  end if;
  if p_einbaudatum < p_ausbaudatum then
    raise exception 'Das Einbaudatum darf nicht vor dem Ausbaudatum liegen.' using errcode = '23514';
  end if;

  select * into v_old from public.zaehler where id = p_alter_zaehler_id for update;
  if v_old.id is null or v_old.parzelle_id <> p_parzelle_id or v_old.medium <> p_medium or v_old.status <> 'aktiv' or v_old.ausgebaut_am is not null then
    raise exception 'Der angegebene alte Zähler ist nicht der aktive Zähler des Kontexts.' using errcode = '23514';
  end if;
  if p_ausbaudatum < v_old.eingebaut_am then
    raise exception 'Das Ausbaudatum liegt vor dem Einbau des alten Zählers.' using errcode = '23514';
  end if;
  if exists (select 1 from public.zaehler z where z.parzelle_id = p_parzelle_id and z.medium = p_medium and z.status = 'aktiv' and z.ausgebaut_am is null and z.id <> p_alter_zaehler_id) then
    raise exception 'Für Parzelle und Medium existiert bereits ein weiterer aktiver Zähler.' using errcode = '23505';
  end if;
  select a.stand into v_last_stand from public.zaehler_ablesung a where a.zaehler_id = v_old.id order by a.ablesedatum desc, a.id desc limit 1;
  if v_last_stand is not null and p_endstand < v_last_stand then
    raise exception 'Der Endstand darf nicht kleiner als der letzte bekannte Stand sein.' using errcode = '23514';
  end if;

  insert into public.zaehler_ablesung(zaehler_id, ablesedatum, stand, art, foto_pfad, freigegeben, pruefstatus)
  values (v_old.id, p_ausbaudatum, p_endstand, 'ausbau', nullif(trim(p_ausbau_foto_pfad), ''), true, 'freigegeben');
  update public.zaehler set ausgebaut_am = p_ausbaudatum, status = 'ausgebaut' where id = v_old.id;
  insert into public.zaehler(parzelle_id, medium, zaehlernummer, eichdatum, eingebaut_am, status)
  values (p_parzelle_id, p_medium, trim(p_neue_zaehlernummer), p_eichdatum, p_einbaudatum, 'aktiv') returning * into v_new;
  insert into public.zaehler_ablesung(zaehler_id, ablesedatum, stand, art, foto_pfad, freigegeben, pruefstatus)
  values (v_new.id, p_einbaudatum, p_anfangsstand, 'einbau', nullif(trim(p_einbau_foto_pfad), ''), true, 'freigegeben');
  return query select v_old.id, v_new.id;
end;
$$;
create or replace function public.submit_own_meter_change_request(
  p_alter_zaehler_id bigint, p_ausbaudatum date, p_endstand numeric, p_ausbau_foto_pfad text,
  p_neue_zaehlernummer text, p_eichdatum date, p_einbaudatum date, p_anfangsstand numeric, p_einbau_foto_pfad text
)
returns public.zaehlerwechsel_antrag
language plpgsql security definer set search_path to 'public', 'pg_temp'
as $$
declare v_old public.zaehler%rowtype; v_request public.zaehlerwechsel_antrag%rowtype;
begin
  if public.current_mitglied_id() is null then raise exception 'Mitgliedskontext fehlt.' using errcode = '42501'; end if;
  select * into v_old from public.zaehler where id = p_alter_zaehler_id for share;
  if v_old.id is null or v_old.status <> 'aktiv' or v_old.ausgebaut_am is not null then raise exception 'Der gewählte Zähler ist nicht aktiv.' using errcode = '23514'; end if;
  if not public.current_user_has_active_lease_for_parcel(v_old.parzelle_id, current_date) then raise exception 'Die Parzelle ist dem aktuellen Mitglied nicht wirksam zugeordnet.' using errcode = '42501'; end if;
  if p_ausbaudatum is null or p_einbaudatum is null or p_eichdatum is null or p_endstand is null or p_endstand < 0 or p_anfangsstand is null or p_anfangsstand < 0 or nullif(trim(p_neue_zaehlernummer), '') is null or p_einbaudatum < p_ausbaudatum then raise exception 'Die Wechselangaben sind ungültig.' using errcode = '23514'; end if;
  insert into public.zaehlerwechsel_antrag(antragsteller_mitglied_id, parzelle_id, medium, alter_zaehler_id, ausbaudatum, endstand, ausbau_foto_pfad, neue_zaehlernummer, einbaudatum, eichdatum, anfangsstand, einbau_foto_pfad)
  values (public.current_mitglied_id(), v_old.parzelle_id, v_old.medium, v_old.id, p_ausbaudatum, p_endstand, nullif(trim(p_ausbau_foto_pfad), ''), trim(p_neue_zaehlernummer), p_einbaudatum, p_eichdatum, p_anfangsstand, nullif(trim(p_einbau_foto_pfad), '')) returning * into v_request;
  return v_request;
end;
$$;
create or replace function public.review_meter_change_request(p_antrag_id bigint, p_action text, p_kommentar text default null)
returns public.zaehlerwechsel_antrag
language plpgsql security definer set search_path to 'public', 'pg_temp'
as $$
declare v_request public.zaehlerwechsel_antrag%rowtype; v_new_id bigint;
begin
  if not public.has_effective_permission(64) then raise exception 'Für die Prüfung von Zählerwechseln besteht keine Berechtigung.' using errcode = '42501'; end if;
  select * into v_request from public.zaehlerwechsel_antrag where id = p_antrag_id for update;
  if v_request.id is null or v_request.status <> 'eingereicht' then raise exception 'Der Antrag ist nicht mehr offen.' using errcode = '23514'; end if;
  if p_action = 'ablehnen' then
    update public.zaehlerwechsel_antrag set status = 'abgelehnt', geprueft_von = public.current_mitglied_id(), geprueft_am = now(), pruefkommentar = nullif(trim(p_kommentar), '') where id = v_request.id returning * into v_request;
    return v_request;
  end if;
  if p_action <> 'ausfuehren' then raise exception 'Unbekannte Prüfaktion.' using errcode = '23514'; end if;
  select neuer_zaehler_id into v_new_id from public.execute_meter_change(v_request.alter_zaehler_id, v_request.parzelle_id, v_request.medium, v_request.ausbaudatum, v_request.endstand, v_request.ausbau_foto_pfad, v_request.neue_zaehlernummer, v_request.eichdatum, v_request.einbaudatum, v_request.anfangsstand, v_request.einbau_foto_pfad);
  update public.zaehlerwechsel_antrag set status = 'ausgefuehrt', ausgefuehrter_neuer_zaehler_id = v_new_id, geprueft_von = public.current_mitglied_id(), geprueft_am = now(), pruefkommentar = nullif(trim(p_kommentar), '') where id = v_request.id returning * into v_request;
  return v_request;
end;
$$;
create or replace function public.correct_meter_change_request(
  p_antrag_id bigint, p_ausbaudatum date, p_endstand numeric, p_neue_zaehlernummer text,
  p_eichdatum date, p_einbaudatum date, p_anfangsstand numeric, p_kommentar text default null
)
returns public.zaehlerwechsel_antrag
language plpgsql security definer set search_path to 'public', 'pg_temp'
as $$
declare v_request public.zaehlerwechsel_antrag%rowtype;
begin
  if not public.has_effective_permission(64) then raise exception 'Für die Prüfung von Zählerwechseln besteht keine Berechtigung.' using errcode = '42501'; end if;
  select * into v_request from public.zaehlerwechsel_antrag where id = p_antrag_id for update;
  if v_request.id is null or v_request.status <> 'eingereicht' then raise exception 'Der Antrag ist nicht mehr offen.' using errcode = '23514'; end if;
  if p_ausbaudatum is null or p_einbaudatum is null or p_eichdatum is null or p_endstand is null or p_endstand < 0 or p_anfangsstand is null or p_anfangsstand < 0 or nullif(trim(p_neue_zaehlernummer), '') is null or p_einbaudatum < p_ausbaudatum then raise exception 'Die korrigierten Wechselangaben sind ungültig.' using errcode = '23514'; end if;
  update public.zaehlerwechsel_antrag set ausbaudatum = p_ausbaudatum, endstand = p_endstand, neue_zaehlernummer = trim(p_neue_zaehlernummer), eichdatum = p_eichdatum, einbaudatum = p_einbaudatum, anfangsstand = p_anfangsstand, geprueft_von = public.current_mitglied_id(), geprueft_am = now(), pruefkommentar = nullif(trim(p_kommentar), '') where id = v_request.id returning * into v_request;
  return v_request;
end;
$$;
alter table public.zaehlerwechsel_antrag enable row level security;
drop policy if exists zaehlerwechsel_antrag_select_own_or_manager on public.zaehlerwechsel_antrag;
create policy zaehlerwechsel_antrag_select_own_or_manager on public.zaehlerwechsel_antrag for select to authenticated using (
  antragsteller_mitglied_id = public.current_mitglied_id() or public.has_effective_permission(64)
);
grant execute on function public.current_user_has_active_lease_for_parcel(bigint, date) to authenticated;
grant execute on function public.submit_own_meter_reading(bigint, date, numeric, public.ablesung_art, text, text, text) to authenticated;
grant execute on function public.capture_meter_reading(bigint, date, numeric, public.ablesung_art, text, text, text) to authenticated;
grant execute on function public.execute_meter_change(bigint, bigint, public.zaehler_medium, date, numeric, text, text, date, date, numeric, text) to authenticated;
grant execute on function public.submit_own_meter_change_request(bigint, date, numeric, text, text, date, date, numeric, text) to authenticated;
grant execute on function public.review_meter_change_request(bigint, text, text) to authenticated;
grant execute on function public.correct_meter_change_request(bigint, date, numeric, text, date, date, numeric, text) to authenticated;
do $$
begin
  if exists (
    select 1 from public.zaehler where status = 'aktiv' and ausgebaut_am is null group by parzelle_id, medium having count(*) > 1
  ) then
    raise warning 'Kein Unique-Index für aktive Zähler angelegt: Bestandsdaten enthalten mehrere aktive Zähler je Parzelle/Medium.';
  else
    create unique index if not exists ux_zaehler_one_active_per_parcel_medium on public.zaehler(parzelle_id, medium) where status = 'aktiv' and ausgebaut_am is null;
  end if;
end;
$$;
