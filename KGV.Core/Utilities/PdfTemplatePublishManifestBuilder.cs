using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using KGV.Core.Models;
using PdfSharpCore.Pdf.IO;

namespace KGV.Core.Utilities;

/// <summary>Builds a local, non-executing publication manifest for one new PDF template version.</summary>
public static class PdfTemplatePublishManifestBuilder
{
    private const string StorageBucket = "pdf-templates";
    private const string FieldSpecSchemaVersion = "kgv-pdf-field-spec/v1";
    private static readonly Regex TemplateKeyPattern = new("^[a-z0-9_-]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static PdfTemplatePublishManifest Build(PdfTemplatePublishRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequest(request);

        var catalogVersion = "v" + request.Version;
        var pdfBytes = ReadRequiredFile(request.TemplatePdfPath, "PDF-Vorlage");
        var fieldSpecBytes = ReadRequiredFile(request.FieldSpecPath, "Feldspezifikation");
        var fields = ValidateFieldSpec(fieldSpecBytes);
        ValidatePdf(pdfBytes, fields);

        return new PdfTemplatePublishManifest(
            request.TemplateKey,
            catalogVersion,
            PdfTemplateCatalogContract.BuildPdfPath(request.TemplateKey, catalogVersion),
            PdfTemplateCatalogContract.BuildFieldSpecPath(request.TemplateKey, catalogVersion),
            request.FieldSpecVersion,
            request.RendererVersion,
            Hash(pdfBytes),
            Hash(fieldSpecBytes),
            BuildStorageCommand(request.TemplatePdfPath, PdfTemplateCatalogContract.BuildPdfPath(request.TemplateKey, catalogVersion), "application/pdf"),
            BuildStorageCommand(request.FieldSpecPath, PdfTemplateCatalogContract.BuildFieldSpecPath(request.TemplateKey, catalogVersion), "application/json"),
            BuildActivationSql(request.TemplateKey, catalogVersion, request.FieldSpecVersion, request.RendererVersion,
                PdfTemplateCatalogContract.BuildPdfPath(request.TemplateKey, catalogVersion),
                PdfTemplateCatalogContract.BuildFieldSpecPath(request.TemplateKey, catalogVersion), Hash(pdfBytes), Hash(fieldSpecBytes)));
    }

