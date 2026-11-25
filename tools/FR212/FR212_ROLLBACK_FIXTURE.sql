-- Only after installing the candidate in a disposable clone or inside the same
-- enclosing transaction (strip the candidate's outer BEGIN/COMMIT). Never commit.
-- Set fr212.fixture_target explicitly to the UUID of a verified synthetic demo account.
-- Set fr212.fixture_actor to the protected demo administrator's UUID. No user data printed.
DO $preflight$
DECLARE a uuid := current_setting('fr212.fixture_actor')::uuid;
        t uuid := current_setting('fr212.fixture_target')::uuid;
BEGIN
 IF a=t OR NOT public.ap02_user_has_effective_right(a,'benutzer.verwalten')
  OR NOT public.ap02_user_has_effective_right(a,'berechtigungen.verwalten')
  OR NOT EXISTS(SELECT 1 FROM public.app_user u JOIN public.mitglied m ON m.id=u.mitglied_id AND m.auth_user_id=u.user_id JOIN auth.users x ON x.id=u.user_id WHERE u.user_id=t AND public.ap03_has_current_membership(t))
  OR EXISTS(SELECT 1 FROM public.protected_admin_identity WHERE user_id=t)
  OR EXISTS(SELECT 1 FROM public.app_user_zusatzrolle WHERE user_id=t AND zusatzrolle_code='admin')
  OR EXISTS(SELECT 1 FROM public.auth_user_delete_lock) THEN RAISE EXCEPTION 'FR212 fixture prerequisites not met'; END IF;
 PERFORM set_config('fr212.member',(SELECT mitglied_id::text FROM public.app_user WHERE user_id=t),true);
 PERFORM set_config('fr212.op',gen_random_uuid()::text,true);
 -- Ensure positive rights restoration checks cannot pass with empty rows.
 INSERT INTO public.app_user_fachrecht_override(user_id,fachrecht_code,entscheidung,grund)
 VALUES(t,'mitglied.bearbeiten','deny','FR212 rollback fixture') ON CONFLICT(user_id,fachrecht_code) DO UPDATE SET entscheidung='deny';
 IF NOT EXISTS(SELECT 1 FROM public.app_user_hauptrolle WHERE user_id=t) THEN RAISE EXCEPTION 'FR212 target has no main role'; END IF;
 PERFORM set_config('fr212.before',md5(jsonb_build_object(
 'app',(SELECT to_jsonb(u) FROM public.app_user u WHERE u.user_id=t),
 'main',(SELECT jsonb_agg(to_jsonb(h) ORDER BY h.hauptrolle_code) FROM public.app_user_hauptrolle h WHERE h.user_id=t),
 'extra',(SELECT jsonb_agg(to_jsonb(z) ORDER BY z.zusatzrolle_code) FROM public.app_user_zusatzrolle z WHERE z.user_id=t),
 'override',(SELECT jsonb_agg(to_jsonb(o) ORDER BY o.fachrecht_code) FROM public.app_user_fachrecht_override o WHERE o.user_id=t))::text),true);
END $preflight$;
SET LOCAL ROLE authenticated;
DO $denied$
BEGIN
 PERFORM set_config('request.jwt.claim.sub',current_setting('fr212.fixture_target'),true);
 PERFORM set_config('request.jwt.claims',jsonb_build_object('sub',current_setting('fr212.fixture_target'),'role','authenticated')::text,true);
 BEGIN
   PERFORM public.fr212_user_account(current_setting('fr212.member')::bigint);
   RAISE EXCEPTION 'FR212 unauthorized account read succeeded';
 EXCEPTION WHEN insufficient_privilege THEN NULL; END;
 BEGIN
   PERFORM public.ap02_list_user_access();
   RAISE EXCEPTION 'FR212 unauthorized rights read succeeded';
 EXCEPTION WHEN insufficient_privilege THEN NULL; END;
END $denied$;
DO $allowed$
DECLARE n bigint;
BEGIN
 PERFORM set_config('request.jwt.claim.sub',current_setting('fr212.fixture_actor'),true);
 PERFORM set_config('request.jwt.claims',jsonb_build_object('sub',current_setting('fr212.fixture_actor'),'role','authenticated')::text,true);
 SELECT count(*) INTO n FROM public.fr212_user_account(current_setting('fr212.member')::bigint) r WHERE r.user_id=current_setting('fr212.fixture_target')::uuid;
 IF n<>1 THEN RAISE EXCEPTION 'FR212 positive account lookup failed'; END IF;
 SELECT count(*) INTO n FROM public.ap02_list_user_access() r WHERE r.user_id=current_setting('fr212.fixture_target')::uuid;
 IF n<>1 THEN RAISE EXCEPTION 'FR212 positive rights lookup failed'; END IF;
 IF NOT public.begin_auth_user_deletion(current_setting('fr212.fixture_target')::uuid,current_setting('fr212.member')::bigint,current_setting('fr212.op')::uuid) THEN RAISE EXCEPTION 'FR212 reservation failed'; END IF;
END $allowed$;
RESET ROLE;
DO $unlinked$
BEGIN
 IF EXISTS(SELECT 1 FROM public.app_user WHERE user_id=current_setting('fr212.fixture_target')::uuid)
 OR NOT EXISTS(SELECT 1 FROM public.auth_user_delete_lock WHERE user_id=current_setting('fr212.fixture_target')::uuid AND jsonb_array_length(fr212_rights_snapshot->'hauptrolle')=1 AND jsonb_array_length(fr212_rights_snapshot->'overrides')>0) THEN RAISE EXCEPTION 'FR212 snapshot/unlink failed'; END IF;
END $unlinked$;
SET LOCAL ROLE service_role;
DO $restore$
BEGIN
 PERFORM set_config('request.jwt.claim.role','service_role',true);
 PERFORM set_config('request.jwt.claims','{"role":"service_role"}',true);
 IF NOT public.restore_auth_user_deletion(current_setting('fr212.fixture_target')::uuid,current_setting('fr212.op')::uuid) THEN RAISE EXCEPTION 'FR212 restore failed'; END IF;
END $restore$;
RESET ROLE;
DO $compare$
DECLARE t uuid := current_setting('fr212.fixture_target')::uuid; actual text;
BEGIN
 SELECT md5(jsonb_build_object(
 'app',(SELECT to_jsonb(u) FROM public.app_user u WHERE u.user_id=t),
 'main',(SELECT jsonb_agg(to_jsonb(h) ORDER BY h.hauptrolle_code) FROM public.app_user_hauptrolle h WHERE h.user_id=t),
 'extra',(SELECT jsonb_agg(to_jsonb(z) ORDER BY z.zusatzrolle_code) FROM public.app_user_zusatzrolle z WHERE z.user_id=t),
 'override',(SELECT jsonb_agg(to_jsonb(o) ORDER BY o.fachrecht_code) FROM public.app_user_fachrecht_override o WHERE o.user_id=t))::text) INTO actual;
 IF actual IS DISTINCT FROM current_setting('fr212.before') OR EXISTS(SELECT 1 FROM public.auth_user_delete_lock WHERE user_id=t)
 OR NOT EXISTS(SELECT 1 FROM public.mitglied WHERE id=current_setting('fr212.member')::bigint AND auth_user_id=t) THEN RAISE EXCEPTION 'FR212 exact compensation mismatch'; END IF;
END $compare$;
SELECT 'FR212_ROLLBACK_FIXTURE_PASS' AS result;
-- Caller MUST ROLLBACK; independently verify original fingerprint afterwards.
