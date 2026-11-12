-- P11 AP-02B.2 / R10: real local PostgreSQL/RLS acceptance.
-- Synthetic data only; the transaction always rolls back.
begin;

insert into auth.users(id,aud,role,email,encrypted_password,email_confirmed_at,raw_app_meta_data,raw_user_meta_data,created_at,updated_at) values
  ('00000000-0000-0000-0000-00000000b221','authenticated','authenticated','p11-b22-former@example.test','x',now(),'{}','{}',now(),now()),
  ('00000000-0000-0000-0000-00000000b222','authenticated','authenticated','p11-b22-successor@example.test','x',now(),'{}','{}',now(),now()),
  ('00000000-0000-0000-0000-00000000b223','authenticated','authenticated','p11-b22-revoked@example.test','x',now(),'{}','{}',now(),now());

insert into public.mitglied(name,vorname,aktiv,aufnahme_status,auth_user_id) values
  ('P11 B22 Former tenant','Test',true,'aufgenommen','00000000-0000-0000-0000-00000000b221'),
  ('P11 B22 Successor','Test',true,'aufgenommen','00000000-0000-0000-0000-00000000b222'),
  ('P11 B22 Revoked administrator','Test',true,'aufgenommen','00000000-0000-0000-0000-00000000b223');

create temporary table p11_b22_ids(
  former_member_id bigint not null, successor_member_id bigint not null,
  parcel_one_id bigint not null, parcel_two_id bigint not null,
  old_relation_id bigint not null, other_relation_id bigint not null,
  old_document_id bigint not null, other_document_id bigint not null,
  successor_document_id bigint not null, ordinary_document_id bigint not null,
  old_occupancy_id bigint not null
) on commit drop;

insert into public.app_user(user_id,mitglied_id,role,permission_grants,permission_revocations)
select '00000000-0000-0000-0000-00000000b221'::uuid,id,'user',0,0 from public.mitglied where auth_user_id='00000000-0000-0000-0000-00000000b221'
union all select '00000000-0000-0000-0000-00000000b222'::uuid,id,'user',0,0 from public.mitglied where auth_user_id='00000000-0000-0000-0000-00000000b222'
union all select '00000000-0000-0000-0000-00000000b223'::uuid,id,'admin',0,638992 from public.mitglied where auth_user_id='00000000-0000-0000-0000-00000000b223'
on conflict(user_id) do update set mitglied_id=excluded.mitglied_id,role=excluded.role,permission_grants=excluded.permission_grants,permission_revocations=excluded.permission_revocations;

set local session_replication_role=replica;
insert into public.parzelle(garten_nr,"Anlage") values ('B22-1','P11'),('B22-2','P11');
insert into p11_b22_ids
select
  (select id from public.mitglied where auth_user_id='00000000-0000-0000-0000-00000000b221'),
  (select id from public.mitglied where auth_user_id='00000000-0000-0000-0000-00000000b222'),
  (select id from public.parzelle where garten_nr='B22-1'),
  (select id from public.parzelle where garten_nr='B22-2'),
  0,0,0,0,0,0,0;

insert into public.parzellen_belegung(parzelle_id,mitglied_id,von_datum,bis_datum)
select parcel_one_id,former_member_id,date '2020-01-01',(public.kgv_local_now()::date - 1) from p11_b22_ids;
update p11_b22_ids set old_occupancy_id=(select id from public.parzellen_belegung where parzelle_id=parcel_one_id and mitglied_id=former_member_id);
insert into public.parzellen_belegung(parzelle_id,mitglied_id,von_datum,bis_datum)
select parcel_two_id,former_member_id,date '2018-01-01',date '2020-12-31' from p11_b22_ids;

