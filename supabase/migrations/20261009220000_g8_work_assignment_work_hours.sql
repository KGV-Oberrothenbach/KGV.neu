-- G8.6: work hours may be linked to one work-assignment registration only.
alter table public.arbeitsstunde add column if not exists arbeitseinsatz_anmeldung_id bigint null;
alter table public.arbeitsstunde drop constraint if exists arbeitsstunde_arbeitseinsatz_anmeldung_id_fkey;
alter table public.arbeitsstunde add constraint arbeitsstunde_arbeitseinsatz_anmeldung_id_fkey foreign key (arbeitseinsatz_anmeldung_id) references public.arbeitseinsatz_anmeldung(id) on delete set null;
create unique index if not exists ux_arbeitsstunde_arbeitseinsatz_anmeldung on public.arbeitsstunde (arbeitseinsatz_anmeldung_id) where arbeitseinsatz_anmeldung_id is not null;

-- Generic G7 inserts remain free work-hour inserts; only the dedicated RPCs below may create a link.
drop policy if exists arbeitsstunde_insert_own_open on public.arbeitsstunde;
create policy arbeitsstunde_insert_own_open on public.arbeitsstunde for insert to authenticated with check (
  mitglied_id = public.current_mitglied_id() and status = 'offen' and freigegeben = false and genehmigt_von is null and genehmigt_am is null and arbeitseinsatz_anmeldung_id is null
  and (not public.is_demo_or_reviewer() or public.is_demo_mitglied_id(mitglied_id))
);
drop policy if exists arbeitsstunde_manage_work_hours_insert on public.arbeitsstunde;
create policy arbeitsstunde_manage_work_hours_insert on public.arbeitsstunde for insert to authenticated with check (
  public.has_effective_permission(256) and arbeitseinsatz_anmeldung_id is null and (not public.is_demo_or_reviewer() or public.is_demo_mitglied_id(mitglied_id))
  and status = 'genehmigt' and freigegeben = true and genehmigt_von = public.current_mitglied_id() and genehmigt_am is not null
);

create or replace function public.enforce_own_open_arbeitsstunde_update()
returns trigger language plpgsql security definer set search_path = public as $$
declare v_assignment_date date;
begin
  if current_setting('app.g7_work_hour_review', true) = 'true' and public.has_effective_permission(256) then
    if old.arbeitseinsatz_anmeldung_id is not null then
      if new.arbeitseinsatz_anmeldung_id is distinct from old.arbeitseinsatz_anmeldung_id then raise exception 'Die Arbeitseinsatzzuordnung ist unveränderlich.' using errcode = '42501'; end if;
      select a.datum into v_assignment_date from public.arbeitseinsatz_anmeldung aa join public.arbeitseinsatz a on a.id = aa.arbeitseinsatz_id where aa.id = old.arbeitseinsatz_anmeldung_id;
      if new.datum <> v_assignment_date then raise exception 'Das Datum einer Einsatz-Arbeitsstunde entspricht immer dem Einsatzdatum.' using errcode = '42501'; end if;
    end if;
    return new;
  end if;
  if old.mitglied_id <> public.current_mitglied_id() or old.status <> 'offen' or old.freigegeben <> false or old.genehmigt_von is not null or old.genehmigt_am is not null
     or new.mitglied_id <> public.current_mitglied_id() or new.status <> 'offen' or new.freigegeben <> false or new.genehmigt_von is not null or new.genehmigt_am is not null then raise exception 'Nur eigene offene Arbeitsstunden dürfen bearbeitet werden.' using errcode = '42501'; end if;
  if new.id is distinct from old.id or new.mitglied_id is distinct from old.mitglied_id or new.saison_id is distinct from old.saison_id or new.status is distinct from old.status or new.freigegeben is distinct from old.freigegeben or new.is_demo is distinct from old.is_demo or new.genehmigt_von is distinct from old.genehmigt_von or new.genehmigt_am is distinct from old.genehmigt_am or new.arbeitseinsatz_anmeldung_id is distinct from old.arbeitseinsatz_anmeldung_id then raise exception 'Nur Datum, Stunden und Art der Arbeit dürfen geändert werden.' using errcode = '42501'; end if;
  if old.arbeitseinsatz_anmeldung_id is not null then select a.datum into v_assignment_date from public.arbeitseinsatz_anmeldung aa join public.arbeitseinsatz a on a.id = aa.arbeitseinsatz_id where aa.id = old.arbeitseinsatz_anmeldung_id; if new.datum <> v_assignment_date then raise exception 'Das Datum einer Einsatz-Arbeitsstunde entspricht immer dem Einsatzdatum.' using errcode = '42501'; end if; end if;
  if new.lockedbyuserid is not distinct from old.lockedbyuserid and new.lockat is not distinct from old.lockat then return new; end if;
  if new.lockedbyuserid = auth.uid() and new.lockat is not null and exists (select 1 from public.browser_edit_lock l where l.entity_type = 'arbeitsstunde' and l.entity_id = old.id::text and l.locked_by_user_id = auth.uid() and l.expires_at > now()) then return new; end if;
  if old.lockedbyuserid = auth.uid() and new.lockedbyuserid is null and new.lockat is null then return new; end if;
  raise exception 'Arbeitsstunden-Sperren dürfen nur über den Browser-Lock geändert werden.' using errcode = '42501';
