-- P11 Demo-Bereinigung D5: final physical schema and security contract.
-- Every synthetic change is rolled back.
begin;

do $$
declare
  v_leftovers text;
begin
  select string_agg(kind || ' ' || object_name, E'\n' order by kind, object_name)
    into v_leftovers
  from (
    select 'column' kind, table_schema || '.' || table_name || '.' || column_name object_name
      from information_schema.columns
     where table_schema = 'public' and column_name ilike '%demo%'
    union all
    select 'function', p.oid::regprocedure::text
      from pg_proc p join pg_namespace n on n.oid = p.pronamespace
     where n.nspname = 'public' and p.prokind = 'f'
       and (p.proname ilike '%demo%' or pg_get_functiondef(p.oid) ilike '%demo%')
    union all
    select 'policy', schemaname || '.' || tablename || '.' || policyname
      from pg_policies
     where schemaname in ('public', 'storage')
       and (policyname ilike '%demo%' or coalesce(qual, '') ilike '%demo%'
            or coalesce(with_check, '') ilike '%demo%')
    union all
    select 'view', schemaname || '.' || viewname
      from pg_views
     where schemaname = 'public' and definition ilike '%demo%'
    union all
    select 'trigger', n.nspname || '.' || c.relname || '.' || t.tgname
      from pg_trigger t
      join pg_class c on c.oid = t.tgrelid
      join pg_namespace n on n.oid = c.relnamespace
     where not t.tgisinternal and n.nspname = 'public'
       and (t.tgname ilike '%demo%' or pg_get_triggerdef(t.oid) ilike '%demo%')
    union all
    select 'constraint', n.nspname || '.' || c.relname || '.' || con.conname
      from pg_constraint con
      join pg_class c on c.oid = con.conrelid
      join pg_namespace n on n.oid = c.relnamespace
     where n.nspname = 'public'
       and (con.conname ilike '%demo%' or pg_get_constraintdef(con.oid) ilike '%demo%')
    union all
    select 'index', schemaname || '.' || indexname
      from pg_indexes
     where schemaname = 'public'
       and (indexname ilike '%demo%' or indexdef ilike '%demo%')
  ) leftovers;

  if v_leftovers is not null then
    raise exception 'D5 active Demo schema objects remain:%', E'\n' || v_leftovers;
  end if;

  if to_regprocedure('public.reserve_fachnummer(text,text,bigint)') is null
     or to_regprocedure('public.reserve_fachnummer(text,text,bigint,boolean)') is not null then
    raise exception 'D5 Fachnummer RPC contract is not the D4 three-argument contract';
  end if;

  if to_regprocedure('public.rpc_export_mitgliederliste(text,boolean)') is null
     or to_regprocedure('public.rpc_export_rfid_status(text,text)') is null then
    raise exception 'D5 removed a legacy export overload';
  end if;
  if pg_get_functiondef('public.rpc_export_mitgliederliste(text,boolean)'::regprocedure) ilike '%is_demo%'
     or pg_get_functiondef('public.rpc_export_rfid_status(text,text)'::regprocedure) ilike '%is_demo%' then
    raise exception 'D5 legacy export overload still contains a Demo filter';
  end if;

  if not exists (
    select 1 from information_schema.columns
     where table_schema = 'public' and table_name = 'app_user'
       and column_name = 'is_protected'
  ) then
    raise exception 'D5 removed is_protected';
  end if;
end;
$$;

-- Exercise both retained overloads against the real rebuilt schema. LIMIT 0
-- still plans and resolves every referenced table/column without exporting
-- member data from the fixture.
select * from public.rpc_export_mitgliederliste('name',null) limit 0;
select * from public.rpc_export_rfid_status(null,'alle') limit 0;

-- Reproduce the additional physical columns found in the historical DemoDB
-- and prove that the guarded D5 operation removes only the column, not rows.
do $$
declare
  v_table text;
  v_before bigint;
  v_after bigint;
begin
  foreach v_table in array array[
    'arbeitseinsatz_anmeldung','arbeitsstunde','client_diagnostics_log','dokument',
    'impressum_funktion_slot','mitglied_gesetzlicher_vertreter','mitglied_saison',
    'parzellen_belegung','vereinskonfiguration','wartungsvertrag_zuordnungen',
    'zaehler','zaehler_ablesung'
  ] loop
    if to_regclass(format('public.%I',v_table)) is null then
      continue;
    end if;
    execute format('select count(*) from public.%I',v_table) into v_before;
    execute format('alter table public.%I add column is_demo boolean',v_table);
    execute format('alter table public.%I drop column is_demo',v_table);
    execute format('select count(*) from public.%I',v_table) into v_after;
    if v_after is distinct from v_before then
      raise exception 'D5 historical column removal changed rows in %',v_table;
    end if;
  end loop;
end;
$$;

insert into public.mitglied(id, name, vorname, aktiv, aufnahme_status)
values
  (95101, 'D5 Eigen', 'Mitglied', true, 'aufgenommen'),
  (95102, 'D5 Fremd', 'Mitglied', true, 'aufgenommen');

insert into public.app_user(
  user_id, mitglied_id, role, permission_grants, permission_revocations, is_protected
) values
  ('00000000-0000-0000-0000-00000000d501', 95101, 'user', 524288, 0, true),
  ('00000000-0000-0000-0000-00000000d502', null, 'user', 0, 0, false),
  ('00000000-0000-0000-0000-00000000d503', null, 'admin', 0, 524288, false);

set local role authenticated;
select set_config('request.jwt.claim.role', 'authenticated', true);

select set_config('request.jwt.claim.sub', '00000000-0000-0000-0000-00000000d501', true);
do $$
declare
  v_context record;
begin
  select * into strict v_context from public.current_app_user_context();
  if v_context.user_id <> '00000000-0000-0000-0000-00000000d501'::uuid
     or v_context.mitglied_id <> 95101
     or v_context.role <> 'user'
     or v_context.permission_grants <> 524288
     or v_context.permission_revocations <> 0
     or v_context.is_protected is distinct from true then
    raise exception 'D5 current_app_user_context returned an invalid final contract';
  end if;
  if not public.has_effective_permission(524288) then
    raise exception 'D5 individual permission grant is ineffective';
  end if;
  begin
    perform public.sign_up_for_arbeitseinsatz(-1, 95102);
    raise exception 'D5 membership ownership protection was bypassed';
  exception when sqlstate '42501' then null;
  end;
end;
$$;

select set_config('request.jwt.claim.sub', '00000000-0000-0000-0000-00000000d502', true);
do $$
begin
  if public.has_effective_permission(524288) then
    raise exception 'D5 user without permission received CanCreateMitglied';
  end if;
  begin
    perform public.reserve_fachnummer('mitgliedsantrag', 'kommunikationsvorgang', 95102);
    raise exception 'D5 direct unauthorized RPC call was accepted';
  exception when sqlstate '42501' then null;
  end;
end;
$$;

select set_config('request.jwt.claim.sub', '00000000-0000-0000-0000-00000000d503', true);
do $$
begin
  if public.has_effective_permission(524288) then
    raise exception 'D5 individual revocation did not override admin role basis';
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
  v_first := public.reserve_fachnummer('mitgliedsantrag', 'kommunikationsvorgang', 95103);
  v_second := public.reserve_fachnummer('mitgliedsantrag', 'kommunikationsvorgang', 95103);
  if v_first is distinct from v_second or v_first not between 10000 and 19999 then
    raise exception 'D5 Fachnummer reservation is not idempotent or outside 10xxx';
  end if;
end;
$$;

rollback;
