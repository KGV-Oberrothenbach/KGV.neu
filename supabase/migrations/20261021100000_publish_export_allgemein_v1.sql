-- Shared layout for every active export definition that permits PDF output.
-- Upload both immutable storage objects before applying this migration.
do $$
begin
    if exists (select 1 from public.pdf_template_catalog where template_key = 'export-allgemein' and version = 'v1') then
        raise exception 'PDF template export-allgemein/v1 already exists';
    end if;
    if not exists (select 1 from storage.objects where bucket_id = 'pdf-templates' and name = 'templates/export-allgemein/v1/template.pdf') then
        raise exception 'Missing storage object templates/export-allgemein/v1/template.pdf';
    end if;
    if not exists (select 1 from storage.objects where bucket_id = 'pdf-templates' and name = 'templates/export-allgemein/v1/field-spec.json') then
        raise exception 'Missing storage object templates/export-allgemein/v1/field-spec.json';
    end if;
end;
$$;

update public.pdf_template_catalog
set aktiv = false
where template_key in ('export-zaehler-eichfaelligkeit-hoch', 'export-zaehler-eichfaelligkeit-quer')
  and aktiv = true;

insert into public.pdf_template_catalog
  (template_key, version, storage_bucket, storage_path, content_type, checksum,
   field_spec_path, field_spec_checksum, field_spec_version, renderer_version,
   status, aktiv, published_at)
values
  ('export-allgemein', 'v1', 'pdf-templates',
   'templates/export-allgemein/v1/template.pdf', 'application/pdf',
   '05c43ad11087a083d3164be803653dc8f0ddee39911befad029fdd4197727c0b',
   'templates/export-allgemein/v1/field-spec.json',
   '3459429f728ae7ba28919f10482b9443b6c3a9f051b99418c1519ca2c296df1c',
   'export-allgemein-field-spec/v1', 'top-left/v1', 'published', true, now());
