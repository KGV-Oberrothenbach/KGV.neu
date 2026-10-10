CREATE OR REPLACE VIEW public.v_startseite_bekanntmachungen
WITH (security_invoker = 'true') AS
SELECT id, titel, inhalt_html, sichtbar_ab, sichtbar_bis, sort_order, created_at
FROM public.bekanntmachung b
WHERE aktiv = true
  AND (sichtbar_ab IS NULL OR sichtbar_ab <= public.kgv_local_now())
  AND (sichtbar_bis IS NULL OR sichtbar_bis >= public.kgv_local_now())
ORDER BY sort_order ASC NULLS LAST, created_at DESC NULLS LAST, id DESC;
