-- Enables marking an active meter as defective without changing its meter lifecycle.
ALTER TABLE public.zaehler
    ADD COLUMN IF NOT EXISTS defekt boolean NOT NULL DEFAULT false;

-- Defective active meters remain in this view and are treated as overdue.
CREATE OR REPLACE VIEW public.v_zaehler_eichstatus WITH (security_invoker = true) AS
SELECT z.id,
       z.parzelle_id,
       p."Anlage" AS anlage,
       p.garten_nr,
       z.medium,
       z.zaehlernummer,
       z.eichdatum,
       z.eichfaellig_am,
       z.eingebaut_am,
       z.status,
       (z.eichfaellig_am - CURRENT_DATE) AS tage_bis_faellig,
       CASE
           WHEN z.defekt = true THEN 'ueberfaellig'::text
           WHEN z.eichfaellig_am < CURRENT_DATE THEN 'ueberfaellig'::text
           WHEN z.eichfaellig_am <= CURRENT_DATE + 180 THEN 'bald_faellig'::text
           ELSE 'ok'::text
       END AS eichstatus,
       z.defekt
FROM public.zaehler z
JOIN public.parzelle p ON p.id = z.parzelle_id
WHERE z.status = 'aktiv'::public.zaehler_status
  AND z.ausgebaut_am IS NULL;
