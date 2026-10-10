-- G11.1: Additive foundation only. Duty-hour calculation remains in G7/G11.1b.
ALTER TABLE public.wartungsvertraege
  ADD COLUMN IF NOT EXISTS arbeitsstunden_gutschrift numeric NOT NULL DEFAULT 0;

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'wartungsvertraege_arbeitsstunden_gutschrift_nonnegative') THEN
    ALTER TABLE public.wartungsvertraege
      ADD CONSTRAINT wartungsvertraege_arbeitsstunden_gutschrift_nonnegative
      CHECK (arbeitsstunden_gutschrift >= 0);
  END IF;

  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'wartungsvertraege_befreiung_oder_gutschrift') THEN
    ALTER TABLE public.wartungsvertraege
      ADD CONSTRAINT wartungsvertraege_befreiung_oder_gutschrift
      CHECK (NOT (befreit_von_pflichtstunden = true AND arbeitsstunden_gutschrift > 0));
  END IF;
END $$;
