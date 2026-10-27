-- NB-2B-B3: bind replacement workflows to their immediate predecessor and
-- switch authority only after the replacement has completed successfully.

alter table public.kommunikationsvorgang
  add column if not exists ersetzt_pachtvertragsfassung_id bigint
    references public.pachtvertragsfassung(id) on delete restrict;

alter table public.pachtvertragsfassung
  add column if not exists nicht_mehr_massgeblich_am timestamptz;

create index if not exists ix_kommunikationsvorgang_ersetzt_pachtvertragsfassung
  on public.kommunikationsvorgang(ersetzt_pachtvertragsfassung_id)
  where ersetzt_pachtvertragsfassung_id is not null;

create unique index if not exists ux_kommunikationsvorgang_aktive_ersatzfassung
  on public.kommunikationsvorgang(ersetzt_pachtvertragsfassung_id)
  where ersetzt_pachtvertragsfassung_id is not null
    and status not in ('erledigt', 'abgebrochen');

comment on column public.kommunikationsvorgang.ersetzt_pachtvertragsfassung_id is
  'Unmittelbarer, beim Start fest gebundener Vorgänger eines Prepared-Korrektur-/Ersatzvertrags.';
comment on column public.pachtvertragsfassung.nicht_mehr_massgeblich_am is
  'Wird erst beim erfolgreichen, atomaren Abschluss der unmittelbaren Nachfolgerfassung gesetzt.';

create or replace function public.guard_kommunikationsvorgang_ersatzbindung()
returns trigger
language plpgsql
set search_path = public, pg_temp
as $$
begin
  if new.ersetzt_pachtvertragsfassung_id is distinct from old.ersetzt_pachtvertragsfassung_id then
    raise exception 'Der gebundene Vorgänger eines Ersatzvorgangs ist unveränderlich' using errcode = '55000';
  end if;
  return new;
end;
$$;

create trigger trg_guard_kommunikationsvorgang_ersatzbindung
before update on public.kommunikationsvorgang
for each row execute function public.guard_kommunikationsvorgang_ersatzbindung();

create or replace function public.prepare_pachtvertrag_ersatz(
  p_vorgaenger_fassung_id bigint,
  p_assigned_to uuid
)
returns table(kommunikationsvorgang_id bigint)
language plpgsql
security definer
set search_path = public, pg_temp
as $$
declare
  v_fassung public.pachtvertragsfassung%rowtype;
  v_quelle public.kommunikationsvorgang%rowtype;
  v_parzelle_id bigint;
  v_hauptmitglied_id bigint;
  v_entwurf jsonb;
  v_id bigint;
begin
  if auth.uid() is null or p_assigned_to is distinct from auth.uid() then
    raise exception 'Der Ersatzvorgang muss dem angemeldeten Benutzer zugewiesen werden' using errcode = '42501';
  end if;
  if not exists (
    select 1 from public.app_user au
    where au.user_id = auth.uid() and au.role in ('admin', 'vorstand')
  ) then
    raise exception 'Nur Admin oder Vorstand dürfen einen Ersatzvertrag vorbereiten' using errcode = '42501';
  end if;

  select f.* into v_fassung
  from public.pachtvertragsfassung f
  where f.id = p_vorgaenger_fassung_id
  for update;
  if not found then
    raise exception 'Unbekannte Pachtvertragsfassung: %', p_vorgaenger_fassung_id using errcode = 'P0002';
  end if;
  if v_fassung.nicht_mehr_massgeblich_am is not null
     or exists (select 1 from public.pachtvertragsfassung n where n.vorgaenger_fassung_id = v_fassung.id) then
    raise exception 'Nur die aktuell maßgebliche Vertragsfassung kann ersetzt werden' using errcode = '23514';
  end if;

  select pv.parzelle_id, min(pb.mitglied_id)
    into v_parzelle_id, v_hauptmitglied_id
  from public.pachtverhaeltnis pv
  join public.pachtverhaeltnis_beteiligung pb
    on pb.pachtverhaeltnis_id = pv.id and pb.rolle = 'hauptpaechter' and pb.gueltig_bis is null
  where pv.id = v_fassung.pachtverhaeltnis_id and pv.beendet_am is null
  group by pv.parzelle_id;
  if v_parzelle_id is null or v_hauptmitglied_id is null then
    raise exception 'Für die Vertragsfassung besteht kein aktives Pachtverhältnis' using errcode = '23514';
  end if;

  select kv.* into v_quelle
  from public.dokument d
  join public.kommunikationsvorgang kv on kv.id = d.source_kommunikationsvorgang_id
  where d.id = v_fassung.dokument_id
    and kv.dokumenttyp = 'pachtvertrag' and kv.status = 'erledigt';
  if not found then
    raise exception 'Die maßgebliche Vertragsfassung besitzt keinen abgeschlossenen Prepared-Ursprung' using errcode = '23514';
  end if;

  v_entwurf := jsonb_set(
    v_quelle.entwurf_daten,
    '{draft,effectiveDate}',
    to_jsonb(current_date::text),
    true);

  insert into public.kommunikationsvorgang(
    vorgangstyp, dokumenttyp, status, mitglied_id, parzelle_id,
    entwurf_daten, revision, zugewiesen_an, erstellt_von, bearbeitet_von,
    ersetzt_pachtvertragsfassung_id)
  values (
    'vertrag_signatur', 'pachtvertrag', 'vorbereitet', v_hauptmitglied_id, v_parzelle_id,
    v_entwurf, 1, p_assigned_to, auth.uid(), auth.uid(), v_fassung.id)
  returning id into v_id;

  return query select v_id;
end;
$$;

