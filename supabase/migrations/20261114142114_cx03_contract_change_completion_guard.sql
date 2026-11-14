-- CX-03: applying a frozen parcel change is the last step of the completed
-- lease-contract workflow.  A version inserted during letzte_pruefung alone
-- is not sufficient authorization.

-- The active replacement-contract completion function predates its TABLE
-- return names.  Qualify the one column that otherwise collides with the
-- pachtverhaeltnis_id output parameter in the replacement branch.
do $$
declare
  v_definition text;
  v_old text := 'from public.pachtverhaeltnis_beteiligung where pachtverhaeltnis_id=v_pachtverhaeltnis_id';
  v_new text := 'from public.pachtverhaeltnis_beteiligung pb where pb.pachtverhaeltnis_id=v_pachtverhaeltnis_id';
begin
  select pg_get_functiondef(
    'public.complete_prepared_pachtvertrag_vor_b3(bigint,integer,uuid)'::regprocedure
  ) into v_definition;
  if position(v_old in v_definition) = 0 then
    raise exception 'Expected active replacement completion definition was not found';
  end if;
  execute replace(v_definition, v_old, v_new);
end;
$$;

create or replace function public.wende_signierte_parzellen_vertragsaenderung_an(
  p_pacht_vertragsaenderung_id bigint
)
returns boolean
language plpgsql
security definer
set search_path = public, pg_temp
as $$
declare
  v_change public.pacht_vertragsaenderung%rowtype;
  v_process public.kommunikationsvorgang%rowtype;
  v_version public.pachtvertragsfassung%rowtype;
  v_document public.dokument%rowtype;
  v_old jsonb;
  v_user uuid := auth.uid();
  v_expected text[];
  v_clause text;
