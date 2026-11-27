\set ON_ERROR_STOP on

begin;

create temporary table cx04_ids(key text primary key, value bigint not null);

insert into public.mitglied(name,vorname,geburtsdatum,adresse,plz,ort,email,aktiv,aufnahme_status,auth_user_id)
values
 ('CX04 Bearbeitung','Mara','1980-01-01','Testweg 1','08056','Zwickau','cx04.manager@example.test',true,'aufgenommen',:'manager_user_id'),
 ('CX04 Vorstand Eins','Vera','1981-02-02','Testweg 2','08056','Zwickau','cx04.board1@example.test',true,'aufgenommen',:'board1_user_id'),
 ('CX04 Vorstand Zwei','Viktor','1982-03-03','Testweg 3','08056','Zwickau','cx04.board2@example.test',true,'aufgenommen',:'board2_user_id'),
 ('CX04 Entscheidung','Dora','1983-04-04','Testweg 4','08056','Zwickau','cx04.decider@example.test',true,'aufgenommen',:'decider_user_id'),
 ('CX04 Außenstehend','Olivia','1984-05-05','Testweg 5','08056','Zwickau','cx04.outsider@example.test',true,'aufgenommen',:'outsider_user_id'),
 ('CX04 Antrag A','Anton','1990-06-06','Antragweg 1','08056','Zwickau','cx04.a@example.test',false,'antrag_offen',:'a_user_id'),
 ('CX04 Antrag B','Berta','2012-07-07','Antragweg 2','08056','Zwickau','cx04.b@example.test',false,'antrag_offen',:'b_user_id'),
 ('CX04 Antrag C','Carla','2013-08-08','Antragweg 3','08056','Zwickau','cx04.c@example.test',false,'antrag_offen',:'c_user_id'),
 ('CX04 Antrag D','David','1992-09-09','Antragweg 4','08056','Zwickau','cx04.d@example.test',false,'antrag_offen',:'d_user_id'),
 ('CX04 Antrag E','Eva','1993-10-10','Antragweg 5','08056','Zwickau','cx04.e@example.test',false,'antrag_offen',:'e_user_id'),
 ('CX04 Negativfall','Norbert','1991-01-13','Antragweg 6','08056','Zwickau','cx04.neg@example.test',false,'antrag_offen',:'neg_user_id'),
 ('CX04 Vertreter','Ralf','1975-11-11','Vertreterweg 1','08056','Zwickau','cx04.rep@example.test',true,'aufgenommen',:'rep_user_id')
returning id;

insert into cx04_ids
select case name
 when 'CX04 Bearbeitung' then 'manager' when 'CX04 Vorstand Eins' then 'board1'
 when 'CX04 Vorstand Zwei' then 'board2' when 'CX04 Entscheidung' then 'decider'
 when 'CX04 Außenstehend' then 'outsider' when 'CX04 Antrag A' then 'a'
 when 'CX04 Antrag B' then 'b' when 'CX04 Antrag C' then 'c'
 when 'CX04 Antrag D' then 'd' when 'CX04 Antrag E' then 'e'
 when 'CX04 Negativfall' then 'neg'
 when 'CX04 Vertreter' then 'rep' end,id
from public.mitglied where name like 'CX04 %';

insert into public.mitglied(hauptmitglied_id,name,vorname,geburtsdatum,adresse,plz,ort,email,aktiv,aufnahme_status,auth_user_id)
select value,'CX04 Nebenmitglied','Nina','1994-12-12','Antragweg 4','08056','Zwickau','cx04.secondary@example.test',true,'aufgenommen',:'secondary_user_id'
from cx04_ids where key='d' returning id as secondary_id \gset
insert into cx04_ids values('secondary',:secondary_id);

