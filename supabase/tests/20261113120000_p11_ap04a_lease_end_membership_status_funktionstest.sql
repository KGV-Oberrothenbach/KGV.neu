-- P11 AP-04.1: authenticated, synthetic, transactional R06/R07 acceptance.
begin;
create temporary table p11_ap04a_ids(actor uuid, denied uuid, first_lease bigint, second_lease bigint, principal bigint, co_tenant bigint) on commit drop;
insert into public.mitglied(name,vorname,aktiv,aufnahme_status) values
 ('P11 AP04A Principal','Test',true,'aufgenommen'),('P11 AP04A CoTenant','Test',true,'aufgenommen'),('P11 AP04A Actor','Test',true,'aufgenommen'),('P11 AP04A Denied','Test',true,'aufgenommen');
insert into public.app_user(user_id,mitglied_id,role,permission_grants,permission_revocations) values
 ('00000000-0000-0000-0000-000000000341',(select id from public.mitglied where name='P11 AP04A Actor'),'user',524288,0),
 ('00000000-0000-0000-0000-000000000342',(select id from public.mitglied where name='P11 AP04A Denied'),'user',0,0);
insert into public.parzelle(id,garten_nr,"Anlage") values (9341,'P11-341','Test'),(9342,'P11-342','Test');
insert into public.pachtverhaeltnis(parzelle_id,urspruenglicher_pachtbeginn) values (9341,current_date-10),(9342,current_date-10);
insert into public.pachtverhaeltnis_beteiligung(pachtverhaeltnis_id,mitglied_id,rolle,gueltig_ab)
select r.id,m.id,case when m.name='P11 AP04A Principal' then 'hauptpaechter' else 'mitpaechter' end,current_date-10 from public.pachtverhaeltnis r cross join public.mitglied m where r.parzelle_id=9341 and m.name in ('P11 AP04A Principal','P11 AP04A CoTenant');
insert into p11_ap04a_ids select '00000000-0000-0000-0000-000000000341','00000000-0000-0000-0000-000000000342',(select id from public.pachtverhaeltnis where parzelle_id=9341),(select id from public.pachtverhaeltnis where parzelle_id=9342),(select id from public.mitglied where name='P11 AP04A Principal'),(select id from public.mitglied where name='P11 AP04A CoTenant');
grant select on p11_ap04a_ids to authenticated;
set local role authenticated;
do $$ declare v p11_ap04a_ids%rowtype; r record; begin
 select * into v from p11_ap04a_ids;
 perform set_config('request.jwt.claim.sub',v.denied::text,true);
 begin perform public.beende_pachtverhaeltnis(v.first_lease,current_date,'unberechtigt'); raise exception 'AP04.1 denied end unexpectedly succeeded'; exception when sqlstate '42501' then null; end;
 perform set_config('request.jwt.claim.sub',v.actor::text,true);
 select * into r from public.beende_pachtverhaeltnis(v.first_lease,current_date,'P11 AP04A reguläres Ende');
 if r.bereits_beendet or not (v.principal=any(r.passiv_gesetzte_hauptmitglieder)) then raise exception 'AP04.1 last principal lease did not become passive'; end if;
 select * into r from public.beende_pachtverhaeltnis(v.first_lease,current_date,'P11 AP04A retry');
 if not r.bereits_beendet then raise exception 'AP04.1 retry was not idempotent'; end if;
end $$;
reset role;
do $$ declare v p11_ap04a_ids%rowtype; begin select * into v from p11_ap04a_ids;
 if not exists(select 1 from public.mitglied where id=v.principal and aktiv=false and mitglied_ende is null)
    or not exists(select 1 from public.mitglied where id=v.co_tenant and aktiv=true and mitglied_ende is null)
    or not exists(select 1 from public.pachtverhaeltnis where id=v.first_lease and beendet_am=current_date)
    or not exists(select 1 from public.pachtende_audit where pachtverhaeltnis_id=v.first_lease and akteur_user_id=v.actor)
 then raise exception 'AP04.1 status, history, app access or audit contract failed'; end if; end $$;
rollback;
