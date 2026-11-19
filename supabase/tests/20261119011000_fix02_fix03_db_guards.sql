-- Isolierter Supabase-Replay: nur Struktur-/Privilegien-Guards, KEIN Live-Test.
DO $checks$
DECLARE v_def text; v_trigger text; v_sync text;
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
     WHERE n.nspname='public' AND p.proname='can_upload_meter_reading_photo') THEN
    RAISE EXCEPTION 'FIX-02: missing write-authorization RPC';
  END IF;
  IF has_function_privilege('anon', 'public.can_upload_meter_reading_photo(bigint)', 'EXECUTE') THEN
    RAISE EXCEPTION 'FIX-02: anon can call write-authorization RPC';
  END IF;
  IF NOT has_function_privilege('authenticated', 'public.can_upload_meter_reading_photo(bigint)', 'EXECUTE') THEN
    RAISE EXCEPTION 'FIX-02: authenticated cannot call write-authorization RPC';
  END IF;
  IF has_function_privilege('anon', 'public.guard_meter_reading_creator()', 'EXECUTE') THEN
    RAISE EXCEPTION 'FIX-02: anon can call creator guard';
  END IF;
  IF position('erfasst_von_auth_user_id = auth.uid()' in pg_get_functiondef('public.can_upload_meter_reading_photo(bigint)'::regprocedure))=0 THEN
    RAISE EXCEPTION 'FIX-02: uploader is not bound to original actor';
  END IF;
  SELECT pg_get_functiondef('public.sync_app_user_from_mitglied()'::regprocedure) INTO v_sync;
  IF position('role = excluded.role' in lower(v_sync)) > 0
     OR position('role        = excluded.role' in lower(v_sync)) > 0 THEN
    RAISE EXCEPTION 'FIX-03: member role may overwrite app_user role';
  END IF;
  SELECT pg_get_triggerdef(t.oid) INTO v_trigger FROM pg_trigger t
    WHERE t.tgrelid='public.mitglied'::regclass
      AND t.tgname='trg_sync_app_user_from_mitglied';
  IF v_trigger IS NULL OR position('UPDATE OF auth_user_id, role' in v_trigger)>0 THEN
    RAISE EXCEPTION 'FIX-03: role column remains part of member trigger';
  END IF;
  IF position('current_app_role()' in pg_get_functiondef('public.get_user_role()'::regprocedure))=0
     OR position('current_app_role()' in pg_get_functiondef('public.who_am_i()'::regprocedure))=0 THEN
    RAISE EXCEPTION 'FIX-03: role lookup still differs from production';
  END IF;
  SELECT column_default INTO v_def FROM information_schema.columns
   WHERE table_schema='public' AND table_name='zaehler_ablesung' AND column_name='pruefstatus';
  IF v_def IS NULL OR position('eingereicht' in v_def)=0 THEN
    RAISE EXCEPTION 'FIX-03: wrong default %',v_def;
  END IF;
  RAISE NOTICE 'FIX-02 / FIX-03 schema guards PASS';
END $checks$;
DO $checks$
DECLARE v_definition text;
BEGIN
 SELECT pg_get_constraintdef(oid) INTO v_definition FROM pg_constraint
 WHERE conrelid='public.parzellen_belegung'::regclass
   AND conname='parzellen_belegung_zeitraum_ohne_ueberlappung';
 IF v_definition IS NULL OR position('WHERE' in upper(v_definition)) > 0 THEN
   RAISE EXCEPTION 'FIX-05: overlapping legacy occupancy still permitted';
 END IF;
END $checks$;
