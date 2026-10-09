-- G8.5: controlled administrative participant lifecycle.
create or replace function public.manage_arbeitseinsatz_anmeldung(p_arbeitseinsatz_id bigint, p_mitglied_id bigint, p_action text)
returns public.arbeitseinsatz_anmeldung language plpgsql security definer set search_path = public, pg_temp as $$
declare v_assignment public.arbeitseinsatz; v_row public.arbeitseinsatz_anmeldung; v_begin timestamp without time zone; v_count integer; v_registration_exists boolean;
begin
  if auth.uid() is null or not public.has_effective_permission(1048576) then raise exception 'Für die Teilnehmerverwaltung fehlt ManageWorkAssignments.' using errcode = '42501'; end if;
  if p_action not in ('anmelden', 'absagen', 'nicht_erschienen') then raise exception 'Ungültige Teilnehmeraktion.' using errcode = '22023'; end if;
  select * into v_assignment from public.arbeitseinsatz where id = p_arbeitseinsatz_id for update;
  if not found then raise exception 'Arbeitseinsatz wurde nicht gefunden.' using errcode = 'P0002'; end if;
  if (public.is_demo_or_reviewer() and not public.is_demo_member_arbeitseinsatz_scope(p_mitglied_id, p_arbeitseinsatz_id)) or (not public.is_demo_or_reviewer() and coalesce(v_assignment.is_demo, false)) then raise exception 'Teilnehmer oder Arbeitseinsatz liegen außerhalb des zulässigen Scopes.' using errcode = '42501'; end if;
  v_begin := v_assignment.datum + coalesce(v_assignment.start_uhrzeit, time '23:59');
  select * into v_row from public.arbeitseinsatz_anmeldung where arbeitseinsatz_id = p_arbeitseinsatz_id and mitglied_id = p_mitglied_id for update;
  v_registration_exists := found;
  if p_action = 'anmelden' then
    if not exists (select 1 from public.mitglied where id = p_mitglied_id and aktiv = true) then raise exception 'Mitglied ist nicht aktiv.' using errcode = '42501'; end if;
    if v_assignment.aktiv is distinct from true then raise exception 'Anmeldung nicht möglich: Arbeitseinsatz ist nicht aktiv.'; end if;
    if public.kgv_local_now() >= v_begin then raise exception 'Anmeldung nicht möglich: Arbeitseinsatz hat bereits begonnen.'; end if;
    if v_registration_exists and v_row.status = 'angemeldet' then return v_row; end if;
    if v_registration_exists and v_row.status in ('teilgenommen', 'nicht_erschienen') then raise exception 'Dieser Teilnehmerstatus kann nicht wieder angemeldet werden.' using errcode = '42501'; end if;
    if v_assignment.max_teilnehmer is not null then select count(*) into v_count from public.arbeitseinsatz_anmeldung where arbeitseinsatz_id = p_arbeitseinsatz_id and status = 'angemeldet'; if v_count >= v_assignment.max_teilnehmer then raise exception 'Anmeldung nicht möglich: maximale Teilnehmerzahl erreicht.'; end if; end if;
    if v_registration_exists then update public.arbeitseinsatz_anmeldung set status = 'angemeldet', angemeldet_am = now(), updated_at = now() where id = v_row.id returning * into v_row; else insert into public.arbeitseinsatz_anmeldung(arbeitseinsatz_id, mitglied_id, status) values (p_arbeitseinsatz_id, p_mitglied_id, 'angemeldet') returning * into v_row; end if;
  elsif p_action = 'absagen' then
    if not v_registration_exists then raise exception 'Es besteht keine Anmeldung für diesen Arbeitseinsatz.' using errcode = 'P0002'; end if;
    if v_row.status = 'teilgenommen' then raise exception 'Teilgenommen kann nicht über die Teilnehmerverwaltung geändert werden.' using errcode = '42501'; end if;
    if v_row.status <> 'abgesagt' then update public.arbeitseinsatz_anmeldung set status = 'abgesagt', updated_at = now() where id = v_row.id returning * into v_row; end if;
  else
    if public.kgv_local_now() < v_begin then raise exception 'Nicht erschienen kann erst nach Einsatzbeginn gesetzt werden.' using errcode = '42501'; end if;
    if not v_registration_exists or v_row.status not in ('angemeldet', 'nicht_erschienen') then raise exception 'Nicht erschienen ist nur für angemeldete Teilnehmer möglich.' using errcode = '42501'; end if;
    if v_row.status <> 'nicht_erschienen' then update public.arbeitseinsatz_anmeldung set status = 'nicht_erschienen', updated_at = now() where id = v_row.id returning * into v_row; end if;
  end if;
  return v_row;
