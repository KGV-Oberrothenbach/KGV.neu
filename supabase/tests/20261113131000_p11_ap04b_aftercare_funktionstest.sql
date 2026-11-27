-- P11 AP-04.2: synthetic authenticated acceptance, fully rolled back.
begin;
create temporary table p11_ap04b_ids(u uuid, denied uuid, lease_id bigint, aftercare_id bigint, protocol_id bigint) on commit drop;
-- Synthetic JWT identities must exist in auth.users for creator-FK checks.
insert into auth.users(id,aud,role,email,encrypted_password,email_confirmed_at,raw_app_meta_data,raw_user_meta_data,created_at,updated_at)
values
 ('00000000-0000-0000-0000-000000000351','authenticated','authenticated','p11-ap04b-actor@example.test','x',now(),'{}','{}',now(),now()),
 ('00000000-0000-0000-0000-000000000352','authenticated','authenticated','p11-ap04b-denied@example.test','x',now(),'{}','{}',now(),now());
insert into public.mitglied(name,vorname,aktiv,aufnahme_status) values ('P11 AP04B Actor','Test',true,'aufgenommen'),('P11 AP04B Denied','Test',true,'aufgenommen');
-- FR2 local-only synthetic identities; the entire fixture rolls back.
update public.mitglied set auth_user_id='00000000-0000-0000-0000-000000000351', email=(select email from auth.users where id='00000000-0000-0000-0000-000000000351') where name='P11 AP04B Actor';
update public.mitglied set auth_user_id='00000000-0000-0000-0000-000000000352', email=(select email from auth.users where id='00000000-0000-0000-0000-000000000352') where name='P11 AP04B Denied';
-- The member trigger establishes app_user. Verify that link in both directions.
do $$ begin
 if not exists(select 1 from public.app_user a join public.mitglied m on m.id=a.mitglied_id and m.auth_user_id=a.user_id where a.user_id='00000000-0000-0000-0000-000000000351' and m.name='P11 AP04B Actor' and m.aufnahme_status='aufgenommen' and m.mitglied_ende is null) then raise exception 'FR2 fixture link missing'; end if;
 if not exists(select 1 from public.app_user a join public.mitglied m on m.id=a.mitglied_id and m.auth_user_id=a.user_id where a.user_id='00000000-0000-0000-0000-000000000352' and m.name='P11 AP04B Denied' and m.aufnahme_status='aufgenommen' and m.mitglied_ende is null) then raise exception 'FR2 fixture link missing'; end if;
end $$;
insert into public.app_user_hauptrolle(user_id,hauptrolle_code) values
 ('00000000-0000-0000-0000-000000000351','mitglied'),
 ('00000000-0000-0000-0000-000000000352','mitglied') on conflict (user_id) do update set hauptrolle_code=excluded.hauptrolle_code;
insert into auth.sessions(id,user_id,created_at,updated_at,not_after) values
 ('10000000-0000-0000-0000-000000000351','00000000-0000-0000-0000-000000000351',now(),now(),now()+interval '1 hour'),
 ('10000000-0000-0000-0000-000000000352','00000000-0000-0000-0000-000000000352',now(),now(),now()+interval '1 hour');
