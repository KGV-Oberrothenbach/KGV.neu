-- Nachweis, dass die für den Mitgliedsantrag relevanten Vereinsregelwerke
-- erfolgreich per E-Mail bereitgestellt wurden. NULL bedeutet: noch offen.
alter table public.mitglied
  add column if not exists regelwerke_versandt_am timestamptz;

comment on column public.mitglied.regelwerke_versandt_am is
  'Zeitpunkt, an dem Satzung, Kleingartenordnung und Beitragsordnung erfolgreich per E-Mail versandt wurden. Wird ausschließlich durch die Versand-Edge-Function gesetzt.';

-- Die aktuelle, beim Versand verwendete Fassung jedes Regelwerks liegt in einem
-- privaten Storage-Bucket. Die Edge-Function liest diese Einträge mit der
-- Service-Rolle und hängt die Dateien unmittelbar an die Mail an.
create table if not exists public.vereinsregelwerk (
  schluessel text primary key,
  titel text not null,
  stand date not null,
  storage_path text not null,
  dateiname text not null,
  mime_type text not null default 'application/pdf',
  aktiv boolean not null default true,
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now(),
  constraint vereinsregelwerk_schluessel_chk check (
    schluessel in ('satzung', 'kleingartenordnung', 'beitragsordnung')
  ),
  constraint vereinsregelwerk_storage_path_chk check (length(trim(storage_path)) > 0),
  constraint vereinsregelwerk_dateiname_chk check (length(trim(dateiname)) > 0)
);

alter table public.vereinsregelwerk enable row level security;

insert into storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
values (
  'vereinsregelwerke',
  'vereinsregelwerke',
  false,
  10485760,
  array['application/pdf']
)
on conflict (id) do update
set public = excluded.public,
    file_size_limit = excluded.file_size_limit,
    allowed_mime_types = excluded.allowed_mime_types;

comment on table public.vereinsregelwerk is
  'Aktuelle, versandfähige Fassungen von Satzung, Kleingartenordnung und Beitragsordnung im privaten Bucket vereinsregelwerke.';
