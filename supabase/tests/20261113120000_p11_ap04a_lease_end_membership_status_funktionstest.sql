-- P11 AP-04.1: authenticated, synthetic, transactional R06/R07 acceptance.
begin;
create temporary table p11_ap04a_ids(actor uuid, denied uuid, first_lease bigint, second_lease bigint, principal bigint, co_tenant bigint) on commit drop;
insert into public.mitglied(name,vorname,aktiv,aufnahme_status) values
 ('P11 AP04A Principal','Test',true,'aufgenommen'),('P11 AP04A CoTenant','Test',true,'aufgenommen'),('P11 AP04A Actor','Test',true,'aufgenommen'),('P11 AP04A Denied','Test',true,'aufgenommen');
-- FR2 local-only synthetic identities; the entire fixture rolls back.
insert into auth.users(id,aud,role,email,encrypted_password,email_confirmed_at,raw_app_meta_data,raw_user_meta_data,created_at,updated_at) values
 ('00000000-0000-0000-0000-000000000341','authenticated','authenticated','p11-ap04a-341@example.test','x',now(),'{}','{}',now(),now()),
 ('00000000-0000-0000-0000-000000000342','authenticated','authenticated','p11-ap04a-342@example.test','x',now(),'{}','{}',now(),now());
update public.mitglied set auth_user_id='00000000-0000-0000-0000-000000000341', email=(select email from auth.users where id='00000000-0000-0000-0000-000000000341') where name='P11 AP04A Actor';
update public.mitglied set auth_user_id='00000000-0000-0000-0000-000000000342', email=(select email from auth.users where id='00000000-0000-0000-0000-000000000342') where name='P11 AP04A Denied';
-- The member trigger establishes app_user. Verify that link in both directions.
do $$ begin
 if not exists(select 1 from public.app_user a join public.mitglied m on m.id=a.mitglied_id and m.auth_user_id=a.user_id where a.user_id='00000000-0000-0000-0000-000000000341' and m.name='P11 AP04A Actor' and m.aufnahme_status='aufgenommen' and m.mitglied_ende is null) then raise exception 'FR2 fixture link missing'; end if;
 if not exists(select 1 from public.app_user a join public.mitglied m on m.id=a.mitglied_id and m.auth_user_id=a.user_id where a.user_id='00000000-0000-0000-0000-000000000342' and m.name='P11 AP04A Denied' and m.aufnahme_status='aufgenommen' and m.mitglied_ende is null) then raise exception 'FR2 fixture link missing'; end if;
end $$;
insert into public.app_user_hauptrolle(user_id,hauptrolle_code) values
 ('00000000-0000-0000-0000-000000000341','mitglied'),
 ('00000000-0000-0000-0000-000000000342','mitglied') on conflict (user_id) do update set hauptrolle_code=excluded.hauptrolle_code;
insert into auth.sessions(id,user_id,created_at,updated_at,not_after) values
 ('10000000-0000-0000-0000-000000000341','00000000-0000-0000-0000-000000000341',now(),now(),now()+interval '1 hour'),
 ('10000000-0000-0000-0000-000000000342','00000000-0000-0000-0000-000000000342',now(),now(),now()+interval '1 hour');
insert into public.app_user_fachrecht_override(user_id,fachrecht_code,entscheidung,grund) values ('00000000-0000-0000-0000-000000000341','pachtvertrag.verwalten','grant','FR2 AP04 fixture');
insert into public.parzelle(id,garten_nr,"Anlage") values (9341,'P11-341','Test'),(9342,'P11-342','Test');
insert into public.pachtverhaeltnis(parzelle_id,urspruenglicher_pachtbeginn) values (9341,current_date-10),(9342,current_date-10);
insert into public.pachtverhaeltnis_beteiligung(pachtverhaeltnis_id,mitglied_id,rolle,gueltig_ab)
select r.id,m.id,case when m.name='P11 AP04A Principal' then 'hauptpaechter' else 'mitpaechter' end,current_date-10 from public.pachtverhaeltnis r cross join public.mitglied m where r.parzelle_id=9341 and m.name in ('P11 AP04A Principal','P11 AP04A CoTenant');
insert into p11_ap04a_ids select '00000000-0000-0000-0000-000000000341','00000000-0000-0000-0000-000000000342',(select id from public.pachtverhaeltnis where parzelle_id=9341),(select id from public.pachtverhaeltnis where parzelle_id=9342),(select id from public.mitglied where name='P11 AP04A Principal'),(select id from public.mitglied where name='P11 AP04A CoTenant');
grant select on p11_ap04a_ids to authenticated;
set local role authenticated;
do $$ declare v p11_ap04a_ids%rowtype; r record; begin
 select * into v from p11_ap04a_ids;
 perform set_config('request.jwt.claim.sub',(v.denied)::text,true); perform set_config('request.jwt.claim.role','authenticated',true); perform set_config('request.jwt.claims',jsonb_build_object('sub',(v.denied)::text,'role','authenticated','session_id','1'||substr((v.denied)::text,2))::text,true); if public.fr212_actor_session_valid() is distinct from true then raise exception 'FR2 fixture session invalid'; end if; if public.ap02_has_effective_right('pachtvertrag.verwalten') is distinct from false then raise exception 'FR2 fixture effective right mismatch'; end if;
 begin perform public.beende_pachtverhaeltnis(v.first_lease,current_date,'unberechtigt'); raise exception 'AP04.1 denied end unexpectedly succeeded'; exception when sqlstate '42501' then null; end;
 perform set_config('request.jwt.claim.sub',(v.actor)::text,true); perform set_config('request.jwt.claim.role','authenticated',true); perform set_config('request.jwt.claims',jsonb_build_object('sub',(v.actor)::text,'role','authenticated','session_id','1'||substr((v.actor)::text,2))::text,true); if public.fr212_actor_session_valid() is distinct from true then raise exception 'FR2 fixture session invalid'; end if; if public.ap02_has_effective_right('pachtvertrag.verwalten') is distinct from true then raise exception 'FR2 fixture effective right mismatch'; end if;
 select * into r from public.beende_pachtverhaeltnis(v.first_lease,current_date,'P11 AP04A reguläres Ende');
 if r.bereits_beendet is distinct from false or (v.principal=any(r.passiv_gesetzte_hauptmitglieder)) is distinct from true then raise exception 'AP04.1 last principal lease did not become passive'; end if;
 select * into r from public.beende_pachtverhaeltnis(v.first_lease,current_date,'P11 AP04A retry');
 if r.bereits_beendet is distinct from true then raise exception 'AP04.1 retry was not idempotent'; end if;
end $$;
reset role;
do $$ declare v p11_ap04a_ids%rowtype; begin select * into v from p11_ap04a_ids;
 if not exists(select 1 from public.mitglied where id=v.principal and aktiv=false and mitglied_ende is null)
    or not exists(select 1 from public.mitglied where id=v.co_tenant and aktiv=true and mitglied_ende is null)
    or not exists(select 1 from public.pachtverhaeltnis where id=v.first_lease and beendet_am=current_date)
    or (select count(*) from public.pachtende_audit where pachtverhaeltnis_id=v.first_lease and akteur_user_id=v.actor)<>1
    or not exists(select 1 from public.pachtverhaeltnis where id=v.second_lease and beendet_am is null)
 then raise exception 'AP04.1 status, history, app access or audit contract failed'; end if; end $$;
select 'KGV_FR2_AP04A_PASS';
rollback;
