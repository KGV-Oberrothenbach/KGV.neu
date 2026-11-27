-- P11 AP-04.3: authenticated tenant change and successor occupancy; synthetic and rollback-only.
begin;
create temporary table p11_ap04c_ids(actor uuid, denied uuid, old_lease bigint, aftercare bigint) on commit drop;
insert into public.mitglied(name,vorname,aktiv,aufnahme_status) values
 ('P11 AP04C Alt','Test',true,'aufgenommen'),('P11 AP04C Neu','Test',true,'aufgenommen'),('P11 AP04C Denied','Test',true,'aufgenommen');
-- FR2 local-only synthetic identities; the entire fixture rolls back.
insert into auth.users(id,aud,role,email,encrypted_password,email_confirmed_at,raw_app_meta_data,raw_user_meta_data,created_at,updated_at) values
 ('00000000-0000-0000-0000-000000000461','authenticated','authenticated','p11-ap04c-461@example.test','x',now(),'{}','{}',now(),now()),
 ('00000000-0000-0000-0000-000000000462','authenticated','authenticated','p11-ap04c-462@example.test','x',now(),'{}','{}',now(),now());
update public.mitglied set auth_user_id='00000000-0000-0000-0000-000000000461', email=(select email from auth.users where id='00000000-0000-0000-0000-000000000461') where name='P11 AP04C Alt';
update public.mitglied set auth_user_id='00000000-0000-0000-0000-000000000462', email=(select email from auth.users where id='00000000-0000-0000-0000-000000000462') where name='P11 AP04C Denied';
-- The member trigger establishes app_user. Verify that link in both directions.
do $$ begin
 if not exists(select 1 from public.app_user a join public.mitglied m on m.id=a.mitglied_id and m.auth_user_id=a.user_id where a.user_id='00000000-0000-0000-0000-000000000461' and m.name='P11 AP04C Alt' and m.aufnahme_status='aufgenommen' and m.mitglied_ende is null) then raise exception 'FR2 fixture link missing'; end if;
 if not exists(select 1 from public.app_user a join public.mitglied m on m.id=a.mitglied_id and m.auth_user_id=a.user_id where a.user_id='00000000-0000-0000-0000-000000000462' and m.name='P11 AP04C Denied' and m.aufnahme_status='aufgenommen' and m.mitglied_ende is null) then raise exception 'FR2 fixture link missing'; end if;
end $$;
insert into public.app_user_hauptrolle(user_id,hauptrolle_code) values
 ('00000000-0000-0000-0000-000000000461','mitglied'),
 ('00000000-0000-0000-0000-000000000462','mitglied') on conflict (user_id) do update set hauptrolle_code=excluded.hauptrolle_code;
insert into auth.sessions(id,user_id,created_at,updated_at,not_after) values
 ('10000000-0000-0000-0000-000000000461','00000000-0000-0000-0000-000000000461',now(),now(),now()+interval '1 hour'),
 ('10000000-0000-0000-0000-000000000462','00000000-0000-0000-0000-000000000462',now(),now(),now()+interval '1 hour');
