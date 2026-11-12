-- P11 AP-02B.1 / R05: real local PostgreSQL/RLS acceptance.
-- All persons, documents and context changes are synthetic and rolled back.
begin;

insert into auth.users(id,aud,role,email,encrypted_password,email_confirmed_at,raw_app_meta_data,raw_user_meta_data,created_at,updated_at) values
  ('00000000-0000-0000-0000-00000000b211','authenticated','authenticated','p11-b21-active@example.test','x',now(),'{}','{}',now(),now()),
  ('00000000-0000-0000-0000-00000000b212','authenticated','authenticated','p11-b21-grace@example.test','x',now(),'{}','{}',now(),now()),
  ('00000000-0000-0000-0000-00000000b213','authenticated','authenticated','p11-b21-expired@example.test','x',now(),'{}','{}',now(),now()),
  ('00000000-0000-0000-0000-00000000b214','authenticated','authenticated','p11-b21-foreign@example.test','x',now(),'{}','{}',now(),now()),
  ('00000000-0000-0000-0000-00000000b215','authenticated','authenticated','p11-b21-manager@example.test','x',now(),'{}','{}',now(),now()),
  ('00000000-0000-0000-0000-00000000b216','authenticated','authenticated','p11-b21-revoked@example.test','x',now(),'{}','{}',now(),now());

insert into public.mitglied(name,vorname,aktiv,aufnahme_status,auth_user_id,mitglied_ende) values
  ('P11 B21 Active','Test',true,'aufgenommen','00000000-0000-0000-0000-00000000b211',null),
  ('P11 B21 Grace','Test',false,'aufgenommen','00000000-0000-0000-0000-00000000b212',public.kgv_local_now()::date - 1),
  ('P11 B21 Expired','Test',false,'aufgenommen','00000000-0000-0000-0000-00000000b213',(public.kgv_local_now()::date - interval '2 years')::date),
  ('P11 B21 Foreign','Test',true,'aufgenommen','00000000-0000-0000-0000-00000000b214',null),
  ('P11 B21 Manager','Test',true,'aufgenommen','00000000-0000-0000-0000-00000000b215',null),
  ('P11 B21 Revoked','Test',true,'aufgenommen','00000000-0000-0000-0000-00000000b216',null);

create temporary table p11_b21_ids(
  active_member_id bigint not null, grace_member_id bigint not null,
  expired_member_id bigint not null, foreign_member_id bigint not null,
  active_document_id bigint not null, grace_document_id bigint not null,
  expired_document_id bigint not null, foreign_document_id bigint not null
) on commit drop;

insert into p11_b21_ids
select
  (select id from public.mitglied where auth_user_id='00000000-0000-0000-0000-00000000b211'),
  (select id from public.mitglied where auth_user_id='00000000-0000-0000-0000-00000000b212'),
  (select id from public.mitglied where auth_user_id='00000000-0000-0000-0000-00000000b213'),
  (select id from public.mitglied where auth_user_id='00000000-0000-0000-0000-00000000b214'),
  0,0,0,0;

insert into public.app_user(user_id,mitglied_id,role,permission_grants,permission_revocations)
select '00000000-0000-0000-0000-00000000b211',active_member_id,'user',0,0 from p11_b21_ids
union all select '00000000-0000-0000-0000-00000000b212',grace_member_id,'user',0,0 from p11_b21_ids
union all select '00000000-0000-0000-0000-00000000b213',expired_member_id,'user',0,0 from p11_b21_ids
union all select '00000000-0000-0000-0000-00000000b214',foreign_member_id,'user',0,0 from p11_b21_ids
union all select '00000000-0000-0000-0000-00000000b215',(select id from public.mitglied where auth_user_id='00000000-0000-0000-0000-00000000b215'),'user',65536,0 from p11_b21_ids
union all select '00000000-0000-0000-0000-00000000b216',(select id from public.mitglied where auth_user_id='00000000-0000-0000-0000-00000000b216'),'admin',0,65536 from p11_b21_ids
on conflict(user_id) do update set mitglied_id=excluded.mitglied_id,role=excluded.role,permission_grants=excluded.permission_grants,permission_revocations=excluded.permission_revocations;

