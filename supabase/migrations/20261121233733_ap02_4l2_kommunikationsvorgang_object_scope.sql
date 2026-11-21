-- AP-02.4L2: Vorgangsrecht auf zugewiesene oder selbst erstellte Vorgänge begrenzen.
-- Das alte pauschale ALL-Recht war nicht mit dem Fachrechte-2.0-Objektscope vereinbar.
-- DemoDB only; keine vorhandenen Kommunikationsvorgaenge werden geaendert.
DROP POLICY IF EXISTS kommunikationsvorgang_effective ON public.kommunikationsvorgang;
CREATE POLICY kommunikationsvorgang_effective
 ON public.kommunikationsvorgang
 AS PERMISSIVE FOR ALL TO authenticated
 USING (public.ap02_has_effective_right('vorgang.bearbeiten')
    AND (zugewiesen_an = (SELECT auth.uid()) OR erstellt_von = (SELECT auth.uid())))
 WITH CHECK (public.ap02_has_effective_right('vorgang.bearbeiten')
    AND (zugewiesen_an = (SELECT auth.uid()) OR erstellt_von = (SELECT auth.uid())));
DO $verify$
DECLARE p record;
BEGIN
 SELECT * INTO p FROM pg_policies
 WHERE schemaname='public' AND tablename='kommunikationsvorgang' AND policyname='kommunikationsvorgang_effective';
 IF p.policyname IS NULL OR p.cmd<>'ALL' OR p.qual NOT LIKE '%zugewiesen_an%' OR p.qual NOT LIKE '%erstellt_von%'
 OR p.with_check NOT LIKE '%zugewiesen_an%' OR p.with_check NOT LIKE '%erstellt_von%'
 THEN RAISE EXCEPTION 'Vorgangsscope ist unvollstaendig'; END IF;
END $verify$;;
