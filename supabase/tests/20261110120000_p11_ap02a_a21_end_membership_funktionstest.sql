-- P11 AP-02A / A2.1: real local PostgreSQL acceptance for end_membership.
-- Synthetic fixture only; every change is rolled back.
begin;

create temporary table p11_a21_end_membership_ids (
  granted_user uuid not null,
  denied_user uuid not null,
  revoked_admin_user uuid not null,
  granted_main_member bigint not null,
  granted_secondary_member bigint not null,
  denied_main_member bigint not null,
  revoked_main_member bigint not null
) on commit drop;

insert into public.mitglied(name, vorname, aktiv, aufnahme_status, lockedbyuserid, lockat)
values
  ('P11 A2.1 Granted main', 'Test', true, 'aufgenommen', '00000000-0000-0000-0000-000000000a21', now() at time zone 'UTC'),
  ('P11 A2.1 Granted secondary', 'Test', true, 'aufgenommen', null, null),
  ('P11 A2.1 Denied main', 'Test', true, 'aufgenommen', '00000000-0000-0000-0000-000000000a22', now() at time zone 'UTC'),
  ('P11 A2.1 Revoked main', 'Test', true, 'aufgenommen', '00000000-0000-0000-0000-000000000a23', now() at time zone 'UTC');

update public.mitglied
set hauptmitglied_id = (select id from public.mitglied where name = 'P11 A2.1 Granted main')
where name = 'P11 A2.1 Granted secondary';

insert into public.app_user(user_id, role, permission_grants, permission_revocations)
values
  ('00000000-0000-0000-0000-000000000a21', 'user', 4, 0),
  ('00000000-0000-0000-0000-000000000a22', 'user', 0, 0),
  ('00000000-0000-0000-0000-000000000a23', 'admin', 0, 4);

insert into p11_a21_end_membership_ids(
  granted_user, denied_user, revoked_admin_user,
  granted_main_member, granted_secondary_member, denied_main_member, revoked_main_member
)
select
  '00000000-0000-0000-0000-000000000a21'::uuid,
  '00000000-0000-0000-0000-000000000a22'::uuid,
  '00000000-0000-0000-0000-000000000a23'::uuid,
  (select id from public.mitglied where name = 'P11 A2.1 Granted main'),
  (select id from public.mitglied where name = 'P11 A2.1 Granted secondary'),
  (select id from public.mitglied where name = 'P11 A2.1 Denied main'),
  (select id from public.mitglied where name = 'P11 A2.1 Revoked main');

grant select on p11_a21_end_membership_ids to authenticated;

set local role authenticated;
do $$
declare
  v p11_a21_end_membership_ids%rowtype;
  v_result record;
begin
  select * into v from p11_a21_end_membership_ids;

  -- Individual grant succeeds and preserves the formal main/secondary outcome.
  perform set_config('request.jwt.claim.sub', v.granted_user::text, true);
  select * into v_result from public.end_membership(v.granted_main_member, 'promote_secondary', 'P11 A2.1 formeller Testgrund');
  if not coalesce(v_result.success, false) then
    raise exception 'P11 A2.1 positive CanEditAllMembers test did not return success';
  end if;

  -- A user without the right cannot end another membership via the direct RPC.
  perform set_config('request.jwt.claim.sub', v.denied_user::text, true);
  begin
    perform public.end_membership(v.denied_main_member, null, 'P11 A2.1 unberechtigt');
    raise exception 'P11 A2.1 denied-user test unexpectedly succeeded';
  exception when sqlstate '42501' then null;
  end;

  -- An individual revocation wins over the administrative role base.
  perform set_config('request.jwt.claim.sub', v.revoked_admin_user::text, true);
  begin
    perform public.end_membership(v.revoked_main_member, null, 'P11 A2.1 Entzug');
    raise exception 'P11 A2.1 revoked-admin test unexpectedly succeeded';
  exception when sqlstate '42501' then null;
  end;

  -- A permitted actor still cannot bypass the original formal-process checks.
  perform set_config('request.jwt.claim.sub', v.granted_user::text, true);
  begin
    perform public.end_membership(v.denied_main_member, null, 'P11 A2.1 fremde Sperre');
    raise exception 'P11 A2.1 foreign-lock test unexpectedly succeeded';
  exception when sqlstate '55P03' then null;
  end;
end;
$$;
reset role;

-- Assert the persisted state as postgres. The authenticated test actor has no
-- member-read RLS right, so this deliberately verifies the RPC result outside
-- of the caller's projection.
do $$
declare
  v p11_a21_end_membership_ids%rowtype;
begin
  select * into v from p11_a21_end_membership_ids;
  if not exists (select 1 from public.mitglied where id = v.granted_main_member and aktiv is false and mitglied_ende = public.kgv_local_now()::date)
     or not exists (select 1 from public.mitglied where id = v.granted_secondary_member and aktiv is true and mitglied_ende is null and hauptmitglied_id is null)
     or not exists (select 1 from public.mitglied where id = v.denied_main_member and aktiv is true and mitglied_ende is null)
     or not exists (select 1 from public.mitglied where id = v.revoked_main_member and aktiv is true and mitglied_ende is null) then
    raise exception 'P11 A2.1 membership-end state or denied-call rollback contract was not preserved';
  end if;
end;
$$;

rollback;
