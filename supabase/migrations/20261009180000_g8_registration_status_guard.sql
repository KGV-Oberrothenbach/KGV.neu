-- G8.4 correction: normal members must never overwrite administrative statuses.
create or replace function public.trg_arbeitseinsatz_anmeldung_validate()
returns trigger language plpgsql security definer set search_path = public, pg_temp as $$
declare v_assignment public.arbeitseinsatz; v_begin timestamp without time zone; v_count integer;
begin
  select * into v_assignment from public.arbeitseinsatz where id = new.arbeitseinsatz_id;
  if not found then raise exception 'Arbeitseinsatz wurde nicht gefunden.' using errcode = '23503'; end if;
  v_begin := v_assignment.datum + coalesce(v_assignment.start_uhrzeit, time '23:59');

  if tg_op = 'UPDATE' and public.current_mitglied_id() = new.mitglied_id and not public.has_effective_permission(1048576) then
    if old.status not in ('angemeldet', 'abgesagt') or new.status not in ('angemeldet', 'abgesagt') then
      raise exception 'Verwaltungsstatus darf nicht durch ein Mitglied geändert werden.' using errcode = '42501';
    end if;
  end if;

  if new.status = 'angemeldet' and (tg_op = 'INSERT' or old.status is distinct from 'angemeldet') then
    if v_assignment.aktiv is distinct from true then raise exception 'Anmeldung nicht möglich: Arbeitseinsatz ist nicht aktiv.'; end if;
    if public.kgv_local_now() >= v_begin then raise exception 'Anmeldung nicht möglich: Arbeitseinsatz hat bereits begonnen.'; end if;
    if v_assignment.anmeldung_bis is not null and public.kgv_local_now() > v_assignment.anmeldung_bis then raise exception 'Anmeldung nicht möglich: Anmeldeschluss ist erreicht.'; end if;
    if v_assignment.max_teilnehmer is not null then
      select count(*) into v_count from public.arbeitseinsatz_anmeldung aa where aa.arbeitseinsatz_id = new.arbeitseinsatz_id and aa.status = 'angemeldet' and (tg_op = 'INSERT' or aa.id <> new.id);
      if v_count >= v_assignment.max_teilnehmer then raise exception 'Anmeldung nicht möglich: maximale Teilnehmerzahl erreicht.'; end if;
    end if;
  elsif new.status = 'abgesagt' and (tg_op = 'INSERT' or old.status is distinct from 'abgesagt') then
    if public.kgv_local_now() >= v_begin then raise exception 'Abmeldung nicht möglich: Arbeitseinsatz hat bereits begonnen.'; end if;
  end if;
  return new;
end;
$$;

create or replace function public.sign_up_for_arbeitseinsatz(p_arbeitseinsatz_id bigint, p_mitglied_id bigint)
returns public.arbeitseinsatz_anmeldung language plpgsql security definer set search_path = public, pg_temp as $$
declare v_assignment public.arbeitseinsatz; v_row public.arbeitseinsatz_anmeldung; v_begin timestamp without time zone; v_count integer;
begin
  if auth.uid() is null or public.current_mitglied_id() is null or p_mitglied_id <> public.current_mitglied_id() then raise exception 'Anmeldung ist nur für das eigene Mitglied möglich.' using errcode = '42501'; end if;
  select * into v_assignment from public.arbeitseinsatz where id = p_arbeitseinsatz_id for update;
  if not found then raise exception 'Arbeitseinsatz wurde nicht gefunden.' using errcode = 'P0002'; end if;
  if (public.is_demo_or_reviewer() and not public.is_demo_member_arbeitseinsatz_scope(p_mitglied_id, p_arbeitseinsatz_id)) or (not public.is_demo_or_reviewer() and coalesce(v_assignment.is_demo, false)) then raise exception 'Arbeitseinsatz liegt außerhalb des zulässigen Scopes.' using errcode = '42501'; end if;
  select * into v_row from public.arbeitseinsatz_anmeldung where arbeitseinsatz_id = p_arbeitseinsatz_id and mitglied_id = p_mitglied_id for update;
  if found and v_row.status = 'angemeldet' then return v_row; end if;
  if found and v_row.status in ('teilgenommen', 'nicht_erschienen') then raise exception 'Anmeldung nicht möglich: Der Teilnehmerstatus wird bereits verwaltet.' using errcode = '42501'; end if;
  v_begin := v_assignment.datum + coalesce(v_assignment.start_uhrzeit, time '23:59');
  if v_assignment.aktiv is distinct from true then raise exception 'Anmeldung nicht möglich: Arbeitseinsatz ist nicht aktiv.'; end if;
  if public.kgv_local_now() >= v_begin then raise exception 'Anmeldung nicht möglich: Arbeitseinsatz hat bereits begonnen.'; end if;
  if v_assignment.anmeldung_bis is not null and public.kgv_local_now() > v_assignment.anmeldung_bis then raise exception 'Anmeldung nicht möglich: Anmeldeschluss ist erreicht.'; end if;
  if v_assignment.max_teilnehmer is not null then select count(*) into v_count from public.arbeitseinsatz_anmeldung where arbeitseinsatz_id = p_arbeitseinsatz_id and status = 'angemeldet'; if v_count >= v_assignment.max_teilnehmer then raise exception 'Anmeldung nicht möglich: maximale Teilnehmerzahl erreicht.'; end if; end if;
  if found then update public.arbeitseinsatz_anmeldung set status = 'angemeldet', angemeldet_am = now(), updated_at = now() where id = v_row.id returning * into v_row;
  else insert into public.arbeitseinsatz_anmeldung(arbeitseinsatz_id, mitglied_id, status) values (p_arbeitseinsatz_id, p_mitglied_id, 'angemeldet') returning * into v_row; end if;
  return v_row;
