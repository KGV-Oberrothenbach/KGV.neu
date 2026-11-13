-- P11 AP-03.1: self service is deliberately limited to private contact data.
-- Administrative member maintenance uses the effective CanEditAllMembers bit.

drop policy if exists mitglied_admin_full on public.mitglied;
create policy mitglied_insert_effective_member_creation on public.mitglied
for insert to authenticated
with check (public.has_effective_permission(524288));
create policy mitglied_update_effective_edit_all_members on public.mitglied
for update to authenticated
using (public.has_effective_permission(4))
with check (public.has_effective_permission(4));
create policy mitglied_delete_effective_edit_all_members on public.mitglied
for delete to authenticated
using (public.has_effective_permission(4));

create or replace function public.update_own_member_contact(
  p_adresse text,
  p_plz text,
  p_ort text,
  p_telefon text default null,
  p_handy text default null
)
returns public.mitglied
language plpgsql
security definer
set search_path = public, pg_temp
as $$
declare v_member public.mitglied%rowtype;
begin
  if auth.uid() is null or public.current_mitglied_id() is null then
    raise exception 'Anmeldung mit gültiger Mitgliedsverknüpfung erforderlich.' using errcode='42501';
  end if;
  select * into v_member from public.mitglied
  where id=public.current_mitglied_id() and auth_user_id=auth.uid()
  for update;
  if not found then
    raise exception 'Ungültige Mitgliedsverknüpfung.' using errcode='42501';
  end if;
  update public.mitglied set
    adresse=nullif(btrim(p_adresse),''), plz=nullif(btrim(p_plz),''), ort=nullif(btrim(p_ort),''),
    telefon=nullif(btrim(p_telefon),''), handy=nullif(btrim(p_handy),'')
  where id=v_member.id
  returning * into v_member;
  return v_member;
end;
$$;
revoke all on function public.update_own_member_contact(text,text,text,text,text) from public, anon;
grant execute on function public.update_own_member_contact(text,text,text,text,text) to authenticated;
comment on function public.update_own_member_contact(text,text,text,text,text) is
  'P11 AP-03.1: caller-bound self service for address and telephone only. Email remains Supabase Auth-authoritative through sync_current_member_email().';
