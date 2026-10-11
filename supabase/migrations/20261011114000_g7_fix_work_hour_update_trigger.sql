CREATE OR REPLACE FUNCTION public.enforce_own_open_arbeitsstunde_update()
RETURNS trigger
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE v_assignment_date date;
BEGIN
  IF current_setting('app.g7_work_hour_review', true) = 'true' AND public.has_effective_permission(256) THEN
    IF old.arbeitseinsatz_anmeldung_id IS NOT NULL THEN
      IF new.arbeitseinsatz_anmeldung_id IS DISTINCT FROM old.arbeitseinsatz_anmeldung_id THEN
        RAISE EXCEPTION 'Die Arbeitseinsatzzuordnung ist unveränderlich.' USING errcode = '42501';
      END IF;
      SELECT a.datum INTO v_assignment_date
      FROM public.arbeitseinsatz_anmeldung aa
      JOIN public.arbeitseinsatz a ON a.id = aa.arbeitseinsatz_id
      WHERE aa.id = old.arbeitseinsatz_anmeldung_id;
      IF new.datum <> v_assignment_date THEN
        RAISE EXCEPTION 'Das Datum einer Einsatz-Arbeitsstunde entspricht immer dem Einsatzdatum.' USING errcode = '42501';
      END IF;
    END IF;
    RETURN new;
  END IF;

  IF old.mitglied_id <> public.current_mitglied_id()
     OR old.status <> 'offen'
     OR old.freigegeben <> false
     OR old.genehmigt_von IS NOT NULL
     OR old.genehmigt_am IS NOT NULL
     OR new.mitglied_id <> public.current_mitglied_id()
     OR new.status <> 'offen'
     OR new.freigegeben <> false
     OR new.genehmigt_von IS NOT NULL
     OR new.genehmigt_am IS NOT NULL THEN
    RAISE EXCEPTION 'Nur eigene offene Arbeitsstunden dürfen bearbeitet werden.' USING errcode = '42501';
  END IF;

  IF new.id IS DISTINCT FROM old.id
     OR new.mitglied_id IS DISTINCT FROM old.mitglied_id
     OR new.saison_id IS DISTINCT FROM old.saison_id
     OR new.status IS DISTINCT FROM old.status
     OR new.freigegeben IS DISTINCT FROM old.freigegeben
     OR new.genehmigt_von IS DISTINCT FROM old.genehmigt_von
     OR new.genehmigt_am IS DISTINCT FROM old.genehmigt_am
     OR new.arbeitseinsatz_anmeldung_id IS DISTINCT FROM old.arbeitseinsatz_anmeldung_id THEN
    RAISE EXCEPTION 'Nur Datum, Stunden und Art der Arbeit dürfen geändert werden.' USING errcode = '42501';
  END IF;

  IF old.arbeitseinsatz_anmeldung_id IS NOT NULL THEN
    SELECT a.datum INTO v_assignment_date
    FROM public.arbeitseinsatz_anmeldung aa
    JOIN public.arbeitseinsatz a ON a.id = aa.arbeitseinsatz_id
    WHERE aa.id = old.arbeitseinsatz_anmeldung_id;
    IF new.datum <> v_assignment_date THEN
      RAISE EXCEPTION 'Das Datum einer Einsatz-Arbeitsstunde entspricht immer dem Einsatzdatum.' USING errcode = '42501';
    END IF;
  END IF;

  IF new.lockedbyuserid IS NOT DISTINCT FROM old.lockedbyuserid
     AND new.lockat IS NOT DISTINCT FROM old.lockat THEN
    RETURN new;
  END IF;
  IF new.lockedbyuserid = auth.uid()
     AND new.lockat IS NOT NULL
     AND EXISTS (
       SELECT 1
       FROM public.browser_edit_lock l
       WHERE l.entity_type = 'arbeitsstunde'
         AND l.entity_id = old.id::text
         AND l.locked_by_user_id = auth.uid()
         AND l.expires_at > now()
     ) THEN
    RETURN new;
  END IF;
  IF old.lockedbyuserid = auth.uid()
     AND new.lockedbyuserid IS NULL
     AND new.lockat IS NULL THEN
    RETURN new;
  END IF;
  RAISE EXCEPTION 'Arbeitsstunden-Sperren dürfen nur über den Browser-Lock geändert werden.' USING errcode = '42501';
END;
$$;
