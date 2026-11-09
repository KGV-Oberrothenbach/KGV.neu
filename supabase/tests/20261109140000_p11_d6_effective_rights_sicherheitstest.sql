-- P11 D6: final project-neutral rights and protected-account contract.
begin;

insert into public.app_user(
  user_id, role, permission_grants, permission_revocations, is_protected
) values
  ('00000000-0000-0000-0000-00000000d601', 'user', 512, 0, false),
  ('00000000-0000-0000-0000-00000000d602', 'user', 0, 0, false),
  ('00000000-0000-0000-0000-00000000d603', 'admin', 0, 512, false),
  ('00000000-0000-0000-0000-00000000d604', 'admin', 0, 0, true),
  ('00000000-0000-0000-0000-00000000d605', 'user', 0, 0, false);

set local role authenticated;
select set_config('request.jwt.claim.role', 'authenticated', true);

select set_config('request.jwt.claim.sub', '00000000-0000-0000-0000-00000000d602', true);
do $$
begin
  begin
    perform public.update_app_user_access(
      '00000000-0000-0000-0000-00000000d605', 'user', 0, 0
    );
    raise exception 'D6 user without CanManageRoles changed access';
  exception when sqlstate '42501' then null;
  end;
end;
$$;

select set_config('request.jwt.claim.sub', '00000000-0000-0000-0000-00000000d603', true);
do $$
begin
  if public.has_effective_permission(512) then
    raise exception 'D6 revocation did not override the admin role basis';
  end if;
  begin
    perform public.update_app_user_access(
      '00000000-0000-0000-0000-00000000d605', 'user', 0, 0
    );
    raise exception 'D6 revoked admin changed access';
  exception when sqlstate '42501' then null;
  end;
end;
$$;

select set_config('request.jwt.claim.sub', '00000000-0000-0000-0000-00000000d601', true);
do $$
begin
  if not public.has_effective_permission(512) then
    raise exception 'D6 individual CanManageRoles grant is ineffective';
  end if;

  perform public.update_app_user_access(
    '00000000-0000-0000-0000-00000000d605', 'user', 524288, 0
  );
  if not exists (
    select 1 from public.app_user
     where user_id = '00000000-0000-0000-0000-00000000d605'
       and permission_grants = 524288 and permission_revocations = 0
  ) then
    raise exception 'D6 authorized access update was not persisted';
  end if;

  begin
    perform public.update_app_user_access(
      '00000000-0000-0000-0000-00000000d604', 'admin', 0, 512
    );
    raise exception 'D6 protected account lost CanManageRoles';
  exception when sqlstate '42501' then null;
  end;
end;
$$;

reset role;

do $$
begin
  if exists (
    select 1
      from pg_proc p
      join pg_namespace n on n.oid = p.pronamespace
     where n.nspname = 'public'
       and p.prosecdef
       and p.proname in (
         'update_app_user_access',
         'current_app_user_context',
         'reserve_fachnummer',
         'prepare_pachtvertrag_ersatz',
         'prepare_pachtvertrag_ersatz_vor_phase9'
       )
       and not coalesce(p.proconfig, '{}'::text[]) && array[
         'search_path=public, pg_temp',
         'search_path=public,pg_temp'
       ]
  ) then
    raise exception 'D6 relevant SECURITY DEFINER function lacks a fixed search_path';
  end if;

  if has_function_privilege(
       'anon', 'public.update_app_user_access(uuid,text,bigint,bigint)', 'execute'
     ) then
    raise exception 'D6 anonymous role can execute role management';
  end if;
end;
$$;

rollback;
