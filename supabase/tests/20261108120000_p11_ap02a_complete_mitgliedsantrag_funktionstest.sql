-- P11 AP-02A / P11-R01: real local-database test for the service-only
-- membership-application completion RPC.  All fixture changes are rolled back.
begin;

create temporary table p11_complete_test_ids (
  vorgang_id bigint not null,
  granted_user uuid not null,
  denied_user uuid not null,
  revoked_admin_user uuid not null,
  foreign_granted_user uuid not null
) on commit drop;

insert into public.mitglied(name, vorname, aktiv, aufnahme_status, is_demo)
values ('P11 Abschluss', 'Antrag', false, 'antrag_offen', false)
returning id as applicant_mitglied_id \gset

-- A finalized application and its final document form a provenance cycle.
-- Lifecycle triggers are disabled only while constructing the rollback fixture;
-- all conditions evaluated by the RPC are present in the test data.
set local session_replication_role = replica;
insert into public.kommunikationsvorgang(vorgangstyp, dokumenttyp, status, mitglied_id, revision, zugewiesen_an, entwurf_daten)
values ('vertrag_signatur', 'mitgliedsantrag', 'entwurf', :'applicant_mitglied_id', 1,
        '00000000-0000-0000-0000-000000000201'::uuid, '{"startDate":"2026-11-08"}'::jsonb)
returning id as vorgang_id \gset
insert into public.dokument(mitglied_id, storage_path, titel, dateiname, mime_type, source_kommunikationsvorgang_id, source_revision)
values (:'applicant_mitglied_id', 'p11/complete-mitgliedsantrag.pdf', 'P11 Mitgliedsantrag', 'p11.pdf', 'application/pdf', :'vorgang_id', 1)
returning id as final_dokument_id \gset
update public.kommunikationsvorgang
set status='letzte_pruefung', signatur_revision=1, signatur_gestartet_am=now(), final_dokument_id=:'final_dokument_id'
where id=:'vorgang_id';
set local session_replication_role = origin;

insert into p11_complete_test_ids(vorgang_id, granted_user, denied_user, revoked_admin_user, foreign_granted_user)
values (:'vorgang_id',
        '00000000-0000-0000-0000-000000000201'::uuid,
        '00000000-0000-0000-0000-000000000202'::uuid,
        '00000000-0000-0000-0000-000000000203'::uuid,
        '00000000-0000-0000-0000-000000000204'::uuid);

insert into public.app_user(user_id, role, permission_grants, permission_revocations, is_demo_account)
select granted_user, 'user', 524288, 0, false from p11_complete_test_ids
union all select denied_user, 'user', 0, 0, false from p11_complete_test_ids
union all select revoked_admin_user, 'admin', 0, 524288, false from p11_complete_test_ids
union all select foreign_granted_user, 'user', 524288, 0, false from p11_complete_test_ids;

grant select on p11_complete_test_ids to service_role, authenticated;

set local role service_role;
select set_config('request.jwt.claim.role', 'service_role', true);
do $$
declare
  v_vorgang bigint;
  v_granted uuid;
  v_denied uuid;
  v_revoked uuid;
  v_foreign uuid;
  v_done boolean;
begin
  select vorgang_id, granted_user, denied_user, revoked_admin_user, foreign_granted_user
    into v_vorgang, v_granted, v_denied, v_revoked, v_foreign
  from p11_complete_test_ids;

  -- The caller must be the actor assigned to this application.  A different
  -- user with the same narrowly granted right cannot complete foreign data.
  begin
    perform public.complete_prepared_mitgliedsantrag(v_vorgang, 1, v_foreign);
    raise exception 'P11 assignment-scope test unexpectedly succeeded';
  exception when sqlstate '40001' then null;
  end;

  select bereits_abgeschlossen into v_done
  from public.complete_prepared_mitgliedsantrag(v_vorgang, 1, v_granted);
  if v_done then raise exception 'P11 positive completion was unexpectedly treated as a replay'; end if;

  begin
    perform public.complete_prepared_mitgliedsantrag(v_vorgang, 1, v_denied);
    raise exception 'P11 no-right test unexpectedly succeeded';
  exception when sqlstate '42501' then null;
  end;

  begin
    perform public.complete_prepared_mitgliedsantrag(v_vorgang, 1, v_revoked);
    raise exception 'P11 revoked-admin test unexpectedly succeeded';
  exception when sqlstate '42501' then null;
  end;

  begin
    perform public.complete_prepared_mitgliedsantrag(-1, 1, v_granted);
    raise exception 'P11 invalid-workflow test unexpectedly succeeded';
  exception when sqlstate '23514' then null;
  end;
end;
$$;
reset role;

-- The API role has neither EXECUTE nor a technical service JWT and therefore
-- cannot call the sensitive completion RPC directly.
set local role authenticated;
select set_config('request.jwt.claim.role', 'authenticated', true);
do $$
begin
  perform public.complete_prepared_mitgliedsantrag(1, 1, '00000000-0000-0000-0000-000000000201'::uuid);
  raise exception 'P11 direct authenticated RPC call unexpectedly succeeded';
exception when sqlstate '42501' then null;
end;
$$;
reset role;

rollback;
