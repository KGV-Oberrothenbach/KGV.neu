-- FIX-05: vorhandene GiST-Exclusion galt nur fuer verknuepfte Pachtverhaeltnisse.
-- Historische, noch nicht verknuepfte Belegungen duerfen keine Ueberlappung erzeugen.
-- Bei vorhandenen Datenkonflikten bricht diese Migration TRANSAKTIONAL ab.
BEGIN;
DO $guard$
BEGIN
  IF EXISTS (
    SELECT 1 FROM public.parzellen_belegung a
    JOIN public.parzellen_belegung b ON a.id < b.id AND a.parzelle_id=b.parzelle_id
    WHERE daterange(a.von_datum,coalesce(a.bis_datum+1,'infinity'::date),'[)')
       && daterange(b.von_datum,coalesce(b.bis_datum+1,'infinity'::date),'[)')
  ) THEN
    RAISE EXCEPTION 'FIX-05: existing overlapping occupancy requires manual data review before migration'
      USING ERRCODE='23514';
  END IF;
END;
$guard$;
ALTER TABLE public.parzellen_belegung
  DROP CONSTRAINT parzellen_belegung_zeitraum_ohne_ueberlappung;
ALTER TABLE public.parzellen_belegung
  ADD CONSTRAINT parzellen_belegung_zeitraum_ohne_ueberlappung
  EXCLUDE USING gist (
    parzelle_id WITH =,
    daterange(von_datum,COALESCE(bis_datum + 1,'infinity'::date),'[)'::text) WITH &&
  );
COMMIT;
