-- AP-02.4H1: 16 Bestands-RPCs/Trigger auf Fachrechte 2.0
-- Die bisherigen Prozess- und Datensatzpruefungen bleiben unveraendert.
-- Beruehrt keinerlei Vereins-/Vorgangs-/Dokumentdaten; nur Funktionsdefinitionen.
-- Keine automatische Uebernahme von alten individuellen Rechten.
-- Installationsreihenfolge: nach AP-02.4G1/G2; vor weiteren Fachbereichs-Cutovers.
DO $migration$
DECLARE
  v record;
  replacement record;
  v_function text;
  v_old text;
  v_new text;
  v_count integer := 0;
  v_expected integer := 16;
BEGIN
  IF to_regprocedure('public.ap02_has_effective_right(text)') IS NULL
     OR (SELECT count(*) FROM public.fachrecht) <> 55 THEN
    RAISE EXCEPTION 'AP-02 Fachrechte-Kern noch nicht bereit';
  END IF;
  CREATE TEMP TABLE ap02_h1_map(
     funktionsname text NOT NULL,
     altbit bigint NOT NULL,
     neues_recht text NOT NULL,
     raw_predicate boolean NOT NULL DEFAULT false,
     PRIMARY KEY(funktionsname,altbit)
  ) ON COMMIT DROP;
  INSERT INTO ap02_h1_map VALUES
  ('acquire_browser_edit_lock_core',1048576::bigint,'arbeitseinsatz.verwalten',false),
  ('acquire_browser_edit_lock_core',2097152::bigint,'termin.verwalten',false),
  ('acquire_browser_edit_lock_core',4194304::bigint,'bekanntmachung.verwalten',false),
  ('assert_export_access',33554432::bigint,'export.ausfuehren',false),
  ('can_register_historical_contracts',16777216::bigint,'historische_vertraege.erfassen',false),
  ('kann_vertragsversand_ausfuehren',1073741824::bigint,'vertrag.versenden',false),
  ('manage_arbeitseinsatz_anmeldung',1048576::bigint,'arbeitseinsatz.verwalten',false),
  ('confirm_arbeitseinsatz_arbeitsstunde',1048576::bigint,'arbeitseinsatz.verwalten',false),
  ('confirm_arbeitseinsatz_arbeitsstunde',256::bigint,'arbeitsstunden.freigeben',false),
  ('save_dokumenttyp',134217728::bigint,'dokumenttyp.verwalten',false),
  ('kann_mitgliedsaufnahme_entscheiden',524288::bigint,'mitgliedschaft.entscheiden',false),
  ('start_mitgliedsaufnahme_vorstandsbearbeitung',524288::bigint,'vorstandsvorgang.bearbeiten',false),
  ('resolve_rfid_scan_context',32::bigint,'zaehler.lesen',false),
  ('capture_meter_reading',32::bigint,'ablesung.erfassen',false),
  ('can_upload_meter_reading_photo',32::bigint,'ablesung.erfassen',false),
  ('review_arbeitsstunde',256::bigint,'arbeitsstunden.freigeben',false),
  ('enforce_administrative_arbeitsstunde_insert',256::bigint,'(public.ap02_has_effective_right(''arbeitsstunden.bearbeiten'') AND public.ap02_has_effective_right(''arbeitsstunden.freigeben''))',true),
  ('correct_meter_change_request',64::bigint,'zaehlerwechsel.verwalten',false),
  ('review_meter_change_request',64::bigint,'zaehlerwechsel.verwalten',false);
  IF EXISTS (
     SELECT 1 FROM ap02_h1_map m
     WHERE NOT m.raw_predicate AND NOT EXISTS(
       SELECT 1 FROM public.fachrecht f WHERE f.code=m.neues_recht AND f.aktiv
     )
  ) THEN
     RAISE EXCEPTION 'Fachrecht ungueltig oder inaktiv';
  END IF;
  FOR v IN
    SELECT DISTINCT funktionsname FROM ap02_h1_map ORDER BY funktionsname
  LOOP
    IF (
      SELECT count(*)
      FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
      WHERE n.nspname='public' AND p.proname=v.funktionsname AND p.prokind='f'
    ) <> 1 THEN
      RAISE EXCEPTION 'Erwartet genau eine Funktion: %', v.funktionsname;
    END IF;
    SELECT pg_get_functiondef(p.oid) INTO v_function
    FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
    WHERE n.nspname='public' AND p.proname=v.funktionsname AND p.prokind='f';
    FOR replacement IN
       SELECT * FROM ap02_h1_map m
       WHERE m.funktionsname=v.funktionsname ORDER BY m.altbit
    LOOP
       v_old:=format('public.has_effective_permission(%s)',replacement.altbit);
       IF position(v_old IN v_function)=0 THEN
         RAISE EXCEPTION 'Vorbedingung nicht erfuellt: %, Bit %',v.funktionsname,replacement.altbit;
       END IF;
       IF replacement.raw_predicate THEN
         v_new:=replacement.neues_recht;
       ELSE
         v_new:=format('public.ap02_has_effective_right(%L)',replacement.neues_recht);
       END IF;
       v_function:=replace(v_function,v_old,v_new);
    END LOOP;
    IF position('public.has_effective_permission(' IN v_function)>0 THEN
       RAISE EXCEPTION 'Weitere alte Rechtepruefungen in %',v.funktionsname;
    END IF;
    EXECUTE v_function;
    v_count:=v_count+1;
  END LOOP;
  IF v_count <> v_expected THEN
    RAISE EXCEPTION 'Erwartet % Funktionen, erhalten %',v_expected,v_count;
  END IF;
