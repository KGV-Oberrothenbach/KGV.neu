-- The checked-in v2 field spec is the published contract.  Earlier source
-- migration text contained a stale checksum; repair already-applied catalogs
-- without changing any other template version or status.
update public.pdf_template_catalog
set field_spec_checksum = '8d526a6e7ebf7180dad5c6e33ad98141ce48f985b0d846c4f8319b504c1675cd'
where template_key = 'export-allgemein'
  and version = 'v2'
  and field_spec_checksum = '3459429f728ae7ba28919f10482b9443b6c3a9f051b99418c1519ca2c296df1c';
