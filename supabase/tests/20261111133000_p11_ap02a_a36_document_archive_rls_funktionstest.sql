-- P11 A3.6: synthetic local PostgreSQL/RLS test; all data rolls back.
begin;
insert into auth.users(id,aud,role,email,encrypted_password,email_confirmed_at,raw_app_meta_data,raw_user_meta_data,created_at,updated_at) values
 ('00000000-0000-0000-0000-000000001a61','authenticated','authenticated','p11-a36-manage@example.test','x',now(),'{}','{}',now(),now()),
 ('00000000-0000-0000-0000-000000001a62','authenticated','authenticated','p11-a36-none@example.test','x',now(),'{}','{}',now(),now()),
 ('00000000-0000-0000-0000-000000001a63','authenticated','authenticated','p11-a36-revoked@example.test','x',now(),'{}','{}',now(),now());
insert into public.mitglied(name,vorname,aktiv,aufnahme_status,auth_user_id) values('P11 A36 Owner','Test',true,'aufgenommen','00000000-0000-0000-0000-000000001a61') returning id as owner_id \gset
insert into public.mitglied(name,vorname,aktiv,aufnahme_status) values('P11 A36 Foreign','Test',true,'aufgenommen') returning id as foreign_id \gset
update public.app_user set mitglied_id=:'owner_id',role='user',permission_grants=16,permission_revocations=0 where user_id='00000000-0000-0000-0000-000000001a61';
insert into public.app_user(user_id,role,permission_grants,permission_revocations) values('00000000-0000-0000-0000-000000001a62','user',0,0),('00000000-0000-0000-0000-000000001a63','admin',0,16) on conflict(user_id) do update set role=excluded.role,permission_grants=excluded.permission_grants,permission_revocations=excluded.permission_revocations;
set local session_replication_role=replica;
insert into public.dokument(mitglied_id,bucket,storage_path,titel,dateiname,mime_type) values
(:'owner_id','dokumente','p11/a36/own.pdf','P11 A36 Own','own.pdf','application/pdf'),
(:'foreign_id','dokumente','p11/a36/foreign.pdf','P11 A36 Foreign','foreign.pdf','application/pdf'),
(:'owner_id','dokumente','p11/a36/history.pdf','P11 A36 History','history.pdf','application/pdf');
update public.dokument set source_kommunikationsvorgang_id=1,source_revision=1 where storage_path='p11/a36/history.pdf';
set local session_replication_role=origin;
create temporary table p11_a36(id bigint,foreign_id bigint,history_id bigint) on commit drop;
insert into p11_a36 select max(id) filter(where storage_path='p11/a36/own.pdf'),max(id) filter(where storage_path='p11/a36/foreign.pdf'),max(id) filter(where storage_path='p11/a36/history.pdf') from public.dokument where storage_path like 'p11/a36/%';
grant select on p11_a36 to authenticated,service_role;
set local role authenticated; select set_config('request.jwt.claim.sub','00000000-0000-0000-0000-000000001a61',true); select set_config('request.jwt.claim.role','authenticated',true);
do $$ declare x p11_a36%rowtype; begin select * into x from p11_a36; update public.dokument set titel='P11 A36 Updated' where id=x.id; begin update public.dokument set archiviert_at=now(),archiviert_by='00000000-0000-0000-0000-000000001a61',archiviert_begruendung='P11 A36' where id=x.id; raise exception 'direct archive succeeded'; exception when sqlstate '42501' then null; end; begin update public.dokument set storage_path='p11/a36/changed.pdf' where id=x.history_id; raise exception 'history mutation succeeded'; exception when sqlstate '55000' then null; end; end $$;
select set_config('request.jwt.claim.sub','00000000-0000-0000-0000-000000001a62',true); do $$ declare x p11_a36%rowtype; begin select * into x from p11_a36; if exists(select 1 from public.dokument where id=x.foreign_id) then raise exception 'foreign document visible without right'; end if; end $$;
select set_config('request.jwt.claim.sub','00000000-0000-0000-0000-000000001a63',true); do $$ declare x p11_a36%rowtype; begin select * into x from p11_a36; begin update public.dokument set titel='bad' where id=x.id; if found then raise exception 'revoked admin write succeeded'; end if; exception when sqlstate '42501' then null; end; end $$;
reset role; set local role service_role; update public.dokument set archiviert_at=now(),archiviert_by='00000000-0000-0000-0000-000000001a61',archiviert_begruendung='P11 A36' where id=(select id from p11_a36); reset role;
do $$ declare x p11_a36%rowtype; begin select * into x from p11_a36; begin update public.dokument set titel='after archive' where id=x.id; raise exception 'archived document changed'; exception when sqlstate '55000' then null; end; end $$;
rollback;
