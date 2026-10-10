-- G11.4: Zentralisierte Zuordnungsregeln auch für direkte Datenbankzugriffe.
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conrelid = 'public.wartungsvertrag_zuordnungen'::regclass
          AND conname = 'ck_wvz_gueltigkeit'
    ) THEN
        ALTER TABLE public.wartungsvertrag_zuordnungen
            ADD CONSTRAINT ck_wvz_gueltigkeit
            CHECK (gueltig_bis IS NULL OR gueltig_bis >= gueltig_ab);
    END IF;
END;
$$;

CREATE OR REPLACE FUNCTION public.validate_wartungsvertrag_zuordnung()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    v_mitglied_exists boolean;
    v_max_aktive_zuordnungen integer;
    v_spitzenbelegung integer;
BEGIN
    SELECT true
      INTO v_mitglied_exists
    FROM public.mitglied m
    WHERE m.id = new.hauptmitglied_id;

    IF v_mitglied_exists IS DISTINCT FROM true THEN
        RAISE EXCEPTION 'Mitglied % existiert nicht.', new.hauptmitglied_id;
    END IF;

    IF public.get_hauptmitglied_id(new.hauptmitglied_id) IS DISTINCT FROM new.hauptmitglied_id THEN
        RAISE EXCEPTION 'Wartungsvertragszuordnungen müssen dem Hauptmitglied zugeordnet werden.';
    END IF;

    SELECT w.max_aktive_zuordnungen
      INTO v_max_aktive_zuordnungen
    FROM public.wartungsvertraege w
    WHERE w.id = new.wartungsvertrag_id;

    IF v_max_aktive_zuordnungen IS NULL THEN
        RAISE EXCEPTION 'Wartungsvertrag % existiert nicht.', new.wartungsvertrag_id;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM public.wartungsvertrag_zuordnungen z
        WHERE z.wartungsvertrag_id = new.wartungsvertrag_id
          AND z.hauptmitglied_id = new.hauptmitglied_id
          AND z.id <> coalesce(new.id, -1)
          AND daterange(z.gueltig_ab, coalesce(z.gueltig_bis, 'infinity'::date), '[]')
              && daterange(new.gueltig_ab, coalesce(new.gueltig_bis, 'infinity'::date), '[]')
    ) THEN
        RAISE EXCEPTION
            'Das Mitglied % hat den Wartungsvertrag % im angegebenen Zeitraum bereits zugeordnet.',
            new.hauptmitglied_id,
            new.wartungsvertrag_id;
    END IF;

    WITH existing_rows AS (
        SELECT z.gueltig_ab, z.gueltig_bis
        FROM public.wartungsvertrag_zuordnungen z
        WHERE z.wartungsvertrag_id = new.wartungsvertrag_id
          AND z.id <> coalesce(new.id, -1)
          AND daterange(z.gueltig_ab, coalesce(z.gueltig_bis, 'infinity'::date), '[]')
              && daterange(new.gueltig_ab, coalesce(new.gueltig_bis, 'infinity'::date), '[]')
    ),
    candidate_dates AS (
        SELECT new.gueltig_ab AS d
        UNION
        SELECT new.gueltig_bis
        WHERE new.gueltig_bis IS NOT NULL
        UNION
        SELECT e.gueltig_ab
        FROM existing_rows e
        WHERE e.gueltig_ab BETWEEN new.gueltig_ab AND coalesce(new.gueltig_bis, 'infinity'::date)
        UNION
        SELECT e.gueltig_bis
        FROM existing_rows e
        WHERE e.gueltig_bis IS NOT NULL
          AND e.gueltig_bis BETWEEN new.gueltig_ab AND coalesce(new.gueltig_bis, 'infinity'::date)
    )
    SELECT coalesce(
               max((
                   SELECT count(*)
                   FROM public.wartungsvertrag_zuordnungen z
                   WHERE z.wartungsvertrag_id = new.wartungsvertrag_id
                     AND z.id <> coalesce(new.id, -1)
                     AND c.d BETWEEN z.gueltig_ab AND coalesce(z.gueltig_bis, 'infinity'::date)
               ) + 1),
               1
           )
      INTO v_spitzenbelegung
    FROM candidate_dates c;

    IF v_spitzenbelegung > v_max_aktive_zuordnungen THEN
        RAISE EXCEPTION
            'Maximale Anzahl aktiver Zuordnungen (%) für Wartungsvertrag % würde überschritten.',
            v_max_aktive_zuordnungen,
            new.wartungsvertrag_id;
    END IF;

    RETURN new;
END;
$$;
