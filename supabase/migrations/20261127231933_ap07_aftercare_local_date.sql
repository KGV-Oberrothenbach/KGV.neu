-- Fix the nonexistent date helper in manual aftercare completion.
-- Use the same club-local day as all surrounding guards and the successor trigger.
DO $guard$
DECLARE
  definition text;
  old_fragment constant text := 'public.kgv_local_today()';
  new_fragment constant text := 'public.kgv_local_now()::date';
BEGIN
  SELECT pg_get_functiondef('public.beende_pacht_nachsorge(bigint,text,text,bigint)'::regprocedure)
    INTO STRICT definition;
  IF md5(definition)<>'2586518c98f6404a02b1a04c5eeda582'
     OR (length(definition)-length(replace(definition,old_fragment,'')))<>length(old_fragment) THEN
    RAISE EXCEPTION 'AP07_AFTERCARE_DATE_PRECONDITION';
  END IF;
  EXECUTE replace(definition,old_fragment,new_fragment);
END $guard$;
