-- Run ONLY inside BEGIN/ROLLBACK on the DemoDB, never as a migration.
-- Existing non-protected demo identity; temporary right overrides are rolled back.
DO $fr211_fixture$
DECLARE uid uuid;
BEGIN
  SELECT u.user_id INTO uid FROM public.app_user u
  JOIN public.app_user_hauptrolle h ON h.user_id=u.user_id
  JOIN public.mitglied m ON m.id=u.mitglied_id AND m.auth_user_id=u.user_id
  JOIN auth.users a ON a.id=u.user_id
  WHERE public.ap03_has_current_membership(u.user_id)
    AND NOT EXISTS (SELECT 1 FROM public.protected_admin_identity p WHERE p.user_id=u.user_id)
  ORDER BY u.user_id LIMIT 1;
  IF uid IS NULL THEN RAISE EXCEPTION 'FR211 fixture: no non-protected demo identity'; END IF;
  PERFORM set_config('fr211.fixture_uid',uid::text,true);
  INSERT INTO public.app_user_fachrecht_override(user_id,fachrecht_code,entscheidung,grund)
  SELECT uid,code,'deny','FR211 transaction fixture' FROM public.fachrecht
   WHERE code IN ('auswertung.lesen','export.ausfuehren','mitglied.lesen','parzelle.lesen','arbeitsstunden.lesen','zaehler.lesen','serienbrief.erstellen')
  ON CONFLICT (user_id,fachrecht_code) DO UPDATE SET entscheidung=excluded.entscheidung,grund=excluded.grund;
END;
$fr211_fixture$;
SET LOCAL ROLE authenticated;
DO $fr211_denied$
BEGIN
  PERFORM set_config('request.jwt.claim.sub',current_setting('fr211.fixture_uid'),true);
  PERFORM set_config('request.jwt.claims',jsonb_build_object('sub',current_setting('fr211.fixture_uid'),'role','authenticated')::text,true);
  IF public.ap02_has_effective_right('export.ausfuehren') THEN RAISE EXCEPTION 'fixture deny not effective'; END IF;
  BEGIN
    PERFORM public.rpc_execute_export('mitgliederliste','{}');
    RAISE EXCEPTION 'FR211 unauthorized export succeeded';
  EXCEPTION WHEN insufficient_privilege THEN NULL; END;
  BEGIN
    PERFORM public.rpc_fr211_jae_export(2026);
    RAISE EXCEPTION 'FR211 unauthorized JAE succeeded';
  EXCEPTION WHEN insufficient_privilege THEN NULL; END;
  BEGIN
    PERFORM public.rpc_export_mitgliederliste_json('{}');
    RAISE EXCEPTION 'FR211 raw export succeeded';
  EXCEPTION WHEN insufficient_privilege THEN NULL; END;
END;
$fr211_denied$;
RESET ROLE;
UPDATE public.app_user_fachrecht_override SET entscheidung='grant'
 WHERE user_id=current_setting('fr211.fixture_uid')::uuid AND fachrecht_code IN ('auswertung.lesen','export.ausfuehren');
SET LOCAL ROLE authenticated;
DO $fr211_domain_denied$
BEGIN
  IF NOT public.ap02_has_effective_right('auswertung.lesen') OR NOT public.ap02_has_effective_right('export.ausfuehren') THEN RAISE EXCEPTION 'fixture grant not effective'; END IF;
  BEGIN
    PERFORM public.rpc_execute_export('mitgliederliste','{}');
    RAISE EXCEPTION 'FR211 export without domain read succeeded';
  EXCEPTION WHEN insufficient_privilege THEN NULL; END;
END;
$fr211_domain_denied$;
RESET ROLE;
UPDATE public.app_user_fachrecht_override SET entscheidung='grant'
 WHERE user_id=current_setting('fr211.fixture_uid')::uuid AND fachrecht_code IN ('mitglied.lesen','parzelle.lesen','arbeitsstunden.lesen','zaehler.lesen');
UPDATE public.app_user_fachrecht_override SET entscheidung='deny'
 WHERE user_id=current_setting('fr211.fixture_uid')::uuid AND fachrecht_code='auswertung.lesen';
SET LOCAL ROLE authenticated;
DO $fr211_no_analysis$
BEGIN
  BEGIN
    PERFORM public.rpc_execute_export('mitgliederliste','{}');
    RAISE EXCEPTION 'FR211 export without analysis right succeeded';
  EXCEPTION WHEN insufficient_privilege THEN NULL; END;
END;
$fr211_no_analysis$;
RESET ROLE;
UPDATE public.app_user_fachrecht_override SET entscheidung='grant'
 WHERE user_id=current_setting('fr211.fixture_uid')::uuid AND fachrecht_code='auswertung.lesen';