insert into public.app_user_fachrecht_override(user_id,fachrecht_code,entscheidung,grund) values ('00000000-0000-0000-0000-000000000351','pachtvertrag.verwalten','grant','FR2 AP04 fixture');
insert into public.app_user_zusatzrolle(user_id,zusatzrolle_code) values ('00000000-0000-0000-0000-000000000352','admin');
insert into public.app_user_fachrecht_override(user_id,fachrecht_code,entscheidung,grund) values ('00000000-0000-0000-0000-000000000352','pachtvertrag.verwalten','grant','FR2 before revocation');
set local role authenticated;
do $$ begin perform set_config('request.jwt.claim.sub',('00000000-0000-0000-0000-000000000352')::text,true); perform set_config('request.jwt.claim.role','authenticated',true); perform set_config('request.jwt.claims',jsonb_build_object('sub',('00000000-0000-0000-0000-000000000352')::text,'role','authenticated','session_id','1'||substr(('00000000-0000-0000-0000-000000000352')::text,2))::text,true); if public.fr212_actor_session_valid() is distinct from true then raise exception 'FR2 fixture session invalid'; end if; if public.ap02_has_effective_right('pachtvertrag.verwalten') is distinct from true then raise exception 'FR2 before-deny grant missing'; end if; end $$;
reset role;
select set_config('request.jwt.claim.sub','',true);
select set_config('request.jwt.claim.role','',true);
select set_config('request.jwt.claims','{}',true);
update public.app_user_fachrecht_override set entscheidung='deny',grund='FR2 explicit revocation' where user_id='00000000-0000-0000-0000-000000000352' and fachrecht_code='pachtvertrag.verwalten';
insert into public.parzelle(id,garten_nr,"Anlage") values(9351,'P11-351','Test'); insert into public.pachtverhaeltnis(parzelle_id,urspruenglicher_pachtbeginn,beendet_am,beendigungsgrund) values(9351,current_date-20,current_date-10,'sonstiges');
insert into p11_ap04b_ids select '00000000-0000-0000-0000-000000000351','00000000-0000-0000-0000-000000000352',(select id from public.pachtverhaeltnis where parzelle_id=9351),null,null;
grant select,update on p11_ap04b_ids to authenticated;
set local role authenticated;
do $$ declare v p11_ap04b_ids%rowtype; x bigint; begin select * into v from p11_ap04b_ids; perform set_config('request.jwt.claim.sub',(v.denied)::text,true); perform set_config('request.jwt.claim.role','authenticated',true); perform set_config('request.jwt.claims',jsonb_build_object('sub',(v.denied)::text,'role','authenticated','session_id','1'||substr((v.denied)::text,2))::text,true); if public.fr212_actor_session_valid() is distinct from true then raise exception 'FR2 fixture session invalid'; end if; if public.ap02_has_effective_right('pachtvertrag.verwalten') is distinct from false then raise exception 'FR2 fixture effective right mismatch'; end if; begin perform public.erstelle_pacht_nachsorge(v.lease_id,current_date); raise exception 'denied aftercare succeeded'; exception when sqlstate '42501' then null; end; perform set_config('request.jwt.claim.sub',(v.u)::text,true); perform set_config('request.jwt.claim.role','authenticated',true); perform set_config('request.jwt.claims',jsonb_build_object('sub',(v.u)::text,'role','authenticated','session_id','1'||substr((v.u)::text,2))::text,true); if public.fr212_actor_session_valid() is distinct from true then raise exception 'FR2 fixture session invalid'; end if; if public.ap02_has_effective_right('pachtvertrag.verwalten') is distinct from true then raise exception 'FR2 fixture effective right mismatch'; end if; x:=public.erstelle_pacht_nachsorge(v.lease_id,current_date+100); update p11_ap04b_ids set aftercare_id=x; begin perform public.beende_pacht_nachsorge(x,'nachpaechteruebernahme','planned',v.lease_id); raise exception 'planned successor succeeded'; exception when sqlstate '23514' then null; end; end $$;
reset role;
do $$ begin if not exists(select 1 from public.pacht_nachsorge n join p11_ap04b_ids v on v.aftercare_id=n.id where n.beginnt_am=current_date-10 and n.beendet_am is null) then raise exception 'aftercare history changed unexpectedly'; end if; end $$;
-- A closed return protocol is bound to the ended lease, preserving its existing
-- meter/photo/document references rather than creating a parallel protocol type.
insert into public.parzellen_protokoll(parzelle_id,protokoll_typ,status,pachtverhaeltnis_id,erstellt_von,abgeschlossen_von,abgeschlossen_am)
select 9351,'rueckgabe','abgeschlossen',lease_id,u,u,now() from p11_ap04b_ids;
update p11_ap04b_ids set protocol_id=(select id from public.parzellen_protokoll where parzelle_id=9351 and pachtverhaeltnis_id=lease_id);
set local role authenticated;
do $$ declare v p11_ap04b_ids%rowtype; begin select * into v from p11_ap04b_ids; perform set_config('request.jwt.claim.sub',(v.u)::text,true); perform set_config('request.jwt.claim.role','authenticated',true); perform set_config('request.jwt.claims',jsonb_build_object('sub',(v.u)::text,'role','authenticated','session_id','1'||substr((v.u)::text,2))::text,true); if public.fr212_actor_session_valid() is distinct from true then raise exception 'FR2 fixture session invalid'; end if; if public.ap02_has_effective_right('pachtvertrag.verwalten') is distinct from true then raise exception 'FR2 fixture effective right mismatch'; end if; begin
   perform public.ordne_nachsorge_protokoll_zu(v.aftercare_id,v.protocol_id,'Rückgabe ohne Dokument.');
   raise exception 'FR2 missing protocol document accepted';
 exception when sqlstate '23514' then
   if SQLERRM <> 'Abgeschlossenes passendes Übergabeprotokoll erforderlich.' then raise; end if;
 end;
