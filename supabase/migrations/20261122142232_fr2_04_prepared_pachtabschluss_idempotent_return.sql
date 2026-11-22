-- FR2-04: explicit exit after RETURN QUERY in the completed-contract retry path.
-- PostgreSQL PL/pgSQL RETURN QUERY appends rows but DOES NOT exit the function.
-- Without RETURN the old function always raises the "no contract version" exception
-- even though the contract version was retrieved successfully.
--
-- Preserve the existing service_role check, actor authorization, argument checks,
-- object locks, RLS policy, function security and the finalized 20xxx record.
-- The exact original function text must match ONCE, otherwise abort atomically.
DO $fr204_pacht_retry$
DECLARE
  v_function regprocedure := 'public.complete_prepared_pachtvertrag_vor_b3(bigint,integer,uuid)'::regprocedure;
  v_definition text;
  v_old text := $old$    if found then return query select v_pachtverhaeltnis_id,v_fassung_id,true; end if;$old$;
  v_new text := $new$    if found then return query select v_pachtverhaeltnis_id,v_fassung_id,true; return; end if;$new$;
BEGIN
  v_definition := pg_get_functiondef(v_function);
  IF v_definition IS NULL
     OR (length(v_definition) - length(replace(v_definition, v_old, ''))) <> length(v_old)
     OR position(v_new IN v_definition) > 0 THEN
    RAISE EXCEPTION 'FR2-04: completed-contract retry baseline differs; no changes applied'
      USING ERRCODE = '23514';
  END IF;

  EXECUTE replace(v_definition, v_old, v_new);

  IF position(v_new IN pg_get_functiondef(v_function)) = 0 THEN
    RAISE EXCEPTION 'FR2-04: completed-contract retry guard missing after migration'
      USING ERRCODE = '23514';
  END IF;
END
$fr204_pacht_retry$;
