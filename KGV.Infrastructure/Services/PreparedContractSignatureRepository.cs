using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Infrastructure.Supabase;

namespace KGV.Infrastructure.Services;

public sealed class PreparedContractSignatureRepository : IPreparedContractSignatureRepository
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    private readonly ISupabaseClientFactory _clientFactory;
    private readonly IAuthService _auth;
    private readonly Func<HttpClient> _httpClientFactory;

    public PreparedContractSignatureRepository(ISupabaseClientFactory clientFactory, IAuthService auth)
        : this(clientFactory, auth, () => new HttpClient()) { }

    public PreparedContractSignatureRepository(ISupabaseClientFactory clientFactory, IAuthService auth, Func<HttpClient> httpClientFactory)
    { _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory)); _auth = auth ?? throw new ArgumentNullException(nameof(auth)); _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory)); }

    public async Task<IReadOnlyList<PreparedContractSignatureDto>> ListAsync(long kommunikationsvorgangId, int signaturRevision, CancellationToken cancellationToken = default)
    {
        ValidateIds(kommunikationsvorgangId, signaturRevision);
        return await SendRowsAsync(HttpMethod.Get, $"prepared_contract_signatur?kommunikationsvorgang_id=eq.{kommunikationsvorgangId}&signatur_revision=eq.{signaturRevision}&select=*&order=sort_order.asc,signatur_schluessel.asc", null, cancellationToken);
    }

    public async Task<PreparedContractSignatureDto> EnsurePositionAsync(long kommunikationsvorgangId, int signaturRevision, PreparedContractSignatureSlot slot, CancellationToken cancellationToken = default)
    {
        ValidateIds(kommunikationsvorgangId, signaturRevision); ArgumentNullException.ThrowIfNull(slot);
        var rows = await SendRowsAsync(HttpMethod.Post, "rpc/ensure_prepared_contract_signatur_position", new
        {
            p_kommunikationsvorgang_id = kommunikationsvorgangId,
            p_signatur_revision = signaturRevision,
            p_signatur_schluessel = slot.Key,
            p_sort_order = slot.SortOrder,
            p_pflicht = slot.Required,
            p_unterzeichner_typ = slot.SignerType,
            p_mitglied_id = slot.MemberId,
            p_unterzeichner_name_snapshot = (string?)null
        }, cancellationToken);
        return Single(rows, "Die Signaturposition konnte nicht sichergestellt werden.");
    }

    public async Task<PreparedContractSignatureDto> SaveAsync(PreparedContractSignatureSaveRequest request, string storagePath, string sha256, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request); ValidateIds(request.KommunikationsvorgangId, request.SignaturRevision);
        var rows = await SendRowsAsync(HttpMethod.Post, "rpc/speichere_prepared_contract_signatur", new
        {
            p_kommunikationsvorgang_id = request.KommunikationsvorgangId,
            p_signatur_revision = request.SignaturRevision,
            p_signatur_schluessel = request.SignaturSchluessel,
            p_mitglied_id = request.MitgliedId,
            p_unterzeichner_name_snapshot = request.UnterzeichnerNameSnapshot,
            p_vereinsfunktion_besetzung_id = request.VereinsfunktionBesetzungId,
            p_vertretung_id = request.VertretungId,
            p_vertretene_besetzung_id = request.VertreteneBesetzungId,
            p_storage_path = storagePath,
            p_signatur_sha256 = sha256,
            p_unterzeichnet_am = (request.UnterzeichnetAm ?? DateTimeOffset.UtcNow).ToUniversalTime().ToString("O")
        }, cancellationToken);
        return Single(rows, "Die Signatur konnte nicht persistent gespeichert werden.");
    }

    public async Task<IReadOnlyList<PreparedContractAuthorizedSignatoryDto>> ListAuthorizedClubSignatoriesAsync(long kommunikationsvorgangId, int signaturRevision, CancellationToken cancellationToken = default)
    {
        var token = await TokenAsync();
        using var client = _httpClientFactory();
        ValidateIds(kommunikationsvorgangId, signaturRevision);
        using var request = CreateRequest(HttpMethod.Post, "rpc/prepared_contract_snapshot_unterzeichner", token);
        request.Content = new StringContent(JsonSerializer.Serialize(new { p_kommunikationsvorgang_id = kommunikationsvorgangId, p_signatur_revision = signaturRevision }), Encoding.UTF8, "application/json");
        using var response = await client.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, "Die Vertragsunterzeichner konnten nicht geladen werden.");
        return JsonSerializer.Deserialize<List<PreparedContractAuthorizedSignatoryDto>>(content, Json) ?? [];
    }

    public async Task<bool> AreRequiredSignaturesCompleteAsync(long kommunikationsvorgangId, int signaturRevision, CancellationToken cancellationToken = default)
    {
        ValidateIds(kommunikationsvorgangId, signaturRevision);
        var token = await TokenAsync();
        using var client = _httpClientFactory();
        using var request = CreateRequest(HttpMethod.Post, "rpc/prepared_contract_signaturen_vollstaendig", token);
        request.Content = new StringContent(JsonSerializer.Serialize(new { p_kommunikationsvorgang_id = kommunikationsvorgangId, p_signatur_revision = signaturRevision }), Encoding.UTF8, "application/json");
        using var response = await client.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, "Die Vollständigkeit der Signaturen konnte nicht geprüft werden.");
        return JsonSerializer.Deserialize<bool>(content, Json);
    }

    private async Task<List<PreparedContractSignatureDto>> SendRowsAsync(HttpMethod method, string relative, object? body, CancellationToken cancellationToken)
    {
        var token = await TokenAsync();
        using var client = _httpClientFactory();
        using var request = CreateRequest(method, relative, token);
        request.Headers.Add("Prefer", "return=representation");
        if (body is not null) request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        using var response = await client.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, "Die Signaturdaten konnten nicht verarbeitet werden.");
        return JsonSerializer.Deserialize<List<PreparedContractSignatureDto>>(content, Json) ?? [];
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string relative, string token)
    {
        var request = new HttpRequestMessage(method, $"{_clientFactory.Url.TrimEnd('/')}/rest/v1/{relative}");
        request.Headers.Add("apikey", _clientFactory.Key); request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }
    private async Task<string> TokenAsync() => await _auth.GetAccessTokenAsync() ?? throw new PreparedContractConflictException("Die Anmeldung ist abgelaufen.");
    private static PreparedContractSignatureDto Single(IReadOnlyList<PreparedContractSignatureDto> rows, string message) => rows.Count == 1 ? rows[0] : throw new PreparedContractConflictException(message);
    private static void ValidateIds(long id, int revision) { if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id)); if (revision < 1) throw new ArgumentOutOfRangeException(nameof(revision)); }
    private static void EnsureSuccess(HttpResponseMessage response, string message)
    { if (response.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.PreconditionFailed) throw new PreparedContractConflictException(message); if (!response.IsSuccessStatusCode) throw new PreparedContractRepositoryException($"{message} ({(int)response.StatusCode})"); }
}
