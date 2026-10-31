-- DOC-UI0a: Atomare chronologische Nachfolge einer Vereinsdokumentfassung.
create or replace function public.create_vereinsdokument_fassung(
  p_dokumenttyp_code text,
  p_fassung text,
  p_gueltig_ab date,
  p_storage_path text,
  p_dateiname text,
  p_sha256 text
)
returns public.vereinsdokument_fassung
language plpgsql
security definer
set search_path = public, pg_temp
as $$
declare
  v_latest public.vereinsdokument_fassung;
  v_result public.vereinsdokument_fassung;
  v_has_latest boolean;
begin
  if not (
    auth.role() = 'service_role'
    or exists (select 1 from public.current_app_user_context() c where c.role in ('admin', 'vorstand'))
  ) then
    raise exception 'Nur Admin oder Vorstand dürfen Vereinsdokumentfassungen anlegen' using errcode = '42501';
  end if;

  if not exists (select 1 from public.vereinsdokument_typ where code = p_dokumenttyp_code) then
    raise exception 'Unbekannter Vereinsdokumenttyp: %', p_dokumenttyp_code using errcode = '23503';
  end if;
  if p_gueltig_ab is null or length(btrim(p_fassung)) = 0
     or length(btrim(p_storage_path)) = 0 or length(btrim(p_dateiname)) = 0
     or length(btrim(p_sha256)) = 0 then
    raise exception 'Fassung, gueltig_ab, storage_path, dateiname und sha256 sind erforderlich' using errcode = '23514';
  end if;
  if p_sha256 !~ '^[0-9a-f]{64}$' then
    raise exception 'sha256 muss ein 64-stelliger hexadezimaler Wert sein' using errcode = '23514';
  end if;
  if p_storage_path not like 'vereinsunterlagen/' || p_dokumenttyp_code || '/%' then
    raise exception 'storage_path muss unter vereinsunterlagen/% liegen', p_dokumenttyp_code using errcode = '23514';
  end if;

  -- Serialisiert auch zwei parallele Erst-Anlagen, bei denen noch keine Zeile
  -- für SELECT ... FOR UPDATE existiert.
  perform pg_advisory_xact_lock(hashtextextended(p_dokumenttyp_code, 0));
  perform 1 from public.vereinsdokument_fassung
    where dokumenttyp_code = p_dokumenttyp_code for update;
  select * into v_latest from public.vereinsdokument_fassung
    where dokumenttyp_code = p_dokumenttyp_code order by gueltig_ab desc limit 1;
  v_has_latest := found;

  if v_has_latest and p_gueltig_ab <= v_latest.gueltig_ab then
    raise exception 'Eine Nachfolgefassung muss nach der zeitlich letzten Fassung beginnen' using errcode = '23514';
  end if;

  if v_has_latest and v_latest.gueltig_bis is null then
    update public.vereinsdokument_fassung
      set gueltig_bis = p_gueltig_ab - 1
      where id = v_latest.id;
  end if;

  insert into public.vereinsdokument_fassung
    (dokumenttyp_code, fassung, gueltig_ab, storage_path, dateiname, sha256)
  values (p_dokumenttyp_code, p_fassung, p_gueltig_ab, p_storage_path, p_dateiname, p_sha256)
  returning * into v_result;
  return v_result;
end;
$$;
-- Nur unregistrierte Upload-Reste dürfen clientseitig bereinigt werden.
-- Sobald eine Fassung den Pfad referenziert, ist die Datei nicht löschbar.
create policy vereinsunterlagen_cleanup_unregistered_admin_vorstand
on storage.objects
for delete to authenticated
using (
  bucket_id = 'vereinsregelwerke'
  and (storage.foldername(name))[1] = 'vereinsunterlagen'
  and not exists (
    select 1 from public.vereinsdokument_fassung f
    where f.storage_path = name
  )
  and exists (
    select 1 from public.current_app_user_context() c
    where c.role in ('admin', 'vorstand')
  )
);
-- Fassungen dürfen für angemeldete Benutzer ausschließlich über die RPC
-- angelegt bzw. fortgeschrieben werden. Die Lesesicht für Vorstand/Admin bleibt.
drop policy if exists vereinsdokument_fassung_admin_vorstand_all on public.vereinsdokument_fassung;
create policy vereinsdokument_fassung_read_admin_vorstand
on public.vereinsdokument_fassung
for select to authenticated
using (
  exists (
    select 1 from public.current_app_user_context() c
    where c.role in ('admin', 'vorstand')
  )
);
revoke insert, update, delete on table public.vereinsdokument_fassung from authenticated;
grant select on table public.vereinsdokument_fassung to authenticated;
revoke all on function public.create_vereinsdokument_fassung(text, text, date, text, text, text) from public, anon;
grant execute on function public.create_vereinsdokument_fassung(text, text, date, text, text, text) to authenticated, service_role;
