alter table public.dokument
    add column if not exists drive_file_id text;

alter table public.dokument
    drop constraint if exists dokument_owner_chk;

alter table public.dokument
    add constraint dokument_owner_chk
    check (((mitglied_id is not null)::int + (parzelle_id is not null)::int) = 1);

create index if not exists ix_dokument_drive_file_id
    on public.dokument (drive_file_id);
