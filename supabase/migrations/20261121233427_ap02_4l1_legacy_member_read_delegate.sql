-- AP-02.4L1: alter 2-Parameter-Mitgliederlese-Wrapper delegiert vollstaendig an AP02.
-- p_auth_user_id bleibt nur zur historischen Signaturstabilitaet erhalten und
-- wird NICHT als Autorisierungssignal verwendet; auth.uid()/Mitgliedsbindung
-- werden ausschliesslich in ap02_can_read_mitglied verifiziert.
CREATE OR REPLACE FUNCTION public.can_read_mitglied(p_mitglied_id bigint,p_auth_user_id uuid)
RETURNS boolean LANGUAGE sql STABLE SECURITY DEFINER SET search_path TO pg_catalog,public
AS $fn$
 SELECT public.ap02_can_read_mitglied(p_mitglied_id)
$fn$;
DO $check$ BEGIN
 IF (SELECT count(*) FROM public.fachrecht)<>55
 OR to_regprocedure('public.ap02_can_read_mitglied(bigint)') IS NULL THEN
 RAISE EXCEPTION 'Fachrechte-2.0-Mitgliederlesekern fehlt'; END IF;
END $check$;;
