-- AP-03.1-03.3: only current admitted members can exercise Fachrechte,
-- edit own contact details, and participate in or inspect admission decisions.
-- Passive members without a formal membership end remain current members.
-- The historical vote snapshots and terminated membership history are preserved.
-- DemoDB migration. Do not deploy to VDB without separate authorization.

DO $pre$
BEGIN
  IF (SELECT count(*) FROM public.fachrecht) <> 55 OR
     (SELECT count(*) FROM public.protected_admin_identity) <> 1 OR
     to_regprocedure('public.ap02_calculate_user_rights(uuid)') IS NULL OR
     to_regprocedure('public.update_own_member_contact(text,text,text,text,text)') IS NULL OR
     to_regprocedure('public.ap02_can_read_mitglied(bigint)') IS NULL OR
     to_regprocedure('public.darf_mitgliedsaufnahme_lesen(bigint)') IS NULL OR
     to_regprocedure('public.can_access_mitgliedsaufnahme_vorstand_document(uuid,bigint)') IS NULL OR
     to_regprocedure('public.stimme_mitgliedsaufnahme_ab(bigint,text,text)') IS NULL OR
     to_regprocedure('public.end_membership(bigint,text,text)') IS NULL THEN
    RAISE EXCEPTION 'AP03 precondition failed: incomplete/changed DemoDB basis';
  END IF;
END $pre$;

CREATE OR REPLACE FUNCTION public.ap03_has_current_membership(p_user_id uuid)
RETURNS boolean LANGUAGE sql STABLE SECURITY DEFINER
SET search_path TO pg_catalog, public
AS $fn$
  SELECT EXISTS (
    SELECT 1 FROM public.app_user au
    JOIN public.mitglied m ON m.id=au.mitglied_id AND m.auth_user_id=au.user_id
    WHERE au.user_id=p_user_id
      AND m.aufnahme_status='aufgenommen'
      AND m.mitglied_ende IS NULL
  )
$fn$;
REVOKE ALL ON FUNCTION public.ap03_has_current_membership(uuid) FROM PUBLIC, anon, authenticated;

DO $patch$
DECLARE v_def text;v_old text;v_new text;
BEGIN
  -- AP03.1/AP03.2: former members and applicants must not retain old board rights.
  v_def := pg_get_functiondef('public.ap02_calculate_user_rights(uuid)'::regprocedure);
  v_old := '  WHERE u.user_id = p_user_id';
  v_new := v_old || E'\n    AND public.ap03_has_current_membership(p_user_id)';
  IF (length(v_def)-length(replace(v_def,v_old,'')))/length(v_old) <> 1
  THEN RAISE EXCEPTION 'AP03 V2 right-resolution anchor not unique'; END IF;
  EXECUTE replace(v_def,v_old,v_new);

  -- The own-data RPC is available to active or passive members, but not to
  -- unadmitted applicants or formally terminated former members.
  v_def := pg_get_functiondef('public.update_own_member_contact(text,text,text,text,text)'::regprocedure);
  v_old := '  select * into v_member from public.mitglied';
  v_new := E'  if not public.ap03_has_current_membership(auth.uid()) then\n    raise exception ''Keine bestehende Mitgliedschaft fuer die Eigendatenpflege.'' using errcode=''42501'';\n  end if;\n' || v_old;
  IF (length(v_def)-length(replace(v_def,v_old,'')))/length(v_old) <> 1
  THEN RAISE EXCEPTION 'AP03 own-contact anchor not unique'; END IF;
  EXECUTE replace(v_def,v_old,v_new);

  -- A former legal representative may still read their own history, but no
  -- longer receives the live child's member record via representative scope.
  v_def := pg_get_functiondef('public.ap02_can_read_mitglied(bigint)'::regprocedure);
  v_old := '      WHERE v.vertreter_mitglied_id = a.mitglied_id';
  v_new := v_old || E'\n        AND public.ap03_has_current_membership(a.user_id)';
  IF (length(v_def)-length(replace(v_def,v_old,'')))/length(v_old) <> 1
  THEN RAISE EXCEPTION 'AP03 representative read anchor not unique'; END IF;
  EXECUTE replace(v_def,v_old,v_new);

  -- Snapshot-based membership document access is only for current members.
  v_def := pg_get_functiondef('public.can_access_mitgliedsaufnahme_vorstand_document(uuid,bigint)'::regprocedure);
  v_old := '   where d.id=p_dokument_id and d.archiviert_at is null';
  v_new := v_old || E'\n     and public.ap03_has_current_membership(p_auth_user_id)';
  IF (length(v_def)-length(replace(v_def,v_old,'')))/length(v_old) <> 1
  THEN RAISE EXCEPTION 'AP03 board-document anchor not unique'; END IF;
  EXECUTE replace(v_def,v_old,v_new);

  -- A vote snapshot preserves history; it does not authorize a former member
  -- to cast or change a vote after their membership ends.
  v_def := pg_get_functiondef('public.stimme_mitgliedsaufnahme_ab(bigint,text,text)'::regprocedure);
  v_old := E'begin\n if caller is null or p_stimme';
  v_new := E'begin\n if not public.ap03_has_current_membership(caller) then raise exception ''Keine aktuelle Mitgliedschaft fuer die Stimmabgabe.'' using errcode=''42501''; end if;\n if caller is null or p_stimme';
  IF (length(v_def)-length(replace(v_def,v_old,'')))/length(v_old) <> 1
  THEN RAISE EXCEPTION 'AP03 vote anchor not unique'; END IF;
  EXECUTE replace(v_def,v_old,v_new);

  -- The protected administrator must not lose all administrative access as a
  -- side effect of formal membership termination. Transfer protection first.
  v_def := pg_get_functiondef('public.end_membership(bigint,text,text)'::regprocedure);
  v_old := '  select m.* into v_main_member from public.mitglied m where m.id = p_main_member_id for update;';
  v_new := E'  if exists (select 1 from public.protected_admin_identity p join public.app_user au on au.user_id=p.user_id where au.mitglied_id=p_main_member_id) then\n    raise exception ''Geschuetzte Adminidentitaet muss vor dem Mitgliedschaftsende umgestellt werden.'' using errcode=''42501'';\n  end if;\n' || v_old;
  IF (length(v_def)-length(replace(v_def,v_old,'')))/length(v_old) <> 1
  THEN RAISE EXCEPTION 'AP03 protected main-member anchor not unique'; END IF;
  v_def := replace(v_def,v_old,v_new);
  v_old := '  perform set_config(''kgv.formal_membership_termination'', ''1'', true);';
  v_new := E'  if v_has_secondary and p_secondary_decision = ''end_secondary'' and exists (select 1 from public.protected_admin_identity p join public.app_user au on au.user_id=p.user_id where au.mitglied_id=v_secondary_member.id) then\n    raise exception ''Geschuetzte Adminidentitaet darf nicht als Nebenmitglied beendet werden.'' using errcode=''42501'';\n  end if;\n' || v_old;
  IF (length(v_def)-length(replace(v_def,v_old,'')))/length(v_old) <> 1
  THEN RAISE EXCEPTION 'AP03 protected secondary-member anchor not unique'; END IF;
  EXECUTE replace(v_def,v_old,v_new);
