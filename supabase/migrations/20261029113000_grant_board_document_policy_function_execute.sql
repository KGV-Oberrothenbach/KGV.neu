-- RLS evaluates this boolean-only helper for the board-review document policy.
-- The grant allows policy evaluation; the helper itself grants no document data.
grant execute on function public.can_access_mitgliedsaufnahme_vorstand_document(uuid,bigint) to authenticated;
