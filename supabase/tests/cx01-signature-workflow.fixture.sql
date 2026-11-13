\set ON_ERROR_STOP on

begin;

insert into public.mitglied(name, vorname, aktiv, aufnahme_status, auth_user_id)
values ('CX01 Pächter', 'Tessa', true, 'aufgenommen', :'tenant_user_id')
returning id as tenant_member_id \gset

insert into public.mitglied(name, vorname, aktiv, aufnahme_status, auth_user_id)
values ('CX01 Bearbeitung', 'Mara', true, 'aufgenommen', :'manager_user_id')
returning id as manager_member_id \gset

insert into public.mitglied(name, vorname, aktiv, aufnahme_status, auth_user_id)
values ('CX01 Vorstand Eins', 'Vera', true, 'aufgenommen', :'board1_user_id')
returning id as board1_member_id \gset

insert into public.mitglied(name, vorname, aktiv, aufnahme_status, auth_user_id)
values ('CX01 Vorstand Zwei', 'Viktor', true, 'aufgenommen', :'board2_user_id')
returning id as board2_member_id \gset

insert into public.mitglied(name, vorname, aktiv, aufnahme_status, auth_user_id)
values ('CX01 Außenstehend', 'Olivia', true, 'aufgenommen', :'outsider_user_id')
returning id as outsider_member_id \gset

insert into public.app_user(user_id, mitglied_id, role, permission_grants, permission_revocations)
values
  (:'manager_user_id', :manager_member_id, 'admin', 524288, 0),
  (:'tenant_user_id', :tenant_member_id, 'user', 0, 0),
  (:'board1_user_id', :board1_member_id, 'user', 0, 0),
  (:'board2_user_id', :board2_member_id, 'user', 0, 0),
  (:'outsider_user_id', :outsider_member_id, 'user', 0, 0)
on conflict (user_id) do update
set mitglied_id = excluded.mitglied_id,
    role = excluded.role,
    permission_grants = excluded.permission_grants,
    permission_revocations = excluded.permission_revocations;

set local session_replication_role = replica;
insert into public.parzelle(garten_nr, "Anlage", flaeche_qm, hat_strom, hat_wasser)
values ('CX01-LOCAL', 'Synthetische Testanlage', 100, false, false)
returning id as parcel_id \gset

insert into public.kommunikationsvorgang(vorgangstyp, dokumenttyp, status, mitglied_id, parzelle_id, revision, entwurf_daten)
values ('vertrag_signatur', 'pachtvertrag', 'erledigt', :tenant_member_id, :parcel_id, 1, '{"draft":{"startDate":"2026-01-01"}}')
returning id as source_id \gset

insert into public.dokument(parzelle_id, storage_path, titel, dateiname, mime_type, source_kommunikationsvorgang_id, source_revision)
values (:parcel_id, 'cx01/source.pdf', 'CX-01 Ausgangsvertrag', 'cx01-source.pdf', 'application/pdf', :source_id, 1)
returning id as document_id \gset

insert into public.pachtverhaeltnis(parzelle_id, urspruenglicher_pachtbeginn)
values (:parcel_id, '2026-01-01')
returning id as lease_id \gset

insert into public.pachtverhaeltnis_beteiligung(pachtverhaeltnis_id, mitglied_id, rolle, gueltig_ab)
values (:lease_id, :tenant_member_id, 'hauptpaechter', '2026-01-01');

insert into public.pachtvertragsfassung(pachtverhaeltnis_id, dokument_id, fassungstyp)
values (:lease_id, :document_id, 'erstvertrag')
returning id as predecessor_id \gset
set local session_replication_role = origin;

insert into public.vereinsfunktion(schluessel, bezeichnung, aktiv, erlaubt_mehrfachbesetzung)
values
  ('cx01_vorstand_1', 'CX-01 Vorstand 1', true, false),
  ('cx01_vorstand_2', 'CX-01 Vorstand 2', true, false)
returning id;

select id as board1_function_id from public.vereinsfunktion where schluessel = 'cx01_vorstand_1' \gset
select id as board2_function_id from public.vereinsfunktion where schluessel = 'cx01_vorstand_2' \gset

insert into public.vereinsfunktion_verwendung(vereinsfunktion_id, verwendungszweck, sort_order)
values
  (:board1_function_id, 'vertragsunterzeichnung', 10),
  (:board2_function_id, 'vertragsunterzeichnung', 20);

insert into public.vereinsfunktion_besetzung(vereinsfunktion_id, mitglied_id, gueltig_ab, sort_order)
values (:board1_function_id, :board1_member_id, '2020-01-01', 10)
returning id as board1_assignment_id \gset

insert into public.vereinsfunktion_besetzung(vereinsfunktion_id, mitglied_id, gueltig_ab, sort_order)
values (:board2_function_id, :board2_member_id, '2020-01-01', 10)
returning id as board2_assignment_id \gset

commit;

select json_build_object(
  'predecessor_id', :predecessor_id,
  'tenant_member_id', :tenant_member_id,
  'manager_member_id', :manager_member_id,
  'board1_member_id', :board1_member_id,
  'board2_member_id', :board2_member_id,
  'outsider_member_id', :outsider_member_id,
  'board1_assignment_id', :board1_assignment_id,
  'board2_assignment_id', :board2_assignment_id
)::text;
