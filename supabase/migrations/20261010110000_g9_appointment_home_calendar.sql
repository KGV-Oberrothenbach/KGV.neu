CREATE OR REPLACE VIEW public.v_startseite_termine
WITH (security_invoker = 'true') AS
SELECT id, titel, beschreibung, datum, start_uhrzeit, end_uhrzeit, sichtbar_ab, sichtbar_bis
FROM public.termin t
WHERE aktiv = true
  AND (sichtbar_ab IS NULL OR sichtbar_ab <= public.kgv_local_now())
  AND (sichtbar_bis IS NULL OR sichtbar_bis >= public.kgv_local_now())
  AND datum >= public.kgv_local_now()::date
ORDER BY datum, start_uhrzeit NULLS LAST, end_uhrzeit NULLS LAST, titel, id;