begin
  if v_user is null or not public.has_effective_permission(524288) then
    raise exception 'Keine Berechtigung für Vertragsänderungen.' using errcode = '42501';
  end if;

  select * into v_change
  from public.pacht_vertragsaenderung
  where id = p_pacht_vertragsaenderung_id
  for update;
  if not found then
    raise exception 'Unbekannte Vertragsänderung.' using errcode = 'P0002';
  end if;
  if v_change.angewendet_am is not null then
    return true;
  end if;

  select * into v_process
  from public.kommunikationsvorgang
  where id = v_change.kommunikationsvorgang_id
  for key share;
  select * into v_version
  from public.pachtvertragsfassung
  where id = v_change.pachtvertragsfassung_id
  for key share;
  if v_process.id is null or v_version.id is null then
    raise exception 'Vollständig abgeschlossene Vertragsänderung fehlt.' using errcode = '23514';
  end if;
  select * into v_document
  from public.dokument
  where id = v_version.dokument_id and archiviert_at is null
  for key share;

  v_expected := array['lease.member', 'vorstand_1', 'vorstand_2'];
  if coalesce((v_process.entwurf_daten #>> '{draft,includeSecondaryMember}')::boolean, false) then
    v_expected := array_append(v_expected, 'lease.second_party');
  end if;
  v_clause := public.p11_contract_change_clause(v_change.aenderungsstand, v_change.begruendung);

  if v_change.signiert_am is null
     or v_process.vorgangstyp <> 'vertrag_signatur'
     or v_process.dokumenttyp <> 'pachtvertrag'
     or v_process.status <> 'erledigt'
     or v_process.signatur_revision is null
     or v_process.signatur_revision <> v_process.revision
     or v_process.final_dokument_id is distinct from v_version.dokument_id
     or v_document.id is null
     or v_document.source_kommunikationsvorgang_id is distinct from v_process.id
     or v_document.source_revision is distinct from v_process.signatur_revision
     or v_version.pachtverhaeltnis_id is distinct from v_change.pachtverhaeltnis_id
     or v_version.vorgaenger_fassung_id is distinct from v_change.vorgaenger_fassung_id
     or v_process.ersetzt_pachtvertragsfassung_id is distinct from v_change.vorgaenger_fassung_id
     or v_process.entwurf_daten #>> '{draft,p11_contract_change_id}' is distinct from v_change.id::text
     or v_process.entwurf_daten #>> '{draft,p11_contract_change_hash}' is distinct from v_change.aenderungs_hash
     or position(v_clause in coalesce(v_process.entwurf_daten #>> '{draft,additionalAgreements}', '')) = 0
     or exists (
       select 1 from unnest(v_expected) expected_key
       where not exists (
         select 1 from public.prepared_contract_signatur s
         where s.kommunikationsvorgang_id = v_process.id
           and s.signatur_revision = v_process.signatur_revision
           and s.signatur_schluessel = expected_key
           and s.pflicht
           and s.status = 'unterschrieben'
       )
     )
     or not exists (
       select 1 from public.pachtverhaeltnis p
       where p.id = v_change.pachtverhaeltnis_id and p.beendet_am is null
     ) then
    raise exception 'Die Vertragsänderung ist nicht vollständig und revisionsgenau abgeschlossen.' using errcode = '23514';
  end if;

  select jsonb_build_object(
    'flaeche_qm', pa.flaeche_qm,
    'hat_strom', pa.hat_strom,
    'hat_wasser', pa.hat_wasser
  ) into v_old
  from public.parzelle pa
  join public.pachtverhaeltnis p on p.parzelle_id = pa.id
  where p.id = v_change.pachtverhaeltnis_id
  for update of pa;

  insert into public.p11_pacht_aenderung_ausfuehrung(pacht_vertragsaenderung_id, txid)
  values (v_change.id, txid_current());
  update public.parzelle
  set flaeche_qm = (v_change.aenderungsstand->>'flaeche_qm')::numeric,
      hat_strom = (v_change.aenderungsstand->>'hat_strom')::boolean,
      hat_wasser = (v_change.aenderungsstand->>'hat_wasser')::boolean
  where id = (select parzelle_id from public.pachtverhaeltnis where id = v_change.pachtverhaeltnis_id);
  insert into public.pacht_daten_audit(
    pachtverhaeltnis_id, parzelle_id, pachtvertragsfassung_id, art,
    vorher, nachher, begruendung, akteur_user_id
  )
  select p.id, p.parzelle_id, v_change.pachtvertragsfassung_id, 'vertragsaenderung',
         v_old, v_change.aenderungsstand, v_change.begruendung, v_user
  from public.pachtverhaeltnis p where p.id = v_change.pachtverhaeltnis_id;
  update public.pacht_vertragsaenderung
  set angewendet_am = now(), angewendet_von = v_user
  where id = v_change.id;
  delete from public.p11_pacht_aenderung_ausfuehrung
  where pacht_vertragsaenderung_id = v_change.id;
  return true;
end;
$$;

revoke all on function public.wende_signierte_parzellen_vertragsaenderung_an(bigint) from public, anon;
grant execute on function public.wende_signierte_parzellen_vertragsaenderung_an(bigint) to authenticated;

-- Contract versions are evidence.  The only in-place transition retained for
-- the productive completion RPC is marking the direct predecessor obsolete.
create or replace function public.guard_pachtvertragsfassung_historie()
returns trigger
language plpgsql
security definer
set search_path = public, pg_temp
as $$
begin
  if tg_op = 'UPDATE'
     and old.nicht_mehr_massgeblich_am is null
     and new.nicht_mehr_massgeblich_am is not null
     and (to_jsonb(new) - 'nicht_mehr_massgeblich_am') = (to_jsonb(old) - 'nicht_mehr_massgeblich_am')
     and exists (
       select 1 from public.pachtvertragsfassung successor
       where successor.vorgaenger_fassung_id = old.id
     ) then
    return new;
  end if;
  raise exception 'Gespeicherte Vertragsfassungen sind unveränderliche historische Originale.' using errcode = '55000';
end;
$$;

drop trigger if exists trg_guard_pachtvertragsfassung_historie on public.pachtvertragsfassung;
create trigger trg_guard_pachtvertragsfassung_historie
before update or delete on public.pachtvertragsfassung
for each row execute function public.guard_pachtvertragsfassung_historie();

revoke all on function public.guard_pachtvertragsfassung_historie() from public, anon, authenticated;
