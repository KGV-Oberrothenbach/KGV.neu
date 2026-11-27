-- AP08: evaluate row-independent FR2 rights once per statement (initplans).
-- Preserve the existing OR predicates, own-object scope, roles and write policies.
-- Fail closed if the underlying policy definitions have changed.
DO $guard$
DECLARE
  item record;
  actual text;
  policy_count integer;
BEGIN
  FOR item IN SELECT * FROM (VALUES
    ('parzelle', 'parzelle_select_effective_or_own_lease', $p$(ap02_has_effective_right('parzelle.lesen'::text) OR ap02_has_effective_right('parzelle.bearbeiten'::text) OR ap02_has_effective_right('belegung.verwalten'::text) OR ap02_has_effective_right('ablesung.erfassen'::text) OR ap02_has_effective_right('ablesung.freigeben'::text) OR ap02_has_effective_right('zaehlerwechsel.verwalten'::text) OR ap02_has_effective_right('rfid.verwalten'::text) OR can_access_own_lease_parcel(id))$p$),
    ('parzellen_belegung', 'parzellen_belegung_select_effective_or_own_lease', $p$(ap02_has_effective_right('parzelle.lesen'::text) OR ap02_has_effective_right('belegung.verwalten'::text) OR can_access_own_lease_occupancy(id))$p$),
    ('zaehler', 'zaehler_select_effective_permission', $p$(ap02_has_effective_right('zaehler.lesen'::text) OR ap02_has_effective_right('ablesung.erfassen'::text) OR ap02_has_effective_right('ablesung.freigeben'::text) OR ap02_has_effective_right('zaehlerwechsel.verwalten'::text) OR ap02_has_effective_right('rfid.verwalten'::text) OR (EXISTS ( SELECT 1 FROM parzellen_belegung pb WHERE ((pb.parzelle_id = zaehler.parzelle_id) AND (pb.mitglied_id = current_mitglied_id())))))$p$),
    ('zaehler_ablesung', 'zaehler_ablesung_select_effective_permission', $p$(ap02_has_effective_right('zaehler.lesen'::text) OR ap02_has_effective_right('ablesung.erfassen'::text) OR ap02_has_effective_right('ablesung.freigeben'::text) OR ap02_has_effective_right('zaehlerwechsel.verwalten'::text) OR can_access_own_meter_reading(id))$p$)
  ) AS expected(table_name, policy_name, expression)
  LOOP
    SELECT count(*), max(qual) INTO policy_count, actual
    FROM pg_catalog.pg_policies
    WHERE schemaname = 'public' AND tablename = item.table_name
      AND policyname = item.policy_name AND cmd = 'SELECT'
      AND permissive = 'PERMISSIVE' AND roles = ARRAY['authenticated']::name[]
      AND with_check IS NULL;
    IF policy_count <> 1 OR
       regexp_replace(replace(actual, 'public.', ''), '[[:space:]]+', '', 'g')
       IS DISTINCT FROM regexp_replace(item.expression, '[[:space:]]+', '', 'g') THEN
      RAISE EXCEPTION 'AP08_JAE_POLICY_PRECONDITION: %', item.policy_name;
    END IF;
  END LOOP;
END $guard$;

ALTER POLICY parzelle_select_effective_or_own_lease ON public.parzelle
USING (
  (SELECT public.ap02_has_effective_right('parzelle.lesen'))
  OR (SELECT public.ap02_has_effective_right('parzelle.bearbeiten'))
  OR (SELECT public.ap02_has_effective_right('belegung.verwalten'))
  OR (SELECT public.ap02_has_effective_right('ablesung.erfassen'))
  OR (SELECT public.ap02_has_effective_right('ablesung.freigeben'))
  OR (SELECT public.ap02_has_effective_right('zaehlerwechsel.verwalten'))
  OR (SELECT public.ap02_has_effective_right('rfid.verwalten'))
  OR public.can_access_own_lease_parcel(id)
);

ALTER POLICY parzellen_belegung_select_effective_or_own_lease ON public.parzellen_belegung
USING (
  (SELECT public.ap02_has_effective_right('parzelle.lesen'))
  OR (SELECT public.ap02_has_effective_right('belegung.verwalten'))
  OR public.can_access_own_lease_occupancy(id)
);

ALTER POLICY zaehler_select_effective_permission ON public.zaehler
USING (
  (SELECT public.ap02_has_effective_right('zaehler.lesen'))
  OR (SELECT public.ap02_has_effective_right('ablesung.erfassen'))
  OR (SELECT public.ap02_has_effective_right('ablesung.freigeben'))
  OR (SELECT public.ap02_has_effective_right('zaehlerwechsel.verwalten'))
  OR (SELECT public.ap02_has_effective_right('rfid.verwalten'))
  OR EXISTS (
    SELECT 1 FROM public.parzellen_belegung pb
    WHERE pb.parzelle_id = zaehler.parzelle_id
      AND pb.mitglied_id = public.current_mitglied_id()
  )
);

ALTER POLICY zaehler_ablesung_select_effective_permission ON public.zaehler_ablesung
USING (
  (SELECT public.ap02_has_effective_right('zaehler.lesen'))
  OR (SELECT public.ap02_has_effective_right('ablesung.erfassen'))
  OR (SELECT public.ap02_has_effective_right('ablesung.freigeben'))
  OR (SELECT public.ap02_has_effective_right('zaehlerwechsel.verwalten'))
  OR public.can_access_own_meter_reading(id)
);
