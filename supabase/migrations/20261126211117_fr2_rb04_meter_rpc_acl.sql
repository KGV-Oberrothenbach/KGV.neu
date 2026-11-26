-- FR2-RB04a: deny anonymous invocation of seven meter mutation RPCs.
-- Preserve function bodies and the existing authenticated/service role grants.
DO $verify_before$
DECLARE
  f regprocedure;
BEGIN
  FOREACH f IN ARRAY ARRAY[
    'public.capture_meter_reading(bigint,date,numeric,public.ablesung_art,text,text,text)'::regprocedure,
    'public.correct_meter_change_request(bigint,date,numeric,text,date,date,numeric,text)'::regprocedure,
    'public.execute_hauptzaehler_meter_change(bigint,public.zaehler_medium,text,date,numeric,text,text,date,date,numeric,text)'::regprocedure,
    'public.execute_meter_change(bigint,bigint,public.zaehler_medium,date,numeric,text,text,date,date,numeric,text)'::regprocedure,
    'public.review_meter_change_request(bigint,text,text)'::regprocedure,
    'public.submit_own_meter_change_request(bigint,date,numeric,text,text,date,date,numeric,text)'::regprocedure,
    'public.submit_own_meter_reading(bigint,date,numeric,public.ablesung_art,text,text,text)'::regprocedure
  ] LOOP
    IF NOT has_function_privilege('authenticated',f,'EXECUTE')
       OR NOT has_function_privilege('service_role',f,'EXECUTE') THEN
      RAISE EXCEPTION 'Unexpected existing ACL: %', f;
    END IF;
    EXECUTE format('REVOKE EXECUTE ON FUNCTION %s FROM PUBLIC, anon',f);
    IF has_function_privilege('anon',f,'EXECUTE')
       OR NOT has_function_privilege('authenticated',f,'EXECUTE')
       OR NOT has_function_privilege('service_role',f,'EXECUTE') THEN
      RAISE EXCEPTION 'Unexpected resulting ACL: %', f;
    END IF;
  END LOOP;
END
$verify_before$;
