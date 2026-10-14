using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Infrastructure.Supabase;

namespace KGV.Infrastructure.Services;

public sealed class SupabasePdfTemplateProvider : IPdfTemplateProvider
{
    private readonly ISupabaseClientFactory _clientFactory;
    public SupabasePdfTemplateProvider(ISupabaseClientFactory clientFactory) => _clientFactory = clientFactory;

    public async Task<PdfTemplateAsset> GetActiveAsync(string templateKey, string expectedFieldSpecVersion, string expectedRendererVersion, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(templateKey)) throw new InvalidOperationException("Der PDF-Vorlagenschlüssel fehlt.");
        var client = await _clientFactory.CreateAsync();
        var response = await client.From<PdfTemplateCatalogRecord>().Where(x => x.TemplateKey == templateKey.Trim()).Where(x => x.Aktiv == true).Get();
        var record = response.Models?.Where(x => string.Equals(x.Status, "published", StringComparison.OrdinalIgnoreCase)).OrderByDescending(x => x.CreatedAt).FirstOrDefault();
        if (record == null) throw new InvalidOperationException($"Für die PDF-Vorlage '{templateKey}' ist keine aktive veröffentlichte Version hinterlegt.");
        if (!string.Equals(record.FieldSpecVersion, expectedFieldSpecVersion, StringComparison.Ordinal) || !string.Equals(record.RendererVersion, expectedRendererVersion, StringComparison.Ordinal))
            throw new InvalidOperationException($"Die Vorlage '{templateKey}/{record.Version}' ist nicht mit der erwarteten Feldspec- oder Renderer-Version kompatibel.");
        if (!string.Equals(record.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Die veröffentlichte Vorlagendatei ist keine PDF.");
        var pdf = await client.Storage.From(record.StorageBucket).Download(record.StoragePath, (EventHandler<float>?)null);
        var specBytes = await client.Storage.From(record.StorageBucket).Download(record.FieldSpecPath, (EventHandler<float>?)null);
        VerifyChecksum(pdf, record.Checksum, "PDF-Vorlage"); VerifyChecksum(specBytes, record.FieldSpecChecksum, "Feldspezifikation");
        var fields = ParseFieldSpec(specBytes);
        if (fields.Count == 0) throw new InvalidOperationException("Die Feldspezifikation der PDF-Vorlage enthält keine Felder.");
        return new PdfTemplateAsset { TemplateKey = record.TemplateKey, Version = record.Version, FieldSpecVersion = record.FieldSpecVersion, RendererVersion = record.RendererVersion, PdfBytes = pdf, Fields = fields };
    }

    private static void VerifyChecksum(byte[] bytes, string checksum, string label)
    {
        var actual = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(checksum) || !string.Equals(actual, checksum.Trim().ToLowerInvariant(), StringComparison.Ordinal)) throw new InvalidOperationException($"Die Prüfsumme für {label} stimmt nicht mit dem Template-Katalog überein.");
    }

    private static IReadOnlyList<PdfTemplateFieldSpec> ParseFieldSpec(byte[] bytes)
    {
        using var doc = JsonDocument.Parse(bytes); var result = new List<PdfTemplateFieldSpec>();
        foreach (var field in doc.RootElement.GetProperty("fields").EnumerateArray())
        {
            var rect = field.GetProperty("rect"); var type = field.GetProperty("type").GetString() ?? "text";
            var multiline = field.TryGetProperty("flags", out var flags) && flags.TryGetProperty("multiline", out var ml) && ml.ValueKind == JsonValueKind.True;
            result.Add(new PdfTemplateFieldSpec(field.GetProperty("name").GetString() ?? string.Empty, multiline ? "multiline" : type, field.TryGetProperty("page", out var page) ? page.GetInt32() : 1, rect.GetProperty("x").GetDouble(), rect.GetProperty("y").GetDouble(), rect.GetProperty("w").GetDouble(), rect.GetProperty("h").GetDouble()));
        }
        return result;
    }
}
