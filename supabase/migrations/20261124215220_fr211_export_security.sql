-- FR2-11: Export security; based on verified FR2-10 catalog.
-- Deployment is gated on rollback SQL tests and local builds.
DO $fr211_pre$
DECLARE v_bad text;
BEGIN
  IF to_regprocedure('public.ap02_has_effective_right(text)') IS NULL
     OR to_regprocedure('public.assert_export_access()') IS NULL
     OR to_regprocedure('public.rpc_execute_export(text,jsonb)') IS NULL
  THEN
    RAISE EXCEPTION 'FR211_PRECONDITION: Fachrechte or export gateway absent';
  END IF;
  IF (SELECT count(*) FROM public.fachrecht) <> 55 THEN
    RAISE EXCEPTION 'FR211_PRECONDITION: unexpected fachrecht catalog size';
  END IF;
  SELECT d.export_key INTO v_bad
  FROM public.app_export_definition d
  WHERE d.aktiv AND NOT (
      (d.export_key = 'mitgliederliste' AND d.quelle_typ = 'rpc' AND d.quelle_name = 'rpc_export_mitgliederliste_json') OR
      (d.export_key = 'parzellenliste' AND d.quelle_typ = 'rpc' AND d.quelle_name = 'rpc_export_parzellenliste') OR
      (d.export_key = 'datenschutz_aktive_mitglieder' AND d.quelle_typ = 'rpc' AND d.quelle_name = 'rpc_export_datenschutz_aktive_mitglieder_json') OR
      (d.export_key = 'arbeitsstunden_uebersicht' AND d.quelle_typ = 'rpc' AND d.quelle_name = 'rpc_export_arbeitsstunden_uebersicht_json') OR
      (d.export_key = 'jahresablesung_status' AND d.quelle_typ = 'rpc' AND d.quelle_name = 'rpc_export_jahresablesung_status_json') OR
      (d.export_key = 'zaehler_eichfaelligkeit' AND d.quelle_typ = 'rpc' AND d.quelle_name = 'rpc_export_zaehler_eichfaelligkeit_json')
  ) LIMIT 1;
  IF v_bad IS NOT NULL THEN
    RAISE EXCEPTION 'FR211_PRECONDITION: unexpected active export contract %', v_bad;
  END IF;
  IF (SELECT count(*) FROM public.app_export_definition WHERE aktiv) <> 6 THEN
    RAISE EXCEPTION 'FR211_PRECONDITION: active exports changed; rebase after FR2-10';
  END IF;
END;
$fr211_pre$;

-- One central authorization matrix. No new Fachrechte introduced.
CREATE OR REPLACE FUNCTION public.fr211_assert_export_source_read(p_export_key text)
RETURNS void
LANGUAGE plpgsql STABLE SECURITY INVOKER
SET search_path TO pg_catalog, public, pg_temp
AS $fr211_rights$
DECLARE v_required text[];
BEGIN
  CASE p_export_key
    WHEN 'mitgliederliste', 'datenschutz_aktive_mitglieder'
      THEN v_required := ARRAY['mitglied.lesen', 'parzelle.lesen'];
    WHEN 'parzellenliste'
      THEN v_required := ARRAY['parzelle.lesen', 'mitglied.lesen'];
    WHEN 'arbeitsstunden_uebersicht'
      THEN v_required := ARRAY['arbeitsstunden.lesen', 'mitglied.lesen', 'parzelle.lesen'];
    WHEN 'jahresablesung_status', 'zaehler_eichfaelligkeit'
      THEN v_required := ARRAY['zaehler.lesen', 'parzelle.lesen', 'mitglied.lesen'];
    WHEN 'jae_xlsx' THEN v_required := ARRAY['zaehler.lesen', 'parzelle.lesen', 'mitglied.lesen'];
    ELSE RAISE EXCEPTION 'EXPORT_NOT_ALLOWED' USING ERRCODE = '42501';
  END CASE;

  IF auth.uid() IS NULL OR NOT public.ap02_has_effective_right('auswertung.lesen')
     OR NOT public.ap02_has_effective_right('export.ausfuehren')
     OR EXISTS (SELECT 1 FROM unnest(v_required) AS right_name
                WHERE NOT public.ap02_has_effective_right(right_name))
  THEN
    RAISE EXCEPTION 'EXPORT_FORBIDDEN' USING ERRCODE = '42501';
  END IF;
