-- G13.5: Auth owns the email address once a member has an application account.
update public.mitglied m
set email = u.email
from public.app_user au
join auth.users u on u.id = au.user_id
where au.mitglied_id = m.id
  and m.auth_user_id = au.user_id
  and au.role in ('admin', 'vorstand', 'user')
  and nullif(trim(u.email), '') is not null
  and lower(trim(coalesce(m.email, ''))) <> lower(trim(u.email));
create or replace function public.guard_linked_member_email()
returns trigger language plpgsql security definer set search_path = public, pg_temp as $$
declare v_auth_email text;
begin
  if new.email is not distinct from old.email then return new; end if;
  -- A formerly linked member cannot combine unlink and an arbitrary email write.
  if old.auth_user_id is not null then
    select u.email into v_auth_email from auth.users u where u.id = old.auth_user_id;
    if nullif(trim(v_auth_email), '') is null or lower(trim(new.email)) <> lower(trim(v_auth_email)) then
      raise exception 'Linked member email is controlled by Supabase Auth' using errcode = '42501';
    end if;
    return new;
  end if;
  if new.auth_user_id is not null then
    select u.email into v_auth_email from auth.users u where u.id = new.auth_user_id;
    if nullif(trim(v_auth_email), '') is null or lower(trim(new.email)) <> lower(trim(v_auth_email)) then
      raise exception 'Linked member email must match Supabase Auth' using errcode = '42501';
    end if;
  end if;
  return new;
end;
$$;
drop trigger if exists guard_linked_member_email on public.mitglied;
create trigger guard_linked_member_email before update of email, auth_user_id on public.mitglied
for each row execute function public.guard_linked_member_email();
create or replace function public.sync_current_member_email()
returns text language plpgsql security definer set search_path = public, pg_temp as $$
declare v_context record; v_email text;
begin
  if auth.uid() is null then raise exception 'Authentication required' using errcode = '42501'; end if;
  select * into v_context from public.current_app_user_context() limit 1;
  if not found or v_context.user_id <> auth.uid() then raise exception 'Valid app user context required' using errcode = '42501'; end if;
  select email into v_email from auth.users where id = auth.uid();
  if nullif(trim(v_email), '') is null then raise exception 'Auth email missing' using errcode = '23514'; end if;
  update public.mitglied set email = v_email
  where id = v_context.mitglied_id and auth_user_id = auth.uid();
  if not found then raise exception 'Invalid member link' using errcode = '23514'; end if;
  return v_email;
end;
$$;
grant execute on function public.sync_current_member_email() to authenticated;
