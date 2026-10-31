-- NB-2A-D4: register a new template version; do not execute without separate approval.
do $$
begin
    if exists (select 1 from public.pdf_template_catalog where template_key = 'pachtvertrag' and version = 'v5') then
        raise exception 'PDF template pachtvertrag/v5 already exists';
    end if;
    if not exists (select 1 from storage.objects where bucket_id = 'pdf-templates' and name = 'templates/pachtvertrag/v5/template.pdf') then
        raise exception 'Missing storage object templates/pachtvertrag/v5/template.pdf';
    end if;
    if not exists (select 1 from storage.objects where bucket_id = 'pdf-templates' and name = 'templates/pachtvertrag/v5/field-spec.json') then
        raise exception 'Missing storage object templates/pachtvertrag/v5/field-spec.json';
    end if;
end;
$$;
update public.pdf_template_catalog set aktiv = false where template_key = 'pachtvertrag' and aktiv = true;
insert into public.pdf_template_catalog
  (template_key, version, storage_bucket, storage_path, content_type, checksum, field_spec_path, field_spec_checksum, field_spec_version, renderer_version, status, aktiv, published_at)
values
  ('pachtvertrag', 'v5', 'pdf-templates', 'templates/pachtvertrag/v5/template.pdf', 'application/pdf',
   '31749f1546c186960808b60a609eaac1dc29f488fff080fdaa590472e0d8b165',
   'templates/pachtvertrag/v5/field-spec.json', '0c688da1910b40c06d3421fc603f777ee418d8e8fa4c59c8254db47cd3ae5483',
   'pachtvertrag-field-spec/v5', 'top-left/v1', 'published', true, now());
