-- P11 A4.1: synthetic local PostgreSQL and RLS test; all data rolls back.
begin;
insert into auth.users(id,aud,role,email,encrypted_password,email_confirmed_at,raw_app_meta_data,raw_user_meta_data,created_at,updated_at) values
 ('00000000-0000-0000-0000-000000004101','authenticated','authenticated','p11-a41-manage@example.test','x',now(),'{}','{}',now(),now()),
 ('00000000-0000-0000-0000-000000004102','authenticated','authenticated','p11-a41-none@example.test','x',now(),'{}','{}',now(),now()),
 ('00000000-0000-0000-0000-000000004103','authenticated','authenticated','p11-a41-revoked@example.test','x',now(),'{}','{}',now(),now());
insert into public.mitglied(name,vorname,aktiv,aufnahme_status,auth_user_id,mitglied_seit) values
 ('P11 A41 Manager','Test',true,'aufgenommen','00000000-0000-0000-0000-000000004101','2020-01-01'),
 ('P11 A41 Owner','Test',true,'aufgenommen','00000000-0000-0000-0000-000000004102','2020-01-01'),
 ('P11 A41 Foreign','Test',true,'aufgenommen',null,'2020-01-01');
create temporary table p11_a41(manager_id bigint,owner_id bigint,foreign_id bigint,contract_id bigint,assignment_id bigint) on commit drop;
insert into p11_a41(manager_id,owner_id,foreign_id) select max(id) filter(where auth_user_id='00000000-0000-0000-0000-000000004101'),max(id) filter(where auth_user_id='00000000-0000-0000-0000-000000004102'),max(id) filter(where name='P11 A41 Foreign') from public.mitglied;
update public.app_user set mitglied_id=(select manager_id from p11_a41),role='user',permission_grants=4,permission_revocations=0 where user_id='00000000-0000-0000-0000-000000004101';
update public.app_user set mitglied_id=(select owner_id from p11_a41),role='user',permission_grants=0,permission_revocations=0 where user_id='00000000-0000-0000-0000-000000004102';
insert into public.app_user(user_id,role,permission_grants,permission_revocations) values('00000000-0000-0000-0000-000000004103','admin',0,4) on conflict(user_id) do update set role=excluded.role,permission_grants=excluded.permission_grants,permission_revocations=excluded.permission_revocations;
grant select,update on p11_a41 to authenticated,service_role;
set local role authenticated;
select set_config('request.jwt.claim.sub','00000000-0000-0000-0000-000000004101',true); select set_config('request.jwt.claim.role','authenticated',true);
insert into public.wartungsvertraege(titel,max_aktive_zuordnungen,befreit_von_pflichtstunden,arbeitsstunden_gutschrift,aktiv) values('P11 A41 Vertrag',2,true,0,true) returning id as contract_id \gset
update p11_a41 set contract_id=:'contract_id';
insert into public.wartungsvertrag_zuordnungen(wartungsvertrag_id,hauptmitglied_id,vertragspartei_mitglied_id,gueltig_ab,gueltig_bis,bemerkung) values((select contract_id from p11_a41),(select owner_id from p11_a41),(select owner_id from p11_a41),'2030-01-01','2030-12-31','P11 A41') returning id as assignment_id \gset
update p11_a41 set assignment_id=:'assignment_id';
select * from public.ensure_wartungsvertrag_dokument_workflow((select assignment_id from p11_a41));
do $$ begin if not exists(select 1 from public.wartungsvertrag_dokument_workflow where wartungsvertrag_zuordnung_id=(select assignment_id from p11_a41)) then raise exception 'positive workflow missing'; end if; begin insert into public.wartungsvertrag_zuordnungen(wartungsvertrag_id,hauptmitglied_id,vertragspartei_mitglied_id,gueltig_ab,gueltig_bis) values((select contract_id from p11_a41),(select foreign_id from p11_a41),(select owner_id from p11_a41),'2030-01-01','2030-12-31'); raise exception 'wrong party accepted'; exception when sqlstate '23514' then null; end; begin insert into public.wartungsvertrag_zuordnungen(wartungsvertrag_id,hauptmitglied_id,vertragspartei_mitglied_id,gueltig_ab,gueltig_bis) values((select contract_id from p11_a41),(select owner_id from p11_a41),(select owner_id from p11_a41),'2030-12-31','2030-01-01'); raise exception 'invalid period accepted'; exception when sqlstate '22000' then null; end; begin insert into public.wartungsvertrag_zuordnungen(wartungsvertrag_id,hauptmitglied_id,vertragspartei_mitglied_id,gueltig_ab,gueltig_bis) values((select contract_id from p11_a41),(select owner_id from p11_a41),(select owner_id from p11_a41),'2030-06-01','2030-12-31'); raise exception 'overlap accepted'; exception when others then if sqlstate not in ('P0001','23514') then raise; end if; end; end $$;
select set_config('request.jwt.claim.sub','00000000-0000-0000-0000-000000004102',true);
discard plans;
do $$ begin if not exists(select 1 from public.wartungsvertrag_zuordnungen where id=(select assignment_id from p11_a41)) then raise exception 'owner cannot read own assignment'; end if; if not exists(select 1 from public.wartungsvertraege where id=(select contract_id from p11_a41)) then raise exception 'owner cannot read own contract'; end if; begin insert into public.wartungsvertraege(titel,max_aktive_zuordnungen,befreit_von_pflichtstunden,arbeitsstunden_gutschrift,aktiv) values('P11 A41 forbidden',1,false,0,true); raise exception 'missing permission insert accepted'; exception when sqlstate '42501' then null; end; begin perform public.ensure_wartungsvertrag_dokument_workflow((select assignment_id from p11_a41)); raise exception 'missing permission rpc accepted'; exception when sqlstate '42501' then null; end; end $$;
select set_config('request.jwt.claim.sub','00000000-0000-0000-0000-000000004103',true);
discard plans;
do $$ begin if public.has_effective_permission(4) then raise exception 'administrative revocation was ignored'; end if; end $$;
reset role;
set local role service_role;
do $$ begin begin update public.wartungsvertraege set titel='historical mutation' where id=(select contract_id from p11_a41); raise exception 'historical contract mutation accepted'; exception when sqlstate '55000' then null; end; begin update public.wartungsvertrag_zuordnungen set vertragspartei_mitglied_id=(select foreign_id from p11_a41) where id=(select assignment_id from p11_a41); raise exception 'historical party mutation accepted'; exception when sqlstate '55000' then null; end; end $$;
reset role;
do $$ begin if not exists(select 1 from public.wartungsvertrag_zuordnungen where id=(select assignment_id from p11_a41) and gueltig_ab<='2030-01-01' and gueltig_bis>='2030-12-31') then raise exception 'season-wide maintenance assignment missing'; end if; end $$;
rollback;
