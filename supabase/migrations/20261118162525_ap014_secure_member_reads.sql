-- AP-01.4: member rows contain personal data and therefore need a single,
-- caller-bound read decision.  CanViewMembers from the ordinary user role is
-- only a UI/navigation capability while CanSeeOwnDataOnly limits its scope.
-- An individual CanViewMembers grant is treated as an explicit global grant;
-- CanEditAllMembers always implies the read access needed for updates.

begin;

create or replace function public.can_read_mitglied(p_mitglied_id bigint, p_auth_user_id uuid)
returns boolean
language sql
stable
security definer
set search_path = public, pg_temp
as $function$
  with actor as (
    select
      au.mitglied_id,
      public.permission_role_base(au.role) as base_permissions,
      coalesce(au.permission_grants, 0::bigint) as permission_grants,
      (public.permission_role_base(au.role) | coalesce(au.permission_grants, 0::bigint))
        & ~coalesce(au.permission_revocations, 0::bigint) as effective_permissions
    from public.app_user au
    where au.user_id = (select auth.uid())
    limit 1
  )
  select coalesce((
    select
      -- Administrative write access includes the SELECT required by UPDATE.
      (a.effective_permissions & 4::bigint) = 4::bigint
      or (
        (a.effective_permissions & 2::bigint) = 2::bigint
        and (
          -- Vorstand/admin receive global member visibility from their role
          -- template.  A user needs an explicit individual global grant.
          (a.base_permissions & 8::bigint) = 0::bigint
          or (a.permission_grants & 2::bigint) = 2::bigint
        )
      )
      or (
        -- Own access requires the effective permission and both persisted
        -- sides of the Auth/app_user/mitglied binding to agree.
        (a.effective_permissions & 8::bigint) = 8::bigint
        and a.mitglied_id = p_mitglied_id
        and p_auth_user_id = (select auth.uid())
      )
      or (
        -- An active legal representative may read the represented minor, but
        -- receives no visibility into unrelated member rows.
        (a.effective_permissions & 8::bigint) = 8::bigint
        and a.mitglied_id is not null
        and exists (
          select 1
          from public.mitglied_gesetzlicher_vertreter relation
          where relation.vertreter_mitglied_id = a.mitglied_id
            and relation.minderjaehriges_mitglied_id = p_mitglied_id
            and relation.gueltig_ab <= current_date
            and (relation.gueltig_bis is null or relation.gueltig_bis >= current_date)
        )
      )
    from actor a
  ), false);
$function$;

revoke all on function public.can_read_mitglied(bigint, uuid)
  from public, anon, authenticated;
grant execute on function public.can_read_mitglied(bigint, uuid)
  to authenticated;

drop policy if exists mitglied_select_effective_or_own on public.mitglied;
drop policy if exists mitglied_select_own_or_admin on public.mitglied;
create policy mitglied_select_authorized_scope
on public.mitglied
for select
to authenticated
using (public.can_read_mitglied(id, auth_user_id));

comment on function public.can_read_mitglied(bigint, uuid) is
  'AP-01.4: fail-closed member-row read authorization using effective rights, consistent own binding and active legal representation.';
comment on policy mitglied_select_authorized_scope on public.mitglied is
  'AP-01.4: blocks foreign personal member data for own-only users while preserving effective administration and active legal representation.';

commit;
