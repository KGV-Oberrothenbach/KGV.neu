-- P11 Demo-Bereinigung D2: project-neutral database scope and effective rights.
begin;

create temporary table p11_d2_ids (
  granted_user uuid not null,
  revoked_admin uuid not null
) on commit drop;

insert into p11_d2_ids values
  ('00000000-0000-0000-0000-00000000d201'::uuid,
   '00000000-0000-0000-0000-00000000d202'::uuid);

insert into public.app_user(user_id, role, permission_grants, permission_revocations, is_demo_account)
select granted_user, 'user', 2097152, 0, true from p11_d2_ids
union all
select revoked_admin, 'admin', 0, 2097152, true from p11_d2_ids;

insert into public.auth_allowlist(email, allow_demo_access)
values ('p11-d2-project-neutral@example.test', true)
on conflict (email) do update set allow_demo_access = excluded.allow_demo_access;

insert into public.mitglied(name, vorname, aktiv, aufnahme_status, is_demo)
values ('D2 Projektneutral', 'Test', true, 'aufgenommen', true);

do $$
begin
  if exists (select 1 from public.app_user where is_demo_account) then
    raise exception 'D2 app_user compatibility trigger did not neutralize is_demo_account';
  end if;
  if exists (select 1 from public.auth_allowlist where email='p11-d2-project-neutral@example.test' and allow_demo_access) then
    raise exception 'D2 allowlist compatibility trigger did not neutralize allow_demo_access';
  end if;
  if exists (select 1 from public.mitglied where name='D2 Projektneutral' and vorname='Test' and is_demo) then
    raise exception 'D2 entity compatibility trigger did not neutralize is_demo';
  end if;
  if public.is_demo_user() or public.is_playstore_reviewer() or public.is_demo_or_reviewer() then
    raise exception 'D2 legacy actor helpers still classify a project-local user';
  end if;
  if not public.can_access_demo_scope() then
    raise exception 'D2 compatibility access helper must not restrict project-local data';
  end if;
  if (select count(*) from pg_constraint where conname like '%_d2_project_neutral_demo_check') <> 10 then
    raise exception 'D2 project-neutral database invariants are incomplete';
  end if;
  if exists (
    select 1 from pg_policies
    where schemaname='public'
      and (coalesce(qual,'') ilike '%demo%' or coalesce(with_check,'') ilike '%demo%')
  ) then
    raise exception 'D2 left a Demo-dependent RLS policy active';
  end if;
end;
$$;

-- The constraint must remain effective even when a maintenance session skips
-- user triggers. This proves that legacy function bodies cannot observe a
-- project-internal Demo/Echt distinction.
set local session_replication_role = replica;
do $$
begin
  begin
    update public.mitglied
       set is_demo = true
     where name = 'D2 Projektneutral' and vorname = 'Test';
    raise exception 'D2 project-neutral constraint allowed is_demo=true';
  exception when sqlstate '23514' then null;
  end;
end;
$$;
set local session_replication_role = origin;

do $$
declare
  v_first integer;
  v_second integer;
begin
  perform set_config('request.jwt.claim.role', 'service_role', true);
  v_first := public.reserve_fachnummer('mitgliedsantrag', 'kommunikationsvorgang', 92001);
  v_second := public.reserve_fachnummer('mitgliedsantrag', 'kommunikationsvorgang', 92001);
  if v_first is distinct from v_second then
    raise exception 'D2 project-local number reservation is not idempotent';
  end if;
  if not exists (
    select 1 from public.fachnummer_reservierung
    where quelle_art = 'kommunikationsvorgang' and quelle_id = 92001
      and is_demo = false and nummer = v_first
  ) or exists (
    select 1 from public.fachnummer_reservierung
    where quelle_art = 'kommunikationsvorgang' and quelle_id = 92001 and is_demo = true
  ) then
    raise exception 'D2 number reservation retained a Demo namespace';
  end if;
end;
$$;

grant select on p11_d2_ids to authenticated;

set local role authenticated;
do $$
declare
  v_granted uuid;
  v_revoked uuid;
  v_is_demo boolean;
begin
  select granted_user, revoked_admin into v_granted, v_revoked from p11_d2_ids;

  perform set_config('request.jwt.claim.sub', v_granted::text, true);
  if not public.has_effective_permission(2097152) then
    raise exception 'D2 individual appointment grant is not effective';
  end if;
  insert into public.termin(titel, datum, is_demo)
  values ('D2 Projektneutral', current_date + 1, true)
  returning is_demo into v_is_demo;
  if v_is_demo then
    raise exception 'D2 appointment insert retained a Demo marker';
  end if;

  perform set_config('request.jwt.claim.sub', v_revoked::text, true);
  if public.has_effective_permission(2097152) then
    raise exception 'D2 individual revocation did not override admin role base';
  end if;
  begin
    insert into public.termin(titel, datum, is_demo)
    values ('D2 Entzug', current_date + 1, false);
    raise exception 'D2 revoked admin unexpectedly created an appointment';
  exception when sqlstate '42501' then null;
  end;
end;
$$;
reset role;

rollback;