revoke all on function public.prepare_pachtvertrag_ersatz(bigint, uuid) from public, anon;
grant execute on function public.prepare_pachtvertrag_ersatz(bigint, uuid) to authenticated;

alter function public.complete_prepared_pachtvertrag(bigint, integer, uuid)
  rename to complete_prepared_pachtvertrag_vor_b3;
revoke all on function public.complete_prepared_pachtvertrag_vor_b3(bigint, integer, uuid)
  from public, anon, authenticated, service_role;
grant execute on function public.complete_prepared_pachtvertrag_vor_b3(bigint, integer, uuid)
  to service_role;

create function public.complete_prepared_pachtvertrag(
  p_kommunikationsvorgang_id bigint,
  p_expected_revision integer,
  p_created_by uuid
)
returns table(pachtverhaeltnis_id bigint, pachtvertragsfassung_id bigint, bereits_abgeschlossen boolean)
language plpgsql
security definer
set search_path = public, pg_temp
as $$
declare
  v_vorgang public.kommunikationsvorgang%rowtype;
  v_result record;
  v_vorgaenger public.pachtvertragsfassung%rowtype;
begin
  if auth.role() is distinct from 'service_role' then
    raise exception 'Der Pachtvertragsabschluss ist nur serverseitig zulässig' using errcode = '42501';
  end if;

  select * into v_vorgang
  from public.kommunikationsvorgang
  where id = p_kommunikationsvorgang_id
  for update;
  if not found then
    raise exception 'Unbekannter Kommunikationsvorgang: %', p_kommunikationsvorgang_id using errcode = 'P0002';
  end if;

  if v_vorgang.ersetzt_pachtvertragsfassung_id is not null then
    select * into v_vorgaenger
    from public.pachtvertragsfassung
    where id = v_vorgang.ersetzt_pachtvertragsfassung_id
    for update;
    if not found then
      raise exception 'Der gebundene Vorgänger existiert nicht' using errcode = '23514';
    end if;
    if v_vorgang.status <> 'erledigt' and (
      v_vorgaenger.nicht_mehr_massgeblich_am is not null
      or exists (select 1 from public.pachtvertragsfassung n where n.vorgaenger_fassung_id = v_vorgaenger.id)
    ) then
      raise exception 'Der gebundene Vorgänger ist nicht mehr die aktuelle Vertragsfassung' using errcode = '40001';
    end if;
  end if;

  select * into v_result
  from public.complete_prepared_pachtvertrag_vor_b3(
    p_kommunikationsvorgang_id, p_expected_revision, p_created_by);

  if v_vorgang.ersetzt_pachtvertragsfassung_id is not null then
    if not exists (
      select 1 from public.pachtvertragsfassung f
      where f.id = v_result.pachtvertragsfassung_id
        and f.vorgaenger_fassung_id = v_vorgang.ersetzt_pachtvertragsfassung_id
        and f.pachtverhaeltnis_id = v_vorgaenger.pachtverhaeltnis_id
    ) then
      raise exception 'Die neue Vertragsfassung verweist nicht auf den gebundenen unmittelbaren Vorgänger' using errcode = '23514';
    end if;
    update public.pachtvertragsfassung
    set nicht_mehr_massgeblich_am = coalesce(nicht_mehr_massgeblich_am, now())
    where id = v_vorgang.ersetzt_pachtvertragsfassung_id;
  end if;

  return query select v_result.pachtverhaeltnis_id, v_result.pachtvertragsfassung_id, v_result.bereits_abgeschlossen;
end;
$$;

revoke all on function public.complete_prepared_pachtvertrag(bigint, integer, uuid)
  from public, anon, authenticated, service_role;
grant execute on function public.complete_prepared_pachtvertrag(bigint, integer, uuid)
  to service_role;

create or replace view public.v_pachtvertragsfassung_uebersicht
with (security_invoker = true)
as
select
  f.id,
  f.pachtverhaeltnis_id,
  pv.parzelle_id,
  f.dokument_id,
  f.vorgaenger_fassung_id,
  f.nicht_mehr_massgeblich_am,
  fr.nummer as vertragsnummer,
  vfr.nummer as vorgaenger_vertragsnummer,
  nf.id as nachfolger_fassung_id,
  nfr.nummer as nachfolger_vertragsnummer,
  case when f.nicht_mehr_massgeblich_am is null and nf.id is null then 'massgeblich' else 'nicht_mehr_massgeblich' end as massgeblichkeit
from public.pachtvertragsfassung f
join public.pachtverhaeltnis pv on pv.id = f.pachtverhaeltnis_id
join public.dokument d on d.id = f.dokument_id
left join public.fachnummer_reservierung fr
  on fr.quelle_art = 'kommunikationsvorgang' and fr.quelle_id = d.source_kommunikationsvorgang_id and fr.art = 'pachtvertrag'
left join public.pachtvertragsfassung vf on vf.id = f.vorgaenger_fassung_id
left join public.dokument vd on vd.id = vf.dokument_id
left join public.fachnummer_reservierung vfr
  on vfr.quelle_art = 'kommunikationsvorgang' and vfr.quelle_id = vd.source_kommunikationsvorgang_id and vfr.art = 'pachtvertrag'
left join public.pachtvertragsfassung nf on nf.vorgaenger_fassung_id = f.id
left join public.dokument nd on nd.id = nf.dokument_id
left join public.fachnummer_reservierung nfr
  on nfr.quelle_art = 'kommunikationsvorgang' and nfr.quelle_id = nd.source_kommunikationsvorgang_id and nfr.art = 'pachtvertrag';

grant select on public.v_pachtvertragsfassung_uebersicht to authenticated, service_role;
