-- FR2-04.5: Fail closed even if the JWT role claim is absent.
-- The authenticated role still needs the effective, individually revocable
-- Fachrecht historische_vertraege.erfassen. No role-matrix changes.
CREATE OR REPLACE FUNCTION public.can_register_historical_contracts()
RETURNS boolean
LANGUAGE sql STABLE SECURITY DEFINER
SET search_path TO 'pg_catalog','public'
AS $fr204$
  SELECT COALESCE(auth.role() = 'service_role', false)
      OR (auth.uid() IS NOT NULL AND public.ap02_has_effective_right('historische_vertraege.erfassen'));
$fr204$;

-- Anonymous sessions must not invoke the historical authorization helper or
-- the lease-aftercare creation RPC directly.
REVOKE EXECUTE ON FUNCTION public.can_register_historical_contracts() FROM PUBLIC, anon;
GRANT EXECUTE ON FUNCTION public.can_register_historical_contracts() TO authenticated, service_role;
REVOKE EXECUTE ON FUNCTION public.erstelle_pacht_nachsorge(bigint,date) FROM PUBLIC, anon;

DO $fr204_assert$
BEGIN
  IF has_function_privilege('anon','public.can_register_historical_contracts()','EXECUTE')
     OR has_function_privilege('anon','public.erstelle_pacht_nachsorge(bigint,date)','EXECUTE')
     OR NOT has_function_privilege('authenticated','public.can_register_historical_contracts()','EXECUTE')
  THEN
     RAISE EXCEPTION 'FR2-04.5: Sicherheits-ACL fehlerhaft.' USING ERRCODE='42501';
  END IF;
END $fr204_assert$;
