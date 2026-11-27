\set ON_ERROR_STOP on

begin;

insert into public.mitglied(name,vorname,geburtsdatum,adresse,plz,ort,email,aktiv,aufnahme_status,auth_user_id)
values ('CX06 Hauptmitglied','Hanna','1980-02-03','Testweg 6','08056','Zwickau','cx06.main@example.test',true,'aufgenommen',:'main_user_id')
returning id as main_member_id \gset

insert into public.mitglied(hauptmitglied_id,name,vorname,geburtsdatum,adresse,plz,ort,email,aktiv,aufnahme_status,auth_user_id)
values (:main_member_id,'CX06 Nebenmitglied','Nora','1982-04-05','Testweg 6','08056','Zwickau','cx06.secondary@example.test',true,'aufgenommen',:'secondary_user_id')
returning id as secondary_member_id \gset
select mitgliedsnummer as main_member_number from public.mitglied where id=:main_member_id \gset
select mitgliedsnummer as secondary_member_number from public.mitglied where id=:secondary_member_id \gset

insert into public.mitglied(name,vorname,aktiv,aufnahme_status,auth_user_id)
values ('CX06 Wartungsverwaltung','Mara',true,'aufgenommen',:'manager_user_id')
returning id as manager_member_id \gset

insert into public.mitglied(name,vorname,aktiv,aufnahme_status,auth_user_id)
values ('CX06 Außenstehend','Olivia',true,'aufgenommen',:'outsider_user_id')
returning id as outsider_member_id \gset

insert into public.mitglied(name,vorname,aktiv,aufnahme_status,auth_user_id)
values ('CX06 Entzogen','Rita',true,'aufgenommen',:'revoked_user_id')
returning id as revoked_member_id \gset

insert into public.mitglied(name,vorname,aktiv,aufnahme_status,auth_user_id)
values ('CX06 Vorstand Eins','Vera',true,'aufgenommen',:'board1_user_id')
returning id as board1_member_id \gset

insert into public.mitglied(name,vorname,aktiv,aufnahme_status,auth_user_id)
values ('CX06 Vorstand Zwei','Viktor',true,'aufgenommen',:'board2_user_id')
returning id as board2_member_id \gset

-- The manager keeps CanEditAllMembers through the admin template but has the
-- unrelated CanCreateMitglied capability individually revoked.  The revoked
-- user demonstrates that an individual removal of bit 4 remains effective.
-- FR2: only synthetic identities created in this fixture; no administrative role.
create temporary table fr2_fixture_actor(label text primary key,uid uuid not null unique,mid bigint not null,grants text[] not null,denials text[] not null) on commit drop;
insert into fr2_fixture_actor values
('main', :'main_user_id'::uuid, :main_member_id, ARRAY[]::text[], ARRAY[]::text[]),
('secondary', :'secondary_user_id'::uuid, :secondary_member_id, ARRAY[]::text[], ARRAY[]::text[]),
('manager', :'manager_user_id'::uuid, :manager_member_id, ARRAY['mitgliedschaft.verwalten','vertrag.vorbereiten','mitglied.lesen','vorgang.bearbeiten']::text[], ARRAY['mitglied.anlegen']::text[]),
('outsider', :'outsider_user_id'::uuid, :outsider_member_id, ARRAY[]::text[], ARRAY[]::text[]),
('revoked', :'revoked_user_id'::uuid, :revoked_member_id, ARRAY['mitgliedschaft.verwalten']::text[], ARRAY['mitgliedschaft.verwalten']::text[]),
('board1', :'board1_user_id'::uuid, :board1_member_id, ARRAY[]::text[], ARRAY[]::text[]),
('board2', :'board2_user_id'::uuid, :board2_member_id, ARRAY[]::text[], ARRAY[]::text[]);
do $fr2$ begin
 if exists(select 1 from fr2_fixture_actor x where not exists(select 1 from public.app_user a join public.mitglied m on m.id=a.mitglied_id and m.auth_user_id=a.user_id where a.user_id=x.uid and a.mitglied_id=x.mid and not a.is_protected)) then raise exception 'FR2_FIXTURE_LINK_INVALID'; end if;
 if exists(select 1 from fr2_fixture_actor x where not exists(select 1 from auth.sessions s where s.user_id=x.uid and (s.not_after is null or s.not_after>now()))) then raise exception 'FR2_FIXTURE_SESSION_MISSING'; end if;
 if exists(select 1 from fr2_fixture_actor x join public.app_user_zusatzrolle z on z.user_id=x.uid) then raise exception 'FR2_FIXTURE_UNEXPECTED_ADDITIONAL_ROLE'; end if;
end $fr2$;
insert into public.app_user_hauptrolle(user_id,hauptrolle_code) select uid,'mitglied' from fr2_fixture_actor on conflict(user_id) do update set hauptrolle_code=excluded.hauptrolle_code;
insert into public.app_user_fachrecht_override(user_id,fachrecht_code,entscheidung,grund)
 select uid,r,'grant','FR2 synthetic CX06 fixture' from fr2_fixture_actor cross join lateral unnest(grants) r;
do $fr2$ begin
 if exists(select 1 from fr2_fixture_actor x cross join lateral unnest(x.grants) r where not exists(select 1 from public.ap02_calculate_user_rights(x.uid) p where p.code=r and p.erlaubt)) then raise exception 'FR2_FIXTURE_GRANT_NOT_EFFECTIVE'; end if;
