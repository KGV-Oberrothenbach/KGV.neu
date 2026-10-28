-- Phase 9: server-side tenant eligibility and retry hardening for lease contracts.
-- Source only. This migration is deliberately not executed by this change.

create or replace function public.guard_pachtverhaeltnis_beteiligung_mitgliedsstatus()
returns trigger
language plpgsql
security definer
set search_path=public,pg_temp as $$
begin
  if not exists (
    select 1 from public.mitglied m
    where m.id=new.mitglied_id
      and m.aufnahme_status='aufgenommen'
      and m.aktiv=true
  ) then
    raise exception 'Pächter müssen aktive aufgenommene Mitglieder sein.' using errcode='23514';
  end if;
  return new;
end;
$$;

drop trigger if exists trg_guard_pachtverhaeltnis_beteiligung_mitgliedsstatus on public.pachtverhaeltnis_beteiligung;
create trigger trg_guard_pachtverhaeltnis_beteiligung_mitgliedsstatus
before insert on public.pachtverhaeltnis_beteiligung
for each row execute function public.guard_pachtverhaeltnis_beteiligung_mitgliedsstatus();

alter function public.complete_prepared_pachtvertrag(bigint,integer,uuid)
  rename to complete_prepared_pachtvertrag_vor_phase9;
revoke all on function public.complete_prepared_pachtvertrag_vor_phase9(bigint,integer,uuid)
  from public,anon,authenticated,service_role;
grant execute on function public.complete_prepared_pachtvertrag_vor_phase9(bigint,integer,uuid)
  to service_role;

create function public.complete_prepared_pachtvertrag(
  p_kommunikationsvorgang_id bigint,
  p_expected_revision integer,
  p_created_by uuid
)
returns table(pachtverhaeltnis_id bigint,pachtvertragsfassung_id bigint,bereits_abgeschlossen boolean)
language plpgsql
security definer
set search_path=public,pg_temp as $$
declare
  v_vorgang public.kommunikationsvorgang%rowtype;
  v_mitpaechter_id bigint;
begin
  if auth.role() is distinct from 'service_role' then
    raise exception 'Der Pachtvertragsabschluss ist nur serverseitig zulässig' using errcode='42501';
  end if;

  select * into v_vorgang
  from public.kommunikationsvorgang
  where id=p_kommunikationsvorgang_id
  for key share;
  if not found then
    raise exception 'Unbekannter Kommunikationsvorgang: %',p_kommunikationsvorgang_id using errcode='P0002';
  end if;

  -- A completed retry must remain readable even if membership ends later.
  if v_vorgang.status<>'erledigt' then
    if not exists (
      select 1 from public.mitglied m
      where m.id=v_vorgang.mitglied_id
        and m.aufnahme_status='aufgenommen'
        and m.aktiv=true
    ) then
      raise exception 'Pächter 1 muss ein aktives aufgenommenes Mitglied sein.' using errcode='23514';
    end if;

    if coalesce((v_vorgang.entwurf_daten #>> '{draft,includeSecondaryMember}')::boolean,false) then
      if coalesce(v_vorgang.entwurf_daten #>> '{draft,secondaryMemberId}','') !~ '^\d+$' then
        raise exception 'Für Pächter 2 fehlt ein gültiges Mitglied.' using errcode='23514';
      end if;
      v_mitpaechter_id := (v_vorgang.entwurf_daten #>> '{draft,secondaryMemberId}')::bigint;
      if v_mitpaechter_id=v_vorgang.mitglied_id or not exists (
        select 1 from public.mitglied m
        where m.id=v_mitpaechter_id
          and m.aufnahme_status='aufgenommen'
          and m.aktiv=true
      ) then
        raise exception 'Pächter 2 muss ein anderes aktives aufgenommenes Mitglied sein.' using errcode='23514';
      end if;
    end if;
  end if;

  return query
  select * from public.complete_prepared_pachtvertrag_vor_phase9(
    p_kommunikationsvorgang_id,p_expected_revision,p_created_by);
end;
$$;

revoke all on function public.complete_prepared_pachtvertrag(bigint,integer,uuid)
  from public,anon,authenticated,service_role;
grant execute on function public.complete_prepared_pachtvertrag(bigint,integer,uuid)
  to service_role;

alter function public.prepare_pachtvertrag_ersatz(bigint,uuid)
  rename to prepare_pachtvertrag_ersatz_vor_phase9;
revoke all on function public.prepare_pachtvertrag_ersatz_vor_phase9(bigint,uuid)
  from public,anon,authenticated;
grant execute on function public.prepare_pachtvertrag_ersatz_vor_phase9(bigint,uuid)
  to authenticated;

create function public.prepare_pachtvertrag_ersatz(
  p_vorgaenger_fassung_id bigint,
  p_assigned_to uuid
)
returns table(kommunikationsvorgang_id bigint)
language plpgsql
security definer
set search_path=public,pg_temp as $$
declare
  v_id bigint;
begin
  if auth.uid() is null or p_assigned_to is distinct from auth.uid() then
    raise exception 'Der Ersatzvorgang muss dem angemeldeten Benutzer zugewiesen werden' using errcode='42501';
  end if;
  if not exists (select 1 from public.app_user au where au.user_id=auth.uid() and au.role in ('admin','vorstand')) then
    raise exception 'Nur Admin oder Vorstand dürfen einen Ersatzvertrag vorbereiten' using errcode='42501';
  end if;

  select id into v_id
  from public.kommunikationsvorgang
  where ersetzt_pachtvertragsfassung_id=p_vorgaenger_fassung_id
    and zugewiesen_an=p_assigned_to
    and status not in ('erledigt','abgebrochen')
  for update;
  if found then
    return query select v_id;
    return;
  end if;

  return query
  select * from public.prepare_pachtvertrag_ersatz_vor_phase9(
    p_vorgaenger_fassung_id,p_assigned_to);
end;
$$;

revoke all on function public.prepare_pachtvertrag_ersatz(bigint,uuid)
  from public,anon;
grant execute on function public.prepare_pachtvertrag_ersatz(bigint,uuid)
  to authenticated;

revoke all on function public.guard_pachtverhaeltnis_beteiligung_mitgliedsstatus()
  from public,anon,authenticated;