END;
$fr211_rights$;

-- Keep the existing source implementations, but expose them only through
-- an invoker gateway. Private copies retain the original query semantics and
-- run with caller RLS. No private schema may be added to the Data API schemas.
CREATE SCHEMA fr211_export_private;
REVOKE ALL ON SCHEMA fr211_export_private FROM PUBLIC, anon, authenticated;
GRANT USAGE ON SCHEMA fr211_export_private TO authenticated;
DO $fr211_copy$
DECLARE f record; v_oid oid; v_def text; v_signature text;
BEGIN
  FOR f IN SELECT * FROM (VALUES
    ('rpc_export_mitgliederliste(text,text)', ARRAY['889e30b893e7bba83d9a9a53da8b039e']),
    ('rpc_export_datenschutz_aktive_mitglieder()', ARRAY['839980281f9c54a91185c2f9058f45a8']),
    ('rpc_export_arbeitsstunden_uebersicht(integer,boolean,boolean,boolean,text)', ARRAY['25717f58a7394b86a16037b37d056538']),
    -- The first hash is deployed. The second is the fresh-replay body after
    -- 6844150e added the enum-to-text cast required by an unseeded rebuild.
    ('rpc_export_jahresablesung_status(integer,boolean)', ARRAY['70ba7ca448d1f89072ee242f95895cbc', '93eee349ebc60c314aca27995612b680']),
    ('rpc_export_zaehler_eichfaelligkeit(text)', ARRAY['184e9a6018eafa43b9088b7aec31079e']),
    ('rpc_export_parzellenliste(jsonb)', ARRAY['cade40b4e116e26d2249ad425ef62df3']),
    ('rpc_export_mitgliederliste_json(jsonb)', ARRAY['c2bcb73292005577417f630a05784a78']),
    ('rpc_export_datenschutz_aktive_mitglieder_json(jsonb)', ARRAY['c4552126a2d77d7bb35e2d9e54fcae17']),
    ('rpc_export_arbeitsstunden_uebersicht_json(jsonb)', ARRAY['9b312cf61df8eb32a76eb7b76b848c36']),
    ('rpc_export_jahresablesung_status_json(jsonb)', ARRAY['7e80e0d5452a4170516dcc1fd28321f8']),
    ('rpc_export_zaehler_eichfaelligkeit_json(jsonb)', ARRAY['b2d5121d73da4eb542269566deeb3891'])
  ) AS x(signature, source_md5s)
  LOOP
    v_oid := to_regprocedure('public.' || f.signature);
    IF v_oid IS NULL OR NOT EXISTS (
      SELECT 1 FROM pg_proc p WHERE p.oid = v_oid AND NOT p.prosecdef
      AND md5(replace(p.prosrc, E'\r\n', E'\n')) = ANY(f.source_md5s)
    ) THEN RAISE EXCEPTION 'FR211 source drift: %', f.signature; END IF;
    v_def := replace(pg_get_functiondef(v_oid), 'public.rpc_export_', 'fr211_export_private.rpc_export_');
    IF v_def NOT LIKE 'CREATE OR REPLACE FUNCTION fr211_export_private.%' THEN
      RAISE EXCEPTION 'FR211 unexpected function definition: %', f.signature;
    END IF;
    EXECUTE v_def;
    v_signature := 'fr211_export_private.' || f.signature;
    EXECUTE 'ALTER FUNCTION ' || v_signature || ' SECURITY INVOKER';
    EXECUTE 'ALTER FUNCTION ' || v_signature || ' SET search_path TO pg_catalog, public, pg_temp';
    EXECUTE 'REVOKE ALL ON FUNCTION ' || v_signature || ' FROM PUBLIC, anon, authenticated';
    EXECUTE 'GRANT EXECUTE ON FUNCTION ' || v_signature || ' TO authenticated';
  END LOOP;
