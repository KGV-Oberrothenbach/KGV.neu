alter table public.zaehler_ablesung
  add column if not exists foto_drive_file_id text,
  add column if not exists foto_dateiname text;

comment on column public.zaehler_ablesung.foto_drive_file_id is 'Geschützte Google-Drive-Datei-ID des Ablesefotos.';
comment on column public.zaehler_ablesung.foto_dateiname is 'Originalname des geschützten Ablesefotos.';