end;
$$;

create or replace function public.submit_arbeitseinsatz_arbeitsstunde(p_arbeitseinsatz_anmeldung_id bigint, p_stunden numeric, p_art_der_arbeit text default null)
returns public.arbeitsstunde language plpgsql security definer set search_path = public, pg_temp as $$
declare v_registration public.arbeitseinsatz_anmeldung; v_assignment public.arbeitseinsatz; v_saison_id integer; v_row public.arbeitsstunde; v_end timestamp without time zone;
begin
  if auth.uid() is null or public.current_mitglied_id() is null then raise exception 'Es ist ein angemeldetes Mitglied erforderlich.' using errcode = '42501'; end if;
  if p_stunden is null or p_stunden <= 0 then raise exception 'Die Stunden müssen größer als 0 sein.' using errcode = '22023'; end if;
  select * into v_registration from public.arbeitseinsatz_anmeldung where id = p_arbeitseinsatz_anmeldung_id for update;
  if not found or v_registration.mitglied_id <> public.current_mitglied_id() then raise exception 'Die Anmeldung gehört nicht zum aktuellen Mitglied.' using errcode = '42501'; end if;
  select * into v_assignment from public.arbeitseinsatz where id = v_registration.arbeitseinsatz_id;
  if not found or v_assignment.aktiv is distinct from true then raise exception 'Der Arbeitseinsatz ist nicht aktiv.' using errcode = '42501'; end if;
  if not ((not public.is_demo_or_reviewer() and coalesce(v_assignment.is_demo, false) = false and not public.is_demo_mitglied_id(v_registration.mitglied_id)) or (public.is_demo_or_reviewer() and public.is_demo_member_arbeitseinsatz_scope(v_registration.mitglied_id, v_assignment.id))) then raise exception 'Arbeitseinsatz liegt außerhalb des zulässigen Scopes.' using errcode = '42501'; end if;
  if v_registration.status <> 'angemeldet' then raise exception 'Arbeitsstunden können nur für angemeldete Teilnehmer eingereicht werden.' using errcode = '42501'; end if;
  v_end := v_assignment.datum + coalesce(v_assignment.end_uhrzeit, v_assignment.start_uhrzeit, time '23:59');
  if public.kgv_local_now() < v_end then raise exception 'Arbeitsstunden können erst nach Einsatzende eingereicht werden.' using errcode = '42501'; end if;
  select id into v_saison_id from public.saison where jahr = extract(year from v_assignment.datum)::integer limit 1;
  if v_saison_id is null then raise exception 'Für das Einsatzjahr ist keine Saison hinterlegt.' using errcode = 'P0002'; end if;
  if exists (select 1 from public.arbeitsstunde where arbeitseinsatz_anmeldung_id = v_registration.id) then raise exception 'Für diese Anmeldung wurden bereits Arbeitsstunden erfasst.' using errcode = '23505'; end if;
  insert into public.arbeitsstunde(mitglied_id,saison_id,datum,stunden,art_der_arbeit,status,freigegeben,genehmigt_von,genehmigt_am,arbeitseinsatz_anmeldung_id) values (v_registration.mitglied_id,v_saison_id,v_assignment.datum,p_stunden,coalesce(nullif(btrim(p_art_der_arbeit),''),v_assignment.titel),'offen',false,null,null,v_registration.id) returning * into v_row;
  return v_row;
end; $$;