insert into public.dokument(mitglied_id,bucket,storage_path,titel,dateiname,mime_type) values
  ((select former_member_id from p11_b22_ids),'dokumente','p11/b22/old-lease.pdf','P11 B22 old lease','old-lease.pdf','application/pdf'),
  ((select former_member_id from p11_b22_ids),'dokumente','p11/b22/other-lease.pdf','P11 B22 other lease','other-lease.pdf','application/pdf'),
  ((select successor_member_id from p11_b22_ids),'dokumente','p11/b22/successor-lease.pdf','P11 B22 successor lease','successor-lease.pdf','application/pdf'),
  ((select former_member_id from p11_b22_ids),'dokumente','p11/b22/ordinary.pdf','P11 B22 ordinary member history','ordinary.pdf','application/pdf');
update p11_b22_ids set
  old_document_id=(select id from public.dokument where storage_path='p11/b22/old-lease.pdf'),
  other_document_id=(select id from public.dokument where storage_path='p11/b22/other-lease.pdf'),
  successor_document_id=(select id from public.dokument where storage_path='p11/b22/successor-lease.pdf'),
  ordinary_document_id=(select id from public.dokument where storage_path='p11/b22/ordinary.pdf');

insert into public.pachtverhaeltnis(parzelle_id,urspruenglicher_pachtbeginn,beendet_am,beendigungsgrund)
select parcel_one_id,date '2020-01-01',(public.kgv_local_now()::date - 1),'wechsel' from p11_b22_ids;
update p11_b22_ids set old_relation_id=(select max(id) from public.pachtverhaeltnis where parzelle_id=parcel_one_id);
insert into public.pachtverhaeltnis_beteiligung(pachtverhaeltnis_id,mitglied_id,rolle,gueltig_ab,gueltig_bis)
select old_relation_id,former_member_id,'hauptpaechter',date '2020-01-01',(public.kgv_local_now()::date - 1) from p11_b22_ids;
insert into public.pachtvertragsfassung(pachtverhaeltnis_id,dokument_id,fassungstyp,vertragsdatum,wirksam_ab)
select old_relation_id,old_document_id,'import',date '2020-01-01',date '2020-01-01' from p11_b22_ids;

insert into public.pachtverhaeltnis(parzelle_id,urspruenglicher_pachtbeginn,beendet_am,beendigungsgrund)
select parcel_two_id,date '2018-01-01',date '2020-12-31','wechsel' from p11_b22_ids;
update p11_b22_ids set other_relation_id=(select max(id) from public.pachtverhaeltnis where parzelle_id=parcel_two_id);
insert into public.pachtverhaeltnis_beteiligung(pachtverhaeltnis_id,mitglied_id,rolle,gueltig_ab,gueltig_bis)
select other_relation_id,former_member_id,'mitpaechter',date '2018-01-01',date '2020-12-31' from p11_b22_ids;
insert into public.pachtvertragsfassung(pachtverhaeltnis_id,dokument_id,fassungstyp,vertragsdatum,wirksam_ab)
select other_relation_id,other_document_id,'import',date '2018-01-01',date '2018-01-01' from p11_b22_ids;
set local session_replication_role=origin;
-- A scheduled future assignment is authoritative only once its start date has
-- arrived.  It is inserted by the fixture owner, not by the tested user.
insert into public.parzellen_belegung(parzelle_id,mitglied_id,von_datum,bis_datum)
select parcel_one_id,successor_member_id,public.kgv_local_now()::date + 1,null from p11_b22_ids;
grant select on p11_b22_ids to authenticated;

