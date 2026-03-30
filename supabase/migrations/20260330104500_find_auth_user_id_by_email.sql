create or replace function public.find_auth_user_id_by_email(p_email text)
returns table(auth_user_id uuid)
language plpgsql
stable
security definer
set search_path to 'public', 'auth', 'pg_temp'
as $$
begin
    if nullif(btrim(p_email), '') is null then
        return;
    end if;

    if auth.role() <> 'service_role' and not public.is_admin_or_vorstand() then
        raise exception 'not authorized';
    end if;

    return query
    select u.id
    from auth.users u
    where lower(coalesce(u.email, '')) = lower(btrim(p_email))
    order by u.created_at desc nulls last, u.id desc
    limit 1;
end;
$$;

alter function public.find_auth_user_id_by_email(text) owner to postgres;

grant execute on function public.find_auth_user_id_by_email(text) to authenticated;
grant execute on function public.find_auth_user_id_by_email(text) to service_role;
