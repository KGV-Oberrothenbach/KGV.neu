-- P11 AP-02B.1 / R05: an ended member may read only their own history until
-- 31 December of the following calendar year.  The date calculation is kept
-- separate from the authorization function so the boundary remains testable
-- without accepting a caller-controlled reference date.
begin;

create or replace function public.former_member_history_access_until(p_mitglied_ende date)
returns date
language sql
immutable
strict
set search_path = public, pg_temp
as $$
  select make_date(extract(year from p_mitglied_ende)::integer + 1, 12, 31);
$$;

create or replace function public.can_access_own_member_history_document(p_dokument_id bigint)
returns boolean
language sql
stable
security definer
set search_path = public, pg_temp
as $$
  select auth.uid() is not null
    and public.current_mitglied_id() is not null
    and exists (
      select 1
      from public.dokument d
      join public.mitglied m on m.id = d.mitglied_id
      where d.id = p_dokument_id
        and m.id = public.current_mitglied_id()
        and (
          m.mitglied_ende is null
          or public.kgv_local_now()::date <= public.former_member_history_access_until(m.mitglied_ende)
        )
    );
$$;

revoke all on function public.former_member_history_access_until(date) from public, anon;
revoke all on function public.can_access_own_member_history_document(bigint) from public, anon;
grant execute on function public.can_access_own_member_history_document(bigint) to authenticated, service_role;

-- The management branch deliberately remains separate.  It continues to use
-- effective rights, including individual grants and revocations; this change
-- only replaces the former unlimited self-member branch.
drop policy if exists dokument_select_effective on public.dokument;
create policy dokument_select_effective on public.dokument for select to authenticated
using (
  public.has_effective_permission(65536)
  or public.has_effective_permission(16)
  or public.can_access_own_member_history_document(dokument.id)
  or (parzelle_id is not null and exists (
    select 1 from public.parzellen_belegung b
    where b.parzelle_id = dokument.parzelle_id
      and b.mitglied_id = public.current_mitglied_id()
      and (b.von_datum is null or b.von_datum <= public.kgv_local_now()::date)
      and (b.bis_datum is null or b.bis_datum >= public.kgv_local_now()::date)
  ))
);

commit;
