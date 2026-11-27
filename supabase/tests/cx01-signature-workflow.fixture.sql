\set ON_ERROR_STOP on

begin;

insert into public.mitglied(name, vorname, geburtsdatum, adresse, plz, ort, telefon, email, aktiv, aufnahme_status, auth_user_id)
values ('CX01 Pächter', 'Tessa', '1985-06-12', 'Testweg 1', '08056', 'Zwickau', '0375 123456', 'tessa.paechter@example.test', true, 'aufgenommen', :'tenant_user_id')
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

-- FR2: only synthetic identities created in this fixture; no administrative role.
create temporary table fr2_fixture_actor(label text primary key,uid uuid not null unique,mid bigint not null,grants text[] not null,denials text[] not null) on commit drop;
insert into fr2_fixture_actor values
('manager', :'manager_user_id'::uuid, :manager_member_id, ARRAY['pachtvertrag.verwalten','vertrag.vorbereiten','mitglied.lesen','parzelle.lesen','parzelle.bearbeiten','vorgang.bearbeiten']::text[], ARRAY[]::text[]),
('tenant', :'tenant_user_id'::uuid, :tenant_member_id, ARRAY[]::text[], ARRAY[]::text[]),
('board1', :'board1_user_id'::uuid, :board1_member_id, ARRAY[]::text[], ARRAY[]::text[]),
('board2', :'board2_user_id'::uuid, :board2_member_id, ARRAY[]::text[], ARRAY[]::text[]),
('outsider', :'outsider_user_id'::uuid, :outsider_member_id, ARRAY[]::text[], ARRAY[]::text[]);
do $fr2$ begin
 if exists(select 1 from fr2_fixture_actor x where not exists(select 1 from public.app_user a join public.mitglied m on m.id=a.mitglied_id and m.auth_user_id=a.user_id where a.user_id=x.uid and a.mitglied_id=x.mid and not a.is_protected)) then raise exception 'FR2_FIXTURE_LINK_INVALID'; end if;
 if exists(select 1 from fr2_fixture_actor x where not exists(select 1 from auth.sessions s where s.user_id=x.uid and (s.not_after is null or s.not_after>now()))) then raise exception 'FR2_FIXTURE_SESSION_MISSING'; end if;
 if exists(select 1 from fr2_fixture_actor x join public.app_user_zusatzrolle z on z.user_id=x.uid) then raise exception 'FR2_FIXTURE_UNEXPECTED_ADDITIONAL_ROLE'; end if;
end $fr2$;
insert into public.app_user_hauptrolle(user_id,hauptrolle_code) select uid,'mitglied' from fr2_fixture_actor on conflict(user_id) do update set hauptrolle_code=excluded.hauptrolle_code;
insert into public.app_user_fachrecht_override(user_id,fachrecht_code,entscheidung,grund)
 select uid,r,'grant','FR2 synthetic CX01 fixture' from fr2_fixture_actor cross join lateral unnest(grants) r;
do $fr2$ begin
 if exists(select 1 from fr2_fixture_actor x cross join lateral unnest(x.grants) r where not exists(select 1 from public.ap02_calculate_user_rights(x.uid) p where p.code=r and p.erlaubt)) then raise exception 'FR2_FIXTURE_GRANT_NOT_EFFECTIVE'; end if;
end $fr2$;
insert into public.app_user_fachrecht_override(user_id,fachrecht_code,entscheidung,grund)
 select uid,r,'deny','FR2 synthetic CX01 denial' from fr2_fixture_actor cross join lateral unnest(denials) r where true
 on conflict(user_id,fachrecht_code) do update set entscheidung=excluded.entscheidung,grund=excluded.grund;
do $fr2$ begin
 if exists(select 1 from fr2_fixture_actor x cross join lateral public.ap02_calculate_user_rights(x.uid) p where p.erlaubt is distinct from (p.code=any(x.grants) and not p.code=any(x.denials))) then raise exception 'FR2_FIXTURE_RIGHT_SET_MISMATCH'; end if;
end $fr2$;

set local session_replication_role = replica;
insert into public.parzelle(garten_nr, "Anlage", flaeche_qm, hat_strom, hat_wasser)
values ('CX01-LOCAL', 'Synthetische Testanlage', 100, false, false)
returning id as parcel_id \gset

insert into public.kommunikationsvorgang(vorgangstyp, dokumenttyp, status, mitglied_id, parzelle_id, revision, entwurf_daten)
values ('vertrag_signatur', 'pachtvertrag', 'erledigt', :tenant_member_id, :parcel_id, 1,
  '{"draft":{"startDate":"2026-01-01"},"vereinsunterlagenNachweis":[{"dokumenttyp":"kleingartenordnung","bereitstellungsart":"papier","bereitgestellt_am":"2026-11-14T10:00:00Z","bemerkung":"CX-03 lokale Papierübergabe-Fixture"},{"dokumenttyp":"beitragsordnung","bereitstellungsart":"papier","bereitgestellt_am":"2026-11-14T10:00:00Z","bemerkung":"CX-03 lokale Papierübergabe-Fixture"},{"dokumenttyp":"arbeitsordnung","bereitstellungsart":"papier","bereitgestellt_am":"2026-11-14T10:00:00Z","bemerkung":"CX-03 lokale Papierübergabe-Fixture"}]}'::jsonb)
returning id as source_id \gset

insert into public.dokument(parzelle_id, storage_path, titel, dateiname, mime_type, source_kommunikationsvorgang_id, source_revision)
values (:parcel_id, 'cx01/source.pdf', 'CX-01 Ausgangsvertrag', 'cx01-source.pdf', 'application/pdf', :source_id, 1)
returning id as document_id \gset

insert into public.pachtverhaeltnis(parzelle_id, urspruenglicher_pachtbeginn)
values (:parcel_id, '2026-01-01')
returning id as lease_id \gset

insert into public.pachtverhaeltnis_beteiligung(pachtverhaeltnis_id, mitglied_id, rolle, gueltig_ab)
values (:lease_id, :tenant_member_id, 'hauptpaechter', '2026-01-01');

insert into public.pachtvertragsfassung(pachtverhaeltnis_id, dokument_id, fassungstyp, vertragsdatum, wirksam_ab)
values (:lease_id, :document_id, 'erstvertrag', '2026-01-01', '2026-01-01')
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
  'board2_assignment_id', :board2_assignment_id,
  'parcel_id', :parcel_id
)::text;
