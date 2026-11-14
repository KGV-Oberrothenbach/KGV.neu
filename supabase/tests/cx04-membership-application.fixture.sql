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

insert into public.app_user(user_id,mitglied_id,role,permission_grants,permission_revocations)
select x.user_id,i.value,x.role,x.grants,0 from (values
 (:'manager_user_id'::uuid,'manager','admin',524288::bigint),
 (:'board1_user_id'::uuid,'board1','user',0::bigint),(:'board2_user_id'::uuid,'board2','user',0::bigint),
 (:'decider_user_id'::uuid,'decider','user',524288::bigint),(:'outsider_user_id'::uuid,'outsider','user',0::bigint),
 (:'a_user_id'::uuid,'a','user',0::bigint),(:'b_user_id'::uuid,'b','user',0::bigint),
 (:'c_user_id'::uuid,'c','user',0::bigint),(:'d_user_id'::uuid,'d','user',0::bigint),
 (:'e_user_id'::uuid,'e','user',0::bigint),(:'rep_user_id'::uuid,'rep','user',0::bigint),
 (:'neg_user_id'::uuid,'neg','user',0::bigint),
 (:'secondary_user_id'::uuid,'secondary','user',0::bigint)
) x(user_id,key,role,grants) join cx04_ids i using(key)
on conflict(user_id) do update set mitglied_id=excluded.mitglied_id,role=excluded.role,
 permission_grants=excluded.permission_grants,permission_revocations=0;

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
