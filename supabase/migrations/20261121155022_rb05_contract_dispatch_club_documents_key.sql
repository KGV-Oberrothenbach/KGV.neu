-- RB-05: Vertragseinreichungen aus Web/MAUI verwenden den kanonischen
-- JSONB-Schluessel "vereinsunterlagen_nachweis". Bestehende Altvorgaenge koennen
-- "vereinsunterlagenNachweis" enthalten. Die Versand-Snapshot-Erzeugung muss
-- beides lesen; sonst scheitert die abschliessende Vertragsverknuepfung trotz
-- nachgewiesener Papieruebergabe. Keine Berechtigungen oder Dokumente aendern.
DO $rb05$
DECLARE
  v_definition text;
  v_old text := $old$v.entwurf_daten->'vereinsunterlagenNachweis'$old$;
  v_new text := $new$coalesce(v.entwurf_daten->'vereinsunterlagen_nachweis',v.entwurf_daten->'vereinsunterlagenNachweis')$new$;
BEGIN
  SELECT pg_get_functiondef(p.oid) INTO STRICT v_definition
  FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
  WHERE n.nspname='public' AND p.proname='ensure_vertragsversand_snapshot'
    AND pg_get_function_identity_arguments(p.oid)='p_kommunikationsvorgang_id bigint';

  IF length(v_definition)-length(replace(v_definition,v_old,'')) <> length(v_old) THEN
    RAISE EXCEPTION 'RB05: expected legacy key access missing or duplicated; migration aborted';
  END IF;

  EXECUTE replace(v_definition,v_old,v_new);

  IF NOT EXISTS (
    SELECT 1 FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
    WHERE n.nspname='public' AND p.proname='ensure_vertragsversand_snapshot'
      AND position(v_new IN pg_get_functiondef(p.oid))>0
  ) THEN
    RAISE EXCEPTION 'RB05: key compatibility change was not installed';
  END IF;
END;
$rb05$;
