-- P11 AP-02A / P11-R01: real local-database test for the membership-review
-- entry RPC.  It deliberately uses a rolled-back fixture and invokes the
-- SECURITY DEFINER function as the authenticated database role.
begin;

create temporary table p11_start_test_ids (
  vorgang_id bigint not null,
  granted_user uuid not null,
  denied_user uuid not null,
  revoked_admin_user uuid not null
) on commit drop;

insert into public.mitglied(name, vorname, aktiv, aufnahme_status, is_demo)
values ('P11 Antrag', 'Test', false, 'antrag_offen', false)
returning id as applicant_mitglied_id \gset

insert into public.mitglied(name, vorname, aktiv, aufnahme_status, is_demo)
values ('P11 Abstimmung', 'Test', true, 'aufgenommen', false)
returning id as voter_mitglied_id \gset

insert into public.vereinsfunktion(schluessel, bezeichnung)
values ('p11_test_abstimmung', 'P11-Testabstimmung')
returning id as voter_funktion_id \gset

insert into public.vereinsfunktion_verwendung(vereinsfunktion_id, verwendungszweck)
values (:'voter_funktion_id', 'mitgliedsaufnahme_abstimmung');

insert into public.vereinsfunktion_besetzung(vereinsfunktion_id, mitglied_id, gueltig_ab)
values (:'voter_funktion_id', :'voter_mitglied_id', current_date);

-- The final document and its source form a deliberate cycle.  The application
-- itself never accepts this intermediate state; the fixture bypasses only
-- lifecycle triggers while retaining all constraints observed by the RPC.
set local session_replication_role = replica;
insert into public.kommunikationsvorgang(vorgangstyp, dokumenttyp, status, mitglied_id, revision)
values ('vertrag_signatur', 'mitgliedsantrag', 'entwurf', :'applicant_mitglied_id', 1)
returning id as vorgang_id \gset
insert into public.dokument(mitglied_id, storage_path, titel, dateiname, mime_type, source_kommunikationsvorgang_id, source_revision)
values (:'applicant_mitglied_id', 'p11/start-mitgliedsaufnahme.pdf', 'P11 Mitgliedsantrag', 'p11.pdf', 'application/pdf', :'vorgang_id', 1)
returning id as final_dokument_id \gset
update public.kommunikationsvorgang
set status = 'letzte_pruefung', signatur_revision = 1, signatur_gestartet_am = now(), final_dokument_id = :'final_dokument_id'
where id = :'vorgang_id';
set local session_replication_role = origin;

insert into p11_start_test_ids(vorgang_id, granted_user, denied_user, revoked_admin_user)
values (:'vorgang_id',
        '00000000-0000-0000-0000-000000000101'::uuid,
        '00000000-0000-0000-0000-000000000102'::uuid,
        '00000000-0000-0000-0000-000000000103'::uuid);

insert into public.app_user(user_id, role, permission_grants, permission_revocations)
select granted_user, 'user', 524288, 0 from p11_start_test_ids
union all
select denied_user, 'user', 0, 0 from p11_start_test_ids
union all
select revoked_admin_user, 'admin', 0, 524288 from p11_start_test_ids;

grant select on p11_start_test_ids to authenticated;

set local role authenticated;
do $$
declare
  v_vorgang bigint;
  v_granted uuid;
  v_denied uuid;
  v_revoked uuid;
  v_bearbeitung bigint;
begin
  select vorgang_id, granted_user, denied_user, revoked_admin_user
    into v_vorgang, v_granted, v_denied, v_revoked
  from p11_start_test_ids;

  -- Individual grant: a normal user may start exactly this administrative
  -- membership workflow and the original snapshot logic still succeeds.
  perform set_config('request.jwt.claim.sub', v_granted::text, true);
  v_bearbeitung := public.start_mitgliedsaufnahme_vorstandsbearbeitung(v_vorgang);
  if v_bearbeitung is null then
    raise exception 'P11 positive grant test did not create the board review';
  end if;

  -- No right: the authorization gate fires before any workflow data are read.
  perform set_config('request.jwt.claim.sub', v_denied::text, true);
  begin
    perform public.start_mitgliedsaufnahme_vorstandsbearbeitung(v_vorgang);
    raise exception 'P11 denied-user test unexpectedly succeeded';
  exception when sqlstate '42501' then null;
  end;

  -- An individual revocation wins over the administrative role base.
  perform set_config('request.jwt.claim.sub', v_revoked::text, true);
  begin
    perform public.start_mitgliedsaufnahme_vorstandsbearbeitung(v_vorgang);
    raise exception 'P11 revoked-admin test unexpectedly succeeded';
  exception when sqlstate '42501' then null;
  end;

  -- The historic readiness validation remains in force even for a permitted
  -- caller, so direct RPC use cannot skip the status/provenance contract.
  perform set_config('request.jwt.claim.sub', v_granted::text, true);
  begin
    perform public.start_mitgliedsaufnahme_vorstandsbearbeitung(-1);
    raise exception 'P11 invalid-workflow test unexpectedly succeeded';
  exception when sqlstate '23514' then null;
  end;
end;
$$;
reset role;

rollback;