END $patch$;

CREATE OR REPLACE FUNCTION public.darf_mitgliedsaufnahme_lesen(p_bearbeitung_id bigint)
RETURNS boolean LANGUAGE sql STABLE SECURITY DEFINER
SET search_path TO public, pg_temp
AS $fn$
  SELECT public.ap03_has_current_membership(auth.uid()) AND (
    EXISTS (
      SELECT 1 FROM public.mitgliedsaufnahme_abstimmung_snapshot_person sp
      JOIN public.app_user au ON au.mitglied_id=sp.mitglied_id
      WHERE sp.vorstandsbearbeitung_id=p_bearbeitung_id AND au.user_id=auth.uid()
    )
    OR EXISTS (
      SELECT 1 FROM public.mitgliedsaufnahme_berechtigungen('mitgliedsaufnahme_entscheidung',current_date) x
      JOIN public.app_user au ON au.mitglied_id=x.mitglied_id
      WHERE au.user_id=auth.uid()
    )
  )
$fn$;

DO $post$
BEGIN
  IF has_function_privilege('anon','public.ap03_has_current_membership(uuid)','EXECUTE') OR
     has_function_privilege('authenticated','public.ap03_has_current_membership(uuid)','EXECUTE') THEN
    RAISE EXCEPTION 'AP03 internal membership test is publicly executable';
  END IF;
  IF position('ap03_has_current_membership' IN pg_get_functiondef('public.ap02_calculate_user_rights(uuid)'::regprocedure))=0 OR
     position('ap03_has_current_membership' IN pg_get_functiondef('public.ap02_can_read_mitglied(bigint)'::regprocedure))=0 OR
     position('ap03_has_current_membership' IN pg_get_functiondef('public.update_own_member_contact(text,text,text,text,text)'::regprocedure))=0 OR
     position('ap03_has_current_membership' IN pg_get_functiondef('public.can_access_mitgliedsaufnahme_vorstand_document(uuid,bigint)'::regprocedure))=0 OR
     position('ap03_has_current_membership' IN pg_get_functiondef('public.stimme_mitgliedsaufnahme_ab(bigint,text,text)'::regprocedure))=0 OR
     position('ap03_has_current_membership' IN pg_get_functiondef('public.darf_mitgliedsaufnahme_lesen(bigint)'::regprocedure))=0 THEN
    RAISE EXCEPTION 'AP03 live membership guard not installed everywhere';
  END IF;
END $post$;
