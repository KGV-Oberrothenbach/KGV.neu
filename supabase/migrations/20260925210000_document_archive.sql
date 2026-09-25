-- Dokumente werden fachlich archiviert, nie physisch aus Google Drive entfernt.
alter table public.dokument
  add column if not exists archiviert_at timestamptz,
  add column if not exists archiviert_by uuid references auth.users(id),
  add column if not exists archiviert_begruendung text;

do $$
begin
  alter table public.dokument
    add constraint dokument_archivierung_chk check (
      (archiviert_at is null and archiviert_by is null and archiviert_begruendung is null)
      or
      (archiviert_at is not null and archiviert_by is not null and length(trim(coalesce(archiviert_begruendung, ''))) >= 3)
    ) not valid;
exception when duplicate_object then null;
end $$;

alter table public.dokument validate constraint dokument_archivierung_chk;

create index if not exists ix_dokument_aktiv on public.dokument (mitglied_id, parzelle_id, updated_at desc)
  where archiviert_at is null;

comment on column public.dokument.archiviert_at is 'Zeitpunkt der fachlichen Archivierung; die Drive-Datei bleibt erhalten.';
comment on column public.dokument.archiviert_by is 'Admin, der die Archivierung mit dem gesonderten Archivpasswort bestätigt hat.';
comment on column public.dokument.archiviert_begruendung is 'Pflichtbegründung für die fachliche Archivierung.';
