-- AP-02.4F.1: Eine aktive Vereinsfunktion Bauausschuss mit Mehrfachbesetzung.
-- VDB: drei historische Einzelpositionen werden nicht geloescht oder umbenannt.
-- Ihre laufenden Besetzungen enden am Vortag des Deployments und beginnen
-- bei der neuen gemeinsamen Funktion ab Deploymenttag. Referenzen bleiben bestehen.
-- DemoDB: vorhandene gemeinsame Funktion unveraendert lassen.
-- Gemeinsame Migration: keine fest kodierten Mitglieds- oder Funktions-IDs.

LOCK TABLE public.vereinsfunktion, public.vereinsfunktion_besetzung,
  public.vereinsfunktion_verwendung IN SHARE ROW EXCLUSIVE MODE;

DO $merge$
DECLARE
  v_unified_id bigint;
  v_legacy_count integer;
  v_metadata_variants integer;
  v_assignment record;
BEGIN
  SELECT id INTO v_unified_id
  FROM public.vereinsfunktion WHERE schluessel = 'bauausschuss';

  SELECT count(*) INTO v_legacy_count FROM public.vereinsfunktion
  WHERE schluessel IN ('bauausschuss_1','bauausschuss_2','bauausschuss_3');

  IF v_unified_id IS NOT NULL THEN
    IF NOT EXISTS (
      SELECT 1 FROM public.vereinsfunktion
      WHERE id=v_unified_id AND aktiv AND erlaubt_mehrfachbesetzung
        AND bezeichnung='Bauausschuss'
    ) OR EXISTS (
      SELECT 1 FROM public.vereinsfunktion
      WHERE schluessel IN ('bauausschuss_1','bauausschuss_2','bauausschuss_3')
        AND aktiv
    ) OR EXISTS (
      SELECT 1 FROM public.vereinsfunktion_besetzung b
      JOIN public.vereinsfunktion f ON f.id=b.vereinsfunktion_id
      WHERE f.schluessel IN ('bauausschuss_1','bauausschuss_2','bauausschuss_3')
        AND (b.gueltig_bis IS NULL OR b.gueltig_bis >= current_date)
    ) THEN
      RAISE EXCEPTION 'Bauausschuss: gemischter oder ungueltiger Bestand'
        USING ERRCODE='55000';
    END IF;
    RETURN;
  END IF;

  IF v_legacy_count <> 3 OR (
    SELECT count(*) FROM public.vereinsfunktion
    WHERE schluessel IN ('bauausschuss_1','bauausschuss_2','bauausschuss_3')
      AND aktiv AND bezeichnung='Bauausschuss'
  ) <> 3 THEN
    RAISE EXCEPTION 'Bauausschuss: erwartete drei aktive Legacy-Funktionen fehlen'
      USING ERRCODE='55000';
  END IF;

  IF EXISTS (
    SELECT 1 FROM public.vereinsfunktion_besetzung b
    JOIN public.vereinsfunktion f ON f.id=b.vereinsfunktion_id
    WHERE f.schluessel IN ('bauausschuss_1','bauausschuss_2','bauausschuss_3')
      AND b.gueltig_ab >= current_date
  ) THEN
    RAISE EXCEPTION 'Bauausschuss: heutige oder zukuenftige Legacy-Besetzung erfordert Pruefung'
      USING ERRCODE='55000';
  END IF;

  IF EXISTS (
    SELECT b.mitglied_id FROM public.vereinsfunktion_besetzung b
    JOIN public.vereinsfunktion f ON f.id=b.vereinsfunktion_id
    WHERE f.schluessel IN ('bauausschuss_1','bauausschuss_2','bauausschuss_3')
      AND b.gueltig_ab <= current_date
      AND (b.gueltig_bis IS NULL OR b.gueltig_bis >= current_date)
    GROUP BY b.mitglied_id HAVING count(*) > 1
  ) THEN
    RAISE EXCEPTION 'Bauausschuss: doppelte laufende Mitgliedsbesetzung'
      USING ERRCODE='55000';
  END IF;

  -- Nur der bereits vorhandene Impressum-Zweck wird fuer die neue Funktion
  -- uebernommen. Abweichende oder neue Zwecke duerfen keine Rechte erweitern.
  IF (SELECT count(*) FROM public.vereinsfunktion_verwendung v
      JOIN public.vereinsfunktion f ON f.id=v.vereinsfunktion_id
      WHERE f.schluessel IN ('bauausschuss_1','bauausschuss_2','bauausschuss_3')
        AND v.verwendungszweck='impressum') <> 3
    OR (SELECT count(*) FROM public.vereinsfunktion_verwendung v
      JOIN public.vereinsfunktion f ON f.id=v.vereinsfunktion_id
      WHERE f.schluessel IN ('bauausschuss_1','bauausschuss_2','bauausschuss_3')) <> 3
  THEN
    RAISE EXCEPTION 'Bauausschuss: Verwendungszwecke weichen vom getesteten Bestand ab'
      USING ERRCODE='55000';
  END IF;

  SELECT count(DISTINCT (
    v.impressum_gruppe, v.impressum_gruppe_bezeichnung,
    v.impressum_gruppen_sort_order, v.impressum_ist_hauptkontakt,
    v.impressum_zeige_ladungsfaehige_anschrift
  )) INTO v_metadata_variants
  FROM public.vereinsfunktion_verwendung v
  JOIN public.vereinsfunktion f ON f.id=v.vereinsfunktion_id
  WHERE f.schluessel IN ('bauausschuss_1','bauausschuss_2','bauausschuss_3');

  IF v_metadata_variants <> 1 THEN
    RAISE EXCEPTION 'Bauausschuss: Impressum-Metadaten sind nicht einheitlich'
      USING ERRCODE='55000';
  END IF;

  IF EXISTS (
    SELECT 1 FROM public.vereinsfunktion_vertretung d
    JOIN public.vereinsfunktion_besetzung b
      ON b.id IN (d.vertretene_besetzung_id,d.vertreter_besetzung_id)
    JOIN public.vereinsfunktion f ON f.id=b.vereinsfunktion_id
    WHERE f.schluessel IN ('bauausschuss_1','bauausschuss_2','bauausschuss_3')
      AND (d.gueltig_bis IS NULL OR d.gueltig_bis >= current_date)
  ) THEN
    RAISE EXCEPTION 'Bauausschuss: laufende Vertretung muss gesondert uebernommen werden'
      USING ERRCODE='55000';
  END IF;

  -- Ausgangsbestandsdaten koennen explizit gesetzte IDs enthalten.
  -- Sequenzen vor allen Neuanlagen an den hoechsten vorhandenen Datensatz anbinden.
  PERFORM setval('public.vereinsfunktion_id_seq'::regclass,
    GREATEST((SELECT COALESCE(MAX(id),1) FROM public.vereinsfunktion),
             (SELECT last_value FROM public.vereinsfunktion_id_seq)),true);
  PERFORM setval('public.vereinsfunktion_besetzung_id_seq'::regclass,
    GREATEST((SELECT COALESCE(MAX(id),1) FROM public.vereinsfunktion_besetzung),
             (SELECT last_value FROM public.vereinsfunktion_besetzung_id_seq)),true);
  PERFORM setval('public.vereinsfunktion_verwendung_id_seq'::regclass,
    GREATEST((SELECT COALESCE(MAX(id),1) FROM public.vereinsfunktion_verwendung),
             (SELECT last_value FROM public.vereinsfunktion_verwendung_id_seq)),true);

  INSERT INTO public.vereinsfunktion
    (id,schluessel,bezeichnung,aktiv,erlaubt_mehrfachbesetzung)
  VALUES (nextval('public.vereinsfunktion_id_seq'),
    'bauausschuss','Bauausschuss',true,true)
  RETURNING id INTO v_unified_id;

  INSERT INTO public.vereinsfunktion_verwendung (
    id,vereinsfunktion_id,verwendungszweck,sort_order,
    impressum_gruppe,impressum_gruppe_bezeichnung,
    impressum_gruppen_sort_order,impressum_ist_hauptkontakt,
    impressum_zeige_ladungsfaehige_anschrift
  )
  SELECT nextval('public.vereinsfunktion_verwendung_id_seq'),
    v_unified_id,v.verwendungszweck,v.sort_order,
    v.impressum_gruppe,v.impressum_gruppe_bezeichnung,
    v.impressum_gruppen_sort_order,v.impressum_ist_hauptkontakt,
    v.impressum_zeige_ladungsfaehige_anschrift
  FROM public.vereinsfunktion_verwendung v
  JOIN public.vereinsfunktion f ON f.id=v.vereinsfunktion_id
  WHERE f.schluessel='bauausschuss_1';

  FOR v_assignment IN
    SELECT b.id,b.mitglied_id,b.sort_order
    FROM public.vereinsfunktion_besetzung b
    JOIN public.vereinsfunktion f ON f.id=b.vereinsfunktion_id
    WHERE f.schluessel IN ('bauausschuss_1','bauausschuss_2','bauausschuss_3')
      AND b.gueltig_ab < current_date
      AND (b.gueltig_bis IS NULL OR b.gueltig_bis >= current_date)
    ORDER BY b.id
  LOOP
    UPDATE public.vereinsfunktion_besetzung
      SET gueltig_bis = current_date - 1
      WHERE id = v_assignment.id;

    INSERT INTO public.vereinsfunktion_besetzung
      (id,vereinsfunktion_id,mitglied_id,gueltig_ab,gueltig_bis,sort_order)
    VALUES (
      nextval('public.vereinsfunktion_besetzung_id_seq'),
      v_unified_id,v_assignment.mitglied_id,current_date,NULL,v_assignment.sort_order
    );
  END LOOP;

  UPDATE public.vereinsfunktion SET aktiv=false
  WHERE schluessel IN ('bauausschuss_1','bauausschuss_2','bauausschuss_3');

  IF (SELECT count(*) FROM public.vereinsfunktion
      WHERE aktiv AND schluessel IN ('bauausschuss','bauausschuss_1','bauausschuss_2','bauausschuss_3')) <> 1
    OR EXISTS (
      SELECT 1 FROM public.vereinsfunktion_besetzung b
      JOIN public.vereinsfunktion f ON f.id=b.vereinsfunktion_id
      WHERE f.schluessel IN ('bauausschuss_1','bauausschuss_2','bauausschuss_3')
        AND (b.gueltig_bis IS NULL OR b.gueltig_bis >= current_date)
    ) THEN
    RAISE EXCEPTION 'Bauausschuss: Konsolidierung unvollstaendig'
      USING ERRCODE='55000';
  END IF;
END
$merge$;
