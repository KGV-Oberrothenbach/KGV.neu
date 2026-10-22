using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Utilities;
using KGV.Infrastructure.Supabase;

namespace KGV.Infrastructure.Services;

public sealed class SupabasePreparedContractSignatureStorage : IPreparedContractSignatureStorage
{
    private readonly ISupabaseClientFactory _clientFactory;
    private readonly IAuthService _auth;
    private readonly Func<HttpClient> _httpClientFactory;

    public SupabasePreparedContractSignatureStorage(ISupabaseClientFactory clientFactory, IAuthService auth)
        : this(clientFactory, auth, () => new HttpClient()) { }
    public SupabasePreparedContractSignatureStorage(ISupabaseClientFactory clientFactory, IAuthService auth, Func<HttpClient> httpClientFactory)
    { _clientFactory = clientFactory; _auth = auth; _httpClientFactory = httpClientFactory; }

    public async Task<PreparedContractSignatureStorageUploadResult> UploadAsync(PreparedContractSignatureStorageUploadRequest upload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(upload);
        if (upload.KommunikationsvorgangId <= 0 || upload.SignaturRevision < 1 || string.IsNullOrWhiteSpace(upload.SignaturSchluessel) || upload.PngBytes.IsEmpty)
            throw new ArgumentException("Die Signatur-Uploadanfrage ist unvollständig.", nameof(upload));
        var token = await TokenAsync(); using var client = _httpClientFactory();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_clientFactory.Url.TrimEnd('/')}/functions/v1/upload-prepared-contract-signature");
        request.Headers.Add("apikey", _clientFactory.Key); request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(JsonSerializer.Serialize(new {
            kommunikationsvorgang_id = upload.KommunikationsvorgangId,
            signatur_revision = upload.SignaturRevision,
            signatur_schluessel = upload.SignaturSchluessel,
            png_base64 = Convert.ToBase64String(upload.PngBytes.Span)
        }), Encoding.UTF8, "application/json");
        using var response = await client.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode) throw new PreparedContractRepositoryException($"Die Signaturdatei konnte nicht sicher hochgeladen werden ({(int)response.StatusCode}).");
        var result = JsonSerializer.Deserialize<UploadResponse>(payload) ?? throw new PreparedContractRepositoryException("Der sichere Signatur-Upload lieferte keine Antwort.");
        if (!result.Success || string.IsNullOrWhiteSpace(result.StoragePath) || string.IsNullOrWhiteSpace(result.Sha256))
            throw new PreparedContractRepositoryException(result.Message ?? "Der sichere Signatur-Upload konnte nicht bestätigt werden.");
        return new PreparedContractSignatureStorageUploadResult(result.StoragePath, result.Sha256);
    }
    private async Task<string> TokenAsync() => await _auth.GetAccessTokenAsync() ?? throw new PreparedContractConflictException("Die Anmeldung ist abgelaufen.");
    private sealed record UploadResponse(
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("storage_path")] string? StoragePath,
        [property: JsonPropertyName("sha256")] string? Sha256,
        [property: JsonPropertyName("message")] string? Message);
}