    private static void ValidateRequest(PdfTemplatePublishRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TemplateKey) || !TemplateKeyPattern.IsMatch(request.TemplateKey))
            throw new InvalidOperationException("template_key muss nur Kleinbuchstaben, Ziffern, Bindestriche oder Unterstriche enthalten.");
        if (request.Version <= 0) throw new InvalidOperationException("Die Versionsnummer muss eine positive ganze Zahl sein.");
        Require(request.FieldSpecVersion, "field_spec_version");
        Require(request.RendererVersion, "renderer_version");
        Require(request.ExpectedFieldSpecVersion, "expected field_spec_version");
        Require(request.ExpectedRendererVersion, "expected renderer_version");
        if (!string.Equals(request.FieldSpecVersion, request.ExpectedFieldSpecVersion, StringComparison.Ordinal))
            throw new InvalidOperationException("field_spec_version stimmt nicht mit dem erwarteten Dokumentvertrag überein.");
        if (!string.Equals(request.RendererVersion, request.ExpectedRendererVersion, StringComparison.Ordinal))
            throw new InvalidOperationException("renderer_version stimmt nicht mit dem erwarteten Dokumentvertrag überein.");
    }

    private static byte[] ReadRequiredFile(string path, string label)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            throw new InvalidOperationException($"{label} wurde lokal nicht gefunden.");
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length == 0) throw new InvalidOperationException($"{label} ist leer.");
        return bytes;
    }

    private static IReadOnlyList<PdfTemplateFieldSpec> ValidateFieldSpec(byte[] bytes)
    {
        try
        {
            using var document = JsonDocument.Parse(bytes);
            if (document.RootElement.TryGetProperty("schema_version", out var schemaVersion)
                && !string.Equals(schemaVersion.GetString(), FieldSpecSchemaVersion, StringComparison.Ordinal))
                throw new InvalidOperationException($"Die Feldspezifikation muss schema_version '{FieldSpecSchemaVersion}' verwenden.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Die Feldspezifikation ist kein gültiges JSON.", ex);
        }

        var fields = PdfTemplateFieldSpecParser.Parse(bytes);
        if (fields.Count == 0) throw new InvalidOperationException("Die Feldspezifikation enthält keine Felder.");
        return fields;
    }

    private static void ValidatePdf(byte[] bytes, IReadOnlyList<PdfTemplateFieldSpec> fields)
    {
        try
        {
            using var stream = new MemoryStream(bytes, writable: false);
            using var document = PdfReader.Open(stream, PdfDocumentOpenMode.InformationOnly);
            if (document.PageCount == 0) throw new InvalidOperationException("Die PDF-Vorlage enthält keine Seiten.");
            if (fields.Any(field => field.Page > document.PageCount))
                throw new InvalidOperationException("Mindestens ein Field-Spec-Feld verweist auf eine nicht vorhandene PDF-Seite.");
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Die PDF-Vorlage ist ungültig.", ex);
        }
    }

    private static string BuildStorageCommand(string localPath, string storagePath, string contentType)
        => $"supabase storage cp {QuoteShell(Path.GetFullPath(localPath))} ss:///{StorageBucket}/{storagePath} --content-type {contentType} --experimental --linked";

    private static string BuildActivationSql(
        string templateKey,
        string version,
        string fieldSpecVersion,
        string rendererVersion,
        string pdfPath,
        string fieldSpecPath,
        string pdfHash,
        string fieldSpecHash)
        => $$"""
begin;

update public.pdf_template_catalog
set aktiv = false
where template_key = {{QuoteSql(templateKey)}} and aktiv = true;

insert into public.pdf_template_catalog
  (template_key, version, storage_bucket, storage_path, content_type, checksum,
   field_spec_path, field_spec_checksum, field_spec_version, renderer_version,
   status, aktiv, published_at)
values
  ({{QuoteSql(templateKey)}}, {{QuoteSql(version)}}, '{{StorageBucket}}', {{QuoteSql(pdfPath)}}, 'application/pdf', {{QuoteSql(pdfHash)}},
   {{QuoteSql(fieldSpecPath)}}, {{QuoteSql(fieldSpecHash)}}, {{QuoteSql(fieldSpecVersion)}}, {{QuoteSql(rendererVersion)}},
   'published', true, now());

commit;
""";

    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static string QuoteSql(string value) => "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";

    private static string QuoteShell(string value) => '"' + value.Replace("\"", "\\\"", StringComparison.Ordinal) + '"';

    private static void Require(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException($"{name} fehlt.");
    }
}

public sealed record PdfTemplatePublishRequest(
    string TemplateKey,
    int Version,
    string TemplatePdfPath,
    string FieldSpecPath,
    string FieldSpecVersion,
    string RendererVersion,
    string ExpectedFieldSpecVersion,
    string ExpectedRendererVersion);

public sealed record PdfTemplatePublishManifest(
    string TemplateKey,
    string Version,
    string PdfStoragePath,
    string FieldSpecStoragePath,
    string FieldSpecVersion,
    string RendererVersion,
    string PdfSha256,
    string FieldSpecSha256,
    string PdfStorageCommand,
    string FieldSpecStorageCommand,
    string ActivationSql)
{
    public string ToMarkdown() => $$"""
# PDF-Template Publish Manifest (lokaler Dry Run)

- Template key: `{{TemplateKey}}`
- Version: `{{Version}}`
- PDF path: `{{PdfStoragePath}}`
- Field-Spec path: `{{FieldSpecStoragePath}}`
- Field-Spec-Version: `{{FieldSpecVersion}}`
- Renderer-Version: `{{RendererVersion}}`
- PDF SHA-256: `{{PdfSha256}}`
- Field-Spec SHA-256: `{{FieldSpecSha256}}`

## Vor dem Ausführen

Eine veröffentlichte Version niemals überschreiben. Die Zielpfade müssen in der gewählten Umgebung noch frei sein. Zuerst in DDB testen; erst nach erfolgreicher MA-/PV-Abnahme den identischen Ablauf für VDB wiederholen.

## Storage Upload (manuell nach `supabase link --project-ref <DDB-oder-VDB-ref>`)

```powershell
{{PdfStorageCommand}}
{{FieldSpecStorageCommand}}
```

## Katalog-Aktivierung (manuell im SQL-Editor der gewählten Umgebung)

```sql
{{ActivationSql}}
```
""";
}
