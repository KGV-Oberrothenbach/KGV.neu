-- P11 AP-02A / A3.6: direct document management must not bypass archive authority.
drop policy if exists dokument_write_effective on public.dokument;
create policy dokument_insert_effective on public.dokument for insert to authenticated
  with check (public.has_effective_permission(16));
create policy dokument_update_effective on public.dokument for update to authenticated
  using (public.has_effective_permission(16)) with check (public.has_effective_permission(16));

create or replace function public.guard_dokument_archive_history()
returns trigger language plpgsql set search_path=public,pg_temp as $$
begin
  if old.archiviert_at is not null then
    raise exception 'Archivierte Dokumente sind unveränderlich' using errcode='55000';
  end if;
  if (new.archiviert_at is distinct from old.archiviert_at
      or new.archiviert_by is distinct from old.archiviert_by
      or new.archiviert_begruendung is distinct from old.archiviert_begruendung)
     and current_user not in ('postgres','service_role') then
    raise exception 'Dokumentarchivierung ist nur über den geschützten Archivpfad zulässig' using errcode='42501';
  end if;
  if old.source_kommunikationsvorgang_id is not null
     and current_user not in ('postgres','service_role')
     and (new.mitglied_id is distinct from old.mitglied_id or new.parzelle_id is distinct from old.parzelle_id
          or new.storage_path is distinct from old.storage_path or new.drive_file_id is distinct from old.drive_file_id
          or new.source_kommunikationsvorgang_id is distinct from old.source_kommunikationsvorgang_id
          or new.source_revision is distinct from old.source_revision) then
    raise exception 'Historische Vertragsdokumente sind unveränderlich' using errcode='55000';
  end if;
  return new;
end $$;
drop trigger if exists trg_guard_dokument_archive_history on public.dokument;
create trigger trg_guard_dokument_archive_history before update on public.dokument
  for each row execute function public.guard_dokument_archive_history();
