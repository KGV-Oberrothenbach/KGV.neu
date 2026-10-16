using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Infrastructure.Supabase;
using Microsoft.Extensions.Logging;

namespace KGV.Infrastructure.Services;

public sealed class SupabasePdfTemplateProvider : IPdfTemplateProvider
{
    private static readonly HttpClient TemplateHttpClient = new() { Timeout = TimeSpan.FromSeconds(45) };
    private readonly ISupabaseClientFactory _clientFactory;
    private readonly IAuthService _authService;
    private readonly ILogger<SupabasePdfTemplateProvider>? _logger;

    public SupabasePdfTemplateProvider(
        ISupabaseClientFactory clientFactory,
        IAuthService authService,
        ILogger<SupabasePdfTemplateProvider>? logger = null)
    {
        _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _logger = logger;
    }

    public async Task<PdfTemplateAsset> GetActiveAsync(PdfTemplateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var templateKey = request.TemplateKey;
        if (string.IsNullOrWhiteSpace(templateKey)) throw new InvalidOperationException("Der PDF-Vorlagenschlüssel fehlt.");
        var record = await LoadActiveCatalogRecordAsync(templateKey.Trim(), cancellationToken);

        if (record == null) throw Fail("catalog-load", templateKey, "Keine aktive veröffentlichte Version ist hinterlegt.");
        if (!string.Equals(record.FieldSpecVersion, request.ExpectedFieldSpecVersion, StringComparison.Ordinal) || !string.Equals(record.RendererVersion, request.ExpectedRendererVersion, StringComparison.Ordinal))
            throw Fail("compatibility-check", templateKey, $"Die Vorlage '{templateKey}/{record.Version}' ist nicht mit der erwarteten Feldspec- oder Renderer-Version kompatibel.");
        if (!string.Equals(record.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase)) throw Fail("compatibility-check", templateKey, "Die veröffentlichte Vorlagendatei ist keine PDF.");
        var pdf = await DownloadPrivateObjectAsync(record.StorageBucket, record.StoragePath, templateKey, "pdf-download", cancellationToken);
        var specBytes = await DownloadPrivateObjectAsync(record.StorageBucket, record.FieldSpecPath, templateKey, "field-spec-download", cancellationToken);

        try
        {
            VerifyChecksum(pdf, record.Checksum, "PDF-Vorlage");
            VerifyChecksum(specBytes, record.FieldSpecChecksum, "Feldspezifikation");
        }
        catch (Exception ex)
        {
            throw Fail("checksum", templateKey, "Die Prüfsumme stimmt nicht mit dem Template-Katalog überein.", ex);
        }

        IReadOnlyList<PdfTemplateFieldSpec> fields;
        try
        {
            fields = ParseFieldSpec(specBytes);
        }
        catch (Exception ex)
        {
            throw Fail("field-spec-parse", templateKey, "Die Feldspezifikation ist ungültig.", ex);
        }

        if (fields.Count == 0) throw Fail("field-spec-parse", templateKey, "Die Feldspezifikation der PDF-Vorlage enthält keine Felder.");
        return new PdfTemplateAsset { TemplateKey = record.TemplateKey, Version = record.Version, FieldSpecVersion = record.FieldSpecVersion, RendererVersion = record.RendererVersion, PdfBytes = pdf, Fields = fields };
    }

    private async Task<PdfTemplateCatalogRecord?> LoadActiveCatalogRecordAsync(string templateKey, CancellationToken cancellationToken)
    {
        var accessToken = await _authService.GetAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw Fail("catalog-load", templateKey, "Die aktuelle Anmeldung ist abgelaufen. Bitte erneut anmelden.");
        }

        const string select = "template_key,version,storage_bucket,storage_path,content_type,checksum,field_spec_path,field_spec_checksum,field_spec_version,renderer_version,status,aktiv,created_at";
        var query = string.Join("&", new[]
        {
            $"select={Uri.EscapeDataString(select)}",
            $"template_key=eq.{Uri.EscapeDataString(templateKey)}",
            "aktiv=eq.true",
            "status=eq.published",
            "order=created_at.desc",
            "limit=1"
        });
        var endpoint = new Uri(new Uri(_clientFactory.Url.TrimEnd('/') + "/"), $"rest/v1/pdf_template_catalog?{query}");

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Add("apikey", _clientFactory.Key);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await TemplateHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Der PDF-Template-Katalog wurde abgelehnt ({(int)response.StatusCode}): {responseBody}");

