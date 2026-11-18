-- AP-01.4 follow-up: real PostgreSQL RLS simulation of PostgREST
-- Prefer: return=representation and the immediately following workflow reads.
-- Synthetic rows only; the whole test is rolled back.
begin;

insert into auth.users(
  id, aud, role, email, encrypted_password, email_confirmed_at,
  raw_app_meta_data, raw_user_meta_data, created_at, updated_at
) values
  ('00000000-0000-0000-0000-000000014101','authenticated','authenticated','ap014-creator@example.test','x',now(),'{}','{}',now(),now()),
  ('00000000-0000-0000-0000-000000014102','authenticated','authenticated','ap014-other-creator@example.test','x',now(),'{}','{}',now(),now());

insert into public.app_user(user_id, role, permission_grants, permission_revocations)
values
  ('00000000-0000-0000-0000-000000014101','user',524288,0),
  ('00000000-0000-0000-0000-000000014102','user',524288,0)
on conflict(user_id) do update set
  mitglied_id=null,
  role=excluded.role,
  permission_grants=excluded.permission_grants,
  permission_revocations=excluded.permission_revocations;

create temporary table ap014_created_member_ids(
  key text primary key,
  id bigint not null
) on commit drop;
grant select, insert on ap014_created_member_ids to authenticated;

set local role authenticated;
select set_config('request.jwt.claim.role','authenticated',true);
select set_config('request.jwt.claim.sub','00000000-0000-0000-0000-000000014101',true);

-- INSERT ... RETURNING exercises the same RLS combination as PostgREST
-- return=representation. It failed with 42501 before this migration.
with inserted as (
  insert into public.mitglied(
    name, vorname, aktiv, aufnahme_status, email, adresse, plz, ort
  ) values (
    'AP014 Created','Test',false,'antrag_offen','ap014-created@example.test',
    'Erstellerweg 1','14101','Erstellerort'
  )
  returning id, erstellt_von_auth_user_id
)
insert into ap014_created_member_ids(key, id)
select 'creator', id
from inserted
where erstellt_von_auth_user_id='00000000-0000-0000-0000-000000014101';

do $test$
declare v_count integer;
begin
  select count(*) into v_count from ap014_created_member_ids where key='creator';
  if v_count <> 1 then
    raise exception 'AP-01.4 creator INSERT RETURNING did not return its bound member row';
  end if;

  select count(*) into v_count
  from public.mitglied
  where id=(select id from ap014_created_member_ids where key='creator');
  if v_count <> 1 then
    raise exception 'AP-01.4 creator could not reload the newly created member';
  end if;

  select count(*) into v_count
  from public.v_mitglied_mitgliedsantrag_nachweisstatus
  where mitglied_id=(select id from ap014_created_member_ids where key='creator');
  if v_count <> 1 then
    raise exception 'AP-01.4 membership-application view could not load the newly created member';
  end if;
end
$test$;

-- A second creator's row remains foreign even though both actors have the
-- identical CanCreateMitglied grant.
select set_config('request.jwt.claim.sub','00000000-0000-0000-0000-000000014102',true);
with inserted as (
  insert into public.mitglied(name, vorname, aktiv, aufnahme_status, email)
  values ('AP014 Other Created','Test',false,'antrag_offen','ap014-other-created@example.test')
  returning id
)
insert into ap014_created_member_ids(key, id)
select 'other', id from inserted;

select set_config('request.jwt.claim.sub','00000000-0000-0000-0000-000000014101',true);
do $test$
begin
  if exists (
    select 1 from public.mitglied
    where id=(select id from ap014_created_member_ids where key='other')
  ) then
    raise exception 'AP-01.4 CanCreateMitglied exposed another creator''s member row';
  end if;
end
$test$;

-- Creation does not imply edit-all. The existing UPDATE policy remains the
-- authority for later changes.
update public.mitglied
set name='AP014 Illicit Update'
where id=(select id from ap014_created_member_ids where key='creator');

reset role;
do $test$
declare v_name text;
begin
  select name into v_name from public.mitglied
  where id=(select id from ap014_created_member_ids where key='creator');
  if v_name <> 'AP014 Created' then
    raise exception 'AP-01.4 creator received unintended edit-all access';
  end if;
end
$test$;

-- An individual revocation wins immediately, including for creator scope.
update public.app_user
set permission_revocations = permission_revocations | 524288
where user_id='00000000-0000-0000-0000-000000014101';

set local role authenticated;
select set_config('request.jwt.claim.role','authenticated',true);
select set_config('request.jwt.claim.sub','00000000-0000-0000-0000-000000014101',true);
do $test$
begin
  if exists (
    select 1 from public.mitglied
    where id=(select id from ap014_created_member_ids where key='creator')
  ) then
    raise exception 'AP-01.4 CanCreateMitglied revocation did not remove creator scope';
  end if;
end
$test$;

reset role;
rollback;
