-- Upload immutable local artifacts before applying. This migration is intentionally not run locally.
do $$ begin
  if exists(select 1 from public.pdf_template_catalog where template_key='wartungsvertrag' and version='v1') then raise exception 'PDF template wartungsvertrag/v1 already exists'; end if;
  if not exists(select 1 from storage.objects where bucket_id='pdf-templates' and name='templates/wartungsvertrag/v1/template.pdf') then raise exception 'Missing storage object templates/wartungsvertrag/v1/template.pdf'; end if;
  if not exists(select 1 from storage.objects where bucket_id='pdf-templates' and name='templates/wartungsvertrag/v1/field-spec.json') then raise exception 'Missing storage object templates/wartungsvertrag/v1/field-spec.json'; end if;
end $$;
update public.pdf_template_catalog set aktiv=false where template_key='wartungsvertrag' and aktiv=true;
insert into public.pdf_template_catalog(template_key,version,storage_bucket,storage_path,content_type,checksum,field_spec_path,field_spec_checksum,field_spec_version,renderer_version,status,aktiv,published_at)
values('wartungsvertrag','v1','pdf-templates','templates/wartungsvertrag/v1/template.pdf','application/pdf','7fe8ee60173e9634f1f299f24e1e42f8c64e6bc5efd285e5b07d8f0ce027a050','templates/wartungsvertrag/v1/field-spec.json','ad601d1250cc18237821b2d764ad4a0a3625022972a4e9dc93c2f53a8cb55638','wartungsvertrag-field-spec/v1','top-left/v1','published',true,now());
