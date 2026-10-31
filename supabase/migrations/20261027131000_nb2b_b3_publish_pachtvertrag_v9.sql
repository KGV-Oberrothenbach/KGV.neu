-- NB-2B-B3: register the lease template with visible 20xxx field.
-- Commit only; do not execute or upload/activate without separate approval.
do $$
begin
  if exists (select 1 from public.pdf_template_catalog where template_key = 'pachtvertrag' and version = 'v9') then
    raise exception 'PDF template pachtvertrag/v9 already exists';
  end if;
  if not exists (select 1 from storage.objects where bucket_id = 'pdf-templates' and name = 'templates/pachtvertrag/v9/template.pdf') then
    raise exception 'Missing storage object templates/pachtvertrag/v9/template.pdf';
  end if;
  if not exists (select 1 from storage.objects where bucket_id = 'pdf-templates' and name = 'templates/pachtvertrag/v9/field-spec.json') then
    raise exception 'Missing storage object templates/pachtvertrag/v9/field-spec.json';
  end if;
end;
$$;
update public.pdf_template_catalog set aktiv = false where template_key = 'pachtvertrag' and aktiv = true;
insert into public.pdf_template_catalog
  (template_key, version, storage_bucket, storage_path, content_type, checksum, field_spec_path, field_spec_checksum, field_spec_version, renderer_version, status, aktiv, published_at)
values
  ('pachtvertrag', 'v9', 'pdf-templates', 'templates/pachtvertrag/v9/template.pdf', 'application/pdf',
   '0fa618ce6405e570c7a1de1588ad31df89029e7daf89ee2259d3106874277940',
   'templates/pachtvertrag/v9/field-spec.json', '1b9833e769812c9949b6fbc888507398aad2afa49ac6b481ec9f1805dfa52032',
   'pachtvertrag-field-spec/v9', 'top-left/v1', 'published', true, now());
