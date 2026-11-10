-- P11 AP-02A / A3.1: Starting a prepared contract signature is part of the
-- membership/lease contract workflow and therefore requires CanCreateMitglied.
begin;
do $$
declare
  v_definition text;
  v_old text := 'if auth.uid() is null or auth.uid() is distinct from p_assigned_to or not public.is_admin_or_vorstand() then';
  v_new text := 'if auth.uid() is null or auth.uid() is distinct from p_assigned_to or not public.has_effective_permission(524288) then';
begin
  select pg_get_functiondef('public.start_prepared_contract_signature(bigint,integer,uuid)'::regprocedure) into v_definition;
  if position(v_old in v_definition) = 0 then
    raise exception 'P11 A3.1: expected role-template guard in start_prepared_contract_signature was not found' using errcode = '2BP01';
  end if;
  v_definition := replace(v_definition, v_old, v_new);
  execute v_definition;
  if position(v_new in pg_get_functiondef('public.start_prepared_contract_signature(bigint,integer,uuid)'::regprocedure)) = 0 then
    raise exception 'P11 A3.1: effective CanCreateMitglied guard was not applied' using errcode = '2BP01';
  end if;
end;
$$;
comment on function public.start_prepared_contract_signature(bigint,integer,uuid) is
  'P11 AP-02A A3.1: requires effective CanCreateMitglied (524288), caller-to-assignment binding and immutable signature snapshot/revision checks.';
commit;