END;
$fr211_copy$;

-- The API executes with the authenticated caller, never the table owner.
CREATE OR REPLACE FUNCTION public.rpc_execute_export(
  p_export_key text,
  p_parameters jsonb DEFAULT '{}'::jsonb
)
RETURNS SETOF jsonb
LANGUAGE plpgsql
SECURITY INVOKER
SET search_path TO pg_catalog, public, pg_temp
AS $fr211_gateway$
DECLARE
  v_source text;
  v_expected text;
  v_parameter_keys text[];
  v_allowed_columns text[];
  v_fixed_columns text[];
  v_serienbrief boolean;
  v_item jsonb;
  v_projection jsonb;
  v_result_count integer := 0;
BEGIN
  PERFORM public.fr211_assert_export_source_read(p_export_key);

  p_parameters := coalesce(p_parameters, '{}'::jsonb);
  IF jsonb_typeof(p_parameters) <> 'object'
     OR octet_length(p_parameters::text) > 8192 THEN
    RAISE EXCEPTION 'EXPORT_INVALID_PARAMETERS' USING ERRCODE = '22023';
  END IF;

  SELECT coalesce(jsonb_object_agg(e.key, CASE WHEN e.value = '""'::jsonb THEN 'null'::jsonb ELSE e.value END), '{}'::jsonb)
    INTO p_parameters FROM jsonb_each(p_parameters) e;

  CASE p_export_key
    WHEN 'mitgliederliste' THEN
      v_fixed_columns := ARRAY['adresse_block','aktiv_export','app','bemerkung','email','gartennummern','geburtsdatum','handy','info','kontakt_block','mitglied','mitglied_seit','mitgliedsnr','name','ort','plz','re','strasse_hsnr','telefon','vorname','wa'];
      v_expected := 'rpc_export_mitgliederliste_json';
      v_parameter_keys := ARRAY['aktiv_filter', 'sortierung'];
    WHEN 'parzellenliste' THEN
      v_fixed_columns := ARRAY['anlage','belegt','flaeche_qm','garten_nr','paechter'];
      v_expected := 'rpc_export_parzellenliste';
      v_parameter_keys := ARRAY['belegung'];
    WHEN 'datenschutz_aktive_mitglieder' THEN
      v_fixed_columns := ARRAY['email','email_info','email_rechnung','garten_nr','mitgliedsnummer','name','whatsapp'];
      v_expected := 'rpc_export_datenschutz_aktive_mitglieder_json';
      v_parameter_keys := ARRAY[]::text[];
    WHEN 'arbeitsstunden_uebersicht' THEN
      v_fixed_columns := ARRAY['datum','garten_nr','geleistete_stunden','jahr','leistendes_mitglied','mitgliedsnummer','nachname','offene_stunden','pflichtstunden_soll','regelgrund','status','stunden_dieses_mitglieds','taetigkeit','vorname','wartungsvertraege','wartungsvertrag_gutschrift_stunden','zeilentyp'];
      v_expected := 'rpc_export_arbeitsstunden_uebersicht_json';
      v_parameter_keys := ARRAY['p_jahr', 'p_stunden_offen', 'p_stunden_fertig', 'p_wartungsvertraege', 'p_ansicht'];
    WHEN 'jahresablesung_status' THEN
      v_fixed_columns := ARRAY['eingebaut_am','jae_ablesedatum','jae_stand','jahr','medium','pachtbezug','pachtwechsel_im_jahr','parzelle','pruefstatus','status','zaehler_id','zaehlernummer'];
      v_expected := 'rpc_export_jahresablesung_status_json';
      v_parameter_keys := ARRAY['p_jahr', 'p_nur_fehlend'];
    WHEN 'zaehler_eichfaelligkeit' THEN
      v_fixed_columns := ARRAY['eichdatum','eichfaellig_am','eingebaut_am','faelligkeitsstatus','medium','nachname','parzelle','rfid','tage_bis_faellig','vorname','zaehler_id','zaehler_status','zaehlernummer'];
      v_expected := 'rpc_export_zaehler_eichfaelligkeit_json';
      v_parameter_keys := ARRAY['p_medium'];
    ELSE RAISE EXCEPTION 'EXPORT_NOT_ALLOWED' USING ERRCODE = '42501';
  END CASE;

  IF EXISTS (
      SELECT 1 FROM jsonb_object_keys(p_parameters) AS key_name
      WHERE NOT (key_name = ANY(v_parameter_keys))
  ) THEN
    RAISE EXCEPTION 'EXPORT_UNKNOWN_PARAMETER' USING ERRCODE = '22023';
  END IF;

  -- Validate values as well as parameter names; never trust UI filter metadata.
  IF p_parameters ? 'p_jahr' AND p_parameters->'p_jahr' <> 'null'::jsonb
     AND NOT (CASE WHEN coalesce(p_parameters->>'p_jahr', '') ~ '^[0-9]{4}$'
              THEN (p_parameters->>'p_jahr')::integer BETWEEN 1900 AND 2100 ELSE false END)
  THEN
    RAISE EXCEPTION 'EXPORT_INVALID_YEAR' USING ERRCODE = '22023';
  END IF;
  IF p_export_key = 'mitgliederliste' AND (
      coalesce(p_parameters->>'aktiv_filter', 'aktiv') NOT IN ('aktiv','alle','inaktiv')
      OR coalesce(p_parameters->>'sortierung', 'name') NOT IN ('name','mitgliedsnr','garten')
  ) THEN
    RAISE EXCEPTION 'EXPORT_INVALID_MEMBER_FILTER' USING ERRCODE = '22023';
  END IF;
  IF p_export_key = 'parzellenliste' AND
      coalesce(p_parameters->>'belegung', 'alle') NOT IN ('alle','belegt','frei') THEN
    RAISE EXCEPTION 'EXPORT_INVALID_PARCEL_FILTER' USING ERRCODE = '22023';
  END IF;
  IF p_export_key = 'arbeitsstunden_uebersicht' AND
      coalesce(p_parameters->>'p_ansicht', 'zusammenfassung')
          NOT IN ('zusammenfassung','pro_mitglied','einzeln') THEN
    RAISE EXCEPTION 'EXPORT_INVALID_WORK_HOURS_VIEW' USING ERRCODE = '22023';
  END IF;
  IF p_export_key = 'zaehler_eichfaelligkeit' AND
      p_parameters->>'p_medium' IS NOT NULL AND
      p_parameters->>'p_medium' NOT IN ('strom', 'wasser') THEN
    RAISE EXCEPTION 'EXPORT_INVALID_METER_FILTER' USING ERRCODE = '22023';
  END IF;
  IF EXISTS (
    SELECT 1 FROM jsonb_each(p_parameters) e
    WHERE e.key = ANY(ARRAY['p_stunden_offen','p_stunden_fertig','p_wartungsvertraege','p_nur_fehlend'])
      AND e.value <> 'null'::jsonb
      AND lower(e.value::text) NOT IN ('true','false','"true"','"false"')
  ) THEN
    RAISE EXCEPTION 'EXPORT_INVALID_BOOLEAN' USING ERRCODE = '22023';
  END IF;

  SELECT d.quelle_name, (p_export_key = 'zaehler_eichfaelligkeit' AND d.serienbrief_erlaubt IS TRUE AND
                        public.ap02_has_effective_right('serienbrief.erstellen'))
  INTO v_source, v_serienbrief
  FROM public.app_export_definition d
  WHERE d.export_key = p_export_key
    AND d.aktiv = TRUE AND d.quelle_typ = 'rpc';
  IF v_source IS DISTINCT FROM v_expected THEN
    RAISE EXCEPTION 'EXPORT_SOURCE_NOT_AVAILABLE' USING ERRCODE = '22023';
  END IF;

  SELECT array_agg(c.column_key) INTO v_allowed_columns
  FROM public.app_export_column_definition c
  WHERE c.export_key = p_export_key AND c.column_key = ANY(v_fixed_columns);
  IF coalesce(array_length(v_allowed_columns, 1), 0) = 0 THEN
    RAISE EXCEPTION 'EXPORT_COLUMNS_NOT_AVAILABLE' USING ERRCODE = '22023';
  END IF;

  -- v_source is checked against a hard-coded allow-list before interpolation.
  FOR v_item IN EXECUTE format('SELECT r.payload FROM fr211_export_private.%I($1) AS r(payload) LIMIT 1001', v_expected)
    USING p_parameters
  LOOP
    v_result_count := v_result_count + 1;
    IF v_result_count > 1000 THEN
      RAISE EXCEPTION 'EXPORT_ROW_LIMIT_EXCEEDED' USING ERRCODE = '22023';
    END IF;
    IF jsonb_typeof(v_item) <> 'object' THEN
      RAISE EXCEPTION 'EXPORT_INVALID_SOURCE_ROW' USING ERRCODE = '22023';
    END IF;
    -- Output projection: only explicitly configured metadata columns, plus
    -- serial-letter technical routing keys for users with BOTH rights.
    SELECT coalesce(jsonb_object_agg(e.key, e.value), '{}'::jsonb)
      INTO v_projection
    FROM jsonb_each(v_item) AS e
    WHERE (e.key = ANY(v_allowed_columns)
           AND e.key NOT IN ('mitglied_id', 'parzelle_id', 'serienbrief_empfaenger_status'))
       OR (v_serienbrief AND e.key IN (
          'mitglied_id', 'parzelle_id', 'serienbrief_empfaenger_status'
       ));
    IF octet_length(v_projection::text) > 16384 THEN
      RAISE EXCEPTION 'EXPORT_ROW_TOO_LARGE' USING ERRCODE = '22023';
    END IF;
    RETURN NEXT v_projection;
  END LOOP;
  RETURN;
