-- P11 AP-04.3: authenticated tenant change and successor occupancy; synthetic and rollback-only.
begin;
create temporary table p11_ap04c_ids(actor uuid, denied uuid, old_lease bigint, aftercare bigint) on commit drop;
insert into public.mitglied(name,vorname,aktiv,aufnahme_status) values
 ('P11 AP04C Alt','Test',true,'aufgenommen'),('P11 AP04C Neu','Test',true,'aufgenommen'),('P11 AP04C Denied','Test',true,'aufgenommen');
insert into public.app_user(user_id,mitglied_id,role,permission_grants,permission_revocations) values
 ('00000000-0000-0000-0000-000000000461',(select id from public.mitglied where name='P11 AP04C Alt'),'user',524288,0),
 ('00000000-0000-0000-0000-000000000462',(select id from public.mitglied where name='P11 AP04C Denied'),'admin',0,524288);
insert into public.parzelle(id,garten_nr,"Anlage") values(9461,'P11-461','Test');
insert into public.pachtverhaeltnis(parzelle_id,urspruenglicher_pachtbeginn) values(9461,current_date-30);
insert into p11_ap04c_ids select '00000000-0000-0000-0000-000000000461','00000000-0000-0000-0000-000000000462',(select id from public.pachtverhaeltnis where parzelle_id=9461),null;
insert into public.pachtverhaeltnis_beteiligung(pachtverhaeltnis_id,mitglied_id,rolle,gueltig_ab)
select old_lease,(select id from public.mitglied where name='P11 AP04C Alt'),'hauptpaechter',current_date-30 from p11_ap04c_ids;
grant select,update on p11_ap04c_ids to authenticated;
set local role authenticated;
do $$ declare x p11_ap04c_ids%rowtype; begin select * into x from p11_ap04c_ids; perform set_config('request.jwt.claim.sub',x.denied::text,true); begin perform public.beende_pachtverhaeltnis(x.old_lease,current_date-1,'Nachpächterwechsel'); raise exception 'revoked admin ended lease'; exception when sqlstate '42501' then null; end; perform set_config('request.jwt.claim.sub',x.actor::text,true); perform public.beende_pachtverhaeltnis(x.old_lease,current_date-1,'Nachpächterwechsel'); update p11_ap04c_ids set aftercare=public.erstelle_pacht_nachsorge(x.old_lease,current_date+365); end $$;
reset role;
do $$ declare x p11_ap04c_ids%rowtype; future_lease bigint; begin
 select * into x from p11_ap04c_ids;
 if exists(select 1 from public.parzellen_belegung where pachtverhaeltnis_id=x.old_lease and bis_datum<>current_date-1) then raise exception 'legal end did not close linked occupancy'; end if;
 perform set_config('request.jwt.claim.sub',x.actor::text,true);
 insert into public.pachtverhaeltnis(parzelle_id,urspruenglicher_pachtbeginn) values(9461,current_date+1) returning id into future_lease;
 insert into public.pachtverhaeltnis_beteiligung(pachtverhaeltnis_id,mitglied_id,rolle,gueltig_ab) values(future_lease,(select id from public.mitglied where name='P11 AP04C Neu'),'hauptpaechter',current_date+1);
 if (select beendet_am from public.pacht_nachsorge where id=x.aftercare) is not null then raise exception 'planned tenant closed aftercare'; end if;
 update public.pachtverhaeltnis_beteiligung set gueltig_ab=current_date where pachtverhaeltnis_id=future_lease;
 if (select beendet_am from public.pacht_nachsorge where id=x.aftercare) is null then raise exception 'actual successor did not close aftercare'; end if;
 begin insert into public.parzellen_belegung(parzelle_id,mitglied_id,von_datum) values(9461,(select id from public.mitglied where name='P11 AP04C Denied'),current_date); raise exception 'overlapping occupancy accepted'; exception when exclusion_violation then null; end;
end $$;
rollback;
