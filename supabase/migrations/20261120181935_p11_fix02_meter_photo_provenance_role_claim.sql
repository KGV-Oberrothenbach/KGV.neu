-- FIX-02: PostgREST service-role writes must pass both the actual SQL role and JWT role.
-- Runtime regression 2026-11-20: request.jwt.claim.role can be empty while
-- request.jwt.claims holds {"role":"service_role"}; the old guard returned 42501.
-- Do not authorize solely from user-controllable input or a forged JWT role.
CREATE OR REPLACE FUNCTION public.guard_meter_reading_photo_provenance()
RETURNS trigger
LANGUAGE plpgsql SECURITY DEFINER
SET search_path = public, pg_temp
AS $guard$
DECLARE
  v_trusted_service boolean :=
      coalesce(current_setting('role', true), '') = 'service_role'
      AND auth.role() = 'service_role';
BEGIN
  IF TG_OP = 'INSERT' THEN
    IF NOT coalesce(v_trusted_service, false)
      AND (NEW.foto_pfad IS NOT NULL OR NEW.foto_drive_file_id IS NOT NULL OR NEW.foto_dateiname IS NOT NULL) THEN
      RAISE EXCEPTION 'Foto-Provenance darf nur durch den geschützten Uploadpfad gespeichert werden.' USING ERRCODE = '42501';
    END IF;
    RETURN NEW;
  END IF;

  IF NEW.foto_pfad IS DISTINCT FROM OLD.foto_pfad
    OR NEW.foto_drive_file_id IS DISTINCT FROM OLD.foto_drive_file_id
    OR NEW.foto_dateiname IS DISTINCT FROM OLD.foto_dateiname THEN
    IF NOT coalesce(v_trusted_service, false) THEN
      RAISE EXCEPTION 'Foto-Provenance darf nur durch den geschützten Uploadpfad geändert werden.' USING ERRCODE = '42501';
    END IF;
    IF OLD.foto_drive_file_id IS NOT NULL THEN
      RAISE EXCEPTION 'Eine gespeicherte Foto-Provenance ist unveränderlich.' USING ERRCODE = '55000';
    END IF;
  END IF;
  RETURN NEW;
END;
$guard$;
REVOKE ALL ON FUNCTION public.guard_meter_reading_photo_provenance() FROM PUBLIC, anon, authenticated;
COMMENT ON FUNCTION public.guard_meter_reading_photo_provenance() IS
  'FIX-02: foto-provenance write only if SQL role and JWT role are both service_role, immutable existing Drive association; compatible with request.jwt.claims.';
