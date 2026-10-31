-- Upload both immutable objects before applying this migration. This migration is not run locally.
do $$
begin
    if exists (select 1 from public.pdf_template_catalog where template_key = 'pachtvertrag' and version = 'v4') then
        raise exception 'PDF template pachtvertrag/v4 already exists';
    end if;
    if not exists (select 1 from storage.objects where bucket_id = 'pdf-templates' and name = 'templates/pachtvertrag/v4/template.pdf') then
        raise exception 'Missing storage object templates/pachtvertrag/v4/template.pdf';
    end if;
    if not exists (select 1 from storage.objects where bucket_id = 'pdf-templates' and name = 'templates/pachtvertrag/v4/field-spec.json') then
        raise exception 'Missing storage object templates/pachtvertrag/v4/field-spec.json';
    end if;
end;
$$;
update public.pdf_template_catalog set aktiv = false where template_key = 'pachtvertrag' and aktiv = true;
insert into public.pdf_template_catalog
  (template_key, version, storage_bucket, storage_path, content_type, checksum, field_spec_path, field_spec_checksum, field_spec_version, renderer_version, status, aktiv, published_at)
values
  ('pachtvertrag', 'v4', 'pdf-templates', 'templates/pachtvertrag/v4/template.pdf', 'application/pdf',
   '8db3d9a87c70af931d8571c86c4bee1991d9dcd41e95dd5247c0adf72d791917',
   'templates/pachtvertrag/v4/field-spec.json', '5925b54ab2831503940921dd4104701d9c2e2985e6e0eff8d1b169f63b17a0a8',
   'pachtvertrag-field-spec/v4', 'top-left/v1', 'published', true, now());
