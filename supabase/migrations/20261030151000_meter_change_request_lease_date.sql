-- Der Antrag ist am fachlichen Wechseldatum berechtigt, nicht nur am heutigen Tag.
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
  if p_ausbaudatum is null or p_einbaudatum is null or p_eichdatum is null or p_endstand is null or p_endstand < 0 or p_anfangsstand is null or p_anfangsstand < 0 or nullif(trim(p_neue_zaehlernummer), '') is null or p_einbaudatum < p_ausbaudatum then raise exception 'Die Wechselangaben sind ungültig.' using errcode = '23514'; end if;
  select * into v_old from public.zaehler where id = p_alter_zaehler_id for share;
  if v_old.id is null or v_old.status <> 'aktiv' or v_old.ausgebaut_am is not null or p_ausbaudatum < v_old.eingebaut_am then raise exception 'Der gewählte Zähler ist für das Ausbaudatum nicht aktiv.' using errcode = '23514'; end if;
  if not public.current_user_has_active_lease_for_parcel(v_old.parzelle_id, p_ausbaudatum) then raise exception 'Die Parzelle ist dem aktuellen Mitglied am Ausbaudatum nicht wirksam zugeordnet.' using errcode = '42501'; end if;
  insert into public.zaehlerwechsel_antrag(antragsteller_mitglied_id, parzelle_id, medium, alter_zaehler_id, ausbaudatum, endstand, ausbau_foto_pfad, neue_zaehlernummer, einbaudatum, eichdatum, anfangsstand, einbau_foto_pfad)
  values (public.current_mitglied_id(), v_old.parzelle_id, v_old.medium, v_old.id, p_ausbaudatum, p_endstand, nullif(trim(p_ausbau_foto_pfad), ''), trim(p_neue_zaehlernummer), p_einbaudatum, p_eichdatum, p_anfangsstand, nullif(trim(p_einbau_foto_pfad), '')) returning * into v_request;
  return v_request;
end;
$$;
