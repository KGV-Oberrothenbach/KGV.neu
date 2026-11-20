-- RB-05: repair the authenticated RLS helper call used by the board-review list.
-- The view public.mitgliedsaufnahme_vorstand_liste is SECURITY INVOKER.
-- Its underlying RLS policies call this SECURITY DEFINER helper as the caller,
-- so authenticated requires EXECUTE; otherwise GET /rest/v1/mitgliedsaufnahme_vorstand_liste
-- returns 403 even when there are zero board-review rows.
--
-- The helper uses auth.uid() and returns only whether the current caller can
-- read the requested board review. This change does not grant access to anon,
-- bypass RLS or alter any membership, vote or contract record.

REVOKE ALL ON FUNCTION public.darf_mitgliedsaufnahme_lesen(bigint) FROM PUBLIC, anon;
GRANT EXECUTE ON FUNCTION public.darf_mitgliedsaufnahme_lesen(bigint) TO authenticated;
