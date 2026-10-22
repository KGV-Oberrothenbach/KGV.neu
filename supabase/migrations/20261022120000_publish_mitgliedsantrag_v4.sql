-- Upload the immutable v4 template and field specification before applying this migration.
do $$
begin
    if exists (select 1 from public.pdf_template_catalog where template_key = 'mitgliedsantrag' and version = 'v4') then
        raise exception 'PDF template mitgliedsantrag/v4 already exists';
    end if;
    if not exists (select 1 from storage.objects where bucket_id = 'pdf-templates' and name = 'templates/mitgliedsantrag/v4/template.pdf') then
        raise exception 'Missing storage object templates/mitgliedsantrag/v4/template.pdf';
    end if;
    if not exists (select 1 from storage.objects where bucket_id = 'pdf-templates' and name = 'templates/mitgliedsantrag/v4/field-spec.json') then
        raise exception 'Missing storage object templates/mitgliedsantrag/v4/field-spec.json';
    end if;
end;
$$;

update public.pdf_template_catalog set aktiv = false where template_key = 'mitgliedsantrag' and aktiv = true;
insert into public.pdf_template_catalog
  (template_key, version, storage_bucket, storage_path, content_type, checksum, field_spec_path, field_spec_checksum, field_spec_version, renderer_version, status, aktiv, published_at)
values
  ('mitgliedsantrag', 'v4', 'pdf-templates', 'templates/mitgliedsantrag/v4/template.pdf', 'application/pdf',
   '9a543f2a7fc5471cca2a5e96d9dccb73f953380f8fdbec16149418b80e2a230b',
   'templates/mitgliedsantrag/v4/field-spec.json', '1a467e37adf3c54f606961c3ad8b87f96fb6ffeae56887445f96857bc4297b6f',
   'mitgliedsantrag-field-spec/v4', 'top-left/v1', 'published', true, now());
