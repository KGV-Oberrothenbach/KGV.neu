-- FR2-RB04D2: restrict lock acquisition to valid members, rights and objects.
DO $migration$
DECLARE
  def text;
  patched text;
  f constant regprocedure := 'public.acquire_browser_edit_lock_core(text,text,uuid,integer)'::regprocedure;
  anchor constant text := $anchor$  if p_entity_type in ('arbeitseinsatz', 'arbeitseinsatz_anmeldung') then$anchor$;
BEGIN
  SELECT pg_get_functiondef(f) INTO def;
  IF md5(def) IS DISTINCT FROM '20b1b1f9d532e3ae205c648a31bfabc1'
    OR has_function_privilege('anon',f,'execute')
    OR has_function_privilege('authenticated',f,'execute')
    OR NOT has_function_privilege('service_role',f,'execute') THEN
    RAISE EXCEPTION 'Unexpected lock core baseline';
  END IF;
  def := replace(def,E'\r\n',E'\n');
  IF (length(def)-length(replace(def,anchor,'')))/length(anchor)<>1 THEN
    RAISE EXCEPTION 'Lock core anchor mismatch';
  END IF;
  patched := replace(def,anchor,$guard$  -- FR2-RB04D2: only current members and explicitly authorized object types may acquire locks.
  IF NOT public.ap03_has_current_membership(v_user_id) THEN
    RAISE EXCEPTION 'Keine gueltige Mitgliedschaft oder Sitzung.' USING ERRCODE='42501';
  END IF;
  IF p_entity_type = 'app_user' THEN
    IF p_entity_id !~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$' THEN
      RAISE EXCEPTION 'Ungueltiger Sperrschluessel.' USING ERRCODE='22023';
    END IF;
    IF NOT (public.ap02_has_effective_right('benutzer.verwalten') OR public.ap02_has_effective_right('berechtigungen.verwalten'))
       OR NOT EXISTS (SELECT 1 FROM public.app_user a WHERE a.user_id=p_entity_id::uuid) THEN
      RAISE EXCEPTION 'Benutzerkonto ausserhalb des berechtigten Lock-Scopes.' USING ERRCODE='42501';
    END IF;
  ELSE
    IF p_entity_type NOT IN ('mitglied','parzelle','parzellen_belegung','zaehler_ablesung','arbeitsstunde','arbeitseinsatz','arbeitseinsatz_anmeldung','termin','bekanntmachung','wartungsvertraege','wartungsvertrag_zuordnungen','saison','vereinskonfiguration') THEN
      RAISE EXCEPTION 'Unbekannter Sperrtyp.' USING ERRCODE='42501';
    END IF;
    IF p_entity_id !~ '^[1-9][0-9]{0,18}$' THEN
      RAISE EXCEPTION 'Ungueltiger Sperrschluessel.' USING ERRCODE='22023';
    END IF;
    IF p_entity_id::numeric > 9223372036854775807 OR (p_entity_type='arbeitsstunde' AND p_entity_id::numeric > 2147483647) THEN
      RAISE EXCEPTION 'Ungueltiger Sperrschluessel.' USING ERRCODE='22023';
    END IF;
    IF NOT (CASE p_entity_type
      WHEN 'mitglied' THEN EXISTS (SELECT 1 FROM public.mitglied m WHERE m.id=p_entity_id::bigint AND (m.id=public.current_mitglied_id() OR public.ap02_has_effective_right('mitglied.bearbeiten') OR public.ap02_has_effective_right('mitgliedschaft.verwalten')))
      WHEN 'parzelle' THEN public.ap02_has_effective_right('parzelle.bearbeiten') AND EXISTS (SELECT 1 FROM public.parzelle p WHERE p.id=p_entity_id::bigint)
      WHEN 'parzellen_belegung' THEN public.ap02_has_effective_right('belegung.verwalten') AND EXISTS (SELECT 1 FROM public.parzellen_belegung b WHERE b.id=p_entity_id::bigint)
      WHEN 'zaehler_ablesung' THEN public.ap02_has_effective_right('ablesung.freigeben') AND EXISTS (SELECT 1 FROM public.zaehler_ablesung a WHERE a.id=p_entity_id::bigint)
      WHEN 'wartungsvertraege' THEN public.ap02_has_effective_right('mitgliedschaft.verwalten') AND EXISTS (SELECT 1 FROM public.wartungsvertraege w WHERE w.id=p_entity_id::bigint)
      WHEN 'wartungsvertrag_zuordnungen' THEN public.ap02_has_effective_right('mitgliedschaft.verwalten') AND EXISTS (SELECT 1 FROM public.wartungsvertrag_zuordnungen w WHERE w.id=p_entity_id::bigint)
      WHEN 'saison' THEN public.ap02_has_effective_right('saison.verwalten') AND EXISTS (SELECT 1 FROM public.saison s WHERE s.id=p_entity_id::bigint)
      WHEN 'vereinskonfiguration' THEN public.ap02_has_effective_right('verein.konfiguration') AND EXISTS (SELECT 1 FROM public.vereinskonfiguration v WHERE v.id=p_entity_id::bigint)
      ELSE true -- The five existing scopes below remain unchanged.
    END) THEN
      RAISE EXCEPTION 'Objekt ausserhalb des berechtigten Lock-Scopes.' USING ERRCODE='42501';
    END IF;
  END IF;

$guard$||anchor);
  IF md5(patched) IS DISTINCT FROM 'a8dcf00755a2e01257c883379a5a222b' THEN
    RAISE EXCEPTION 'Unexpected lock core patch fingerprint';
  END IF;
  EXECUTE patched;
  IF md5(pg_get_functiondef(f)) IS DISTINCT FROM 'a8dcf00755a2e01257c883379a5a222b'
    OR has_function_privilege('anon',f,'execute')
    OR has_function_privilege('authenticated',f,'execute')
    OR NOT has_function_privilege('service_role',f,'execute') THEN
    RAISE EXCEPTION 'Unexpected lock core postcondition';
  END IF;
END
$migration$;
