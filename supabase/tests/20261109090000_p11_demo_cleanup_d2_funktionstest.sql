-- D2 compatibility test after the final D5/D6 schema transition.
-- Project separation replaces row markers; effective rights remain decisive.
begin;

do $$
begin
  if exists (
    select 1 from information_schema.columns
     where table_schema = 'public'
       and column_name in ('is_demo', 'is_demo_account', 'allow_demo_access')
  ) then
    raise exception 'D2 final contract still contains a row-level project marker';
  end if;

  if to_regprocedure('public.reserve_fachnummer(text,text,bigint)') is null
     or to_regprocedure('public.reserve_fachnummer(text,text,bigint,boolean)') is not null then
    raise exception 'D2 final number contract is not project-local';
  end if;

  if exists (
    select 1 from pg_policies
     where schemaname in ('public', 'storage')
       and (coalesce(qual, '') ilike '%demo%'
            or coalesce(with_check, '') ilike '%demo%')
  ) then
    raise exception 'D2 final RLS contract still decides by a row-level project marker';
  end if;
end;
$$;

insert into public.app_user(user_id, role, permission_grants, permission_revocations)
values
  ('00000000-0000-0000-0000-00000000d201', 'user', 524288, 0),
  ('00000000-0000-0000-0000-00000000d202', 'admin', 0, 524288);

set local role authenticated;
select set_config('request.jwt.claim.role', 'authenticated', true);
select set_config('request.jwt.claim.sub', '00000000-0000-0000-0000-00000000d201', true);
do $$
begin
  if not public.has_effective_permission(524288) then
    raise exception 'D2 individual permission grant is ineffective';
  end if;
end;
$$;

select set_config('request.jwt.claim.sub', '00000000-0000-0000-0000-00000000d202', true);
do $$
begin
  if public.has_effective_permission(524288) then
    raise exception 'D2 individual revocation did not override admin role basis';
  end if;
end;
$$;

reset role;
select set_config('request.jwt.claim.role', 'service_role', true);
do $$
declare
  v_first integer;
  v_second integer;
begin
  v_first := public.reserve_fachnummer(
    'mitgliedsantrag', 'kommunikationsvorgang', 92001
  );
  v_second := public.reserve_fachnummer(
    'mitgliedsantrag', 'kommunikationsvorgang', 92001
  );
  if v_first is distinct from v_second then
    raise exception 'D2 project-local number reservation is not idempotent';
  end if;
end;
$$;

rollback;
