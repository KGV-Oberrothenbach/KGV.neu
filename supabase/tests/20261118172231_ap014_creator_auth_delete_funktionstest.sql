-- AP-01.4: creator provenance must not block deletion of auth users and must
-- remain immutable through direct authenticated table/REST-equivalent writes.
-- Synthetic rows only; the whole test is rolled back.
begin;

insert into auth.users(
  id, aud, role, email, encrypted_password, email_confirmed_at,
  raw_app_meta_data, raw_user_meta_data, created_at, updated_at
) values
  ('00000000-0000-0000-0000-000000014201','authenticated','authenticated','ap014-delete-creator@example.test','x',now(),'{}','{}',now(),now()),
  ('00000000-0000-0000-0000-000000014202','authenticated','authenticated','ap014-delete-foreign@example.test','x',now(),'{}','{}',now(),now()),
  ('00000000-0000-0000-0000-000000014203','authenticated','authenticated','ap014-delete-editor@example.test','x',now(),'{}','{}',now(),now());

insert into public.mitglied(
  name, vorname, aktiv, aufnahme_status, email, erstellt_von_auth_user_id
) values (
  'AP014 Creator Delete','Test',false,'antrag_offen',
  'ap014-delete-member@example.test','00000000-0000-0000-0000-000000014201'
);

create temporary table ap014_creator_delete_member_id(id bigint primary key) on commit drop;
insert into ap014_creator_delete_member_id(id)
select id from public.mitglied where email='ap014-delete-member@example.test';
grant select on ap014_creator_delete_member_id to authenticated;

-- The editor deliberately has effective CanEditAllMembers. RLS therefore
-- permits the UPDATE, leaving the trigger as the creator-column protection.
insert into public.app_user(user_id, role, permission_grants, permission_revocations)
values ('00000000-0000-0000-0000-000000014203','user',4,0)
on conflict(user_id) do update set
  mitglied_id=null,
  role=excluded.role,
  permission_grants=excluded.permission_grants,
  permission_revocations=excluded.permission_revocations;

set local role authenticated;
select set_config('request.jwt.claim.role','authenticated',true);
select set_config('request.jwt.claim.sub','00000000-0000-0000-0000-000000014203',true);

update public.mitglied
set erstellt_von_auth_user_id='00000000-0000-0000-0000-000000014202'
where id=(select id from ap014_creator_delete_member_id);

do $test$
declare v_creator uuid;
begin
  select erstellt_von_auth_user_id into v_creator
  from public.mitglied
  where id=(select id from ap014_creator_delete_member_id);
  if v_creator is distinct from '00000000-0000-0000-0000-000000014201'::uuid then
    raise exception 'AP-01.4 authenticated update reassigned creator to %', v_creator;
  end if;
end
$test$;

update public.mitglied
set erstellt_von_auth_user_id=null
where id=(select id from ap014_creator_delete_member_id);

do $test$
declare v_creator uuid;
begin
  select erstellt_von_auth_user_id into v_creator
  from public.mitglied
  where id=(select id from ap014_creator_delete_member_id);
  if v_creator is distinct from '00000000-0000-0000-0000-000000014201'::uuid then
    raise exception 'AP-01.4 authenticated update cleared creator to %', v_creator;
  end if;
end
$test$;

reset role;

-- The FK action now reaches NULL instead of being undone by the BEFORE trigger.
delete from auth.users
where id='00000000-0000-0000-0000-000000014201';

do $test$
declare v_creator uuid; v_count integer;
begin
  select count(*) into v_count
  from public.mitglied
  where id=(select id from ap014_creator_delete_member_id);
  if v_count <> 1 then
    raise exception 'AP-01.4 auth deletion unexpectedly deleted the member row';
  end if;
  select erstellt_von_auth_user_id into v_creator
  from public.mitglied
  where id=(select id from ap014_creator_delete_member_id);
  if v_creator is not null then
    raise exception 'AP-01.4 auth deletion left creator binding % instead of NULL', v_creator;
  end if;
  if exists (
    select 1 from auth.users
    where id='00000000-0000-0000-0000-000000014201'
  ) then
    raise exception 'AP-01.4 creator auth user was not deleted';
  end if;
end
$test$;

rollback;