end $fr2$;
insert into public.app_user_fachrecht_override(user_id,fachrecht_code,entscheidung,grund)
 select uid,r,'deny','FR2 synthetic CX06 denial' from fr2_fixture_actor cross join lateral unnest(denials) r where true
 on conflict(user_id,fachrecht_code) do update set entscheidung=excluded.entscheidung,grund=excluded.grund;
do $fr2$ begin
 if exists(select 1 from fr2_fixture_actor x cross join lateral public.ap02_calculate_user_rights(x.uid) p where p.erlaubt is distinct from (p.code=any(x.grants) and not p.code=any(x.denials))) then raise exception 'FR2_FIXTURE_RIGHT_SET_MISMATCH'; end if;
end $fr2$;

insert into public.wartungsvertraege(titel,beschreibung,bereich,max_aktive_zuordnungen,befreit_von_pflichtstunden,arbeitsstunden_gutschrift,aktiv,bemerkung)
values ('CX06 Anlagenpflege','Pflege der Gemeinschaftsflächen und turnusmäßige Kontrolle der Wege.','Gemeinschaftsanlage',2,false,7.5,true,'CX06 eingefrorene Bemerkung A')
returning id as contract_a_id \gset

insert into public.wartungsvertraege(titel,beschreibung,bereich,max_aktive_zuordnungen,befreit_von_pflichtstunden,arbeitsstunden_gutschrift,aktiv,bemerkung)
values ('CX06 Gerätewartung','Wartung, Reinigung und dokumentierte Prüfung der Vereinsgeräte.','Gerätehaus',2,true,0,true,'CX06 eingefrorene Bemerkung B')
returning id as contract_b_id \gset

insert into public.wartungsvertraege(titel,beschreibung,bereich,max_aktive_zuordnungen,befreit_von_pflichtstunden,arbeitsstunden_gutschrift,aktiv,bemerkung)
values ('CX06 Abbruchnachweis','Nur für die Nummerninvariante beim vorzeitigen Verwerfen.','Nummerntest',1,false,0,true,'CX06 Nummer bleibt reserviert')
returning id as contract_abort_id \gset

insert into public.wartungsvertrag_zuordnungen(wartungsvertrag_id,hauptmitglied_id,vertragspartei_mitglied_id,gueltig_ab,gueltig_bis,bemerkung)
values (:contract_a_id,:main_member_id,:main_member_id,'2026-01-01','2026-12-31','CX06 Zuordnung A')
returning id as assignment_a_id \gset

insert into public.wartungsvertrag_zuordnungen(wartungsvertrag_id,hauptmitglied_id,vertragspartei_mitglied_id,gueltig_ab,gueltig_bis,bemerkung)
values (:contract_b_id,:main_member_id,:secondary_member_id,'2026-01-01','2026-12-31','CX06 Zuordnung B')
returning id as assignment_b_id \gset

insert into public.wartungsvertrag_zuordnungen(wartungsvertrag_id,hauptmitglied_id,vertragspartei_mitglied_id,gueltig_ab,gueltig_bis,bemerkung)
values (:contract_abort_id,:main_member_id,:main_member_id,'2026-01-01','2026-12-31','CX06 Abbruch-Zuordnung')
returning id as assignment_abort_id \gset

insert into public.vereinsfunktion(schluessel,bezeichnung,aktiv,erlaubt_mehrfachbesetzung)
values
  ('cx06_vorstand_1','CX06 Vorstand 1',true,false),
  ('cx06_vorstand_2','CX06 Vorstand 2',true,false);
select id as board1_function_id from public.vereinsfunktion where schluessel='cx06_vorstand_1' \gset
select id as board2_function_id from public.vereinsfunktion where schluessel='cx06_vorstand_2' \gset

insert into public.vereinsfunktion_verwendung(vereinsfunktion_id,verwendungszweck,sort_order)
values (:board1_function_id,'vertragsunterzeichnung',10),(:board2_function_id,'vertragsunterzeichnung',20);
insert into public.vereinsfunktion_besetzung(vereinsfunktion_id,mitglied_id,gueltig_ab,sort_order)
values (:board1_function_id,:board1_member_id,'2020-01-01',10)
returning id as board1_assignment_id \gset
insert into public.vereinsfunktion_besetzung(vereinsfunktion_id,mitglied_id,gueltig_ab,sort_order)
values (:board2_function_id,:board2_member_id,'2020-01-01',10)
returning id as board2_assignment_id \gset

commit;

select json_build_object(
  'main_member_id',:main_member_id,
  'main_member_number',:main_member_number,
  'secondary_member_id',:secondary_member_id,
  'secondary_member_number',:secondary_member_number,
  'manager_member_id',:manager_member_id,
  'outsider_member_id',:outsider_member_id,
  'revoked_member_id',:revoked_member_id,
  'board1_member_id',:board1_member_id,
  'board2_member_id',:board2_member_id,
  'board1_assignment_id',:board1_assignment_id,
  'board2_assignment_id',:board2_assignment_id,
  'contract_a_id',:contract_a_id,
  'contract_b_id',:contract_b_id,
  'contract_abort_id',:contract_abort_id,
  'assignment_a_id',:assignment_a_id,
  'assignment_b_id',:assignment_b_id,
  'assignment_abort_id',:assignment_abort_id
)::text;
