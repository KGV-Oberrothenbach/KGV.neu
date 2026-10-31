-- Reserved publication migration for the general association letterhead v1.
-- Do not apply before formal Vorstand approval.
-- Upload both immutable objects to the private `pdf-templates` bucket before
-- applying this migration. The guards intentionally prevent a catalog record
-- without its matching, checksummed storage objects.
do $$
begin
    if exists (
        select 1
        from public.pdf_template_catalog
        where template_key = 'briefbogen-allgemein' and version = 'v1'
    ) then
        raise exception 'PDF template briefbogen-allgemein/v1 already exists';
    end if;

    if not exists (
        select 1
        from storage.objects
        where bucket_id = 'pdf-templates'
          and name = 'templates/briefbogen-allgemein/v1/template.pdf'
    ) then
        raise exception 'Missing storage object templates/briefbogen-allgemein/v1/template.pdf';
    end if;

    if not exists (
        select 1
        from storage.objects
        where bucket_id = 'pdf-templates'
          and name = 'templates/briefbogen-allgemein/v1/field-spec.json'
    ) then
        raise exception 'Missing storage object templates/briefbogen-allgemein/v1/field-spec.json';
    end if;
end;
$$;
update public.pdf_template_catalog
set aktiv = false
where template_key = 'briefbogen-allgemein' and aktiv = true;
insert into public.pdf_template_catalog
  (template_key, version, storage_bucket, storage_path, content_type, checksum,
   field_spec_path, field_spec_checksum, field_spec_version, renderer_version,
   status, aktiv, published_at)
values
  ('briefbogen-allgemein', 'v1', 'pdf-templates', 'templates/briefbogen-allgemein/v1/template.pdf', 'application/pdf',
   'f0ad77f3edfbf27973a147382a354645f667b9cd811f9c0dc93b94eb492a58ad',
   'templates/briefbogen-allgemein/v1/field-spec.json',
   'b829250ca6a8ad7fb229567a6e119c62e5fb781883e950ab6dce59096219b430',
   'briefbogen-allgemein-field-spec/v1', 'top-left/v1', 'published', true, now());
