-- NB-2A-D4: register the letterhead-style lease-contract footer; do not execute without separate approval.
do $$
begin
    if exists (select 1 from public.pdf_template_catalog where template_key = 'pachtvertrag' and version = 'v8') then
        raise exception 'PDF template pachtvertrag/v8 already exists';
    end if;
    if not exists (select 1 from storage.objects where bucket_id = 'pdf-templates' and name = 'templates/pachtvertrag/v8/template.pdf') then
        raise exception 'Missing storage object templates/pachtvertrag/v8/template.pdf';
    end if;
    if not exists (select 1 from storage.objects where bucket_id = 'pdf-templates' and name = 'templates/pachtvertrag/v8/field-spec.json') then
        raise exception 'Missing storage object templates/pachtvertrag/v8/field-spec.json';
    end if;
end;
$$;
update public.pdf_template_catalog set aktiv = false where template_key = 'pachtvertrag' and aktiv = true;
insert into public.pdf_template_catalog
  (template_key, version, storage_bucket, storage_path, content_type, checksum, field_spec_path, field_spec_checksum, field_spec_version, renderer_version, status, aktiv, published_at)
values
  ('pachtvertrag', 'v8', 'pdf-templates', 'templates/pachtvertrag/v8/template.pdf', 'application/pdf',
   '0fa618ce6405e570c7a1de1588ad31df89029e7daf89ee2259d3106874277940',
   'templates/pachtvertrag/v8/field-spec.json', '32951dc11bb57c2ca3fc9278ba01818cccdbd25a15deac7b2c7b823c93bbe666',
   'pachtvertrag-field-spec/v8', 'top-left/v1', 'published', true, now());
