-- RB-05 regression for the board-review read model.
-- Run AFTER the corresponding migration in a local test database.
-- The test is read-only and changes the SQL session role only within a transaction.
BEGIN;
DO $$
BEGIN
  IF has_function_privilege('anon', 'public.darf_mitgliedsaufnahme_lesen(bigint)', 'EXECUTE') THEN
    RAISE EXCEPTION 'RB05: anon must not execute board-review RLS helper';
  END IF;
  IF NOT has_function_privilege('authenticated', 'public.darf_mitgliedsaufnahme_lesen(bigint)', 'EXECUTE') THEN
    RAISE EXCEPTION 'RB05: authenticated cannot execute board-review RLS helper';
  END IF;
END;
$$;
SET LOCAL ROLE authenticated;
SELECT set_config('request.jwt.claim.sub', '00000000-0000-0000-0000-000000000000', true);
-- This SELECT must not error with SQLSTATE 42501 on an otherwise empty result.
SELECT count(*) AS visible_board_reviews_for_unknown_actor
FROM public.mitgliedsaufnahme_vorstand_liste;
-- Direct invocation must not grant an unknown account review access.
DO $$
BEGIN
  IF public.darf_mitgliedsaufnahme_lesen(2147483647) THEN
    RAISE EXCEPTION 'RB05: unknown actor can read an unrelated board review';
  END IF;
END;
$$;
ROLLBACK;
