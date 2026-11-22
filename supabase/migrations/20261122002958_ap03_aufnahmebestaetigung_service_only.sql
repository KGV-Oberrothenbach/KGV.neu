-- AP-03: Abschluss der Aufnahmebestaetigung ist ein serverinterner Dienstschritt.
-- kgv-generate-contract verwendet dafuer den service_role Client.
-- Direkte authentifizierte Clientaufrufe auf den SECURITY DEFINER RPC sperren.
REVOKE EXECUTE ON FUNCTION public.complete_aufnahmebestaetigung(bigint, bigint)
  FROM PUBLIC, anon, authenticated;
GRANT EXECUTE ON FUNCTION public.complete_aufnahmebestaetigung(bigint, bigint)
  TO service_role;

DO $ap03_guard$
BEGIN
  IF has_function_privilege('anon',
       'public.complete_aufnahmebestaetigung(bigint,bigint)', 'EXECUTE')
     OR has_function_privilege('authenticated',
       'public.complete_aufnahmebestaetigung(bigint,bigint)', 'EXECUTE')
     OR NOT has_function_privilege('service_role',
       'public.complete_aufnahmebestaetigung(bigint,bigint)', 'EXECUTE')
  THEN
    RAISE EXCEPTION 'AP03 Aufnahmebestaetigung ACL Test fehlgeschlagen';
  END IF;
END
$ap03_guard$;
