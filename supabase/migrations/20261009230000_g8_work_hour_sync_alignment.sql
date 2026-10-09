-- G8.6: permit the work-hour approval trigger to record participation for the
-- same member without granting general participant-management permission.
create or replace function public.trg_arbeitseinsatz_anmeldung_validate()
returns trigger language plpgsql security definer set search_path = public, pg_temp as $$
declare v_assignment public.arbeitseinsatz; v_begin timestamp without time zone; v_count integer;
begin
  select * into v_assignment from public.arbeitseinsatz where id = new.arbeitseinsatz_id;
  if not found then raise exception 'Arbeitseinsatz wurde nicht gefunden.' using errcode = '23503'; end if;
  v_begin := v_assignment.datum + coalesce(v_assignment.start_uhrzeit, time '23:59');

  if tg_op = 'UPDATE' and public.current_mitglied_id() = new.mitglied_id and not public.has_effective_permission(1048576) and (old.status not in ('angemeldet', 'abgesagt') or new.status not in ('angemeldet', 'abgesagt')) then
    if new.status <> 'teilgenommen' or not exists (
      select 1
      from public.arbeitsstunde a
      where a.arbeitseinsatz_anmeldung_id = new.id
        and a.status = 'genehmigt'
        and a.freigegeben = true
    ) then
      raise exception 'Verwaltungsstatus darf nicht durch ein Mitglied geändert werden.' using errcode = '42501';
    end if;
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