            var records = JsonSerializer.Deserialize<List<PdfTemplateCatalogDto>>(responseBody);
            return records?.FirstOrDefault()?.ToRecord();
        }
        catch (Exception ex)
        {
            throw Fail("catalog-load", templateKey, "Der PDF-Template-Katalog konnte nicht geladen werden.", ex);
        }
    }

    private async Task<byte[]> DownloadPrivateObjectAsync(
        string bucket,
        string storagePath,
        string templateKey,
        string stage,
        CancellationToken cancellationToken)
    {
        var accessToken = await _authService.GetAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw Fail(stage, templateKey, "Die aktuelle Anmeldung ist abgelaufen. Bitte erneut anmelden.");
        }

        var objectPath = string.Join("/", (storagePath ?? string.Empty)
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.EscapeDataString));
        var endpoint = new Uri(new Uri(_clientFactory.Url.TrimEnd('/') + "/"),
            $"storage/v1/object/{Uri.EscapeDataString(bucket)}/{objectPath}");

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Add("apikey", _clientFactory.Key);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));

            using var response = await TemplateHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException($"Der private Template-Download wurde abgelehnt ({(int)response.StatusCode}): {responseBody}");
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            if (bytes.Length == 0)
                throw new InvalidOperationException("Der private Template-Download lieferte keine Daten.");

            return bytes;
        }
        catch (Exception ex)
        {
            throw Fail(stage, templateKey, "Die private Vorlagendatei konnte nicht geladen werden.", ex);
        }
    }

    private void LogFailure(string stage, string templateKey, Exception exception)
        => _logger?.LogError(
            exception,
            "PDF template operation failed. stage={Stage} templateKey={TemplateKey} exceptionType={ExceptionType}",
            stage,
            templateKey,
            exception.GetType().FullName);

    private InvalidOperationException Fail(string stage, string templateKey, string message, Exception? innerException = null)
    {
        var exception = new InvalidOperationException($"PDF-Vorlage '{templateKey}' – Stage '{stage}': {message}", innerException);
        LogFailure(stage, templateKey, exception);
        return exception;
    }

    private static void VerifyChecksum(byte[] bytes, string checksum, string label)
    {
        var actual = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(checksum) || !string.Equals(actual, checksum.Trim().ToLowerInvariant(), StringComparison.Ordinal)) throw new InvalidOperationException($"Die Prüfsumme für {label} stimmt nicht mit dem Template-Katalog überein.");
    }

    private static IReadOnlyList<PdfTemplateFieldSpec> ParseFieldSpec(byte[] bytes)
    {
        using var doc = JsonDocument.Parse(bytes);
        if (!doc.RootElement.TryGetProperty("settings", out var settings)
            || !settings.TryGetProperty("coordinateOrigin", out var coordinateOrigin)
            || !string.Equals(coordinateOrigin.GetString(), "topLeft", StringComparison.Ordinal))
            throw new InvalidOperationException("Die Feldspezifikation verwendet keinen unterstützten Koordinatenursprung.");
        if (!doc.RootElement.TryGetProperty("fields", out var fields) || fields.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("Die Feldspezifikation enthält kein Feld-Array.");
        var result = new List<PdfTemplateFieldSpec>();
        foreach (var field in fields.EnumerateArray())
        {
            var rect = field.GetProperty("rect"); var type = field.GetProperty("type").GetString() ?? "text";
            var multiline = field.TryGetProperty("flags", out var flags) && flags.TryGetProperty("multiline", out var ml) && ml.ValueKind == JsonValueKind.True;
            result.Add(new PdfTemplateFieldSpec(field.GetProperty("name").GetString() ?? string.Empty, multiline ? "multiline" : type, field.TryGetProperty("page", out var page) ? page.GetInt32() : 1, rect.GetProperty("x").GetDouble(), rect.GetProperty("y").GetDouble(), rect.GetProperty("w").GetDouble(), rect.GetProperty("h").GetDouble()));
        }
        return result;
    }

    private sealed class PdfTemplateCatalogDto
    {
        [JsonPropertyName("template_key")] public string TemplateKey { get; init; } = string.Empty;
        [JsonPropertyName("version")] public string Version { get; init; } = string.Empty;
        [JsonPropertyName("storage_bucket")] public string StorageBucket { get; init; } = string.Empty;
        [JsonPropertyName("storage_path")] public string StoragePath { get; init; } = string.Empty;
        [JsonPropertyName("content_type")] public string ContentType { get; init; } = string.Empty;
        [JsonPropertyName("checksum")] public string Checksum { get; init; } = string.Empty;
        [JsonPropertyName("field_spec_path")] public string FieldSpecPath { get; init; } = string.Empty;
        [JsonPropertyName("field_spec_checksum")] public string FieldSpecChecksum { get; init; } = string.Empty;
        [JsonPropertyName("field_spec_version")] public string FieldSpecVersion { get; init; } = string.Empty;
        [JsonPropertyName("renderer_version")] public string RendererVersion { get; init; } = string.Empty;
        [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
        [JsonPropertyName("aktiv")] public bool Aktiv { get; init; }
        [JsonPropertyName("created_at")] public DateTime CreatedAt { get; init; }

        public PdfTemplateCatalogRecord ToRecord() => new()
        {
            TemplateKey = TemplateKey,
            Version = Version,
            StorageBucket = StorageBucket,
            StoragePath = StoragePath,
            ContentType = ContentType,
            Checksum = Checksum,
            FieldSpecPath = FieldSpecPath,
            FieldSpecChecksum = FieldSpecChecksum,
            FieldSpecVersion = FieldSpecVersion,
            RendererVersion = RendererVersion,
            Status = Status,
            Aktiv = Aktiv,
            CreatedAt = CreatedAt
        };
    }
}