END $migration$;

DO $postcheck$
DECLARE v integer;
BEGIN
  SELECT count(*) INTO v
  FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
  WHERE n.nspname='public'
    AND p.proname IN (
      'acquire_browser_edit_lock_core','assert_export_access','can_register_historical_contracts',
      'kann_vertragsversand_ausfuehren','manage_arbeitseinsatz_anmeldung',
      'confirm_arbeitseinsatz_arbeitsstunde','save_dokumenttyp',
      'kann_mitgliedsaufnahme_entscheiden','start_mitgliedsaufnahme_vorstandsbearbeitung',
      'resolve_rfid_scan_context','capture_meter_reading','can_upload_meter_reading_photo',
      'review_arbeitsstunde','enforce_administrative_arbeitsstunde_insert',
      'correct_meter_change_request','review_meter_change_request'
    )
    AND pg_get_functiondef(p.oid) LIKE '%public.has_effective_permission(%';
  IF v<>0 THEN
    RAISE EXCEPTION 'Noch % alte Rechtepruefungen in migrierten Funktionen',v;
  END IF;
  IF ((EXISTS (SELECT 1 FROM auth.users) OR
      EXISTS (SELECT 1 FROM public.app_user) OR
      EXISTS (SELECT 1 FROM public.mitglied) OR
      EXISTS (SELECT 1 FROM public.parzelle) OR
      EXISTS (SELECT 1 FROM public.pachtverhaeltnis) OR
      EXISTS (SELECT 1 FROM public.dokument) OR
      EXISTS (SELECT 1 FROM public.kommunikationsvorgang) OR
      EXISTS (SELECT 1 FROM public.zaehler) OR
      EXISTS (SELECT 1 FROM public.arbeitsstunde) OR
      EXISTS (SELECT 1 FROM public.wartungsvertraege) OR
      EXISTS (SELECT 1 FROM public.vereinsfunktion) OR
      EXISTS (SELECT 1 FROM public.vereinsfunktion_besetzung) OR
      EXISTS (SELECT 1 FROM public.vereinsfunktion_verwendung) OR
      EXISTS (SELECT 1 FROM public.vereinsfunktion_vertretung)) AND NOT EXISTS (SELECT 1 FROM public.protected_admin_identity)) OR
    EXISTS (
      SELECT 1 FROM public.protected_admin_identity p
      WHERE NOT EXISTS (SELECT 1 FROM public.app_user_zusatzrolle z
        WHERE z.user_id=p.user_id AND z.zusatzrolle_code='admin')
    ) OR
    EXISTS (
    SELECT a.user_id FROM public.app_user a WHERE a.is_protected = true
    EXCEPT
    SELECT p.user_id FROM public.protected_admin_identity p
  ) OR EXISTS (
    SELECT p.user_id FROM public.protected_admin_identity p
    EXCEPT
    SELECT a.user_id FROM public.app_user a WHERE a.is_protected = true
  ) THEN
    RAISE EXCEPTION 'Geschuetzter Admin ist inkonsistent';
  END IF;
END $postcheck$;
;