end;
$$;
revoke all on function public.manage_arbeitseinsatz_anmeldung(bigint, bigint, text) from public;
revoke all on function public.manage_arbeitseinsatz_anmeldung(bigint, bigint, text) from anon;
grant execute on function public.manage_arbeitseinsatz_anmeldung(bigint, bigint, text) to authenticated;

-- Management actions are deliberately limited to the RPC above; they may be
-- performed after the start time where the ordinary self-service path may not.
create or replace function public.trg_arbeitseinsatz_anmeldung_validate()
returns trigger language plpgsql security definer set search_path = public, pg_temp as $$
declare v_assignment public.arbeitseinsatz; v_begin timestamp without time zone; v_count integer;
begin
  select * into v_assignment from public.arbeitseinsatz where id = new.arbeitseinsatz_id;
  if not found then raise exception 'Arbeitseinsatz wurde nicht gefunden.' using errcode = '23503'; end if;
  v_begin := v_assignment.datum + coalesce(v_assignment.start_uhrzeit, time '23:59');
  if tg_op = 'UPDATE' and public.current_mitglied_id() = new.mitglied_id and not public.has_effective_permission(1048576) and (old.status not in ('angemeldet', 'abgesagt') or new.status not in ('angemeldet', 'abgesagt')) then
    raise exception 'Verwaltungsstatus darf nicht durch ein Mitglied geändert werden.' using errcode = '42501';
  end if;
  if new.status = 'angemeldet' and (tg_op = 'INSERT' or old.status is distinct from 'angemeldet') then
    if v_assignment.aktiv is distinct from true then raise exception 'Anmeldung nicht möglich: Arbeitseinsatz ist nicht aktiv.'; end if;
    if public.kgv_local_now() >= v_begin then raise exception 'Anmeldung nicht möglich: Arbeitseinsatz hat bereits begonnen.'; end if;
    if not public.has_effective_permission(1048576) and v_assignment.anmeldung_bis is not null and public.kgv_local_now() > v_assignment.anmeldung_bis then raise exception 'Anmeldung nicht möglich: Anmeldeschluss ist erreicht.'; end if;
    if v_assignment.max_teilnehmer is not null then select count(*) into v_count from public.arbeitseinsatz_anmeldung aa where aa.arbeitseinsatz_id = new.arbeitseinsatz_id and aa.status = 'angemeldet' and (tg_op = 'INSERT' or aa.id <> new.id); if v_count >= v_assignment.max_teilnehmer then raise exception 'Anmeldung nicht möglich: maximale Teilnehmerzahl erreicht.'; end if; end if;
  elsif new.status = 'abgesagt' and (tg_op = 'INSERT' or old.status is distinct from 'abgesagt') and not public.has_effective_permission(1048576) then
    if public.kgv_local_now() >= v_begin then raise exception 'Abmeldung nicht möglich: Arbeitseinsatz hat bereits begonnen.'; end if;
  end if;
  return new;
end;
$$;
drop policy if exists arbeitseinsatz_anmeldung_manage_all on public.arbeitseinsatz_anmeldung;
drop policy if exists arbeitseinsatz_anmeldung_delete_own on public.arbeitseinsatz_anmeldung;
drop policy if exists arbeitseinsatz_anmeldung_insert_own_open on public.arbeitseinsatz_anmeldung;
drop policy if exists arbeitseinsatz_anmeldung_update_own_open on public.arbeitseinsatz_anmeldung;
create policy arbeitseinsatz_anmeldung_manage_select on public.arbeitseinsatz_anmeldung for select to authenticated using (
  public.has_effective_permission(1048576) and ((not public.is_demo_or_reviewer() and exists (select 1 from public.arbeitseinsatz a where a.id = arbeitseinsatz_id and coalesce(a.is_demo, false) = false)) or (public.is_demo_or_reviewer() and public.is_demo_member_arbeitseinsatz_scope(mitglied_id, arbeitseinsatz_id))));
