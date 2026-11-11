-- P11 A4.2: ACL-only regression; no persistent test data.
begin;
do $$
begin
  if has_function_privilege('authenticated','public.reserve_fachnummer(text,text,bigint)','execute') then
    raise exception 'reserve_fachnummer remains directly executable by authenticated';
  end if;
  if not has_function_privilege('service_role','public.reserve_fachnummer(text,text,bigint)','execute') then
    raise exception 'service_role lost internal reservation execution';
  end if;
  if not has_function_privilege('authenticated','public.reserve_historische_vertragsnummer(bigint)','execute') then
    raise exception 'authorized historical reservation RPC lost its caller grant';
  end if;
end;
$$;
set local role authenticated;
do $$
begin
  begin
    perform public.reserve_fachnummer('wartungsvertrag','wartungsvertrag',424201);
    raise exception 'direct client reservation succeeded';
  exception when sqlstate '42501' then null;
  end;
end;
$$;
reset role;
rollback;
