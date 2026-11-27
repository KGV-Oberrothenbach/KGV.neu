begin;
create temporary table p11_ap04d_ids(u uuid, lease_id bigint, parcel_id bigint) on commit drop;
insert into public.mitglied(name,vorname,aktiv,aufnahme_status) values('P11 AP04D','Test',true,'aufgenommen');
-- FR2 local-only synthetic identities; the entire fixture rolls back.
insert into auth.users(id,aud,role,email,encrypted_password,email_confirmed_at,raw_app_meta_data,raw_user_meta_data,created_at,updated_at) values
 ('00000000-0000-0000-0000-000000000471','authenticated','authenticated','p11-ap04d-471@example.test','x',now(),'{}','{}',now(),now());
update public.mitglied set auth_user_id='00000000-0000-0000-0000-000000000471', email=(select email from auth.users where id='00000000-0000-0000-0000-000000000471') where name='P11 AP04D';
-- The member trigger establishes app_user. Verify that link in both directions.
do $$ begin
 if not exists(select 1 from public.app_user a join public.mitglied m on m.id=a.mitglied_id and m.auth_user_id=a.user_id where a.user_id='00000000-0000-0000-0000-000000000471' and m.name='P11 AP04D' and m.aufnahme_status='aufgenommen' and m.mitglied_ende is null) then raise exception 'FR2 fixture link missing'; end if;
end $$;
insert into public.app_user_hauptrolle(user_id,hauptrolle_code) values
 ('00000000-0000-0000-0000-000000000471','mitglied') on conflict (user_id) do update set hauptrolle_code=excluded.hauptrolle_code;
insert into auth.sessions(id,user_id,created_at,updated_at,not_after) values
 ('10000000-0000-0000-0000-000000000471','00000000-0000-0000-0000-000000000471',now(),now(),now()+interval '1 hour');
insert into public.app_user_fachrecht_override(user_id,fachrecht_code,entscheidung,grund) values ('00000000-0000-0000-0000-000000000471','pachtvertrag.verwalten','grant','FR2 AP04 fixture');
insert into public.app_user_fachrecht_override(user_id,fachrecht_code,entscheidung,grund) values ('00000000-0000-0000-0000-000000000471','parzelle.bearbeiten','grant','FR2 AP04D parcel control');
insert into public.parzelle(id,garten_nr,"Anlage",flaeche_qm,hat_strom,hat_wasser) values(9471,'P11-471','Test',100,false,false),(9472,'P11-472-Control','Test',100,false,false);
insert into public.pachtverhaeltnis(parzelle_id,urspruenglicher_pachtbeginn) values(9471,current_date-2);
insert into p11_ap04d_ids select '00000000-0000-0000-0000-000000000471',(select id from public.pachtverhaeltnis where parzelle_id=9471),9471;
insert into public.pachtverhaeltnis_beteiligung(pachtverhaeltnis_id,mitglied_id,rolle,gueltig_ab) select lease_id,(select id from public.mitglied where name='P11 AP04D'),'hauptpaechter',current_date-2 from p11_ap04d_ids;
grant select on p11_ap04d_ids to authenticated;
set local role authenticated;
do $$ declare x p11_ap04d_ids%rowtype; begin select * into x from p11_ap04d_ids; perform set_config('request.jwt.claim.sub',(x.u)::text,true); perform set_config('request.jwt.claim.role','authenticated',true); perform set_config('request.jwt.claims',jsonb_build_object('sub',(x.u)::text,'role','authenticated','session_id','1'||substr((x.u)::text,2))::text,true); if public.fr212_actor_session_valid() is distinct from true then raise exception 'FR2 fixture session invalid'; end if; if public.ap02_has_effective_right('pachtvertrag.verwalten') is distinct from true then raise exception 'FR2 fixture effective right mismatch'; end if; if public.ap02_has_effective_right('parzelle.bearbeiten') is distinct from true then raise exception 'FR2 parcel edit grant missing'; end if;
 if not exists(select 1 from public.parzelle where id=x.parcel_id and flaeche_qm=100) then raise exception 'FR2 target parcel invisible'; end if;
 update public.parzelle set flaeche_qm=101 where id=9472;
 if not found then raise exception 'FR2 unleased positive control did not update'; end if;
 begin
  update public.parzelle set flaeche_qm=101 where id=x.parcel_id;
  raise exception 'FR2 leased parcel update was not explicitly rejected';
 exception when sqlstate '42501' then
  if position('laufend verpachteten Parzelle' in SQLERRM)=0 then raise; end if;
 end; if to_regprocedure('public.wende_signierte_parzellen_vertragsaenderung_an(bigint,bigint,numeric,boolean,boolean,text)') is not null then raise exception 'legacy contract-change RPC remains callable'; end if; end $$;
reset role;
do $$ begin if (select flaeche_qm from public.parzelle where id=9471) is distinct from 100::numeric or (select flaeche_qm from public.parzelle where id=9472) is distinct from 101::numeric then raise exception 'rejected change was persisted'; end if; end $$;
-- P11-T07-N: the latest implementation must compare the complete canonical
-- snapshot, rather than merely accepting a matching reason string.
do $$
declare v_definition text;
begin
 select pg_get_functiondef('public.bereite_signierte_parzellen_vertragsaenderung_vor(bigint,numeric,boolean,boolean,text)'::regprocedure) into v_definition;
 if position('Abweichende Wiederholung einer Vertragsänderung' in v_definition)=0
    or position('v_change.aenderungsstand is distinct from v_snapshot' in v_definition)=0 then
   raise exception 'P11-T07 idempotency comparison is missing';
 end if;
 select pg_get_functiondef('public.bind_pacht_vertragsaenderung_an_fassung()'::regprocedure) into v_definition;
 if position('p11_contract_change_clause' in v_definition)=0
    or position('position(v_clause in' in v_definition)=0 then
   raise exception 'P11-T07 complete contract-content binding is missing';
 end if;
end $$;
select 'KGV_FR2_AP04D_PASS';
rollback;
