-- FR2-RB04C/D1: remove direct execution rights from trigger functions and internal resolvers.
-- Preserve the owner/service_role path, all function bodies and existing trigger bindings.
DO $verify_acl$
DECLARE
  f regprocedure;
  s record;
BEGIN
  WITH funcs AS (
  SELECT p.oid,p.proname,md5(pg_get_functiondef(p.oid)) AS fp,
    has_function_privilege('anon',p.oid,'execute') AS anon,
    has_function_privilege('authenticated',p.oid,'execute') AS authenticated,
    has_function_privilege('service_role',p.oid,'execute') AS service_role
  FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
  WHERE n.nspname='public' AND p.prosecdef AND p.proname IN ('create_wartungsvertrag_dokument_workflow','enforce_arbeitsstunde_saison','fr205_restrict_rfid_column_change','freeze_vertragsversand_snapshot','guard_kommunikationsvorgang_update','guard_linked_member_email','guard_mitglied_vereinsdokument_bereitstellung_history','guard_prepared_contract_board_person_unique','guard_prepared_contract_snapshot_start','guard_vereinsdokument_fassung_datei','guard_vertragsversand_anlage_party_snapshot','guard_wv_dispatch_party_known','prevent_delete_published_pdf_template','protect_published_pdf_template','resolve_vertragsunterzeichner','resolve_vertragsversand_berechtigte','set_mitglied_vereinsdokument_bereitgestellt_von','sync_wartungsvertrag_prepared_contract_workflow','sync_wv_dispatch_party_context','trg_arbeitseinsatz_anmeldung_validate','trg_reserve_vertragsnummer','validate_wartungsvertrag_document_source','validate_wartungsvertrag_dokument_workflow','validate_wartungsvertrag_folgeentscheidung','validate_wartungsvertrag_folgezuordnung','validate_wartungsvertrag_kommunikationsvorgang_link','validate_wartungsvertrag_vertragspartei','validate_wartungsvertrag_zuordnung')
), targets AS (
  SELECT count(*) AS n,md5(string_agg(fp,E'\n' ORDER BY proname)) AS digest,string_agg(anon::int::text,'' ORDER BY proname) AS anon,string_agg(authenticated::int::text,'' ORDER BY proname) AS authenticated,string_agg(service_role::int::text,'' ORDER BY proname) AS service_role FROM funcs
), bindings AS (
  SELECT count(*) AS n,md5(string_agg(f.proname||'|'||n.nspname||'.'||c.relname||'|'||t.tgname||'|'||t.tgtype::text||'|'||t.tgenabled::text||'|'||t.tgdeferrable::text||'|'||t.tginitdeferred::text||'|'||t.tgattr::text||'|'||encode(t.tgargs,'hex')||'|'||coalesce(t.tgqual::text,''),E'\n' ORDER BY f.proname,t.tgname)) AS digest FROM funcs f JOIN pg_trigger t ON t.tgfoid=f.oid JOIN pg_class c ON c.oid=t.tgrelid JOIN pg_namespace n ON n.oid=c.relnamespace
)
  SELECT targets.*,bindings.n AS binding_count,bindings.digest AS binding_digest INTO s FROM targets CROSS JOIN bindings;
  IF s.n<>28 OR s.digest IS DISTINCT FROM '71f939404493b8501ab1cfb3f813a68b'
    OR s.binding_count<>26 OR s.binding_digest IS DISTINCT FROM 'beb65ea2b421d6cd705d88f7ae6edd36'
    OR s.anon IS DISTINCT FROM '0111111111111100111110011011'
    OR s.authenticated IS DISTINCT FROM '1111111111111111111111111111'
    OR s.service_role IS DISTINCT FROM '1111111111111111111111111111' THEN
    RAISE EXCEPTION 'Unexpected trigger/resolver baseline';
  END IF;
  FOREACH f IN ARRAY ARRAY[
    'public.create_wartungsvertrag_dokument_workflow()'::regprocedure,
    'public.enforce_arbeitsstunde_saison()'::regprocedure,
    'public.fr205_restrict_rfid_column_change()'::regprocedure,
    'public.freeze_vertragsversand_snapshot()'::regprocedure,
    'public.guard_kommunikationsvorgang_update()'::regprocedure,
    'public.guard_linked_member_email()'::regprocedure,
    'public.guard_mitglied_vereinsdokument_bereitstellung_history()'::regprocedure,
    'public.guard_prepared_contract_board_person_unique()'::regprocedure,
    'public.guard_prepared_contract_snapshot_start()'::regprocedure,
    'public.guard_vereinsdokument_fassung_datei()'::regprocedure,
    'public.guard_vertragsversand_anlage_party_snapshot()'::regprocedure,
    'public.guard_wv_dispatch_party_known()'::regprocedure,
    'public.prevent_delete_published_pdf_template()'::regprocedure,
    'public.protect_published_pdf_template()'::regprocedure,
    'public.resolve_vertragsunterzeichner(date)'::regprocedure,
    'public.resolve_vertragsversand_berechtigte(date)'::regprocedure,
    'public.set_mitglied_vereinsdokument_bereitgestellt_von()'::regprocedure,
    'public.sync_wartungsvertrag_prepared_contract_workflow()'::regprocedure,
    'public.sync_wv_dispatch_party_context()'::regprocedure,
    'public.trg_arbeitseinsatz_anmeldung_validate()'::regprocedure,
    'public.trg_reserve_vertragsnummer()'::regprocedure,
    'public.validate_wartungsvertrag_document_source()'::regprocedure,
    'public.validate_wartungsvertrag_dokument_workflow()'::regprocedure,
    'public.validate_wartungsvertrag_folgeentscheidung()'::regprocedure,
    'public.validate_wartungsvertrag_folgezuordnung()'::regprocedure,
    'public.validate_wartungsvertrag_kommunikationsvorgang_link()'::regprocedure,
    'public.validate_wartungsvertrag_vertragspartei()'::regprocedure,
    'public.validate_wartungsvertrag_zuordnung()'::regprocedure
  ] LOOP
    EXECUTE format('REVOKE EXECUTE ON FUNCTION %s FROM PUBLIC, anon, authenticated',f);
    IF has_function_privilege('anon',f,'execute') OR has_function_privilege('authenticated',f,'execute')
       OR NOT has_function_privilege('service_role',f,'execute') THEN
      RAISE EXCEPTION 'Unexpected resulting ACL: %',f;
    END IF;
  END LOOP;
END
$verify_acl$;
