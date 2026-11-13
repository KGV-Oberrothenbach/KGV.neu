-- P11 AP-04.2: synthetic authenticated acceptance, fully rolled back.
begin;
create temporary table p11_ap04b_ids(u uuid, denied uuid, lease_id bigint, aftercare_id bigint, protocol_id bigint) on commit drop;
insert into public.mitglied(name,vorname,aktiv,aufnahme_status) values ('P11 AP04B Actor','Test',true,'aufgenommen'),('P11 AP04B Denied','Test',true,'aufgenommen');
insert into public.app_user(user_id,mitglied_id,role,permission_grants,permission_revocations) values
 ('00000000-0000-0000-0000-000000000351',(select id from public.mitglied where name='P11 AP04B Actor'),'user',524288,0),('00000000-0000-0000-0000-000000000352',(select id from public.mitglied where name='P11 AP04B Denied'),'admin',0,524288);
insert into public.parzelle(id,garten_nr,"Anlage") values(9351,'P11-351','Test'); insert into public.pachtverhaeltnis(parzelle_id,urspruenglicher_pachtbeginn,beendet_am,beendigungsgrund) values(9351,current_date-20,current_date-10,'sonstiges');
insert into p11_ap04b_ids select '00000000-0000-0000-0000-000000000351','00000000-0000-0000-0000-000000000352',(select id from public.pachtverhaeltnis where parzelle_id=9351),null,null;
grant select,update on p11_ap04b_ids to authenticated;
set local role authenticated;
do $$ declare v p11_ap04b_ids%rowtype; x bigint; begin select * into v from p11_ap04b_ids; perform set_config('request.jwt.claim.sub',v.denied::text,true); begin perform public.erstelle_pacht_nachsorge(v.lease_id,current_date); raise exception 'denied aftercare succeeded'; exception when sqlstate '42501' then null; end; perform set_config('request.jwt.claim.sub',v.u::text,true); x:=public.erstelle_pacht_nachsorge(v.lease_id,current_date+100); update p11_ap04b_ids set aftercare_id=x; begin perform public.beende_pacht_nachsorge(x,'nachpaechteruebernahme','planned',v.lease_id); raise exception 'planned successor succeeded'; exception when sqlstate '23514' then null; end; end $$;
reset role;
do $$ begin if not exists(select 1 from public.pacht_nachsorge n join p11_ap04b_ids v on v.aftercare_id=n.id where n.beginnt_am=current_date-10 and n.beendet_am is null) then raise exception 'aftercare history changed unexpectedly'; end if; end $$;
-- A closed return protocol is bound to the ended lease, preserving its existing
-- meter/photo/document references rather than creating a parallel protocol type.
insert into public.parzellen_protokoll(parzelle_id,protokoll_typ,status,pachtverhaeltnis_id,erstellt_von,abgeschlossen_von,abgeschlossen_am)
select 9351,'rueckgabe','abgeschlossen',lease_id,u,u,now() from p11_ap04b_ids;
update p11_ap04b_ids set protocol_id=(select id from public.parzellen_protokoll where parzelle_id=9351 and pachtverhaeltnis_id=lease_id);
set local role authenticated;
do $$ declare v p11_ap04b_ids%rowtype; begin select * into v from p11_ap04b_ids; perform set_config('request.jwt.claim.sub',v.u::text,true); perform public.ordne_nachsorge_protokoll_zu(v.aftercare_id,v.protocol_id,'Rückgabe protokolliert.'); end $$;
reset role;
-- A future planned successor must not complete aftercare. A present principal
-- successor, created in the same server-side lease transaction, completes it.
do $$ declare v p11_ap04b_ids%rowtype; successor_id bigint; successor_member bigint; begin
 select * into v from p11_ap04b_ids;
 perform set_config('request.jwt.claim.sub',v.u::text,true);
 insert into public.mitglied(name,vorname,aktiv,aufnahme_status) values('P11 AP04B Successor','Test',true,'aufgenommen') returning id into successor_member;
 insert into public.pachtverhaeltnis(parzelle_id,urspruenglicher_pachtbeginn) values(9351,current_date+1) returning id into successor_id;
 insert into public.pachtverhaeltnis_beteiligung(pachtverhaeltnis_id,mitglied_id,rolle,gueltig_ab) values(successor_id,successor_member,'hauptpaechter',current_date+1);
 if (select beendet_am from public.pacht_nachsorge where id=v.aftercare_id) is not null then raise exception 'future successor completed aftercare'; end if;
 update public.pachtverhaeltnis_beteiligung set gueltig_ab=current_date where pachtverhaeltnis_id=successor_id and rolle='hauptpaechter';
 if (select beendigungsgrund from public.pacht_nachsorge where id=v.aftercare_id) <> 'nachpaechteruebernahme' then raise exception 'actual successor did not complete aftercare'; end if;
 if not exists(select 1 from public.pacht_nachsorge_ereignis where nachsorge_id=v.aftercare_id and art='nachpaechteruebernahme' and nachfolger_pachtverhaeltnis_id=successor_id) then raise exception 'successor audit missing'; end if;
end $$;
set local role authenticated;
do $$ declare v p11_ap04b_ids%rowtype; begin select * into v from p11_ap04b_ids; perform set_config('request.jwt.claim.sub',v.u::text,true); if not public.beende_pacht_nachsorge(v.aftercare_id,'entlassung','Idempotenter Wiederholungsaufruf.') then raise exception 'idempotent completion did not return true'; end if; end $$;
reset role;
do $$ begin
 if not exists(select 1 from public.pacht_nachsorge_ereignis e join p11_ap04b_ids v on v.aftercare_id=e.nachsorge_id where e.art='protokoll_zugeordnet' and e.protokoll_id=v.protocol_id) then raise exception 'protocol audit missing'; end if;
 if exists(select 1 from public.pacht_nachsorge n join p11_ap04b_ids v on v.aftercare_id=n.id where n.beschlossen_von is null or n.beschlossen_am is null) then raise exception 'completion actor or time missing'; end if;
end $$;
rollback;