end;
$$;

create or replace function public.sign_off_from_arbeitseinsatz(p_arbeitseinsatz_id bigint, p_mitglied_id bigint)
returns public.arbeitseinsatz_anmeldung language plpgsql security definer set search_path = public, pg_temp as $$
declare v_assignment public.arbeitseinsatz; v_row public.arbeitseinsatz_anmeldung; v_begin timestamp without time zone;
begin
  if auth.uid() is null or public.current_mitglied_id() is null or p_mitglied_id <> public.current_mitglied_id() then raise exception 'Abmeldung ist nur für das eigene Mitglied möglich.' using errcode = '42501'; end if;
  select * into v_assignment from public.arbeitseinsatz where id = p_arbeitseinsatz_id for update;
  if not found then raise exception 'Arbeitseinsatz wurde nicht gefunden.' using errcode = 'P0002'; end if;
  if (public.is_demo_or_reviewer() and not public.is_demo_member_arbeitseinsatz_scope(p_mitglied_id, p_arbeitseinsatz_id)) or (not public.is_demo_or_reviewer() and coalesce(v_assignment.is_demo, false)) then raise exception 'Arbeitseinsatz liegt außerhalb des zulässigen Scopes.' using errcode = '42501'; end if;
  select * into v_row from public.arbeitseinsatz_anmeldung where arbeitseinsatz_id = p_arbeitseinsatz_id and mitglied_id = p_mitglied_id for update;
  if not found then raise exception 'Es besteht keine Anmeldung für diesen Arbeitseinsatz.' using errcode = 'P0002'; end if;
  if v_row.status = 'abgesagt' then return v_row; end if;
  if v_row.status in ('teilgenommen', 'nicht_erschienen') then raise exception 'Abmeldung nicht möglich: Der Teilnehmerstatus wird bereits verwaltet.' using errcode = '42501'; end if;
  v_begin := v_assignment.datum + coalesce(v_assignment.start_uhrzeit, time '23:59');
  if public.kgv_local_now() >= v_begin then raise exception 'Abmeldung nicht möglich: Arbeitseinsatz hat bereits begonnen.'; end if;
  update public.arbeitseinsatz_anmeldung set status = 'abgesagt', updated_at = now() where id = v_row.id returning * into v_row;
  return v_row;
end;
$$;

revoke all on function public.sign_up_for_arbeitseinsatz(bigint, bigint) from public;
revoke all on function public.sign_up_for_arbeitseinsatz(bigint, bigint) from anon;
grant execute on function public.sign_up_for_arbeitseinsatz(bigint, bigint) to authenticated;
revoke all on function public.sign_off_from_arbeitseinsatz(bigint, bigint) from public;
revoke all on function public.sign_off_from_arbeitseinsatz(bigint, bigint) from anon;
grant execute on function public.sign_off_from_arbeitseinsatz(bigint, bigint) to authenticated;
