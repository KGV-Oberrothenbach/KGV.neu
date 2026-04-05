create or replace function public.get_user_role()
returns text
language sql
stable
as $$
  select public.current_app_role();
$$;

create or replace function public.sync_app_user_from_mitglied()
returns trigger
language plpgsql
as $$
begin
  if new.auth_user_id is null then
    if old.auth_user_id is not null then
      delete from public.app_user where user_id = old.auth_user_id;
    end if;
    return new;
  end if;

  insert into public.app_user (user_id, mitglied_id, updated_at)
  values (new.auth_user_id, new.id, now())
  on conflict (user_id) do update
    set mitglied_id = excluded.mitglied_id,
        updated_at = now();

  return new;
end;
$$;

create or replace function public.who_am_i()
returns table(vorname text, role text)
language sql
stable
as $$
  select
    m.vorname,
    public.current_app_role() as role
  from public.mitglied m
  where m.auth_user_id = auth.uid()
  limit 1;
$$;

drop trigger if exists trg_sync_app_user_from_mitglied on public.mitglied;

create trigger trg_sync_app_user_from_mitglied
after insert or update of auth_user_id on public.mitglied
for each row execute function public.sync_app_user_from_mitglied();
