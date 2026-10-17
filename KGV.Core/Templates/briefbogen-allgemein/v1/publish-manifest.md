# PDF-Template Publish Manifest (lokaler Dry Run)

- Template key: `briefbogen-allgemein`
- Version: `v1`
- PDF path: `templates/briefbogen-allgemein/v1/template.pdf`
- Field-Spec path: `templates/briefbogen-allgemein/v1/field-spec.json`
- Field-Spec-Version: `briefbogen-allgemein-field-spec/v1`
- Renderer-Version: `top-left/v1`
- PDF SHA-256: `f0ad77f3edfbf27973a147382a354645f667b9cd811f9c0dc93b94eb492a58ad`
- Field-Spec SHA-256: `b829250ca6a8ad7fb229567a6e119c62e5fb781883e950ab6dce59096219b430`

## Vor dem Ausführen

Eine veröffentlichte Version niemals überschreiben. Die Zielpfade müssen in der gewählten Umgebung noch frei sein. Zuerst in DDB testen; erst nach erfolgreicher MA-/PV-Abnahme den identischen Ablauf für VDB wiederholen.

## Storage Upload (manuell nach `supabase link --project-ref <DDB-oder-VDB-ref>`)

```powershell
supabase storage cp "C:\Users\Andreas\Documents\Codex\2026-10-17\arbeite-ausschlie-lich-auf-main-pr\work\main\KGV.Core\Templates\briefbogen-allgemein\v1\template.pdf" ss:///pdf-templates/templates/briefbogen-allgemein/v1/template.pdf --content-type application/pdf --experimental --linked
supabase storage cp "C:\Users\Andreas\Documents\Codex\2026-10-17\arbeite-ausschlie-lich-auf-main-pr\work\main\KGV.Core\Templates\briefbogen-allgemein\v1\field-spec.json" ss:///pdf-templates/templates/briefbogen-allgemein/v1/field-spec.json --content-type application/json --experimental --linked
```

## Katalog-Aktivierung (manuell im SQL-Editor der gewählten Umgebung)

```sql
begin;

update public.pdf_template_catalog
set aktiv = false
where template_key = 'briefbogen-allgemein' and aktiv = true;

insert into public.pdf_template_catalog
  (template_key, version, storage_bucket, storage_path, content_type, checksum,
   field_spec_path, field_spec_checksum, field_spec_version, renderer_version,
   status, aktiv, published_at)
values
  ('briefbogen-allgemein', 'v1', 'pdf-templates', 'templates/briefbogen-allgemein/v1/template.pdf', 'application/pdf', 'f0ad77f3edfbf27973a147382a354645f667b9cd811f9c0dc93b94eb492a58ad',
   'templates/briefbogen-allgemein/v1/field-spec.json', 'b829250ca6a8ad7fb229567a6e119c62e5fb781883e950ab6dce59096219b430', 'briefbogen-allgemein-field-spec/v1', 'top-left/v1',
   'published', true, now());

commit;
```