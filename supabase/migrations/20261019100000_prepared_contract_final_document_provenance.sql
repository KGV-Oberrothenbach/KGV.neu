-- MA-REF2: immutable provenance for final documents created by a communication workflow.
alter table public.dokument
  add column if not exists source_kommunikationsvorgang_id bigint references public.kommunikationsvorgang(id) on delete restrict,
  add column if not exists source_revision integer;
alter table public.dokument
  drop constraint if exists dokument_final_provenance_chk;
alter table public.dokument
  add constraint dokument_final_provenance_chk check (
    (source_kommunikationsvorgang_id is null and source_revision is null)
    or (source_kommunikationsvorgang_id is not null and source_revision is not null and source_revision >= 1)
  );
-- A workflow can produce at most one final document. This is deliberately independent
-- of filename, timestamp, document title, or the document owner.
create unique index if not exists ux_dokument_source_kommunikationsvorgang
  on public.dokument(source_kommunikationsvorgang_id)
  where source_kommunikationsvorgang_id is not null;
create index if not exists ix_dokument_source_kommunikationsvorgang_revision
  on public.dokument(source_kommunikationsvorgang_id, source_revision)
  where source_kommunikationsvorgang_id is not null;
comment on column public.dokument.source_kommunikationsvorgang_id is 'Kommunikationsvorgang, der dieses finale Dokument erzeugt hat.';
comment on column public.dokument.source_revision is 'Eingefrorene Revision des Kommunikationsvorgangs bei Finalize.';
