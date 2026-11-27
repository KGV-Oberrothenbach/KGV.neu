-- Fix the ambiguous output-column reference in the renewal retry path.
-- Preserve the existing function, authorization, return type, owner and ACL.
DO $guard$
DECLARE
  definition text;
  old_fragment constant text := 'from public.wartungsvertrag_dokument_workflow where folgeentscheidung_id=v_dec.id;';
  new_fragment constant text := 'from public.wartungsvertrag_dokument_workflow w where w.folgeentscheidung_id=v_dec.id;';
BEGIN
  SELECT pg_get_functiondef('public.verlaengere_wartungsvertrag(bigint,date,date,text)'::regprocedure)
    INTO STRICT definition;
  IF md5(definition) <> '568d316afe0e2f68a3babfb4514015f3'
     OR (length(definition)-length(replace(definition,old_fragment,''))) <> length(old_fragment) THEN
    RAISE EXCEPTION 'AP07_RENEWAL_RETRY_PRECONDITION: unexpected function definition';
  END IF;
  EXECUTE replace(definition,old_fragment,new_fragment);
END $guard$;
