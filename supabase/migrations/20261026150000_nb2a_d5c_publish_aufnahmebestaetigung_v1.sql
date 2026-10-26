-- NB-2A-D5C: register the versioned admission-confirmation template.
-- Source only: this migration must not be executed without separate approval.
do $$
begin
    if exists (select 1 from public.pdf_template_catalog where template_key = 'aufnahmebestaetigung' and version = 'v1') then
        raise exception 'PDF template aufnahmebestaetigung/v1 already exists';
    end if;
    if not exists (select 1 from storage.objects where bucket_id = 'pdf-templates' and name = 'templates/aufnahmebestaetigung/v1/template.pdf') then
        raise exception 'Missing storage object templates/aufnahmebestaetigung/v1/template.pdf';
    end if;
    if not exists (select 1 from storage.objects where bucket_id = 'pdf-templates' and name = 'templates/aufnahmebestaetigung/v1/field-spec.json') then
        raise exception 'Missing storage object templates/aufnahmebestaetigung/v1/field-spec.json';
    end if;
end;
$$;

update public.pdf_template_catalog
set aktiv = false
where template_key = 'aufnahmebestaetigung' and aktiv = true;

insert into public.pdf_template_catalog
  (template_key, version, storage_bucket, storage_path, content_type, checksum, field_spec_path, field_spec_checksum, field_spec_version, renderer_version, status, aktiv, published_at)
values
  ('aufnahmebestaetigung', 'v1', 'pdf-templates', 'templates/aufnahmebestaetigung/v1/template.pdf', 'application/pdf',
   '74337529df99797ee7ff6f683171cea932873b205d6fff6a3f387597f7f8bb35',
   'templates/aufnahmebestaetigung/v1/field-spec.json', '0341437f905033be471f5caa5dda3484c26ae2cc1cc86f5922fa1ffec6e1eb0e',
   'aufnahmebestaetigung-field-spec/v1', 'top-left/v1', 'published', true, now());