-- FR2: only synthetic identities created in this fixture; no administrative role.
create temporary table fr2_fixture_actor(label text primary key,uid uuid not null unique,mid bigint not null,grants text[] not null,denials text[] not null) on commit drop;
insert into fr2_fixture_actor values
('manager', :'manager_user_id'::uuid, (select value from cx04_ids where key='manager'), ARRAY['vertrag.vorbereiten','mitglied.lesen','vorgang.bearbeiten','vorstandsvorgang.bearbeiten','mitgliedschaft.verwalten','mitglied.anlegen']::text[], ARRAY[]::text[]),
('board1', :'board1_user_id'::uuid, (select value from cx04_ids where key='board1'), ARRAY[]::text[], ARRAY[]::text[]),
('board2', :'board2_user_id'::uuid, (select value from cx04_ids where key='board2'), ARRAY[]::text[], ARRAY[]::text[]),
('decider', :'decider_user_id'::uuid, (select value from cx04_ids where key='decider'), ARRAY['mitgliedschaft.entscheiden']::text[], ARRAY[]::text[]),
('outsider', :'outsider_user_id'::uuid, (select value from cx04_ids where key='outsider'), ARRAY[]::text[], ARRAY[]::text[]),
('a', :'a_user_id'::uuid, (select value from cx04_ids where key='a'), ARRAY[]::text[], ARRAY[]::text[]),
('b', :'b_user_id'::uuid, (select value from cx04_ids where key='b'), ARRAY[]::text[], ARRAY[]::text[]),
('c', :'c_user_id'::uuid, (select value from cx04_ids where key='c'), ARRAY[]::text[], ARRAY[]::text[]),
('d', :'d_user_id'::uuid, (select value from cx04_ids where key='d'), ARRAY[]::text[], ARRAY[]::text[]),
('e', :'e_user_id'::uuid, (select value from cx04_ids where key='e'), ARRAY[]::text[], ARRAY[]::text[]),
('rep', :'rep_user_id'::uuid, (select value from cx04_ids where key='rep'), ARRAY[]::text[], ARRAY[]::text[]),
('neg', :'neg_user_id'::uuid, (select value from cx04_ids where key='neg'), ARRAY[]::text[], ARRAY[]::text[]),
('secondary', :'secondary_user_id'::uuid, (select value from cx04_ids where key='secondary'), ARRAY[]::text[], ARRAY[]::text[]);
do $fr2$ begin
 if exists(select 1 from fr2_fixture_actor x where not exists(select 1 from public.app_user a join public.mitglied m on m.id=a.mitglied_id and m.auth_user_id=a.user_id where a.user_id=x.uid and a.mitglied_id=x.mid and not a.is_protected)) then raise exception 'FR2_FIXTURE_LINK_INVALID'; end if;
 if exists(select 1 from fr2_fixture_actor x where not exists(select 1 from auth.sessions s where s.user_id=x.uid and (s.not_after is null or s.not_after>now()))) then raise exception 'FR2_FIXTURE_SESSION_MISSING'; end if;
 if exists(select 1 from fr2_fixture_actor x join public.app_user_zusatzrolle z on z.user_id=x.uid) then raise exception 'FR2_FIXTURE_UNEXPECTED_ADDITIONAL_ROLE'; end if;
end $fr2$;
insert into public.app_user_hauptrolle(user_id,hauptrolle_code) select uid,'mitglied' from fr2_fixture_actor on conflict(user_id) do update set hauptrolle_code=excluded.hauptrolle_code;
insert into public.app_user_fachrecht_override(user_id,fachrecht_code,entscheidung,grund)
 select uid,r,'grant','FR2 synthetic CX04 fixture' from fr2_fixture_actor cross join lateral unnest(grants) r;
do $fr2$ begin
 if exists(select 1 from fr2_fixture_actor x cross join lateral unnest(x.grants) r where not exists(select 1 from public.ap02_calculate_user_rights(x.uid) p where p.code=r and p.erlaubt)) then raise exception 'FR2_FIXTURE_GRANT_NOT_EFFECTIVE'; end if;
end $fr2$;
insert into public.app_user_fachrecht_override(user_id,fachrecht_code,entscheidung,grund)
 select uid,r,'deny','FR2 synthetic CX04 denial' from fr2_fixture_actor cross join lateral unnest(denials) r where true
 on conflict(user_id,fachrecht_code) do update set entscheidung=excluded.entscheidung,grund=excluded.grund;
do $fr2$ begin
 if exists(select 1 from fr2_fixture_actor x cross join lateral public.ap02_calculate_user_rights(x.uid) p where p.erlaubt is distinct from (p.code=any(x.grants) and not p.code=any(x.denials))) then raise exception 'FR2_FIXTURE_RIGHT_SET_MISMATCH'; end if;
end $fr2$;

insert into public.vereinsfunktion(schluessel,bezeichnung,aktiv,erlaubt_mehrfachbesetzung)
values ('cx04_vorstand_1','CX-04 Vorstand 1',true,false),('cx04_vorstand_2','CX-04 Vorstand 2',true,false),
       ('cx04_entscheidung','CX-04 Aufnahmeentscheidung',true,false);

insert into public.vereinsfunktion_verwendung(vereinsfunktion_id,verwendungszweck,sort_order)
select id,'vertragsunterzeichnung',10 from public.vereinsfunktion where schluessel='cx04_vorstand_1'
union all select id,'mitgliedsaufnahme_abstimmung',10 from public.vereinsfunktion where schluessel='cx04_vorstand_1'
union all select id,'vertragsunterzeichnung',20 from public.vereinsfunktion where schluessel='cx04_vorstand_2'
union all select id,'mitgliedsaufnahme_abstimmung',20 from public.vereinsfunktion where schluessel='cx04_vorstand_2'
union all select id,'mitgliedsaufnahme_entscheidung',10 from public.vereinsfunktion where schluessel='cx04_entscheidung';

insert into public.vereinsfunktion_besetzung(vereinsfunktion_id,mitglied_id,gueltig_ab,sort_order)
select f.id,i.value,'2020-01-01',10 from public.vereinsfunktion f join cx04_ids i on
 (f.schluessel='cx04_vorstand_1' and i.key='board1') or
 (f.schluessel='cx04_vorstand_2' and i.key='board2') or
 (f.schluessel='cx04_entscheidung' and i.key='decider');

commit;

select json_object_agg(key,value)::text from cx04_ids;
