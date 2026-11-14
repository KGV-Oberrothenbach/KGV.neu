\set ON_ERROR_STOP on

begin;

create temporary table cx05_ids(key text primary key, value bigint not null);

insert into public.mitglied(name,vorname,geburtsdatum,adresse,plz,ort,email,aktiv,aufnahme_status,mitglied_seit,auth_user_id)
values
 ('CX05 Bearbeitung','Mara','1980-01-01','Testweg 1','08056','Zwickau','cx05.manager@example.test',true,'aufgenommen','2000-01-01',:'manager_user_id'),
 ('CX05 Außenstehend','Olivia','1981-02-02','Testweg 2','08056','Zwickau','cx05.outsider@example.test',true,'aufgenommen','2000-01-01',:'outsider_user_id'),
 ('CX05 Entzogen','Rene','1982-03-03','Testweg 3','08056','Zwickau','cx05.revoked@example.test',true,'aufgenommen','2000-01-01',:'revoked_user_id'),
 ('CX05 Hauptmitglied','Anton','1975-04-04','Pachtweg 1','08056','Zwickau','cx05.a@example.test',true,'aufgenommen','2005-01-01',:'a_user_id'),
 ('CX05 Mitpächter','Berta','1976-05-05','Pachtweg 2','08056','Zwickau','cx05.b@example.test',true,'aufgenommen','1999-01-01',:'b_user_id'),
 ('CX05 Fremdmitglied','Fritz','1977-06-06','Fremdweg 1','08056','Zwickau','cx05.foreign@example.test',true,'aufgenommen','2001-01-01',:'foreign_user_id')
returning id;

insert into cx05_ids
select case name
 when 'CX05 Bearbeitung' then 'manager' when 'CX05 Außenstehend' then 'outsider'
 when 'CX05 Entzogen' then 'revoked' when 'CX05 Hauptmitglied' then 'a'
 when 'CX05 Mitpächter' then 'b' when 'CX05 Fremdmitglied' then 'foreign' end,id
from public.mitglied where name like 'CX05 %';

insert into public.app_user(user_id,mitglied_id,role,permission_grants,permission_revocations)
select x.user_id,i.value,x.role,x.grants,x.revocations from (values
 (:'manager_user_id'::uuid,'manager','admin',16777216::bigint,0::bigint),
 (:'outsider_user_id'::uuid,'outsider','user',0::bigint,0::bigint),
 (:'revoked_user_id'::uuid,'revoked','admin',16777216::bigint,16777216::bigint),
 (:'a_user_id'::uuid,'a','user',0::bigint,0::bigint),
 (:'b_user_id'::uuid,'b','user',0::bigint,0::bigint),
 (:'foreign_user_id'::uuid,'foreign','user',0::bigint,0::bigint)
) x(user_id,key,role,grants,revocations) join cx05_ids i using(key)
on conflict(user_id) do update set mitglied_id=excluded.mitglied_id,role=excluded.role,
 permission_grants=excluded.permission_grants,permission_revocations=excluded.permission_revocations;

insert into public.parzelle(id,garten_nr,"Anlage",aktiv)
values (95001,'CX05-1','CX-05 lokal',true),(95002,'CX05-2','CX-05 lokal',true),
       (95003,'CX05-3','CX-05 lokal',true),(95004,'CX05-4','CX-05 lokal',true),
       (95005,'CX05-5','CX-05 lokal',true),(95006,'CX05-6','CX-05 lokal',true),
       (95007,'CX05-F','CX-05 lokal',true);

insert into public.pachtverhaeltnis(id,parzelle_id,urspruenglicher_pachtbeginn)
values (96001,95001,'1998-01-01'),(96002,95002,'1998-01-01'),
       (96003,95003,'2008-01-01'),(96004,95004,'1998-01-01'),
       (96005,95005,'1998-01-01'),(96006,95006,'2010-01-01'),
       (96007,95007,'2000-01-01');

insert into public.pachtverhaeltnis_beteiligung(pachtverhaeltnis_id,mitglied_id,rolle,gueltig_ab,gueltig_bis)
select 96001,value,'hauptpaechter','1998-01-01'::date,null::date from cx05_ids where key='a'
union all select 96002,value,'hauptpaechter','1998-01-01'::date,null::date from cx05_ids where key='a'
union all select 96003,value,'hauptpaechter','2008-01-01'::date,null::date from cx05_ids where key='a'
union all select 96004,value,'hauptpaechter','1998-01-01'::date,null::date from cx05_ids where key='a'
union all select 96005,value,'hauptpaechter','1998-01-01'::date,null::date from cx05_ids where key='a'
union all select 96006,value,'hauptpaechter','2010-01-01'::date,null::date from cx05_ids where key='a'
union all select 96007,value,'hauptpaechter','2000-01-01'::date,null::date from cx05_ids where key='foreign';

insert into public.dokument(mitglied_id,bucket,storage_path,titel,dateiname,mime_type,drive_file_id)
select value,'dokumente','cx05/local-only/timeline-1998.pdf','CX-05 lokale Bestandsfixture 1998','timeline-1998.pdf','application/pdf','cx05-local-timeline-1998'
from cx05_ids where key='a' returning id as timeline_1998_document_id \gset
insert into public.dokument(mitglied_id,bucket,storage_path,titel,dateiname,mime_type,drive_file_id)
select value,'dokumente','cx05/local-only/timeline-2010.pdf','CX-05 lokale Bestandsfixture 2010','timeline-2010.pdf','application/pdf','cx05-local-timeline-2010'
from cx05_ids where key='a' returning id as timeline_2010_document_id \gset

insert into public.pachtvertragsfassung(pachtverhaeltnis_id,dokument_id,fassungstyp,vertragsdatum,wirksam_ab,gueltig_bis)
values (96004,:timeline_1998_document_id,'import','1998-01-01','1998-01-01','2009-12-31')
returning id as timeline_1998_version_id \gset
insert into public.pachtvertragsfassung(pachtverhaeltnis_id,dokument_id,fassungstyp,vertragsdatum,wirksam_ab,vorgaenger_fassung_id)
values (96004,:timeline_2010_document_id,'import','2010-01-01','2010-01-01',:timeline_1998_version_id)
returning id as timeline_2010_version_id \gset

commit;

select json_build_object(
 'manager',(select value from cx05_ids where key='manager'),
 'a',(select value from cx05_ids where key='a'),
 'aMemberNumber',(select mitgliedsnummer from public.mitglied where id=(select value from cx05_ids where key='a')),
 'b',(select value from cx05_ids where key='b'),
 'foreign',(select value from cx05_ids where key='foreign'),
 'timeline1998Document',:timeline_1998_document_id,
 'timeline2010Document',:timeline_2010_document_id
)::text;
