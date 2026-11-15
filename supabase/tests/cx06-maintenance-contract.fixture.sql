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
insert into public.app_user(user_id,mitglied_id,role,permission_grants,permission_revocations)
values
  (:'main_user_id',:main_member_id,'user',0,0),
  (:'secondary_user_id',:secondary_member_id,'user',0,0),
  (:'manager_user_id',:manager_member_id,'admin',4,524288),
  (:'outsider_user_id',:outsider_member_id,'user',0,0),
  (:'revoked_user_id',:revoked_member_id,'admin',0,4),
  (:'board1_user_id',:board1_member_id,'user',0,0),
  (:'board2_user_id',:board2_member_id,'user',0,0)
on conflict(user_id) do update set
  mitglied_id=excluded.mitglied_id,
  role=excluded.role,
  permission_grants=excluded.permission_grants,
  permission_revocations=excluded.permission_revocations;

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
