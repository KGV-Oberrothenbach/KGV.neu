-- FIX-02: Upload ist eine eigene Berechtigung, historisches LESEN bleibt davon getrennt.
-- Auf bestehende Ablesungen wird keine Ersteller-ID rueckwirkend erfunden.
BEGIN;
ALTER TABLE public.zaehler_ablesung
  ADD COLUMN IF NOT EXISTS erfasst_von_auth_user_id uuid;

CREATE OR REPLACE FUNCTION public.guard_meter_reading_creator()
RETURNS trigger LANGUAGE plpgsql SECURITY INVOKER
SET search_path = public, pg_temp
AS $$
BEGIN
  IF TG_OP = 'INSERT' THEN
    -- Ein Client darf auch bei SECURITY DEFINER-RPC keinen fremden Ersteller angeben.
    NEW.erfasst_von_auth_user_id := auth.uid();
  ELSIF NEW.erfasst_von_auth_user_id IS DISTINCT FROM OLD.erfasst_von_auth_user_id THEN
    RAISE EXCEPTION 'Ablesungsersteller darf nicht veraendert werden' USING ERRCODE = '42501';
  END IF;
  RETURN NEW;
END;
$$;
DROP TRIGGER IF EXISTS trg_guard_meter_reading_creator ON public.zaehler_ablesung;
CREATE TRIGGER trg_guard_meter_reading_creator
BEFORE INSERT OR UPDATE ON public.zaehler_ablesung
FOR EACH ROW EXECUTE FUNCTION public.guard_meter_reading_creator();
REVOKE ALL ON FUNCTION public.guard_meter_reading_creator() FROM PUBLIC, anon, authenticated;

CREATE OR REPLACE FUNCTION public.can_upload_meter_reading_photo(p_ablesung_id bigint)
RETURNS boolean LANGUAGE sql STABLE SECURITY DEFINER
SET search_path = public, pg_temp
AS $$
  SELECT auth.uid() IS NOT NULL AND p_ablesung_id IS NOT NULL
  AND EXISTS (
    SELECT 1 FROM public.zaehler_ablesung a
    JOIN public.zaehler z ON z.id = a.zaehler_id
    WHERE a.id = p_ablesung_id
      AND a.foto_drive_file_id IS NULL
      AND a.foto_dateiname IS NULL
      AND a.foto_pfad IS NULL
      AND (
        public.has_effective_permission(32)
        OR (
          a.erfasst_von_auth_user_id = auth.uid()
          AND z.parzelle_id IS NOT NULL
          AND EXISTS (
            SELECT 1 FROM public.mitglied m
            WHERE m.id = public.current_mitglied_id()
              AND (m.mitglied_ende IS NULL OR m.mitglied_ende >= public.kgv_local_now()::date)
          )
          AND public.current_user_has_active_lease_for_parcel(z.parzelle_id, a.ablesedatum::date)
          AND public.current_user_has_active_lease_for_parcel(z.parzelle_id, public.kgv_local_now()::date)
        )
      )
  );
$$;
REVOKE ALL ON FUNCTION public.can_upload_meter_reading_photo(bigint) FROM PUBLIC, anon;
GRANT EXECUTE ON FUNCTION public.can_upload_meter_reading_photo(bigint) TO authenticated, service_role;
COMMENT ON FUNCTION public.can_upload_meter_reading_photo(bigint) IS
  'FIX-02: effektives Recht 32 oder aktive Eigenablesung des identischen Users; historische Foto-Leserechte berechtigen NICHT zum Schreiben';
COMMIT;
