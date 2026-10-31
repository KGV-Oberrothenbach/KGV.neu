-- NB-2A-D5B: register the new membership-application layout; do not execute without separate approval.
do $$
begin
    if exists (select 1 from public.pdf_template_catalog where template_key = 'mitgliedsantrag' and version = 'v5') then
        raise exception 'PDF template mitgliedsantrag/v5 already exists';
    end if;
    if not exists (select 1 from storage.objects where bucket_id = 'pdf-templates' and name = 'templates/mitgliedsantrag/v5/template.pdf') then
        raise exception 'Missing storage object templates/mitgliedsantrag/v5/template.pdf';
    end if;
    if not exists (select 1 from storage.objects where bucket_id = 'pdf-templates' and name = 'templates/mitgliedsantrag/v5/field-spec.json') then
        raise exception 'Missing storage object templates/mitgliedsantrag/v5/field-spec.json';
    end if;
end;
$$;
update public.pdf_template_catalog set aktiv = false where template_key = 'mitgliedsantrag' and aktiv = true;
insert into public.pdf_template_catalog
  (template_key, version, storage_bucket, storage_path, content_type, checksum, field_spec_path, field_spec_checksum, field_spec_version, renderer_version, status, aktiv, published_at)
values
  ('mitgliedsantrag', 'v5', 'pdf-templates', 'templates/mitgliedsantrag/v5/template.pdf', 'application/pdf',
   '9a543f2a7fc5471cca2a5e96d9dccb73f953380f8fdbec16149418b80e2a230b',
   'templates/mitgliedsantrag/v5/field-spec.json', '0aeee9220dae775bb9b2b662d5ce849b339ca5fbf6b4cde03ad7ad558d27f0ff',
   'mitgliedsantrag-field-spec/v5', 'top-left/v1', 'published', true, now());
