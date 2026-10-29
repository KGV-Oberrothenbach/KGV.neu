-- Replace the corrupted v1 export background with a blank, branded v2 template.
do $$
begin
    if exists (
        select 1 from public.pdf_template_catalog
        where template_key = 'export-allgemein' and version = 'v2'
    ) then
        raise exception 'PDF template export-allgemein/v2 already exists';
    end if;
    if not exists (
        select 1 from storage.objects
        where bucket_id = 'pdf-templates'
          and name = 'templates/export-allgemein/v2/template.pdf'
    ) then
        raise exception 'Missing storage object templates/export-allgemein/v2/template.pdf';
    end if;
    if not exists (
        select 1 from storage.objects
        where bucket_id = 'pdf-templates'
          and name = 'templates/export-allgemein/v2/field-spec.json'
    ) then
        raise exception 'Missing storage object templates/export-allgemein/v2/field-spec.json';
    end if;
end;
$$;

update public.pdf_template_catalog
set aktiv = false
where template_key = 'export-allgemein' and aktiv = true;

insert into public.pdf_template_catalog
  (template_key, version, storage_bucket, storage_path, content_type, checksum,
   field_spec_path, field_spec_checksum, field_spec_version, renderer_version,
   status, aktiv, published_at)
values
  ('export-allgemein', 'v2', 'pdf-templates',
   'templates/export-allgemein/v2/template.pdf', 'application/pdf',
   '5f291e6c7a78bf16aa954c67c61372a55529b2af07a678df45006e2f3ccec741',
   'templates/export-allgemein/v2/field-spec.json',
   '8d526a6e7ebf7180dad5c6e33ad98141ce48f985b0d846c4f8319b504c1675cd',
   'export-allgemein-field-spec/v2', 'top-left/v1', 'published', true, now());
