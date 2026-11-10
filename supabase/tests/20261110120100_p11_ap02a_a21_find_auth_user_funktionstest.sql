-- P11 AP-02A / A2.1: real local PostgreSQL acceptance for auth-user lookup.
-- Synthetic fixture only; every change is rolled back.
begin;

insert into auth.users(id, aud, role, email, encrypted_password, email_confirmed_at, raw_app_meta_data, raw_user_meta_data, created_at, updated_at)
values
  ('00000000-0000-0000-0000-000000000a31', 'authenticated', 'authenticated', 'p11-a21-lookup-granted@example.test', 'x', now(), '{}', '{}', now(), now()),
  ('00000000-0000-0000-0000-000000000a32', 'authenticated', 'authenticated', 'p11-a21-lookup-denied@example.test', 'x', now(), '{}', '{}', now(), now()),
  ('00000000-0000-0000-0000-000000000a33', 'authenticated', 'authenticated', 'p11-a21-lookup-revoked@example.test', 'x', now(), '{}', '{}', now(), now()),
  ('00000000-0000-0000-0000-000000000a34', 'authenticated', 'authenticated', 'p11-a21-lookup-target@example.test', 'x', now(), '{}', '{}', now(), now());

insert into public.app_user(user_id, role, permission_grants, permission_revocations)
values
  ('00000000-0000-0000-0000-000000000a31', 'user', 512, 0),
  ('00000000-0000-0000-0000-000000000a32', 'user', 0, 0),
  ('00000000-0000-0000-0000-000000000a33', 'admin', 0, 512);

set local role authenticated;
do $$
declare
  v_found uuid;
begin
  -- An individually granted client caller can use the same protected lookup
  -- that backs account-link administration.
  perform set_config('request.jwt.claim.sub', '00000000-0000-0000-0000-000000000a31', true);
  select auth_user_id into v_found
  from public.find_auth_user_id_by_email('P11-A21-LOOKUP-TARGET@example.test');
  if v_found <> '00000000-0000-0000-0000-000000000a34'::uuid then
    raise exception 'P11 A2.1 positive CanManageRoles auth lookup failed';
  end if;

  perform set_config('request.jwt.claim.sub', '00000000-0000-0000-0000-000000000a32', true);
  begin
    perform public.find_auth_user_id_by_email('p11-a21-lookup-target@example.test');
    raise exception 'P11 A2.1 denied auth lookup unexpectedly succeeded';
  exception when sqlstate 'P0001' then null;
  end;

  perform set_config('request.jwt.claim.sub', '00000000-0000-0000-0000-000000000a33', true);
  begin
    perform public.find_auth_user_id_by_email('p11-a21-lookup-target@example.test');
    raise exception 'P11 A2.1 revoked-admin auth lookup unexpectedly succeeded';
  exception when sqlstate 'P0001' then null;
  end;
end;
$$;
reset role;

-- Service role remains a permitted technical caller, independent of an auth.uid.
set local role service_role;
select set_config('request.jwt.claim.role', 'service_role', true);
do $$
declare v_found uuid;
begin
  select auth_user_id into v_found
  from public.find_auth_user_id_by_email('p11-a21-lookup-target@example.test');
  if v_found <> '00000000-0000-0000-0000-000000000a34'::uuid then
    raise exception 'P11 A2.1 service-role auth lookup regression';
  end if;
end;
$$;
reset role;

rollback;