insert into public.app_user_fachrecht_override(user_id,fachrecht_code,entscheidung,grund) values ('00000000-0000-0000-0000-000000000461','pachtvertrag.verwalten','grant','FR2 AP04 fixture');
insert into public.app_user_zusatzrolle(user_id,zusatzrolle_code) values ('00000000-0000-0000-0000-000000000462','admin');
insert into public.app_user_fachrecht_override(user_id,fachrecht_code,entscheidung,grund) values ('00000000-0000-0000-0000-000000000462','pachtvertrag.verwalten','grant','FR2 before revocation');
set local role authenticated;
do $$ begin perform set_config('request.jwt.claim.sub',('00000000-0000-0000-0000-000000000462')::text,true); perform set_config('request.jwt.claim.role','authenticated',true); perform set_config('request.jwt.claims',jsonb_build_object('sub',('00000000-0000-0000-0000-000000000462')::text,'role','authenticated','session_id','1'||substr(('00000000-0000-0000-0000-000000000462')::text,2))::text,true); if public.fr212_actor_session_valid() is distinct from true then raise exception 'FR2 fixture session invalid'; end if; if public.ap02_has_effective_right('pachtvertrag.verwalten') is distinct from true then raise exception 'FR2 before-deny grant missing'; end if; end $$;
reset role;
select set_config('request.jwt.claim.sub','',true);
select set_config('request.jwt.claim.role','',true);
select set_config('request.jwt.claims','{}',true);
update public.app_user_fachrecht_override set entscheidung='deny',grund='FR2 explicit revocation' where user_id='00000000-0000-0000-0000-000000000462' and fachrecht_code='pachtvertrag.verwalten';
insert into public.parzelle(id,garten_nr,"Anlage") values(9461,'P11-461','Test');
insert into public.pachtverhaeltnis(parzelle_id,urspruenglicher_pachtbeginn) values(9461,current_date-30);
insert into p11_ap04c_ids select '00000000-0000-0000-0000-000000000461','00000000-0000-0000-0000-000000000462',(select id from public.pachtverhaeltnis where parzelle_id=9461),null;
insert into public.pachtverhaeltnis_beteiligung(pachtverhaeltnis_id,mitglied_id,rolle,gueltig_ab)
select old_lease,(select id from public.mitglied where name='P11 AP04C Alt'),'hauptpaechter',current_date-30 from p11_ap04c_ids;
grant select,update on p11_ap04c_ids to authenticated;
set local role authenticated;
do $$ declare x p11_ap04c_ids%rowtype; begin select * into x from p11_ap04c_ids; perform set_config('request.jwt.claim.sub',(x.denied)::text,true); perform set_config('request.jwt.claim.role','authenticated',true); perform set_config('request.jwt.claims',jsonb_build_object('sub',(x.denied)::text,'role','authenticated','session_id','1'||substr((x.denied)::text,2))::text,true); if public.fr212_actor_session_valid() is distinct from true then raise exception 'FR2 fixture session invalid'; end if; if public.ap02_has_effective_right('pachtvertrag.verwalten') is distinct from false then raise exception 'FR2 fixture effective right mismatch'; end if; begin perform public.beende_pachtverhaeltnis(x.old_lease,current_date-1,'Nachpächterwechsel'); raise exception 'revoked admin ended lease'; exception when sqlstate '42501' then null; end; perform set_config('request.jwt.claim.sub',(x.actor)::text,true); perform set_config('request.jwt.claim.role','authenticated',true); perform set_config('request.jwt.claims',jsonb_build_object('sub',(x.actor)::text,'role','authenticated','session_id','1'||substr((x.actor)::text,2))::text,true); if public.fr212_actor_session_valid() is distinct from true then raise exception 'FR2 fixture session invalid'; end if; if public.ap02_has_effective_right('pachtvertrag.verwalten') is distinct from true then raise exception 'FR2 fixture effective right mismatch'; end if; perform public.beende_pachtverhaeltnis(x.old_lease,current_date-1,'Nachpächterwechsel'); update p11_ap04c_ids set aftercare=public.erstelle_pacht_nachsorge(x.old_lease,current_date+365); end $$;
reset role;
do $$ declare x p11_ap04c_ids%rowtype; future_lease bigint; begin
 select * into x from p11_ap04c_ids;
 if not exists(select 1 from public.parzellen_belegung where pachtverhaeltnis_id=x.old_lease) or exists(select 1 from public.parzellen_belegung where pachtverhaeltnis_id=x.old_lease and bis_datum is distinct from current_date-1) then raise exception 'legal end did not close linked occupancy'; end if;
 perform set_config('request.jwt.claim.sub',(x.actor)::text,true); perform set_config('request.jwt.claim.role','authenticated',true); perform set_config('request.jwt.claims',jsonb_build_object('sub',(x.actor)::text,'role','authenticated','session_id','1'||substr((x.actor)::text,2))::text,true); if public.fr212_actor_session_valid() is distinct from true then raise exception 'FR2 fixture session invalid'; end if; if public.ap02_has_effective_right('pachtvertrag.verwalten') is distinct from true then raise exception 'FR2 fixture effective right mismatch'; end if;
 insert into public.pachtverhaeltnis(parzelle_id,urspruenglicher_pachtbeginn) values(9461,current_date+1) returning id into future_lease;
 insert into public.pachtverhaeltnis_beteiligung(pachtverhaeltnis_id,mitglied_id,rolle,gueltig_ab) values(future_lease,(select id from public.mitglied where name='P11 AP04C Neu'),'hauptpaechter',current_date+1);
 if (select beendet_am from public.pacht_nachsorge where id=x.aftercare) is not null then raise exception 'planned tenant closed aftercare'; end if;
 update public.pachtverhaeltnis_beteiligung set gueltig_ab=current_date where pachtverhaeltnis_id=future_lease;
 if (select beendet_am from public.pacht_nachsorge where id=x.aftercare) is null then raise exception 'actual successor did not close aftercare'; end if;
 begin insert into public.parzellen_belegung(parzelle_id,mitglied_id,von_datum) values(9461,(select id from public.mitglied where name='P11 AP04C Denied'),current_date); raise exception 'overlapping occupancy accepted'; exception when exclusion_violation then null; end;
end $$;
select 'KGV_FR2_AP04C_PASS';
rollback;