create or replace function public.confirm_arbeitseinsatz_arbeitsstunde(p_arbeitseinsatz_anmeldung_id bigint, p_stunden numeric, p_art_der_arbeit text default null)
returns public.arbeitsstunde language plpgsql security definer set search_path = public, pg_temp as $$
declare v_registration public.arbeitseinsatz_anmeldung; v_assignment public.arbeitseinsatz; v_saison_id integer; v_reviewer integer; v_row public.arbeitsstunde; v_end timestamp without time zone;
begin
  if not public.has_effective_permission(1048576) or not public.has_effective_permission(256) then raise exception 'Für die Arbeitsstundenbestätigung fehlen ManageWorkAssignments und ManageWorkHours.' using errcode = '42501'; end if;
  if p_stunden is null or p_stunden <= 0 then raise exception 'Die Stunden müssen größer als 0 sein.' using errcode = '22023'; end if;
  v_reviewer := public.current_mitglied_id(); if v_reviewer is null then raise exception 'Der Bearbeiter ist keinem Mitglied zugeordnet.' using errcode = '42501'; end if;
  select * into v_registration from public.arbeitseinsatz_anmeldung where id = p_arbeitseinsatz_anmeldung_id for update;
  if not found then raise exception 'Die Anmeldung existiert nicht.' using errcode = 'P0002'; end if;
  select * into v_assignment from public.arbeitseinsatz where id = v_registration.arbeitseinsatz_id;
  if not found or v_assignment.aktiv is distinct from true then raise exception 'Der Arbeitseinsatz ist nicht aktiv.' using errcode = '42501'; end if;
  if not ((not public.is_demo_or_reviewer() and coalesce(v_assignment.is_demo, false) = false and not public.is_demo_mitglied_id(v_registration.mitglied_id)) or (public.is_demo_or_reviewer() and public.is_demo_member_arbeitseinsatz_scope(v_registration.mitglied_id, v_assignment.id))) then raise exception 'Arbeitseinsatz liegt außerhalb des zulässigen Scopes.' using errcode = '42501'; end if;
  if v_registration.status = 'teilgenommen' or exists (select 1 from public.arbeitsstunde where arbeitseinsatz_anmeldung_id = v_registration.id) then raise exception 'Für diese Anmeldung existiert bereits eine Arbeitsstunde.' using errcode = '23505'; end if;
  v_end := v_assignment.datum + coalesce(v_assignment.end_uhrzeit, v_assignment.start_uhrzeit, time '23:59'); if public.kgv_local_now() < v_end then raise exception 'Arbeitsstunden können erst nach Einsatzende bestätigt werden.' using errcode = '42501'; end if;
  select id into v_saison_id from public.saison where jahr = extract(year from v_assignment.datum)::integer limit 1; if v_saison_id is null then raise exception 'Für das Einsatzjahr ist keine Saison hinterlegt.' using errcode = 'P0002'; end if;
  insert into public.arbeitsstunde(mitglied_id,saison_id,datum,stunden,art_der_arbeit,status,freigegeben,genehmigt_von,genehmigt_am,arbeitseinsatz_anmeldung_id) values (v_registration.mitglied_id,v_saison_id,v_assignment.datum,p_stunden,coalesce(nullif(btrim(p_art_der_arbeit),''),v_assignment.titel),'genehmigt',true,v_reviewer,now(),v_registration.id) returning * into v_row;
  update public.arbeitseinsatz_anmeldung set status = 'teilgenommen', updated_at = now() where id = v_registration.id;
  return v_row;
end; $$;

revoke all on function public.submit_arbeitseinsatz_arbeitsstunde(bigint,numeric,text) from public, anon; grant execute on function public.submit_arbeitseinsatz_arbeitsstunde(bigint,numeric,text) to authenticated;
revoke all on function public.confirm_arbeitseinsatz_arbeitsstunde(bigint,numeric,text) from public, anon; grant execute on function public.confirm_arbeitseinsatz_arbeitsstunde(bigint,numeric,text) to authenticated;

-- The G7 review function remains the sole review entry point. This trigger runs
-- in its transaction, so audit, approval and participant confirmation commit together.
create or replace function public.sync_arbeitseinsatz_teilnahme_from_arbeitsstunde()
returns trigger language plpgsql security definer set search_path = public as $$
begin
  if new.arbeitseinsatz_anmeldung_id is not null and new.status = 'genehmigt' and new.freigegeben = true and (tg_op = 'INSERT' or old.status is distinct from 'genehmigt' or old.freigegeben is distinct from true) then
    update public.arbeitseinsatz_anmeldung set status = 'teilgenommen', updated_at = now() where id = new.arbeitseinsatz_anmeldung_id;
  end if;
  return new;
end; $$;
revoke all on function public.sync_arbeitseinsatz_teilnahme_from_arbeitsstunde() from public, anon, authenticated;
drop trigger if exists trg_sync_arbeitseinsatz_teilnahme_from_arbeitsstunde on public.arbeitsstunde;
create trigger trg_sync_arbeitseinsatz_teilnahme_from_arbeitsstunde after insert or update of status, freigegeben on public.arbeitsstunde for each row execute function public.sync_arbeitseinsatz_teilnahme_from_arbeitsstunde();
