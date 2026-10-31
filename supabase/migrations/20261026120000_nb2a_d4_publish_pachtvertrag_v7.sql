-- NB-2A-D4: register the corrected lease-contract layout; do not execute without separate approval.
do $$
begin
    if exists (select 1 from public.pdf_template_catalog where template_key = 'pachtvertrag' and version = 'v7') then
        raise exception 'PDF template pachtvertrag/v7 already exists';
    end if;
    if not exists (select 1 from storage.objects where bucket_id = 'pdf-templates' and name = 'templates/pachtvertrag/v7/template.pdf') then
        raise exception 'Missing storage object templates/pachtvertrag/v7/template.pdf';
    end if;
    if not exists (select 1 from storage.objects where bucket_id = 'pdf-templates' and name = 'templates/pachtvertrag/v7/field-spec.json') then
        raise exception 'Missing storage object templates/pachtvertrag/v7/field-spec.json';
    end if;
end;
$$;
update public.pdf_template_catalog set aktiv = false where template_key = 'pachtvertrag' and aktiv = true;
insert into public.pdf_template_catalog
  (template_key, version, storage_bucket, storage_path, content_type, checksum, field_spec_path, field_spec_checksum, field_spec_version, renderer_version, status, aktiv, published_at)
values
  ('pachtvertrag', 'v7', 'pdf-templates', 'templates/pachtvertrag/v7/template.pdf', 'application/pdf',
   'd89d6d02b1268acc77990fd9158e9f8f22fa900c725c5ed89fb67790960fdb25',
   'templates/pachtvertrag/v7/field-spec.json', 'cca945bcdb74832e9db567ec6670ab3869f44fd61ef079d104dc7fe7987d213a',
   'pachtvertrag-field-spec/v7', 'top-left/v1', 'published', true, now());
