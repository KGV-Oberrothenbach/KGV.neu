do $migration$
declare
  v_demo_admin_user_id uuid;
  v_is_demo_account boolean;
  v_is_protected boolean;
  v_eligible_count integer := 0;
begin
  if exists (
    select 1
    from public.mitglied
    where id = 7
  ) then
    return;
  end if;

  for v_demo_admin_user_id, v_is_demo_account, v_is_protected in
    select au.user_id, au.is_demo_account, au.is_protected
    from public.app_user au
    join public.mitglied m
      on m.id = au.mitglied_id
     and m.auth_user_id = au.user_id
    where m.id = 910001
      and au.mitglied_id = 910001
      and au.role = 'admin'
    for update of au, m
  loop
    v_eligible_count := v_eligible_count + 1;
    if v_eligible_count > 1 then
      return;
    end if;
  end loop;

  if v_eligible_count <> 1 then
    return;
  end if;

  if v_is_demo_account and v_is_protected then
    return;
  end if;

  if exists (
    select 1
    from public.app_user au
    where au.is_protected = true
      and au.user_id <> v_demo_admin_user_id
  ) then
    raise exception 'Cannot protect DEMO main administrator 910001: another protected app_user already exists.'
      using errcode = '23505';
  end if;

  update public.app_user
  set is_demo_account = true,
      is_protected = true
  where user_id = v_demo_admin_user_id
    and (is_demo_account is distinct from true or is_protected is distinct from true);
end;
$migration$;
