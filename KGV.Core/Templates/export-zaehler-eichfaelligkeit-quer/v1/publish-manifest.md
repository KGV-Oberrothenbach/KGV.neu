# PDF-Template Publish Manifest (lokaler Dry Run)

- Template key: `export-zaehler-eichfaelligkeit-quer`
- Version: `v1`
- PDF path: `templates/export-zaehler-eichfaelligkeit-quer/v1/template.pdf`
- Field-Spec path: `templates/export-zaehler-eichfaelligkeit-quer/v1/field-spec.json`
- Field-Spec-Version: `export-zaehler-eichfaelligkeit-quer-field-spec/v1`
- Renderer-Version: `top-left/v1`
- PDF SHA-256: `05c43ad11087a083d3164be803653dc8f0ddee39911befad029fdd4197727c0b`
- Field-Spec SHA-256: `52550ff3b5468f047b87f28b746e723d4627575eec0d8f7ee9fae373b7abf11f`

## Vor dem Ausführen

Eine veröffentlichte Version niemals überschreiben. Die Zielpfade müssen in der gewählten Umgebung noch frei sein. Zuerst in DDB testen; erst nach erfolgreicher MA-/PV-Abnahme den identischen Ablauf für VDB wiederholen.

## Storage Upload (manuell nach `supabase link --project-ref <DDB-oder-VDB-ref>`)

```powershell
supabase storage cp "C:\Users\Andreas\Documents\Codex\2026-10-17\arbeite-ausschlie-lich-auf-main-pr\work\main\KGV.Core\Templates\export-zaehler-eichfaelligkeit-quer\v1\template.pdf" ss:///pdf-templates/templates/export-zaehler-eichfaelligkeit-quer/v1/template.pdf --content-type application/pdf --experimental --linked
supabase storage cp "C:\Users\Andreas\Documents\Codex\2026-10-17\arbeite-ausschlie-lich-auf-main-pr\work\main\KGV.Core\Templates\export-zaehler-eichfaelligkeit-quer\v1\field-spec.json" ss:///pdf-templates/templates/export-zaehler-eichfaelligkeit-quer/v1/field-spec.json --content-type application/json --experimental --linked
```

## Katalog-Aktivierung (manuell im SQL-Editor der gewählten Umgebung)

```sql
begin;

update public.pdf_template_catalog
set aktiv = false
where template_key = 'export-zaehler-eichfaelligkeit-quer' and aktiv = true;

insert into public.pdf_template_catalog
  (template_key, version, storage_bucket, storage_path, content_type, checksum,
   field_spec_path, field_spec_checksum, field_spec_version, renderer_version,
   status, aktiv, published_at)
values
  ('export-zaehler-eichfaelligkeit-quer', 'v1', 'pdf-templates', 'templates/export-zaehler-eichfaelligkeit-quer/v1/template.pdf', 'application/pdf', '05c43ad11087a083d3164be803653dc8f0ddee39911befad029fdd4197727c0b',
   'templates/export-zaehler-eichfaelligkeit-quer/v1/field-spec.json', '52550ff3b5468f047b87f28b746e723d4627575eec0d8f7ee9fae373b7abf11f', 'export-zaehler-eichfaelligkeit-quer-field-spec/v1', 'top-left/v1',
   'published', true, now());

commit;
```