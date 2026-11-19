-- FIX-03: Rollennamen sind allein Vorlagen, nie Autorisierung.
-- app_user ist massgeblich; keine bestehenden Konten oder Grants werden veraendert.
BEGIN;
CREATE OR REPLACE FUNCTION public.get_user_role()
RETURNS text LANGUAGE sql STABLE
AS $$ SELECT public.current_app_role(); $$;

CREATE OR REPLACE FUNCTION public.who_am_i()
RETURNS TABLE(vorname text, role text) LANGUAGE sql STABLE
AS $$
  SELECT m.vorname, public.current_app_role() AS role
  FROM public.mitglied m
  WHERE m.auth_user_id = auth.uid()
  LIMIT 1;
$$;

CREATE OR REPLACE FUNCTION public.sync_app_user_from_mitglied()
RETURNS trigger LANGUAGE plpgsql
AS $$
BEGIN
  IF NEW.auth_user_id IS NULL THEN
    IF OLD.auth_user_id IS NOT NULL THEN
      DELETE FROM public.app_user WHERE user_id = OLD.auth_user_id;
    END IF;
    RETURN NEW;
  END IF;
  INSERT INTO public.app_user (user_id, mitglied_id, updated_at)
  VALUES (NEW.auth_user_id, NEW.id, now())
  ON CONFLICT (user_id) DO UPDATE
    SET mitglied_id = excluded.mitglied_id,
        updated_at = now();
  RETURN NEW;
END;
$$;
DROP TRIGGER IF EXISTS trg_sync_app_user_from_mitglied ON public.mitglied;
CREATE TRIGGER trg_sync_app_user_from_mitglied
AFTER INSERT OR UPDATE OF auth_user_id ON public.mitglied
FOR EACH ROW EXECUTE FUNCTION public.sync_app_user_from_mitglied();

ALTER TABLE public.zaehler_ablesung ALTER COLUMN pruefstatus SET DEFAULT 'eingereicht'::public.ablesung_pruefstatus;
COMMIT;
