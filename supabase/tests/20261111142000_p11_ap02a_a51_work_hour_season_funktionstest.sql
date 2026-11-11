-- P11 A5.1: synthetic season integrity test; all rows roll back.
begin;
create temporary table p11_a51_season(id integer,jahr integer) on commit drop;
with inserted as (
 insert into public.saison(jahr,pflichtstunden_soll,pacht_pro_qm,pacht_faellig_am) values
 (1,10,1,'2001-01-01'),(2,10,1,'2002-01-01') returning id,jahr
) insert into p11_a51_season select id,jahr from inserted;
insert into public.mitglied(name,vorname,aktiv,aufnahme_status) values('P11 A51','Test',true,'aufgenommen') returning id as member_id \gset
insert into public.arbeitsstunde(mitglied_id,saison_id,datum,stunden,art_der_arbeit,status,freigegeben) select :'member_id',-1,make_date(min(jahr),12,31),1,'P11 A51','offen',false from p11_a51_season returning id as hour_id \gset
do $$ begin
 if (select a.saison_id from public.arbeitsstunde a where a.art_der_arbeit='P11 A51')<>(select id from p11_a51_season order by jahr limit 1) then raise exception 'insert season not derived from work date'; end if;
 begin insert into public.arbeitsstunde(mitglied_id,saison_id,datum,stunden,art_der_arbeit,status,freigegeben) values((select id from public.mitglied where name='P11 A51'),-1,make_date((select max(jahr)+1 from p11_a51_season),1,1),1,'P11 A51 missing','offen',false); raise exception 'missing season accepted'; exception when sqlstate 'P0002' then null; end;
end $$;
rollback;