set local role authenticated;
do $$
declare v p11_b22_ids%rowtype;
begin
  select * into v from p11_b22_ids;
  perform set_config('request.jwt.claim.role','authenticated',true);
  perform set_config('request.jwt.claim.sub','00000000-0000-0000-0000-00000000b221',true);

  -- An active club member remains a member, but receives only their own
  -- historical lease facts before a real successor takeover.
  if not exists(select 1 from public.parzelle where id=v.parcel_one_id)
     or not exists(select 1 from public.parzellen_belegung where id=v.old_occupancy_id)
     or not exists(select 1 from public.pachtverhaeltnis where id=v.old_relation_id)
     or not exists(select 1 from public.pachtvertragsfassung where pachtverhaeltnis_id=v.old_relation_id)
     or not exists(select 1 from public.dokument where id=v.old_document_id)
     or not public.can_access_own_historical_lease_document(v.old_document_id) then
    raise exception 'P11 B22 former tenant cannot read their own history before takeover';
  end if;
  if not exists(select 1 from public.pachtverhaeltnis where id=v.other_relation_id)
     or not exists(select 1 from public.dokument where id=v.other_document_id) then
    raise exception 'P11 B22 independent historical lease was not retained';
  end if;

  -- A planned future successor is not a completed handover.
  if not exists(select 1 from public.dokument where id=v.old_document_id) then
    raise exception 'P11 B22 future successor prematurely blocked history';
  end if;
end;
$$;
reset role;

-- The fixture owner records the factual handover.  The remaining assertions
-- must again run as the authenticated former tenant.
update public.parzellen_belegung set von_datum=public.kgv_local_now()::date
where parzelle_id=(select parcel_one_id from p11_b22_ids)
  and mitglied_id=(select successor_member_id from p11_b22_ids);

set local role authenticated;
do $$
declare v p11_b22_ids%rowtype;
begin
  select * into v from p11_b22_ids;
  perform set_config('request.jwt.claim.role','authenticated',true);
  perform set_config('request.jwt.claim.sub','00000000-0000-0000-0000-00000000b221',true);

  -- The same assignment becomes authoritative only when its start date has
  -- actually arrived.  It then blocks parcel, relationship and document data.
  if exists(select 1 from public.parzelle where id=v.parcel_one_id)
     or exists(select 1 from public.parzellen_belegung where id=v.old_occupancy_id)
     or exists(select 1 from public.pachtverhaeltnis where id=v.old_relation_id)
     or exists(select 1 from public.pachtvertragsfassung where pachtverhaeltnis_id=v.old_relation_id)
     or exists(select 1 from public.dokument where id=v.old_document_id)
     or public.can_access_own_historical_lease_document(v.old_document_id) then
    raise exception 'P11 B22 successor takeover did not close old tenant access';
  end if;
  if exists(select 1 from public.dokument where id=v.successor_document_id) then
    raise exception 'P11 B22 successor document is visible to former tenant';
  end if;

  -- The continuing membership and its non-lease R05 history remain intact.
  if not exists(select 1 from public.mitglied where id=v.former_member_id)
     or not exists(select 1 from public.dokument where id=v.ordinary_document_id) then
    raise exception 'P11 B22 lease handover incorrectly ended membership history';
  end if;
  begin
    update public.dokument set titel='P11 B22 unauthorized write' where id=v.old_document_id;
    if found then raise exception 'P11 B22 former tenant changed historical lease document'; end if;
  exception when sqlstate '42501' then null;
  end;

  -- Manipulated object ids and a complete administrative entitlement revocation
  -- cannot widen the result set.
  if public.can_access_own_historical_lease_document(v.successor_document_id) then
    raise exception 'P11 B22 manipulated document id was accepted';
  end if;
  perform set_config('request.jwt.claim.sub','00000000-0000-0000-0000-00000000b223',true);
  if exists(select 1 from public.parzelle where id=v.parcel_one_id)
     or exists(select 1 from public.pachtverhaeltnis where id=v.other_relation_id)
     or exists(select 1 from public.dokument where id=v.successor_document_id) then
    raise exception 'P11 B22 revoked administrator retained lease access';
  end if;
end;
$$;
reset role;

do $$
declare v p11_b22_ids%rowtype;
begin
  select * into v from p11_b22_ids;
  if (select titel from public.dokument where id=v.old_document_id) <> 'P11 B22 old lease' then
    raise exception 'P11 B22 rejected write changed document provenance';
  end if;
end;
$$;

rollback;
