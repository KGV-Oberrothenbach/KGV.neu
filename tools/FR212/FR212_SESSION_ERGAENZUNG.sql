-- FR212 V7: current actor session, account status and own-object boundary.
CREATE OR REPLACE FUNCTION public.fr212_actor_session_valid()
RETURNS boolean LANGUAGE sql STABLE SECURITY DEFINER
SET search_path TO pg_catalog,public
AS $session$
 SELECT EXISTS (
   SELECT 1 FROM auth.sessions s
   JOIN auth.users u ON u.id=s.user_id
   WHERE s.id=CASE WHEN (auth.jwt()->>'session_id') ~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$'
        THEN (auth.jwt()->>'session_id')::uuid ELSE NULL END
     AND s.user_id=auth.uid()
     AND (s.not_after IS NULL OR s.not_after>now())
     AND u.deleted_at IS NULL
     AND (u.banned_until IS NULL OR u.banned_until<=now())
 );
$session$;
REVOKE ALL ON FUNCTION public.fr212_actor_session_valid() FROM PUBLIC,anon,authenticated;
GRANT EXECUTE ON FUNCTION public.fr212_actor_session_valid() TO authenticated;

CREATE OR REPLACE FUNCTION public.ap03_has_current_membership(p_user_id uuid)
RETURNS boolean LANGUAGE sql STABLE SECURITY DEFINER
SET search_path TO pg_catalog,public
AS $membership$
 SELECT EXISTS (
   SELECT 1 FROM public.app_user au
   JOIN public.mitglied m ON m.id=au.mitglied_id AND m.auth_user_id=au.user_id
   JOIN auth.users u ON u.id=au.user_id AND u.deleted_at IS NULL
     AND (u.banned_until IS NULL OR u.banned_until<=now())
   WHERE au.user_id=p_user_id AND m.aufnahme_status='aufgenommen'
     AND m.mitglied_ende IS NULL
     AND (p_user_id IS DISTINCT FROM auth.uid() OR public.fr212_actor_session_valid())
 );
$membership$;

DO $actor_guards$
DECLARE d text; a text;
BEGIN
 SELECT pg_get_functiondef('public.current_app_user_context()'::regprocedure) INTO d;
 a:=' WHERE au.user_id=auth.uid();';
 IF (length(d)-length(replace(d,a,'')))/length(a)<>1 THEN
   RAISE EXCEPTION 'FR212 V7 current context anchor mismatch' USING ERRCODE='55000'; END IF;
 EXECUTE replace(d,a,' WHERE au.user_id=auth.uid() AND public.fr212_actor_session_valid();');
 SELECT pg_get_functiondef('public.ap02_can_read_mitglied(bigint)'::regprocedure) INTO d;
 a:='  WHERE a.user_id = auth.uid()';
 IF (length(d)-length(replace(d,a,'')))/length(a)<>1 THEN
   RAISE EXCEPTION 'FR212 V7 member read anchor mismatch' USING ERRCODE='55000'; END IF;
 EXECUTE replace(d,a,a||' AND public.fr212_actor_session_valid()');
 SELECT pg_get_functiondef('public.speichere_prepared_contract_signatur(bigint,integer,text,bigint,text,bigint,bigint,bigint,text,text,timestamptz)'::regprocedure) INTO d;
 a:='if auth.uid() is null then raise exception ''Anmeldung erforderlich.'' using errcode=''42501''; end if;';
 IF (length(d)-length(replace(d,a,'')))/length(a)<>1 THEN
   RAISE EXCEPTION 'FR212 V7 signature anchor mismatch' USING ERRCODE='55000'; END IF;
 EXECUTE replace(d,a,'if not exists(select 1 from public.current_app_user_context()) then raise exception ''Anmeldung erforderlich.'' using errcode=''42501''; end if;');
END $actor_guards$;

ALTER POLICY app_user_select_by_effective_roles ON public.app_user
 USING (public.fr212_actor_session_valid() AND
        (user_id=auth.uid() OR public.ap02_has_effective_right('benutzer.lesen')));
