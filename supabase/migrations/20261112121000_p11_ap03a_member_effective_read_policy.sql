-- P11 AP-03.1 follow-up: UPDATE RLS alone is insufficient because PostgreSQL
-- applies SELECT visibility to UPDATE targets.  Preserve own access and the
-- existing member-read capability while making CanEditAllMembers sufficient.

drop policy if exists mitglied_select_own_or_admin on public.mitglied;
create policy mitglied_select_effective_or_own on public.mitglied
for select to authenticated
using (
  public.has_any_effective_permission(2 | 4)
  or id = public.current_mitglied_id()
  or (auth_user_id is not null and auth_user_id = auth.uid())
);

comment on policy mitglied_select_effective_or_own on public.mitglied is
  'P11 AP-03.1: administrative member visibility is governed by effective CanViewMembers (2) or CanEditAllMembers (4); own access remains caller-bound.';