end $$;
reset role;
-- Metadata-only SQL fixture: no PDF bytes, Storage or network access.
-- Verify the rejected operation did not generate an audit event.
do $$ declare v p11_ap04b_ids%rowtype; doc_id bigint; begin
 select * into strict v from p11_ap04b_ids;
 if exists(select 1 from public.pacht_nachsorge_ereignis where nachsorge_id=v.aftercare_id and art='protokoll_zugeordnet') then
   raise exception 'FR2 rejected protocol association persisted';
 end if;
 insert into public.dokument(parzelle_id,storage_path,titel,dateiname,mime_type,created_by)
 values(9351,'fr2-fixture/ap04b/protokoll.pdf','P11 AP04B SQL fixture','protokoll.pdf','application/pdf',v.u)
 returning id into doc_id;
 update public.parzellen_protokoll set dokument_id=doc_id where id=v.protocol_id;
 if not found then raise exception 'FR2 synthetic protocol missing'; end if;
 if not exists(select 1 from public.parzellen_protokoll p join public.dokument d on d.id=p.dokument_id where p.id=v.protocol_id and p.status='abgeschlossen' and d.parzelle_id=p.parzelle_id and d.mitglied_id is null) then
   raise exception 'FR2 synthetic protocol document mismatch';
 end if;
end $$;
set local role authenticated;
do $$ declare v p11_ap04b_ids%rowtype; begin
 select * into strict v from p11_ap04b_ids;
 perform set_config('request.jwt.claim.sub',v.u::text,true);
 perform set_config('request.jwt.claim.role','authenticated',true);
 perform set_config('request.jwt.claims',jsonb_build_object('sub',v.u::text,'role','authenticated','session_id','1'||substr(v.u::text,2))::text,true);
 if public.fr212_actor_session_valid() is distinct from true or public.ap02_has_effective_right('pachtvertrag.verwalten') is distinct from true then raise exception 'FR2 positive actor invalid'; end if;
 if public.ordne_nachsorge_protokoll_zu(v.aftercare_id,v.protocol_id,'Rückgabe protokolliert.') is distinct from true then raise exception 'FR2 positive association failed'; end if;
end $$;
reset role;
do $$ declare v p11_ap04b_ids%rowtype; begin
 select * into strict v from p11_ap04b_ids;
 if (select count(*) from public.pacht_nachsorge_ereignis where nachsorge_id=v.aftercare_id and art='protokoll_zugeordnet' and protokoll_id=v.protocol_id and akteur_user_id=v.u) <> 1 then raise exception 'FR2 positive association audit mismatch'; end if;
 if not exists(select 1 from public.parzellen_protokoll p join public.dokument d on d.id=p.dokument_id where p.id=v.protocol_id and p.pachtverhaeltnis_id=v.lease_id and p.parzelle_id=d.parzelle_id) then raise exception 'FR2 association persistence mismatch'; end if;
