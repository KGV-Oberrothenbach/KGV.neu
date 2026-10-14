using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Infrastructure.Supabase;
using Microsoft.Extensions.Logging;

namespace KGV.Infrastructure.Services;

public sealed class SupabasePdfTemplateProvider : IPdfTemplateProvider
{
    private static readonly HttpClient TemplateDownloadHttpClient = new() { Timeout = TimeSpan.FromSeconds(45) };
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

    public async Task<PdfTemplateAsset> GetActiveAsync(string templateKey, string expectedFieldSpecVersion, string expectedRendererVersion, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(templateKey)) throw new InvalidOperationException("Der PDF-Vorlagenschlüssel fehlt.");
        var client = await _clientFactory.CreateAsync();
        PdfTemplateCatalogRecord? record;
        try
        {
            var response = await client.From<PdfTemplateCatalogRecord>().Where(x => x.TemplateKey == templateKey.Trim()).Where(x => x.Aktiv == true).Get();
            record = response.Models?.Where(x => string.Equals(x.Status, "published", StringComparison.OrdinalIgnoreCase)).OrderByDescending(x => x.CreatedAt).FirstOrDefault();
        }
        catch (Exception ex)
        {
            LogFailure("catalog-load", templateKey, ex);
            throw new InvalidOperationException($"Der PDF-Template-Katalog für '{templateKey}' konnte nicht geladen werden.", ex);
        }

        if (record == null) throw new InvalidOperationException($"Für die PDF-Vorlage '{templateKey}' ist keine aktive veröffentlichte Version hinterlegt.");
        if (!string.Equals(record.FieldSpecVersion, expectedFieldSpecVersion, StringComparison.Ordinal) || !string.Equals(record.RendererVersion, expectedRendererVersion, StringComparison.Ordinal))
            throw new InvalidOperationException($"Die Vorlage '{templateKey}/{record.Version}' ist nicht mit der erwarteten Feldspec- oder Renderer-Version kompatibel.");
        if (!string.Equals(record.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Die veröffentlichte Vorlagendatei ist keine PDF.");
        var pdf = await DownloadPrivateObjectAsync(record.StorageBucket, record.StoragePath, templateKey, "pdf-download", cancellationToken);
        var specBytes = await DownloadPrivateObjectAsync(record.StorageBucket, record.FieldSpecPath, templateKey, "field-spec-download", cancellationToken);

        try
        {
            VerifyChecksum(pdf, record.Checksum, "PDF-Vorlage");
            VerifyChecksum(specBytes, record.FieldSpecChecksum, "Feldspezifikation");
        }
        catch (Exception ex)
        {
            LogFailure("checksum", templateKey, ex);
            throw;
        }

        IReadOnlyList<PdfTemplateFieldSpec> fields;
        try
        {
            fields = ParseFieldSpec(specBytes);
        }
        catch (Exception ex)
        {
            LogFailure("field-spec-parse", templateKey, ex);
            throw new InvalidOperationException($"Die Feldspezifikation für die PDF-Vorlage '{templateKey}' ist ungültig.", ex);
        }

        if (fields.Count == 0) throw new InvalidOperationException("Die Feldspezifikation der PDF-Vorlage enthält keine Felder.");
        return new PdfTemplateAsset { TemplateKey = record.TemplateKey, Version = record.Version, FieldSpecVersion = record.FieldSpecVersion, RendererVersion = record.RendererVersion, PdfBytes = pdf, Fields = fields };
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
            var exception = new InvalidOperationException("Die aktuelle Anmeldung ist abgelaufen. Bitte erneut anmelden.");
            LogFailure(stage, templateKey, exception);
            throw exception;
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

            using var response = await TemplateDownloadHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
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
            LogFailure(stage, templateKey, ex);
            throw new InvalidOperationException($"Die PDF-Vorlage '{templateKey}' konnte nicht geladen werden ({stage}).", ex);
        }
    }

    private void LogFailure(string stage, string templateKey, Exception exception)
        => _logger?.LogError(
            exception,
            "PDF template operation failed. stage={Stage} templateKey={TemplateKey} exceptionType={ExceptionType}",
            stage,
            templateKey,
            exception.GetType().FullName);

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
