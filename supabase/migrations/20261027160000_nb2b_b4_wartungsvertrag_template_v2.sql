-- Register immutable local v2 artifacts as drafts only.
-- Storage upload and template activation require a separate approved operation.
do $$
begin
  if exists (
    select 1
    from public.pdf_template_catalog
    where (template_key, version) in (
      ('wartungsvertrag', 'v2'),
      ('wartungsvertrag-verlaengerung', 'v2')
    )
      and (
        (template_key = 'wartungsvertrag'
          and (storage_bucket <> 'pdf-templates'
            or content_type <> 'application/pdf'
            or renderer_version <> 'top-left/v1'
            or storage_path <> 'templates/wartungsvertrag/v2/template.pdf'
            or checksum <> '7fe8ee60173e9634f1f299f24e1e42f8c64e6bc5efd285e5b07d8f0ce027a050'
            or field_spec_path <> 'templates/wartungsvertrag/v2/field-spec.json'
            or field_spec_checksum <> '13ce4cd971cd48a4f75bb5f08f325d335ffb6805789d531e320ae775abefb755'
            or field_spec_version <> 'wartungsvertrag-field-spec/v2'))
        or (template_key = 'wartungsvertrag-verlaengerung'
          and (storage_bucket <> 'pdf-templates'
            or content_type <> 'application/pdf'
            or renderer_version <> 'top-left/v1'
            or storage_path <> 'templates/wartungsvertrag-verlaengerung/v2/template.pdf'
            or checksum <> '7f1ea293d872fd5762b4a5c24210b5cc4cd0d1ad843463c1b15785d798e281b2'
            or field_spec_path <> 'templates/wartungsvertrag-verlaengerung/v2/field-spec.json'
            or field_spec_checksum <> 'b13024a353b10415482ad03887e8cf0e88b2be2cb660d42ac149fe4c32e1b865'
            or field_spec_version <> 'wartungsvertrag-verlaengerung-field-spec/v2'))
      )
  ) then
    raise exception 'A Wartungsvertrag v2 catalog entry exists with different immutable artifacts';
  end if;

  insert into public.pdf_template_catalog(
    template_key, version, storage_bucket, storage_path, content_type, checksum,
    field_spec_path, field_spec_checksum, field_spec_version, renderer_version,
    status, aktiv, published_at
  )
  select
    'wartungsvertrag', 'v2', 'pdf-templates',
    'templates/wartungsvertrag/v2/template.pdf', 'application/pdf',
    '7fe8ee60173e9634f1f299f24e1e42f8c64e6bc5efd285e5b07d8f0ce027a050',
    'templates/wartungsvertrag/v2/field-spec.json',
    '13ce4cd971cd48a4f75bb5f08f325d335ffb6805789d531e320ae775abefb755',
    'wartungsvertrag-field-spec/v2', 'top-left/v1', 'draft', false, null
  where not exists (
    select 1 from public.pdf_template_catalog
    where template_key = 'wartungsvertrag' and version = 'v2'
  );

  insert into public.pdf_template_catalog(
    template_key, version, storage_bucket, storage_path, content_type, checksum,
    field_spec_path, field_spec_checksum, field_spec_version, renderer_version,
    status, aktiv, published_at
  )
  select
    'wartungsvertrag-verlaengerung', 'v2', 'pdf-templates',
    'templates/wartungsvertrag-verlaengerung/v2/template.pdf', 'application/pdf',
    '7f1ea293d872fd5762b4a5c24210b5cc4cd0d1ad843463c1b15785d798e281b2',
    'templates/wartungsvertrag-verlaengerung/v2/field-spec.json',
    'b13024a353b10415482ad03887e8cf0e88b2be2cb660d42ac149fe4c32e1b865',
    'wartungsvertrag-verlaengerung-field-spec/v2', 'top-left/v1', 'draft', false, null
  where not exists (
    select 1 from public.pdf_template_catalog
    where template_key = 'wartungsvertrag-verlaengerung' and version = 'v2'
  );
end;
$$;