end $$;
-- A future planned successor must not complete aftercare. A present principal
-- successor, created in the same server-side lease transaction, completes it.
do $$ declare v p11_ap04b_ids%rowtype; successor_id bigint; successor_member bigint; begin
 select * into v from p11_ap04b_ids;
 perform set_config('request.jwt.claim.sub',(v.u)::text,true); perform set_config('request.jwt.claim.role','authenticated',true); perform set_config('request.jwt.claims',jsonb_build_object('sub',(v.u)::text,'role','authenticated','session_id','1'||substr((v.u)::text,2))::text,true); if public.fr212_actor_session_valid() is distinct from true then raise exception 'FR2 fixture session invalid'; end if; if public.ap02_has_effective_right('pachtvertrag.verwalten') is distinct from true then raise exception 'FR2 fixture effective right mismatch'; end if;
 insert into public.mitglied(name,vorname,aktiv,aufnahme_status) values('P11 AP04B Successor','Test',true,'aufgenommen') returning id into successor_member;
 insert into public.pachtverhaeltnis(parzelle_id,urspruenglicher_pachtbeginn) values(9351,current_date+1) returning id into successor_id;
 insert into public.pachtverhaeltnis_beteiligung(pachtverhaeltnis_id,mitglied_id,rolle,gueltig_ab) values(successor_id,successor_member,'hauptpaechter',current_date+1);
 if (select beendet_am from public.pacht_nachsorge where id=v.aftercare_id) is not null then raise exception 'future successor completed aftercare'; end if;
 update public.pachtverhaeltnis_beteiligung set gueltig_ab=current_date where pachtverhaeltnis_id=successor_id and rolle='hauptpaechter';
 if (select beendigungsgrund from public.pacht_nachsorge where id=v.aftercare_id) is distinct from 'nachpaechteruebernahme' then raise exception 'actual successor did not complete aftercare'; end if;
 if not exists(select 1 from public.pacht_nachsorge_ereignis where nachsorge_id=v.aftercare_id and art='nachpaechteruebernahme' and nachfolger_pachtverhaeltnis_id=successor_id) then raise exception 'successor audit missing'; end if;
end $$;
set local role authenticated;
do $$ declare v p11_ap04b_ids%rowtype; begin select * into v from p11_ap04b_ids; perform set_config('request.jwt.claim.sub',(v.u)::text,true); perform set_config('request.jwt.claim.role','authenticated',true); perform set_config('request.jwt.claims',jsonb_build_object('sub',(v.u)::text,'role','authenticated','session_id','1'||substr((v.u)::text,2))::text,true); if public.fr212_actor_session_valid() is distinct from true then raise exception 'FR2 fixture session invalid'; end if; if public.ap02_has_effective_right('pachtvertrag.verwalten') is distinct from true then raise exception 'FR2 fixture effective right mismatch'; end if; if public.beende_pacht_nachsorge(v.aftercare_id,'entlassung','Idempotenter Wiederholungsaufruf.') is distinct from true then raise exception 'idempotent completion did not return true'; end if; end $$;
reset role;
do $$ begin
 if not exists(select 1 from public.pacht_nachsorge_ereignis e join p11_ap04b_ids v on v.aftercare_id=e.nachsorge_id where e.art='protokoll_zugeordnet' and e.protokoll_id=v.protocol_id) then raise exception 'protocol audit missing'; end if;
 if exists(select 1 from public.pacht_nachsorge n join p11_ap04b_ids v on v.aftercare_id=n.id where n.beschlossen_von is null or n.beschlossen_am is null) then raise exception 'completion actor or time missing'; end if;
end $$;
select 'KGV_FR2_AP04B_PASS';
rollback;
