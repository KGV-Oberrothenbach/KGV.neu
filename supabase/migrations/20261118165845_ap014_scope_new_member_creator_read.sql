-- AP-01.4 follow-up: an authenticated actor with only CanCreateMitglied must
-- receive the row created through PostgREST return=representation, without
-- gaining read access to any other member row.

begin;

alter table public.mitglied
  add column erstellt_von_auth_user_id uuid
  references auth.users(id) on delete set null;

alter table public.mitglied
  alter column erstellt_von_auth_user_id set default auth.uid();

create index mitglied_erstellt_von_auth_user_id_idx
  on public.mitglied(erstellt_von_auth_user_id)
  where erstellt_von_auth_user_id is not null;

create or replace function public.guard_mitglied_creator_binding()
returns trigger
language plpgsql
security invoker
set search_path = public, pg_temp
as $function$
begin
  if tg_op = 'INSERT' then
    if auth.uid() is not null then
      new.erstellt_von_auth_user_id := auth.uid();
    end if;
    return new;
  end if;

  -- Creator scope is an immutable provenance fact, not an assignable right.
  new.erstellt_von_auth_user_id := old.erstellt_von_auth_user_id;
  return new;
end;
$function$;

revoke all on function public.guard_mitglied_creator_binding()
  from public, anon, authenticated;

drop trigger if exists trg_guard_mitglied_creator_binding on public.mitglied;
create trigger trg_guard_mitglied_creator_binding
before insert or update on public.mitglied
for each row execute function public.guard_mitglied_creator_binding();

drop policy if exists mitglied_insert_effective_member_creation on public.mitglied;
create policy mitglied_insert_effective_member_creation
on public.mitglied
for insert
to authenticated
with check (
  public.has_effective_permission(524288)
  and erstellt_von_auth_user_id = (select auth.uid())
);

drop policy if exists mitglied_select_authorized_scope on public.mitglied;
create policy mitglied_select_authorized_scope
on public.mitglied
for select
to authenticated
using (
  public.can_read_mitglied(id, auth_user_id)
  or (
    erstellt_von_auth_user_id = (select auth.uid())
    and public.has_effective_permission(524288)
  )
);

comment on column public.mitglied.erstellt_von_auth_user_id is
  'Immutable creator binding used only for the narrow AP-01.4 post-create workflow scope.';
comment on policy mitglied_select_authorized_scope on public.mitglied is
  'AP-01.4: effective administration, own/representative scope, or a still-authorized creator reading only the row they created.';

commit;