END;
$fr211_gateway$;


-- Raw RPCs remain in the catalog for historical dependencies, but not as
-- directly callable Data API endpoints. The two option RPCs are replaced below.
DO $fr211_acl$
DECLARE f record;
BEGIN
  FOR f IN SELECT p.oid FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
    WHERE n.nspname='public' AND p.proname LIKE 'rpc_export_%'
    AND p.proname NOT IN ('rpc_export_arbeitsstunden_jahre','rpc_export_jahresablesung_jahre')
  LOOP
    EXECUTE format('REVOKE ALL ON FUNCTION %s FROM PUBLIC, anon, authenticated', f.oid::regprocedure);
  END LOOP;
END;
$fr211_acl$;
REVOKE ALL ON FUNCTION public.rpc_execute_table_or_view_export(text) FROM PUBLIC, anon, authenticated;
REVOKE ALL ON FUNCTION public.assert_export_access() FROM PUBLIC, anon, authenticated;
REVOKE ALL ON FUNCTION public.fr211_assert_export_source_read(text) FROM PUBLIC, anon, authenticated;
GRANT EXECUTE ON FUNCTION public.fr211_assert_export_source_read(text) TO authenticated;
REVOKE ALL ON FUNCTION public.rpc_execute_export(text,jsonb) FROM PUBLIC, anon, authenticated;
GRANT EXECUTE ON FUNCTION public.rpc_execute_export(text,jsonb) TO authenticated;