UPDATE public.app_user_fachrecht_override SET entscheidung='deny'
 WHERE user_id=current_setting('fr211.fixture_uid')::uuid AND fachrecht_code='export.ausfuehren';
SET LOCAL ROLE authenticated;
DO $fr211_no_export$
BEGIN
  BEGIN
    PERFORM public.rpc_execute_export('mitgliederliste','{}');
    RAISE EXCEPTION 'FR211 analysis right substituted export right';
  EXCEPTION WHEN insufficient_privilege THEN NULL; END;
END;
$fr211_no_export$;
RESET ROLE;
UPDATE public.app_user_fachrecht_override SET entscheidung='grant'
 WHERE user_id=current_setting('fr211.fixture_uid')::uuid AND fachrecht_code='export.ausfuehren';
-- The demo has no naturally due meters. Temporarily make one occupied parcel
-- meter due, solely inside the enclosing rollback transaction.
DO $fr211_meter_fixture$
DECLARE v_meter bigint;
BEGIN
  UPDATE public.zaehler z SET defekt=true
  WHERE z.id=(SELECT c.id FROM public.zaehler c
    WHERE c.parzelle_id IS NOT NULL AND c.status::text='aktiv'
      AND c.ausgebaut_am IS NULL AND NOT coalesce(c.defekt,false)
      AND EXISTS (SELECT 1 FROM public.parzellen_belegung b
        WHERE b.parzelle_id=c.parzelle_id AND b.von_datum<=current_date
          AND (b.bis_datum IS NULL OR b.bis_datum>=current_date))
    ORDER BY c.id LIMIT 1)
  RETURNING z.id INTO v_meter;
  IF v_meter IS NULL THEN RAISE EXCEPTION 'FR211 no suitable occupied demo meter'; END IF;
  PERFORM set_config('fr211.fixture_meter',v_meter::text,true);
