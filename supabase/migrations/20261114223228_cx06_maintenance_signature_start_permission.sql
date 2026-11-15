-- CX-06: the shared signature start must authorize the concrete document type.
-- Membership applications and leases retain CanCreateMitglied (524288).
-- Maintenance contracts use the same effective CanEditAllMembers (4) capability
-- as their preparation workflow.  Individual revocations remain effective.
create or replace function public.start_prepared_contract_signature(
  p_kommunikationsvorgang_id bigint,
  p_expected_revision integer,
  p_assigned_to uuid
)
returns public.kommunikationsvorgang
language plpgsql
security definer
set search_path = public, pg_temp
as $$
declare
  v public.kommunikationsvorgang%rowtype;
  r record;
  p_id bigint;
  v_stichtag date := (now() at time zone 'Europe/Berlin')::date;
begin
  if auth.uid() is null or auth.uid() is distinct from p_assigned_to then
    raise exception 'Keine Berechtigung zum Signaturstart' using errcode = '42501';
  end if;

  select * into v
  from public.kommunikationsvorgang
  where id = p_kommunikationsvorgang_id
  for update;

  if not found
     or v.vorgangstyp <> 'vertrag_signatur'
     or v.revision <> p_expected_revision
     or v.zugewiesen_an is distinct from p_assigned_to then
    raise exception 'Der Vertragsvorgang kann nicht zur Signatur gestartet werden' using errcode = '40001';
  end if;

  if (v.dokumenttyp = 'wartungsvertrag' and not public.has_effective_permission(4))
     or (v.dokumenttyp <> 'wartungsvertrag' and not public.has_effective_permission(524288)) then
    raise exception 'Keine Berechtigung zum Signaturstart' using errcode = '42501';
  end if;

  -- Lost-response retry: reuse revision, number, positions and frozen signatories.
  if v.status in ('zur_signatur', 'letzte_pruefung', 'erledigt')
     and v.signatur_gestartet_am is not null
     and v.signatur_revision = p_expected_revision
     and exists (
       select 1 from public.prepared_contract_signatur_snapshot_person p
       where p.kommunikationsvorgang_id = v.id
         and p.signatur_revision = p_expected_revision
     )
     and exists (
       select 1 from public.prepared_contract_signatur p
       where p.kommunikationsvorgang_id = v.id
         and p.signatur_revision = p_expected_revision
     ) then
    return v;
  end if;

  if v.status <> 'vorbereitet'
     or v.signatur_gestartet_am is not null
     or v.signatur_revision is not null then
    raise exception 'Der Vertragsvorgang kann nicht zur Signatur gestartet werden' using errcode = '40001';
  end if;
  if exists (
    select 1 from public.prepared_contract_signatur_snapshot_person p
    where p.kommunikationsvorgang_id = v.id
      and p.signatur_revision = v.revision
  ) then
    raise exception 'Inkonsistenter Signatur-Snapshot vor dem Signaturstart' using errcode = '23514';
  end if;

  for r in select * from public.resolve_vertragsunterzeichner(v_stichtag) loop
    insert into public.prepared_contract_signatur_snapshot_person(
      kommunikationsvorgang_id, signatur_revision, mitglied_id,
      unterzeichner_name_snapshot
    ) values (
      v.id, v.revision, r.mitglied_id, r.unterzeichner_name
    )
    on conflict (kommunikationsvorgang_id, signatur_revision, mitglied_id)
    do update set unterzeichner_name_snapshot = excluded.unterzeichner_name_snapshot
    returning id into p_id;

    insert into public.prepared_contract_signatur_snapshot_quelle(
      snapshot_person_id, berechtigungsart, vereinsfunktion_id,
      vereinsfunktion_bezeichnung_snapshot, vereinsfunktion_besetzung_id,
      besetzung_gueltig_ab_snapshot, besetzung_gueltig_bis_snapshot,
      vertretung_id, vertretung_gueltig_ab_snapshot,
      vertretung_gueltig_bis_snapshot, vertretene_vereinsfunktion_id,
      vertretene_funktion_bezeichnung_snapshot, vertretene_besetzung_id,
      vertretene_besetzung_gueltig_ab_snapshot,
      vertretene_besetzung_gueltig_bis_snapshot
    ) values (
      p_id, r.berechtigungsart, r.vereinsfunktion_id,
      r.vereinsfunktion_bezeichnung, r.vereinsfunktion_besetzung_id,
      r.besetzung_gueltig_ab, r.besetzung_gueltig_bis, r.vertretung_id,
      r.vertretung_gueltig_ab, r.vertretung_gueltig_bis,
      r.vertretene_vereinsfunktion_id, r.vertretene_funktion_bezeichnung,
      r.vertretene_besetzung_id, r.vertretene_besetzung_gueltig_ab,
      r.vertretene_besetzung_gueltig_bis
    ) on conflict do nothing;
  end loop;

  if (
    select count(*)
    from public.prepared_contract_signatur_snapshot_person p
    where p.kommunikationsvorgang_id = v.id
      and p.signatur_revision = v.revision
  ) < 2 then
    raise exception 'Für zwei Vereinsunterschriften sind mindestens zwei unterschiedliche natürliche Personen erforderlich' using errcode = '23514';
  end if;

  update public.kommunikationsvorgang
  set status = 'zur_signatur',
      signatur_gestartet_am = now(),
      signatur_revision = revision
  where id = v.id
  returning * into v;
  return v;
end;
$$;

revoke all on function public.start_prepared_contract_signature(bigint, integer, uuid)
  from public, anon;
grant execute on function public.start_prepared_contract_signature(bigint, integer, uuid)
  to authenticated, service_role;

comment on function public.start_prepared_contract_signature(bigint, integer, uuid) is
  'CX-06: document-specific effective permission: Wartungsvertrag CanEditAllMembers (4), other prepared contracts CanCreateMitglied (524288); assigned caller and immutable retry guards retained.';

-- The existing server-side completion moved the communication to erledigt but
-- left the durable maintenance document workflow at signatur_vorgang without
-- its final document.  Extend that same transaction with the missing binding.
do $$
declare
  v_definition text;
  v_old text := 'return query select v_assignment.id, false;';
  v_new text := $new$
  update public.wartungsvertrag_dokument_workflow as workflow
  set kommunikationsvorgang_id = v_communication.id,
      dokument_id = p_dokument_id,
      status = 'abgeschlossen',
      fehlermeldung = null
  where workflow.wartungsvertrag_zuordnung_id = v_assignment.id
    and workflow.kommunikationsvorgang_id = v_communication.id;
  if not found then
    raise exception 'Der Wartungsvertrags-Dokumentworkflow fehlt oder passt nicht zum Signaturvorgang.'
      using errcode = '23514';
  end if;

  return query select v_assignment.id, false;$new$;
begin
  select pg_get_functiondef(
    'public.complete_prepared_wartungsvertrag(bigint,bigint)'::regprocedure
  ) into v_definition;
  if position(v_old in v_definition) = 0 then
    raise exception 'CX-06: expected Wartungsvertrag completion tail was not found'
      using errcode = '2BP01';
  end if;
  execute replace(v_definition, v_old, v_new);
end;
$$;

comment on function public.complete_prepared_wartungsvertrag(bigint, bigint) is
  'CX-06: service-only atomic Wartungsvertrag completion binds the final document and marks the durable document workflow abgeschlossen; exact retries remain idempotent.';
