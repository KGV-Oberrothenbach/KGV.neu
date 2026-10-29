-- v2 is already published and immutable. Publish v3 with new, immutable
-- storage paths instead of changing any v2 metadata or artifacts.
do $$
begin
  if exists (
    select 1 from public.pdf_template_catalog
    where template_key = 'export-allgemein' and version = 'v3'
      and (storage_bucket <> 'pdf-templates'
        or storage_path <> 'templates/export-allgemein/v3/template.pdf'
        or content_type <> 'application/pdf'
        or checksum <> '5f291e6c7a78bf16aa954c67c61372a55529b2af07a678df45006e2f3ccec741'
        or field_spec_path <> 'templates/export-allgemein/v3/field-spec.json'
        or field_spec_checksum <> '8d526a6e7ebf7180dad5c6e33ad98141ce48f985b0d846c4f8319b504c1675cd'
        or field_spec_version <> 'export-allgemein-field-spec/v3'
        or renderer_version <> 'top-left/v1')
  ) then
    raise exception 'A export-allgemein v3 catalog entry exists with different immutable artifacts';
  end if;

  if not exists (select 1 from storage.objects where bucket_id = 'pdf-templates' and name = 'templates/export-allgemein/v3/template.pdf') then
    raise exception 'Missing storage object templates/export-allgemein/v3/template.pdf';
  end if;
  if not exists (select 1 from storage.objects where bucket_id = 'pdf-templates' and name = 'templates/export-allgemein/v3/field-spec.json') then
    raise exception 'Missing storage object templates/export-allgemein/v3/field-spec.json';
  end if;

  update public.pdf_template_catalog
  set aktiv = false
  where template_key = 'export-allgemein' and aktiv = true and version <> 'v3';

  insert into public.pdf_template_catalog (
    template_key, version, storage_bucket, storage_path, content_type, checksum,
    field_spec_path, field_spec_checksum, field_spec_version, renderer_version,
    status, aktiv, published_at
  )
  select
    'export-allgemein', 'v3', 'pdf-templates',
    'templates/export-allgemein/v3/template.pdf', 'application/pdf',
    '5f291e6c7a78bf16aa954c67c61372a55529b2af07a678df45006e2f3ccec741',
    'templates/export-allgemein/v3/field-spec.json',
    '8d526a6e7ebf7180dad5c6e33ad98141ce48f985b0d846c4f8319b504c1675cd',
    'export-allgemein-field-spec/v3', 'top-left/v1', 'published', true, now()
  where not exists (
    select 1 from public.pdf_template_catalog
    where template_key = 'export-allgemein' and version = 'v3'
  );

  update public.pdf_template_catalog
  set status = 'published', aktiv = true
  where template_key = 'export-allgemein' and version = 'v3';
end;
$$;
