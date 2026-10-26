-- NB-2A-D4: register the revised lease-contract layout; do not execute without separate approval.
do $$
begin
    if exists (select 1 from public.pdf_template_catalog where template_key = 'pachtvertrag' and version = 'v6') then
        raise exception 'PDF template pachtvertrag/v6 already exists';
    end if;
    if not exists (select 1 from storage.objects where bucket_id = 'pdf-templates' and name = 'templates/pachtvertrag/v6/template.pdf') then
        raise exception 'Missing storage object templates/pachtvertrag/v6/template.pdf';
    end if;
    if not exists (select 1 from storage.objects where bucket_id = 'pdf-templates' and name = 'templates/pachtvertrag/v6/field-spec.json') then
        raise exception 'Missing storage object templates/pachtvertrag/v6/field-spec.json';
    end if;
end;
$$;

update public.pdf_template_catalog set aktiv = false where template_key = 'pachtvertrag' and aktiv = true;
insert into public.pdf_template_catalog
  (template_key, version, storage_bucket, storage_path, content_type, checksum, field_spec_path, field_spec_checksum, field_spec_version, renderer_version, status, aktiv, published_at)
values
  ('pachtvertrag', 'v6', 'pdf-templates', 'templates/pachtvertrag/v6/template.pdf', 'application/pdf',
   '21e3a0529abeab8e832c165fc65f7b8e5b05f01d16ca630bba56a72ac00f8438',
   'templates/pachtvertrag/v6/field-spec.json', '42db399945ad8004b674e451ff5827c10ab2a5e774a91e6c2438452f695446c7',
   'pachtvertrag-field-spec/v6', 'top-left/v1', 'published', true, now());
