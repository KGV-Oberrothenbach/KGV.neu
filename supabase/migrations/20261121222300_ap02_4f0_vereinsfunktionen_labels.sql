-- AP-02.4F.0: Einheitliche, fachlich klare sichtbare Vereinsfunktionsnamen.
-- Dieselbe Migration laeuft auf DemoDB und VDB, ohne IDs, technische
-- Schluessel, Amtsbesetzungen, Verwendungen oder Vertrags-Snapshots zu aendern.
-- Freie, nicht aufgefuehrte Vereinsfunktionen bleiben unveraendert.
DO $pre$
DECLARE
  v_primary integer;
  v_secondary integer;
BEGIN
  SELECT count(*) INTO v_primary
  FROM public.vereinsfunktion
  WHERE schluessel IN ('vorsitz_1', 'vorstandsvorsitzender');
  SELECT count(*) INTO v_secondary
  FROM public.vereinsfunktion
  WHERE schluessel IN ('vorsitz_2', 'vertreter');

  IF v_primary <> 1 OR v_secondary <> 1 THEN
    RAISE EXCEPTION 'Vereinsfunktionen fuer 1./2. Vorsitz unklar (% / %)',
      v_primary, v_secondary USING ERRCODE = '55000';
  END IF;

  IF NOT EXISTS (SELECT 1 FROM public.vereinsfunktion WHERE schluessel='kassenwart')
    OR NOT EXISTS (SELECT 1 FROM public.vereinsfunktion WHERE schluessel IN ('schriftfuehrer','schriftführer'))
    OR NOT EXISTS (SELECT 1 FROM public.vereinsfunktion WHERE schluessel IN ('bauausschuss','bauausschuss_1','bauausschuss_2','bauausschuss_3'))
  THEN
    RAISE EXCEPTION 'Mindestens eine erwartete Vereinsfunktion fehlt'
      USING ERRCODE = '55000';
  END IF;

  -- Keine bereits bewusst anderweitig benannten Funktionen uebernehmen.
  IF EXISTS (
    SELECT 1 FROM public.vereinsfunktion f
    WHERE (f.schluessel IN ('vorsitz_1','vorstandsvorsitzender')
           AND f.bezeichnung NOT IN ('Vorstandsvorsitzender','1. Vorsitzender'))
       OR (f.schluessel IN ('vorsitz_2','vertreter')
           AND f.bezeichnung NOT IN ('Vertreter','2. Vorsitzender'))
       OR (f.schluessel IN ('schriftfuehrer','schriftführer')
           AND f.bezeichnung NOT IN ('Schriftführe','Schriftführer'))
       OR (f.schluessel='kassenwart' AND f.bezeichnung <> 'Kassenwart')
       OR (f.schluessel IN ('bauausschuss','bauausschuss_1','bauausschuss_2','bauausschuss_3')
           AND f.bezeichnung <> 'Bauausschuss')
  ) THEN
    RAISE EXCEPTION 'Vereinsfunktionsnamen wurden abweichend angepasst; keine Ueberschreibung'
      USING ERRCODE = '55000';
  END IF;
END
$pre$;

UPDATE public.vereinsfunktion
SET bezeichnung=CASE
  WHEN schluessel IN ('vorsitz_1','vorstandsvorsitzender') THEN '1. Vorsitzender'
  WHEN schluessel IN ('vorsitz_2','vertreter') THEN '2. Vorsitzender'
  WHEN schluessel IN ('schriftfuehrer','schriftführer') THEN 'Schriftführer'
  ELSE bezeichnung
END
WHERE schluessel IN (
  'vorsitz_1','vorstandsvorsitzender','vorsitz_2','vertreter',
  'schriftfuehrer','schriftführer'
)
AND bezeichnung IS DISTINCT FROM CASE
  WHEN schluessel IN ('vorsitz_1','vorstandsvorsitzender') THEN '1. Vorsitzender'
  WHEN schluessel IN ('vorsitz_2','vertreter') THEN '2. Vorsitzender'
  ELSE 'Schriftführer'
END;

DO $post$
BEGIN
  IF (SELECT count(*) FROM public.vereinsfunktion
      WHERE schluessel IN ('vorsitz_1','vorstandsvorsitzender') AND bezeichnung='1. Vorsitzender') <> 1
  OR (SELECT count(*) FROM public.vereinsfunktion
      WHERE schluessel IN ('vorsitz_2','vertreter') AND bezeichnung='2. Vorsitzender') <> 1
  OR NOT EXISTS (SELECT 1 FROM public.vereinsfunktion
                 WHERE schluessel IN ('schriftfuehrer','schriftführer') AND bezeichnung='Schriftführer')
  THEN
    RAISE EXCEPTION 'Vereinsfunktions-Abgleich fehlgeschlagen' USING ERRCODE='55000';
  END IF;
END
$post$;
