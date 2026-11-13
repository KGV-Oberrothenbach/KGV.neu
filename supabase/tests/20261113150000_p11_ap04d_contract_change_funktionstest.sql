begin;
create temporary table p11_ap04d_ids(u uuid, lease_id bigint, parcel_id bigint) on commit drop;
insert into public.mitglied(name,vorname,aktiv,aufnahme_status) values('P11 AP04D','Test',true,'aufgenommen');
insert into public.app_user(user_id,mitglied_id,role,permission_grants,permission_revocations) values('00000000-0000-0000-0000-000000000471',(select id from public.mitglied where name='P11 AP04D'),'user',524288,0);
insert into public.parzelle(id,garten_nr,"Anlage",flaeche_qm,hat_strom,hat_wasser) values(9471,'P11-471','Test',100,false,false);
insert into public.pachtverhaeltnis(parzelle_id,urspruenglicher_pachtbeginn) values(9471,current_date-2);
insert into p11_ap04d_ids select '00000000-0000-0000-0000-000000000471',(select id from public.pachtverhaeltnis where parzelle_id=9471),9471;
insert into public.pachtverhaeltnis_beteiligung(pachtverhaeltnis_id,mitglied_id,rolle,gueltig_ab) select lease_id,(select id from public.mitglied where name='P11 AP04D'),'hauptpaechter',current_date-2 from p11_ap04d_ids;
grant select on p11_ap04d_ids to authenticated;
set local role authenticated;
do $$ declare x p11_ap04d_ids%rowtype; begin select * into x from p11_ap04d_ids; perform set_config('request.jwt.claim.sub',x.u::text,true); update public.parzelle set flaeche_qm=101 where id=x.parcel_id; if found then raise exception 'direct running contract change accepted'; end if; if to_regprocedure('public.wende_signierte_parzellen_vertragsaenderung_an(bigint,bigint,numeric,boolean,boolean,text)') is not null then raise exception 'legacy contract-change RPC remains callable'; end if; end $$;
reset role;
do $$ begin if (select flaeche_qm from public.parzelle where id=9471)<>100 then raise exception 'rejected change was persisted'; end if; end $$;
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
rollback;
