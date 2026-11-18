-- AP-01.4 follow-up: ON DELETE SET NULL is implemented as a nested UPDATE on
-- the referencing table. Permit only that non-privilege-expanding transition;
-- ordinary member updates must keep the immutable creator binding.

begin;

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

  if new.erstellt_von_auth_user_id is not distinct from old.erstellt_von_auth_user_id then
    return new;
  end if;

  -- PostgreSQL executes FK referential actions as nested SQL statements.  The
  -- only permitted creator change is the nested SET NULL caused by deleting
  -- the referenced auth.users row.  This can only remove creator scope.
  if old.erstellt_von_auth_user_id is not null
     and new.erstellt_von_auth_user_id is null
     and pg_trigger_depth() > 1 then
    return new;
  end if;

  new.erstellt_von_auth_user_id := old.erstellt_von_auth_user_id;
  return new;
end;
$function$;

revoke all on function public.guard_mitglied_creator_binding()
  from public, anon, authenticated;

comment on function public.guard_mitglied_creator_binding() is
  'Keeps member creator provenance immutable while allowing the nested FK ON DELETE SET NULL action during auth-user deletion.';

commit;