END;
$fr211_meter_fixture$;
SET LOCAL ROLE authenticated;
DO $fr211_allowed$
DECLARE k text; n bigint; expected bigint; v_jae jsonb; v_row jsonb;
BEGIN
  IF NOT public.ap02_has_effective_right('mitglied.lesen') OR NOT public.ap02_has_effective_right('parzelle.lesen') OR NOT public.ap02_has_effective_right('arbeitsstunden.lesen') OR NOT public.ap02_has_effective_right('zaehler.lesen') THEN RAISE EXCEPTION 'fixture domain grants not effective'; END IF;
  BEGIN
    PERFORM public.rpc_execute_export('mitgliederliste','{"mitglied_id":1}');
    RAISE EXCEPTION 'FR211 unknown filter accepted';
  EXCEPTION WHEN invalid_parameter_value THEN NULL; END;
  BEGIN
    PERFORM public.rpc_execute_export('mitgliederliste','{"aktiv_filter":"other"}');
    RAISE EXCEPTION 'FR211 invalid filter accepted';
  EXCEPTION WHEN invalid_parameter_value THEN NULL; END;
  BEGIN
    PERFORM public.rpc_execute_export('unknown','{}');
    RAISE EXCEPTION 'FR211 unknown export accepted';
  EXCEPTION WHEN insufficient_privilege THEN NULL; END;
  SELECT count(*) INTO expected FROM public.mitglied m JOIN public.mitglied hm ON hm.id=coalesce(m.hauptmitglied_id,m.id);
  SELECT count(*) INTO n FROM public.rpc_execute_export('mitgliederliste','{"aktiv_filter":"alle"}') r;
  IF n <> expected OR n=0 THEN RAISE EXCEPTION 'FR211 member count mismatch: expected %, actual %',expected,n; END IF;
  IF EXISTS(SELECT 1 FROM public.rpc_execute_export('mitgliederliste','{"aktiv_filter":"alle"}') r WHERE r ?| ARRAY['id','auth_user_id','mitglied_id','hauptmitglied_id','principal_member_id']) THEN RAISE EXCEPTION 'FR211 member internal fields leaked'; END IF;
  FOREACH k IN ARRAY ARRAY['parzellenliste','datenschutz_aktive_mitglieder','jahresablesung_status','zaehler_eichfaelligkeit'] LOOP
    SELECT count(*) INTO n FROM public.rpc_execute_export(k,CASE WHEN k='jahresablesung_status' THEN '{"p_jahr":2026}'::jsonb ELSE '{}'::jsonb END);
    RAISE NOTICE 'FR211 SQL execution %: % rows',k,n;
  END LOOP;
  -- Normal UI default: one season, not all 51 historical seasons.
  SELECT count(*) INTO expected FROM fr211_export_private.rpc_export_arbeitsstunden_uebersicht_json('{"p_jahr":2026}');
  SELECT count(*) INTO n FROM public.rpc_execute_export('arbeitsstunden_uebersicht','{"p_jahr":2026}');
  IF n=0 OR n<>expected THEN RAISE EXCEPTION 'FR211 annual work-hours count mismatch: expected %, actual %',expected,n; END IF;
  IF EXISTS (SELECT 1 FROM public.rpc_execute_export('arbeitsstunden_uebersicht','{"p_jahr":2026}') r WHERE r->>'jahr' IS DISTINCT FROM '2026') THEN RAISE EXCEPTION 'FR211 annual work-hours scope mismatch'; END IF;
  RAISE NOTICE 'FR211 SQL work-hours 2026: % rows',n;
  -- Existing history exceeds the API ceiling: enforce rejection, never truncation.
  SELECT count(*) INTO expected FROM fr211_export_private.rpc_export_arbeitsstunden_uebersicht_json('{}');
  IF expected<=1000 THEN RAISE EXCEPTION 'FR211 row-limit fixture no longer exceeds 1000: %',expected; END IF;
  BEGIN
    PERFORM public.rpc_execute_export('arbeitsstunden_uebersicht','{}');
    RAISE EXCEPTION 'FR211 oversized export was accepted or silently truncated';
  EXCEPTION WHEN invalid_parameter_value THEN
    IF SQLERRM <> 'EXPORT_ROW_LIMIT_EXCEEDED' THEN RAISE; END IF;
  END;
  RAISE NOTICE 'FR211 SQL row-limit rejected % source rows',expected;
  IF NOT EXISTS(SELECT 1 FROM public.rpc_execute_export('zaehler_eichfaelligkeit','{}') r WHERE r->>'zaehler_id'=current_setting('fr211.fixture_meter')) THEN RAISE EXCEPTION 'FR211 due-meter fixture not visible without series right'; END IF;
  IF EXISTS(SELECT 1 FROM public.rpc_execute_export('zaehler_eichfaelligkeit','{}') r WHERE r ?| ARRAY['mitglied_id','parzelle_id','serienbrief_empfaenger_status']) THEN RAISE EXCEPTION 'FR211 series routing fields leaked'; END IF;
  PERFORM public.rpc_export_arbeitsstunden_jahre();
  PERFORM public.rpc_export_jahresablesung_jahre();
  v_jae := public.rpc_fr211_jae_export(2026);
  IF jsonb_typeof(v_jae->'segments') IS DISTINCT FROM 'array' OR jsonb_typeof(v_jae->'differences') IS DISTINCT FROM 'array' OR jsonb_typeof(v_jae->'parcels') IS DISTINCT FROM 'array' THEN RAISE EXCEPTION 'FR211 JAE result invalid'; END IF;
  IF EXISTS(SELECT 1 FROM jsonb_array_elements(v_jae->'segments') s WHERE s ?| ARRAY['mitglied_id','start_ablesung_id','end_ablesung_id']) THEN RAISE EXCEPTION 'FR211 JAE internal fields leaked'; END IF;
END;
$fr211_allowed$;
RESET ROLE;
UPDATE public.app_user_fachrecht_override SET entscheidung='grant'
 WHERE user_id=current_setting('fr211.fixture_uid')::uuid AND fachrecht_code='serienbrief.erstellen';
SET LOCAL ROLE authenticated;
DO $fr211_series$
DECLARE n bigint;
BEGIN
  IF NOT public.ap02_has_effective_right('serienbrief.erstellen') THEN RAISE EXCEPTION 'FR211 series fixture grant not effective'; END IF;
  SELECT count(*) INTO n FROM public.rpc_execute_export('zaehler_eichfaelligkeit','{}') r;
  IF n=0 OR NOT EXISTS(SELECT 1 FROM public.rpc_execute_export('zaehler_eichfaelligkeit','{}') r
      WHERE r->>'zaehler_id'=current_setting('fr211.fixture_meter')
        AND r->>'mitglied_id' IS NOT NULL AND r->>'parzelle_id' IS NOT NULL
        AND r->>'serienbrief_empfaenger_status' IN ('hauptpaechter','legacy_eindeutig'))
    OR EXISTS(SELECT 1 FROM public.rpc_execute_export('zaehler_eichfaelligkeit','{}') r WHERE NOT (r ?& ARRAY['mitglied_id','parzelle_id','serienbrief_empfaenger_status'])) THEN
    RAISE EXCEPTION 'FR211 series routing contract missing or positive fixture empty';
  END IF;
END;
$fr211_series$;
RESET ROLE;
SELECT 'FR211_FIXTURE_PASS' AS result;