set local session_replication_role=replica;
insert into public.dokument(mitglied_id,bucket,storage_path,titel,dateiname,mime_type) values
  ((select active_member_id from p11_b21_ids),'dokumente','p11/b21/active.pdf','P11 B21 active','active.pdf','application/pdf'),
  ((select grace_member_id from p11_b21_ids),'dokumente','p11/b21/grace.pdf','P11 B21 grace','grace.pdf','application/pdf'),
  ((select expired_member_id from p11_b21_ids),'dokumente','p11/b21/expired.pdf','P11 B21 expired','expired.pdf','application/pdf'),
  ((select foreign_member_id from p11_b21_ids),'dokumente','p11/b21/foreign.pdf','P11 B21 foreign','foreign.pdf','application/pdf');
set local session_replication_role=origin;

update p11_b21_ids set
  active_document_id=(select id from public.dokument where storage_path='p11/b21/active.pdf'),
  grace_document_id=(select id from public.dokument where storage_path='p11/b21/grace.pdf'),
  expired_document_id=(select id from public.dokument where storage_path='p11/b21/expired.pdf'),
  foreign_document_id=(select id from public.dokument where storage_path='p11/b21/foreign.pdf');
grant select on p11_b21_ids to authenticated;

-- The pure boundary helper documents the inclusive 31.12. and exclusive 01.01.
do $$ begin
  if public.former_member_history_access_until(date '2026-06-30') <> date '2027-12-31'
     or date '2027-12-31' > public.former_member_history_access_until(date '2026-06-30')
     or date '2028-01-01' <= public.former_member_history_access_until(date '2026-06-30') then
    raise exception 'P11 B21 R05 year-boundary calculation is incorrect';
  end if;
end $$;

set local role authenticated;
do $$
declare v p11_b21_ids%rowtype;
begin
  select * into v from p11_b21_ids;
  perform set_config('request.jwt.claim.role','authenticated',true);

  perform set_config('request.jwt.claim.sub','00000000-0000-0000-0000-00000000b211',true);
  if not exists(select 1 from public.dokument where id=v.active_document_id)
     or not public.can_access_own_member_history_document(v.active_document_id) then
    raise exception 'P11 B21 active own document is not readable';
  end if;

  perform set_config('request.jwt.claim.sub','00000000-0000-0000-0000-00000000b212',true);
  if not exists(select 1 from public.dokument where id=v.grace_document_id)
     or not public.can_access_own_member_history_document(v.grace_document_id) then
    raise exception 'P11 B21 ended member within the R05 period is not readable';
  end if;
  if exists(select 1 from public.dokument where id=v.foreign_document_id)
     or public.can_access_own_member_history_document(v.foreign_document_id) then
    raise exception 'P11 B21 foreign document or manipulated document id was accepted';
  end if;

  perform set_config('request.jwt.claim.sub','00000000-0000-0000-0000-00000000b213',true);
  if exists(select 1 from public.dokument where id=v.expired_document_id)
     or public.can_access_own_member_history_document(v.expired_document_id) then
    raise exception 'P11 B21 expired former-member history remained readable';
  end if;
  begin
    update public.dokument set titel='P11 B21 unauthorized write' where id=v.expired_document_id;
    if found then raise exception 'P11 B21 expired member changed a document'; end if;
  exception when sqlstate '42501' then null;
  end;

  perform set_config('request.jwt.claim.sub','00000000-0000-0000-0000-00000000b215',true);
  if not exists(select 1 from public.dokument where id=v.expired_document_id) then
    raise exception 'P11 B21 effective document manager lost administrative read access';
  end if;

  perform set_config('request.jwt.claim.sub','00000000-0000-0000-0000-00000000b216',true);
  if exists(select 1 from public.dokument where id=v.expired_document_id) then
    raise exception 'P11 B21 revoked administrator retained document read access';
  end if;

  perform set_config('request.jwt.claim.sub','00000000-0000-0000-0000-00000000b299',true);
  if exists(select 1 from public.dokument where id=v.active_document_id)
     or public.can_access_own_member_history_document(v.active_document_id) then
    raise exception 'P11 B21 missing member context was accepted';
  end if;
end;
$$;
reset role;

do $$
declare v p11_b21_ids%rowtype;
begin
  select * into v from p11_b21_ids;
  if (select titel from public.dokument where id=v.expired_document_id) <> 'P11 B21 expired' then
    raise exception 'P11 B21 denied operation left a document change';
  end if;
end;
$$;

rollback;
