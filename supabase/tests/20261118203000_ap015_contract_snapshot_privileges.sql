-- AP-01.5/F1 regression: run AFTER the migration on a local or DemoDB.
-- Read-only checks: negative function calls are rejected at EXECUTE permission
-- checking; no live/synthetic contract workflow data is changed.
begin read only;

-- Check the exact access matrix, not merely visible grants (PUBLIC inheritance).
do $check$
begin
  if has_function_privilege('anon',
    'public.ensure_vertragsversand_snapshot(bigint)', 'EXECUTE') then
    raise exception 'AP015: anon may execute internal snapshot RPC';
  end if;
  if has_function_privilege('authenticated',
    'public.ensure_vertragsversand_snapshot(bigint)', 'EXECUTE') then
    raise exception 'AP015: authenticated may execute internal snapshot RPC';
  end if;
  if not has_function_privilege('service_role',
    'public.ensure_vertragsversand_snapshot(bigint)', 'EXECUTE') then
    raise exception 'AP015: service_role cannot execute internal snapshot RPC';
  end if;
  if not has_function_privilege('postgres',
    'public.ensure_vertragsversand_snapshot(bigint)', 'EXECUTE') then
    raise exception 'AP015: postgres owner cannot execute internal snapshot RPC';
  end if;
end
$check$;

-- A valid direct API caller must be refused BEFORE contract lookup.
set local role anon;
do $test$
begin
  begin
    perform public.ensure_vertragsversand_snapshot(-2147483600);
    raise exception 'AP015: anonymous invocation unexpectedly succeeded';
  exception when insufficient_privilege then null;
  end;
end
$test$;
reset role;

set local role authenticated;
do $test$
begin
  begin
    perform public.ensure_vertragsversand_snapshot(-2147483600);
    raise exception 'AP015: authenticated invocation unexpectedly succeeded';
  exception when insufficient_privilege then null;
  end;
end
$test$;
reset role;

rollback;
