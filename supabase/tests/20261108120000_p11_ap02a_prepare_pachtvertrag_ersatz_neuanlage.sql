begin;
create temporary table p11_chain(actor uuid, predecessor bigint, member_id bigint, created_id bigint) on commit drop;
insert into public.mitglied(name,vorname,aktiv,aufnahme_status) values ('P11 Ersatz','Pächter',true,'aufgenommen') returning id as member_id \gset
insert into public.app_user(user_id,role,permission_grants,permission_revocations) values ('00000000-0000-0000-0000-000000000601','user',524288,0);
set local session_replication_role=replica;
insert into public.parzelle(garten_nr,"Anlage") values ('P11-CHAIN','P11') returning id as parcel_id \gset
insert into public.kommunikationsvorgang(vorgangstyp,dokumenttyp,status,mitglied_id,parzelle_id,entwurf_daten) values ('vertrag_signatur','pachtvertrag','erledigt',:'member_id',:'parcel_id','{"draft":{"startDate":"2026-01-01"}}') returning id as source_id \gset
insert into public.dokument(parzelle_id,storage_path,titel,dateiname,mime_type,source_kommunikationsvorgang_id,source_revision) values (:'parcel_id','p11/chain.pdf','P11','p11.pdf','application/pdf',:'source_id',1) returning id as doc_id \gset
insert into public.pachtverhaeltnis(parzelle_id,urspruenglicher_pachtbeginn) values (:'parcel_id','2026-01-01') returning id as lease_id \gset
insert into public.pachtverhaeltnis_beteiligung(pachtverhaeltnis_id,mitglied_id,rolle,gueltig_ab) values (:'lease_id',:'member_id','hauptpaechter','2026-01-01');
insert into public.pachtvertragsfassung(pachtverhaeltnis_id,dokument_id,fassungstyp) values (:'lease_id',:'doc_id','erstvertrag') returning id as predecessor_id \gset
set local session_replication_role=origin;
insert into p11_chain(actor,predecessor,member_id) values ('00000000-0000-0000-0000-000000000601',:'predecessor_id',:'member_id');
grant select,update on p11_chain to authenticated;
set local role authenticated;
do $$ declare v record; x bigint; y bigint; begin
 select * into v from p11_chain; perform set_config('request.jwt.claim.sub',v.actor::text,true);
 select kommunikationsvorgang_id into x from public.prepare_pachtvertrag_ersatz(v.predecessor,v.actor);
 if x is null then raise exception 'RPC lieferte keinen Vorgang zurück'; end if; update p11_chain set created_id=x;
 select kommunikationsvorgang_id into y from public.prepare_pachtvertrag_ersatz(v.predecessor,v.actor);
 if x<>y then raise exception 'RPC-Rückgabewert ist nicht idempotent'; end if;
end $$;
reset role;
do $$ declare v record; begin select * into v from p11_chain; if not exists(select 1 from public.kommunikationsvorgang k where k.id=v.created_id and k.ersetzt_pachtvertragsfassung_id=v.predecessor and k.mitglied_id=v.member_id and k.zugewiesen_an=v.actor and k.status='vorbereitet') or (select count(*) from public.kommunikationsvorgang where ersetzt_pachtvertragsfassung_id=v.predecessor and status not in ('erledigt','abgebrochen'))<>1 then raise exception 'Ersatzvorgang fehlt, ist falsch verknüpft oder doppelt'; end if; end $$;
rollback;
