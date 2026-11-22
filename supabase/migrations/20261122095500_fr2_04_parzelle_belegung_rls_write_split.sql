-- FR2-04.1/04.2: Preserve read/update/insert operations by their own Fachrechte,
-- but prevent destructive direct table DELETEs against parcels and occupancy history.
-- Occupancy managers need parcel metadata to choose free parcels.
BEGIN;
DROP POLICY IF EXISTS parzelle_write_effective ON public.parzelle;
DROP POLICY IF EXISTS parzelle_select_effective_or_own_lease ON public.parzelle;
CREATE POLICY parzelle_select_effective_or_own_lease ON public.parzelle
  FOR SELECT TO authenticated USING (
    public.ap02_has_effective_right('parzelle.lesen')
    OR public.ap02_has_effective_right('parzelle.bearbeiten')
    OR public.ap02_has_effective_right('belegung.verwalten')
    OR public.can_access_own_lease_parcel(id)
  );
CREATE POLICY parzelle_insert_effective ON public.parzelle
  FOR INSERT TO authenticated WITH CHECK (public.ap02_has_effective_right('parzelle.bearbeiten'));
CREATE POLICY parzelle_update_effective ON public.parzelle
  FOR UPDATE TO authenticated
  USING (public.ap02_has_effective_right('parzelle.bearbeiten'))
  WITH CHECK (public.ap02_has_effective_right('parzelle.bearbeiten'));

DROP POLICY IF EXISTS parzellen_belegung_write_effective ON public.parzellen_belegung;
DROP POLICY IF EXISTS parzellen_belegung_select_effective_or_own_lease ON public.parzellen_belegung;
CREATE POLICY parzellen_belegung_select_effective_or_own_lease ON public.parzellen_belegung
  FOR SELECT TO authenticated USING (
    public.ap02_has_effective_right('parzelle.lesen')
    OR public.ap02_has_effective_right('belegung.verwalten')
    OR public.can_access_own_lease_occupancy(id)
  );
CREATE POLICY parzellen_belegung_insert_effective ON public.parzellen_belegung
  FOR INSERT TO authenticated WITH CHECK (public.ap02_has_effective_right('belegung.verwalten'));
CREATE POLICY parzellen_belegung_update_effective ON public.parzellen_belegung
  FOR UPDATE TO authenticated
  USING (public.ap02_has_effective_right('belegung.verwalten'))
  WITH CHECK (public.ap02_has_effective_right('belegung.verwalten'));

-- FR2-04.2 defense in depth: prevent a user with an arbitrary client session
-- from changing a contract-controlled occupancy directly.  Contract RPCs and
-- synchronization triggers execute as a trusted PostgreSQL definer and remain
-- responsible for changes to linked occupancy records.
CREATE FUNCTION public.fr2_04_guard_direct_belegung_mutation()
RETURNS trigger
LANGUAGE plpgsql
SECURITY INVOKER
SET search_path TO 'pg_catalog', 'public'
AS $fr2_guard$
BEGIN
  IF current_user <> 'authenticated' THEN
    RETURN NEW;
  END IF;

  IF TG_OP = 'INSERT' THEN
    IF NEW.pachtverhaeltnis_id IS NOT NULL THEN
      RAISE EXCEPTION 'Vertragsgebundene Belegungen dürfen nur über den Pachtvertragsablauf erstellt werden.'
        USING ERRCODE = '42501';
    END IF;
  ELSIF TG_OP = 'UPDATE' THEN
    IF OLD.pachtverhaeltnis_id IS NOT NULL OR NEW.pachtverhaeltnis_id IS NOT NULL THEN
      RAISE EXCEPTION 'Vertragsgebundene Belegungen dürfen nur über den Pachtvertragsablauf geändert werden.'
        USING ERRCODE = '42501';
    END IF;
    IF NEW.parzelle_id IS DISTINCT FROM OLD.parzelle_id
       OR NEW.mitglied_id IS DISTINCT FROM OLD.mitglied_id
       OR NEW.von_datum IS DISTINCT FROM OLD.von_datum THEN
      RAISE EXCEPTION 'Parzelle, Pächter und Beginn einer bestehenden Belegung sind unveränderlich; erfasse stattdessen einen neuen Vorgang.'
        USING ERRCODE = '42501';
    END IF;
  END IF;

  IF NEW.bis_datum IS NOT NULL AND NEW.bis_datum < NEW.von_datum THEN
    RAISE EXCEPTION 'Das Ende der Belegung darf nicht vor dem Beginn liegen.'
      USING ERRCODE = '23514';
  END IF;
  RETURN NEW;
END;
$fr2_guard$;
REVOKE ALL ON FUNCTION public.fr2_04_guard_direct_belegung_mutation()
  FROM PUBLIC, anon, authenticated;
CREATE TRIGGER trg_fr2_04_guard_direct_belegung_mutation
BEFORE INSERT OR UPDATE ON public.parzellen_belegung
FOR EACH ROW EXECUTE FUNCTION public.fr2_04_guard_direct_belegung_mutation();

-- RLS never applies to TRUNCATE or REFERENCES.  Retain only operations
-- actually needed by authenticated Data API clients.
REVOKE DELETE, TRUNCATE, REFERENCES, TRIGGER
  ON TABLE public.parzelle, public.parzellen_belegung
  FROM PUBLIC, anon, authenticated;

-- Fail the entire migration if an inherited privilege remains available.
DO $fr2_acl_check$
BEGIN
  IF has_table_privilege('authenticated','public.parzelle','TRUNCATE')
     OR has_table_privilege('authenticated','public.parzellen_belegung','TRUNCATE')
     OR has_table_privilege('authenticated','public.parzelle','DELETE')
     OR has_table_privilege('authenticated','public.parzellen_belegung','DELETE') THEN
     RAISE EXCEPTION 'FR2-04: unsafe DELETE/TRUNCATE permission remains for authenticated'
       USING ERRCODE = '42501';
  END IF;
END;
$fr2_acl_check$;

COMMIT;
