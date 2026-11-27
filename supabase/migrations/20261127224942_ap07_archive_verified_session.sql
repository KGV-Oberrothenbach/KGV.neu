-- Bind service-side archive authorization to the verified caller's live session.
DO $guard$
DECLARE
  definition text;
  old_sig constant text := 'p_reason text)';
  old_context constant text := 'PERFORM set_config(''request.jwt.claim.sub'',p_actor::text,true);';
  new_context constant text := $ctx$IF p_session_id IS NULL THEN
    RAISE EXCEPTION 'Archivierung benötigt eine gültige Sitzung' USING ERRCODE='42501';
  END IF;
  PERFORM set_config('request.jwt.claim.sub',p_actor::text,true);
  PERFORM set_config('request.jwt.claims',jsonb_build_object('role','service_role','sub',p_actor,'session_id',p_session_id)::text,true);
  PERFORM set_config('request.jwt.claim',current_setting('request.jwt.claims',true),true);
  IF NOT public.fr212_actor_session_valid() THEN
    RAISE EXCEPTION 'Archivierung benötigt eine gültige Sitzung' USING ERRCODE='42501';
  END IF;$ctx$;
BEGIN
  SELECT pg_get_functiondef('public.fr208_archive_dokument_service(bigint,uuid,text)'::regprocedure) INTO STRICT definition;
  IF md5(definition)<>'b137ce89e3cdcedd6c1d7faec36b4170'
     OR (length(definition)-length(replace(definition,old_sig,'')))<>length(old_sig)
     OR (length(definition)-length(replace(definition,old_context,'')))<>length(old_context)
     OR to_regprocedure('public.fr208_archive_dokument_service(bigint,uuid,text,uuid)') IS NOT NULL THEN
    RAISE EXCEPTION 'AP07_ARCHIVE_SESSION_PRECONDITION';
  END IF;
  EXECUTE replace(replace(definition,old_sig,'p_reason text, p_session_id uuid)'),old_context,new_context);
END $guard$;
REVOKE ALL ON FUNCTION public.fr208_archive_dokument_service(bigint,uuid,text,uuid) FROM PUBLIC,anon,authenticated;
GRANT EXECUTE ON FUNCTION public.fr208_archive_dokument_service(bigint,uuid,text,uuid) TO service_role;
REVOKE EXECUTE ON FUNCTION public.fr208_archive_dokument_service(bigint,uuid,text) FROM service_role;
