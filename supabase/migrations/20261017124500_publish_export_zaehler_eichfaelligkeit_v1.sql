-- Approved v1 export layouts for meter calibration due dates.
-- Upload all four immutable objects to the private `pdf-templates` bucket before
-- applying this migration. The guards intentionally prevent incomplete catalog entries.
do $$
begin
    if exists (
        select 1
        from public.pdf_template_catalog
        where template_key = 'export-zaehler-eichfaelligkeit-hoch' and version = 'v1'
    ) then
        raise exception 'PDF template export-zaehler-eichfaelligkeit-hoch/v1 already exists';
    end if;

    if exists (
        select 1
        from public.pdf_template_catalog
        where template_key = 'export-zaehler-eichfaelligkeit-quer' and version = 'v1'
    ) then
        raise exception 'PDF template export-zaehler-eichfaelligkeit-quer/v1 already exists';
    end if;

    if not exists (
        select 1 from storage.objects
        where bucket_id = 'pdf-templates'
          and name = 'templates/export-zaehler-eichfaelligkeit-hoch/v1/template.pdf'
    ) then
        raise exception 'Missing storage object templates/export-zaehler-eichfaelligkeit-hoch/v1/template.pdf';
    end if;

    if not exists (
        select 1 from storage.objects
        where bucket_id = 'pdf-templates'
          and name = 'templates/export-zaehler-eichfaelligkeit-hoch/v1/field-spec.json'
    ) then
        raise exception 'Missing storage object templates/export-zaehler-eichfaelligkeit-hoch/v1/field-spec.json';
    end if;

    if not exists (
        select 1 from storage.objects
        where bucket_id = 'pdf-templates'
          and name = 'templates/export-zaehler-eichfaelligkeit-quer/v1/template.pdf'
    ) then
        raise exception 'Missing storage object templates/export-zaehler-eichfaelligkeit-quer/v1/template.pdf';
    end if;

    if not exists (
        select 1 from storage.objects
        where bucket_id = 'pdf-templates'
          and name = 'templates/export-zaehler-eichfaelligkeit-quer/v1/field-spec.json'
    ) then
        raise exception 'Missing storage object templates/export-zaehler-eichfaelligkeit-quer/v1/field-spec.json';
    end if;
end;
$$;
update public.pdf_template_catalog
set aktiv = false
where template_key = 'export-zaehler-eichfaelligkeit-hoch' and aktiv = true;
update public.pdf_template_catalog
set aktiv = false
where template_key = 'export-zaehler-eichfaelligkeit-quer' and aktiv = true;
insert into public.pdf_template_catalog
  (template_key, version, storage_bucket, storage_path, content_type, checksum,
   field_spec_path, field_spec_checksum, field_spec_version, renderer_version,
   status, aktiv, published_at)
values
  ('export-zaehler-eichfaelligkeit-hoch', 'v1', 'pdf-templates',
   'templates/export-zaehler-eichfaelligkeit-hoch/v1/template.pdf', 'application/pdf',
   '5a4084156a6cf94e8879932cace7afc705408cbf82d2492c8a532eb1bb75ad7d',
   'templates/export-zaehler-eichfaelligkeit-hoch/v1/field-spec.json',
   '62a19aabf50dd50d0fc32296bc26d5382f49d06c22cc9abea79a876449ff0b75',
   'export-zaehler-eichfaelligkeit-hoch-field-spec/v1', 'top-left/v1', 'published', true, now()),
  ('export-zaehler-eichfaelligkeit-quer', 'v1', 'pdf-templates',
   'templates/export-zaehler-eichfaelligkeit-quer/v1/template.pdf', 'application/pdf',
   '05c43ad11087a083d3164be803653dc8f0ddee39911befad029fdd4197727c0b',
   'templates/export-zaehler-eichfaelligkeit-quer/v1/field-spec.json',
   '52550ff3b5468f047b87f28b746e723d4627575eec0d8f7ee9fae373b7abf11f',
   'export-zaehler-eichfaelligkeit-quer-field-spec/v1', 'top-left/v1', 'published', true, now());
