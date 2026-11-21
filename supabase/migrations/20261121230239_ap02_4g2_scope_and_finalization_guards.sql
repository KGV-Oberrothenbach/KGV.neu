-- AP-02.4G2: Objekt-Scope und Finalisierungs-Guard haerten.
-- DemoDB only. Reproduzierbare Anschlussmigration zu AP-02.4G1.
-- Ein blosses Leserecht darf nicht alle Kommunikationsvorgaenge offenlegen.
DROP POLICY IF EXISTS ap02_kommunikation_lesen ON public.kommunikationsvorgang;
CREATE POLICY ap02_kommunikation_lesen
 ON public.kommunikationsvorgang FOR SELECT TO authenticated
 USING (
    public.ap02_has_effective_right('vorgang.lesen')
    AND (zugewiesen_an = (SELECT auth.uid()) OR erstellt_von = (SELECT auth.uid()))
 );

CREATE OR REPLACE FUNCTION public.ap02_guard_jahresabschluss_status()
RETURNS trigger LANGUAGE plpgsql SECURITY INVOKER
SET search_path TO pg_catalog, public AS $guard$
BEGIN
  IF TG_TABLE_NAME = 'jahresabschluss' THEN
    IF NEW.status = 'abgeschlossen'
       AND (TG_OP = 'INSERT' OR NEW.status IS DISTINCT FROM OLD.status)
       AND NOT public.ap02_has_effective_right('jahresabschluss.finalisieren') THEN
      RAISE EXCEPTION 'Keine Berechtigung fuer finalen Jahresabschluss' USING ERRCODE='42501';
    END IF;
  ELSIF TG_TABLE_NAME = 'jahresabschluss_korrektur' THEN
    IF NOT public.ap02_has_effective_right('jahresabschluss.korrigieren') THEN
      RAISE EXCEPTION 'Korrekturabschluss derzeit gesperrt' USING ERRCODE='42501';
    END IF;
  END IF;
  RETURN NEW;
END $guard$;

DROP TRIGGER IF EXISTS ap02_jahresabschluss_status_guard ON public.jahresabschluss;
CREATE TRIGGER ap02_jahresabschluss_status_guard
 BEFORE INSERT OR UPDATE ON public.jahresabschluss
 FOR EACH ROW EXECUTE FUNCTION public.ap02_guard_jahresabschluss_status();

DROP TRIGGER IF EXISTS ap02_jahresabschluss_korrektur_guard ON public.jahresabschluss_korrektur;
CREATE TRIGGER ap02_jahresabschluss_korrektur_guard
 BEFORE INSERT OR UPDATE ON public.jahresabschluss_korrektur
 FOR EACH ROW EXECUTE FUNCTION public.ap02_guard_jahresabschluss_status();

DO $verify$
BEGIN
 IF NOT EXISTS (
   SELECT 1 FROM pg_policies
   WHERE schemaname='public' AND tablename='kommunikationsvorgang'
     AND policyname='ap02_kommunikation_lesen'
     AND qual LIKE '%zugewiesen_an%'
     AND qual LIKE '%erstellt_von%'
 ) THEN
   RAISE EXCEPTION 'Kommunikationsvorgang scope is not limited';
 END IF;
 IF (SELECT count(*) FROM pg_trigger WHERE tgname IN ('ap02_jahresabschluss_status_guard','ap02_jahresabschluss_korrektur_guard') AND NOT tgisinternal) <> 2 THEN
   RAISE EXCEPTION 'Financial guard triggers missing';
 END IF;
END $verify$;;
