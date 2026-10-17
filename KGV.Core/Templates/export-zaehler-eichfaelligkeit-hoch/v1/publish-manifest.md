# PDF-Template Publish Manifest (lokaler Dry Run)

- Template key: `export-zaehler-eichfaelligkeit-hoch`
- Version: `v1`
- PDF path: `templates/export-zaehler-eichfaelligkeit-hoch/v1/template.pdf`
- Field-Spec path: `templates/export-zaehler-eichfaelligkeit-hoch/v1/field-spec.json`
- Field-Spec-Version: `export-zaehler-eichfaelligkeit-hoch-field-spec/v1`
- Renderer-Version: `top-left/v1`
- PDF SHA-256: `5a4084156a6cf94e8879932cace7afc705408cbf82d2492c8a532eb1bb75ad7d`
- Field-Spec SHA-256: `62a19aabf50dd50d0fc32296bc26d5382f49d06c22cc9abea79a876449ff0b75`

## Vor dem Ausführen

Eine veröffentlichte Version niemals überschreiben. Die Zielpfade müssen in der gewählten Umgebung noch frei sein. Zuerst in DDB testen; erst nach erfolgreicher MA-/PV-Abnahme den identischen Ablauf für VDB wiederholen.

## Storage Upload (manuell nach `supabase link --project-ref <DDB-oder-VDB-ref>`)

```powershell
supabase storage cp "C:\Users\Andreas\Documents\Codex\2026-10-17\arbeite-ausschlie-lich-auf-main-pr\work\main\KGV.Core\Templates\export-zaehler-eichfaelligkeit-hoch\v1\template.pdf" ss:///pdf-templates/templates/export-zaehler-eichfaelligkeit-hoch/v1/template.pdf --content-type application/pdf --experimental --linked
supabase storage cp "C:\Users\Andreas\Documents\Codex\2026-10-17\arbeite-ausschlie-lich-auf-main-pr\work\main\KGV.Core\Templates\export-zaehler-eichfaelligkeit-hoch\v1\field-spec.json" ss:///pdf-templates/templates/export-zaehler-eichfaelligkeit-hoch/v1/field-spec.json --content-type application/json --experimental --linked
```

## Katalog-Aktivierung (manuell im SQL-Editor der gewählten Umgebung)

```sql
begin;

update public.pdf_template_catalog
set aktiv = false
where template_key = 'export-zaehler-eichfaelligkeit-hoch' and aktiv = true;

insert into public.pdf_template_catalog
  (template_key, version, storage_bucket, storage_path, content_type, checksum,
   field_spec_path, field_spec_checksum, field_spec_version, renderer_version,
   status, aktiv, published_at)
values
  ('export-zaehler-eichfaelligkeit-hoch', 'v1', 'pdf-templates', 'templates/export-zaehler-eichfaelligkeit-hoch/v1/template.pdf', 'application/pdf', '5a4084156a6cf94e8879932cace7afc705408cbf82d2492c8a532eb1bb75ad7d',
   'templates/export-zaehler-eichfaelligkeit-hoch/v1/field-spec.json', '62a19aabf50dd50d0fc32296bc26d5382f49d06c22cc9abea79a876449ff0b75', 'export-zaehler-eichfaelligkeit-hoch-field-spec/v1', 'top-left/v1',
   'published', true, now());

commit;
```