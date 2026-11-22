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
COMMIT;