CREATE OR REPLACE FUNCTION public.rpc_export_arbeitsstunden_jahre()
RETURNS TABLE(label text,value text) LANGUAGE plpgsql STABLE SECURITY INVOKER
SET search_path TO pg_catalog, public, pg_temp AS $fr211_years$
BEGIN
  PERFORM public.fr211_assert_export_source_read('arbeitsstunden_uebersicht');
  RETURN QUERY SELECT DISTINCT s.jahr::text,s.jahr::text FROM public.saison s ORDER BY 1 DESC;
  RETURN;
END;
$fr211_years$;
CREATE OR REPLACE FUNCTION public.rpc_export_jahresablesung_jahre()
RETURNS TABLE(label text,value text) LANGUAGE plpgsql STABLE SECURITY INVOKER
SET search_path TO pg_catalog, public, pg_temp AS $fr211_years$
BEGIN
  PERFORM public.fr211_assert_export_source_read('jahresablesung_status');
  RETURN QUERY SELECT DISTINCT extract(year FROM a.ablesedatum)::integer::text,extract(year FROM a.ablesedatum)::integer::text FROM public.zaehler_ablesung a
    UNION SELECT DISTINCT s.jahr::text,s.jahr::text FROM public.saison s ORDER BY 1 DESC;
  RETURN;
