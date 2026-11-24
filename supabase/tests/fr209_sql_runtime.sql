-- Run inside the package's explicit BEGIN/ROLLBACK, after the candidate DDL.
DO $checks$
DECLARE t text; p regprocedure; actor uuid; denied uuid; fixture bigint; n bigint; old_claims text; original_role text;
BEGIN
 IF has_column_privilege('authenticated','public.kommunikationsvorgang','zugewiesen_an','UPDATE') THEN RAISE EXCEPTION 'FR209 assignment bypass'; END IF;
 IF has_table_privilege('authenticated','public.kommunikationsvorgang','DELETE') OR has_table_privilege('authenticated','public.kommunikationsvorgang','TRUNCATE') THEN RAISE EXCEPTION 'FR209 destructive privileges'; END IF;
 FOREACH t IN ARRAY ARRAY['versandinstanz','versandinstanz_dokument_snapshot','versandinstanz_empfaenger_snapshot','versandinstanz_versuch','vertragsversand_anlage_snapshot','vertragsversand_vertragspartei_snapshot','fr209_mail_reservation'] LOOP
  IF NOT (SELECT relrowsecurity FROM pg_class WHERE oid=('public.'||t)::regclass) OR has_table_privilege('authenticated','public.'||t,'INSERT') OR has_table_privilege('authenticated','public.'||t,'UPDATE') OR has_table_privilege('authenticated','public.'||t,'DELETE') OR has_table_privilege('authenticated','public.'||t,'TRUNCATE') OR has_table_privilege('anon','public.'||t,'SELECT') THEN RAISE EXCEPTION 'FR209 ACL or RLS: %',t; END IF;
 END LOOP;
 IF has_table_privilege('authenticated','public.fr209_mail_reservation','SELECT') THEN RAISE EXCEPTION 'FR209 reservation visible'; END IF;
 FOREACH p IN ARRAY ARRAY['public.fr209_claim_kommunikationsvorgang(bigint)'::regprocedure,'public.fr209_release_kommunikationsvorgang(bigint)'::regprocedure,'public.fr209_claim_contract_dispatch(bigint)'::regprocedure] LOOP
  IF has_function_privilege('anon',p,'EXECUTE') OR NOT has_function_privilege('authenticated',p,'EXECUTE') THEN RAISE EXCEPTION 'FR209 RPC grant: %',p; END IF;
 END LOOP;
 FOREACH p IN ARRAY ARRAY['public.fr209_reserve_contract_mail(bigint,bigint,uuid)'::regprocedure,'public.fr209_reserve_club_mail(bigint,bigint[],uuid,text)'::regprocedure] LOOP
  IF has_function_privilege('anon',p,'EXECUTE') OR has_function_privilege('authenticated',p,'EXECUTE') OR NOT has_function_privilege('service_role',p,'EXECUTE') THEN RAISE EXCEPTION 'FR209 reserve grant: %',p; END IF;
 END LOOP;
 SELECT user_id INTO actor FROM public.app_user WHERE public.ap02_user_has_effective_right(user_id,'vorgang.bearbeiten') ORDER BY user_id LIMIT 1;
 SELECT user_id INTO denied FROM public.app_user WHERE NOT public.ap02_user_has_effective_right(user_id,'vorgang.bearbeiten') ORDER BY user_id LIMIT 1;
 IF actor IS NULL OR denied IS NULL THEN RAISE EXCEPTION 'FR209 requires distinct positive and negative demo identities'; END IF;
 -- Existing application fixtures are not touched; only synthetic rollback data.
 INSERT INTO public.kommunikationsvorgang(vorgangstyp,dokumenttyp,status,erstellt_von) VALUES('druck','fr209_sql_rollback_test','druck_bereit',actor) RETURNING id INTO fixture;
 old_claims:=current_setting('request.jwt.claims',true); original_role:=current_user;
 PERFORM set_config('request.jwt.claims',json_build_object('sub',denied,'role','authenticated')::text,true);
 PERFORM set_config('request.jwt.claim.sub',denied::text,true);
 EXECUTE 'SET LOCAL ROLE authenticated';
 BEGIN PERFORM public.fr209_claim_kommunikationsvorgang(fixture); RAISE EXCEPTION 'negative claim unexpectedly succeeded'; EXCEPTION WHEN insufficient_privilege THEN NULL; END;
 EXECUTE format('SET LOCAL ROLE %I',original_role);
 PERFORM set_config('request.jwt.claims',json_build_object('sub',actor,'role','authenticated')::text,true);
 PERFORM set_config('request.jwt.claim.sub',actor::text,true);
 EXECUTE 'SET LOCAL ROLE authenticated';
 SELECT count(*) INTO n FROM public.fr209_claim_kommunikationsvorgang(fixture) WHERE zugewiesen_an=actor AND id=fixture;
 IF n<>1 THEN RAISE EXCEPTION 'positive claim failed'; END IF;
 BEGIN PERFORM public.fr209_claim_kommunikationsvorgang(fixture); RAISE EXCEPTION 'repeat claim unexpectedly succeeded'; EXCEPTION WHEN SQLSTATE 'PT409' THEN NULL; END;
 BEGIN UPDATE public.kommunikationsvorgang SET zugewiesen_an=NULL WHERE id=fixture; RAISE EXCEPTION 'direct assignment unexpectedly succeeded'; EXCEPTION WHEN insufficient_privilege THEN NULL; END;
 SELECT count(*) INTO n FROM public.fr209_release_kommunikationsvorgang(fixture) WHERE zugewiesen_an IS NULL AND id=fixture;
 IF n<>1 THEN RAISE EXCEPTION 'positive release failed'; END IF;
 BEGIN PERFORM public.fr209_release_kommunikationsvorgang(fixture); RAISE EXCEPTION 'repeat release unexpectedly succeeded'; EXCEPTION WHEN SQLSTATE 'PT409' THEN NULL; END;
 EXECUTE format('SET LOCAL ROLE %I',original_role);
 IF (SELECT zugewiesen_an FROM public.kommunikationsvorgang WHERE id=fixture) IS NOT NULL THEN RAISE EXCEPTION 'independent release persistence failed'; END IF;
 PERFORM set_config('request.jwt.claims',coalesce(old_claims,''),true);
 PERFORM set_config('request.jwt.claim.sub','',true);
 -- Unique durable reservation, simulated without SMTP and rolled back.
 INSERT INTO public.fr209_mail_reservation(reservation_key,kind) VALUES('fr209-sql-rollback-test','club');
 INSERT INTO public.fr209_mail_reservation(reservation_key,kind) VALUES('fr209-sql-rollback-test','club') ON CONFLICT DO NOTHING;
 IF FOUND THEN RAISE EXCEPTION 'duplicate mail reservation accepted'; END IF;
END;
$checks$;
SELECT 'FR209_SQL_RUNTIME_OK' AS result;