END;
$fr211_years$;
REVOKE ALL ON FUNCTION public.rpc_export_arbeitsstunden_jahre(), public.rpc_export_jahresablesung_jahre() FROM PUBLIC, anon, authenticated;
GRANT EXECUTE ON FUNCTION public.rpc_export_arbeitsstunden_jahre(), public.rpc_export_jahresablesung_jahre() TO authenticated;

-- RLS must also hold inside the JAE aggregate chain. Existing operational
-- meter-read authorization checks are retained; only the execution identity
-- changes, so aggregates are derived from rows actually visible to the caller.
ALTER FUNCTION public.rpc_hauptzaehler_jahresverbrauch_segmente(integer) SECURITY INVOKER;
ALTER FUNCTION public.rpc_jahresverbrauch_haupt_und_unterzaehler_g6_basis(integer) SECURITY INVOKER;
ALTER FUNCTION public.rpc_jahresverbrauch_haupt_und_unterzaehler(integer) SECURITY INVOKER;
ALTER FUNCTION public.rpc_jahresverbrauch_haupt_unter_differenz(integer) SECURITY INVOKER;

-- Dedicated JAE export gate. The ordinary consumption read endpoints remain
-- unchanged because they also serve operational workflows outside the export.
CREATE OR REPLACE FUNCTION public.rpc_fr211_jae_export(p_jahr integer)
RETURNS jsonb LANGUAGE plpgsql STABLE SECURITY INVOKER
SET search_path TO pg_catalog, public, pg_temp AS $fr211_jae$
DECLARE v_segments jsonb; v_differences jsonb; v_parcels jsonb;
BEGIN
  PERFORM public.fr211_assert_export_source_read('jae_xlsx');
  IF p_jahr IS NULL OR p_jahr NOT BETWEEN 2000 AND 2100 THEN
    RAISE EXCEPTION 'EXPORT_INVALID_YEAR' USING ERRCODE='22023';
  END IF;
  SELECT coalesce(jsonb_agg(to_jsonb(s) - ARRAY['mitglied_id','start_ablesung_id','end_ablesung_id']), '[]'::jsonb)
    INTO v_segments FROM (SELECT * FROM public.rpc_jahresverbrauch_segmente(p_jahr) LIMIT 5001) s;
  SELECT coalesce(jsonb_agg(to_jsonb(d)), '[]'::jsonb)
    INTO v_differences FROM (SELECT * FROM public.rpc_jahresverbrauch_haupt_unter_differenz(p_jahr) LIMIT 5001) d;
  SELECT coalesce(jsonb_agg(jsonb_build_object('id',p.id,'flaeche_qm',p.flaeche_qm)), '[]'::jsonb)
    INTO v_parcels FROM (SELECT id,flaeche_qm FROM public.parzelle LIMIT 5001) p;
  IF greatest(jsonb_array_length(v_segments),jsonb_array_length(v_differences),jsonb_array_length(v_parcels)) > 5000 THEN
    RAISE EXCEPTION 'EXPORT_ROW_LIMIT_EXCEEDED' USING ERRCODE='22023';
  END IF;
  RETURN jsonb_build_object('segments',v_segments,'differences',v_differences,'parcels',v_parcels);
END;
$fr211_jae$;
REVOKE ALL ON FUNCTION public.rpc_fr211_jae_export(integer) FROM PUBLIC, anon, authenticated;
GRANT EXECUTE ON FUNCTION public.rpc_fr211_jae_export(integer) TO authenticated;
