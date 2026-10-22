using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Services;
using KGV.Infrastructure.Supabase;

namespace KGV.Infrastructure.Services;

/// <summary>PostgREST access for the server-owned prepared-contract workflow. It never stores signatures or documents.</summary>
public sealed class PreparedContractRepository : IPreparedContractRepository
{
    private readonly ISupabaseClientFactory _clientFactory;
    private readonly IAuthService _auth;
    private readonly Func<HttpClient> _httpClientFactory;
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public PreparedContractRepository(ISupabaseClientFactory clientFactory, IAuthService auth)
        : this(clientFactory, auth, () => new HttpClient()) { }

    public PreparedContractRepository(ISupabaseClientFactory clientFactory, IAuthService auth, Func<HttpClient> httpClientFactory)
    { _clientFactory = clientFactory; _auth = auth; _httpClientFactory = httpClientFactory; }

    public async Task<KommunikationsvorgangDto> LoadAsync(long id, CancellationToken cancellationToken = default)
    {
        var item = (await SendAsync(HttpMethod.Get, $"kommunikationsvorgang?id=eq.{id}&select=*", null, cancellationToken)).FirstOrDefault()
            ?? throw new PreparedContractNotFoundException("Der Vertragsvorgang wurde nicht gefunden.");
        if (item.Vorgangstyp != PreparedContractConstants.ContractSignatureProcess)
            throw new PreparedContractValidationException("Der Vorgang ist kein Vertragsvorgang.");
        ValidateDraft(item.Dokumenttyp, item.MitgliedId, item.ParzelleId, item.EntwurfDaten);
        return item;
    }

    public async Task<IReadOnlyList<KommunikationsvorgangDto>> ListActiveAsync(CancellationToken cancellationToken = default)
    {
        var items = await SendAsync(HttpMethod.Get,
            $"kommunikationsvorgang?vorgangstyp=eq.{PreparedContractConstants.ContractSignatureProcess}&dokumenttyp=in.({PreparedContractConstants.MembershipApplication},{PreparedContractConstants.LeaseContract},{PreparedContractConstants.MaintenanceContract})&status=in.({PreparedContractConstants.Prepared},{PreparedContractConstants.ReadyForSignature},{PreparedContractConstants.FinalReview})&select=*",
            null,
            cancellationToken);
        foreach (var item in items)
            ValidateDraft(item.Dokumenttyp, item.MitgliedId, item.ParzelleId, item.EntwurfDaten);
        return items;
    }

    public async Task<KommunikationsvorgangDto?> FindActiveAsync(string documentType, int memberId, int? parcelId, CancellationToken cancellationToken = default)
    {
        var parcelFilter = parcelId is null ? "parzelle_id=is.null" : $"parzelle_id=eq.{parcelId}";
        var rows = await SendAsync(HttpMethod.Get, $"kommunikationsvorgang?vorgangstyp=eq.{PreparedContractConstants.ContractSignatureProcess}&dokumenttyp=eq.{documentType}&mitglied_id=eq.{memberId}&{parcelFilter}&status=in.(vorbereitet,zur_signatur,letzte_pruefung)&select=*", null, cancellationToken);
        if (rows.Count == 0) return null;
        if (rows.Count != 1) throw new PreparedContractConflictException("Es wurden mehrere aktive Vertragsvorgänge gefunden.");
        var item = rows[0];
        if (item.Vorgangstyp != PreparedContractConstants.ContractSignatureProcess || item.Dokumenttyp != documentType || item.MitgliedId != memberId || item.ParzelleId != parcelId)
            throw new PreparedContractValidationException("Der aktive Vertragsvorgang passt nicht zur angeforderten Vertragsbindung.");
        ValidateDraft(documentType, memberId, parcelId, item.EntwurfDaten); return item;
    }

    public async Task<KommunikationsvorgangDto> CreateAsync(string documentType, int memberId, int? parcelId, JsonElement draft, CancellationToken cancellationToken = default)
    {
        ValidateDraft(documentType, memberId, parcelId, draft);
        var userId = CurrentUserId();
        var body = JsonSerializer.SerializeToElement(new { vorgangstyp = PreparedContractConstants.ContractSignatureProcess, dokumenttyp = documentType, status = PreparedContractConstants.Prepared, mitglied_id = memberId, parzelle_id = parcelId, entwurf_daten = draft, zugewiesen_an = userId });
        return Single(await SendAsync(HttpMethod.Post, "kommunikationsvorgang", body, cancellationToken), "Der Vertragsvorgang konnte nicht angelegt werden.");
    }

    public async Task<KommunikationsvorgangDto> ClaimAsync(long id, CancellationToken cancellationToken = default)
        => Single(await PatchAsync($"kommunikationsvorgang?id=eq.{id}&zugewiesen_an=is.null", new { zugewiesen_an = CurrentUserId() }, cancellationToken), "Der Vorgang wurde bereits zugewiesen.");

    public async Task ReleaseAsync(long id, CancellationToken cancellationToken = default)
    {
        var records = await PatchAsync($"kommunikationsvorgang?id=eq.{id}&zugewiesen_an=eq.{CurrentUserId()}", new { zugewiesen_an = (Guid?)null }, cancellationToken);
        if (records.Count == 0) throw new PreparedContractConflictException("Der Vorgang ist nicht Ihnen zugewiesen.");
    }

    public async Task<KommunikationsvorgangDto> UpdateDraftAsync(long id, int expectedRevision, JsonElement draft, CancellationToken cancellationToken = default)
    {
        var current = await LoadRequiredAsync(id, cancellationToken);
        EnsureEditable(current, expectedRevision);
        ValidateDraft(current.Dokumenttyp, current.MitgliedId, current.ParzelleId, draft);
        if (PreparedContractDraftParser.AreEquivalent(current.EntwurfDaten, draft)) return current;
        var updated = Single(await PatchAsync($"kommunikationsvorgang?id=eq.{id}&revision=eq.{expectedRevision}&status=eq.{PreparedContractConstants.Prepared}&signatur_gestartet_am=is.null&signatur_revision=is.null&zugewiesen_an=eq.{CurrentUserId()}", new { entwurf_daten = draft, revision = expectedRevision + 1 }, cancellationToken), "Der Vertragsentwurf wurde zwischenzeitlich geändert.");
        if (updated.Revision != expectedRevision + 1) throw new PreparedContractConflictException("Der Server hat die Entwurfsrevision nicht erhöht.");
        return updated;
    }

    public async Task<KommunikationsvorgangDto> StartSignatureAsync(long id, int expectedRevision, CancellationToken cancellationToken = default)
    {
        var updated = Single(await PatchAsync($"kommunikationsvorgang?id=eq.{id}&revision=eq.{expectedRevision}&status=eq.{PreparedContractConstants.Prepared}&signatur_gestartet_am=is.null&signatur_revision=is.null&zugewiesen_an=eq.{CurrentUserId()}", new { status = PreparedContractConstants.ReadyForSignature, signatur_gestartet_am = DateTimeOffset.UtcNow }, cancellationToken), "Die Signatur kann nicht gestartet werden.");
        var loaded = await LoadRequiredAsync(updated.Id, cancellationToken);
        if (loaded.Status != PreparedContractConstants.ReadyForSignature || loaded.SignaturRevision != loaded.Revision || loaded.SignaturGestartetAm is null) throw new PreparedContractConflictException("Der Server hat den Signaturstart nicht bestätigt.");
        return loaded;
    }

    public async Task<FinalDocumentProvenance?> FindFinalDocumentByProvenanceAsync(long sourceKommunikationsvorgangId, int sourceRevision, CancellationToken cancellationToken = default)
    {
        if (sourceKommunikationsvorgangId <= 0 || sourceRevision < 1) throw new ArgumentOutOfRangeException(nameof(sourceKommunikationsvorgangId));
        var token = await _auth.GetAccessTokenAsync() ?? throw new PreparedContractConflictException("Die Anmeldung ist abgelaufen.");
        using var client = _httpClientFactory();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{_clientFactory.Url.TrimEnd('/')}/rest/v1/dokument?select=id,source_kommunikationsvorgang_id,source_revision&source_kommunikationsvorgang_id=eq.{sourceKommunikationsvorgangId}&source_revision=eq.{sourceRevision}&archiviert_at=is.null");
        request.Headers.Add("apikey", _clientFactory.Key); request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode) throw new PreparedContractRepositoryException($"Finales Dokument konnte nicht geladen werden ({(int)response.StatusCode}).");
        var records = JsonSerializer.Deserialize<List<FinalDocumentProvenanceRecord>>(content, Json) ?? [];
        if (records.Count > 1) throw new PreparedContractConflictException("Für den Kommunikationsvorgang existieren mehrere finale Dokumente.");
        var record = records.SingleOrDefault();
        return record is null ? null : new FinalDocumentProvenance(record.Id, record.SourceKommunikationsvorgangId, record.SourceRevision);
    }

    public async Task<KommunikationsvorgangDto> MarkFinalDocumentReadyForReviewAsync(long id, int expectedRevision, long finalDocumentId, CancellationToken cancellationToken = default)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        if (expectedRevision < 1) throw new ArgumentOutOfRangeException(nameof(expectedRevision));
        if (finalDocumentId <= 0) throw new ArgumentOutOfRangeException(nameof(finalDocumentId));
        var ready = Single(await PatchAsync($"kommunikationsvorgang?id=eq.{id}&status=eq.{PreparedContractConstants.ReadyForSignature}&revision=eq.{expectedRevision}&signatur_revision=eq.{expectedRevision}&signatur_gestartet_am=not.is.null&final_dokument_id=is.null&zugewiesen_an=eq.{CurrentUserId()}", new { status = PreparedContractConstants.FinalReview, final_dokument_id = finalDocumentId }, cancellationToken), "Das finale Dokument konnte nicht zur Prüfung vorgemerkt werden.");
        if (ready.Id != id || ready.Status != PreparedContractConstants.FinalReview || ready.Revision != expectedRevision ||
            ready.SignaturRevision != expectedRevision || ready.SignaturGestartetAm is null || ready.FinalDokumentId != finalDocumentId)
            throw new PreparedContractConflictException("Der Server hat die letzte Dokumentprüfung nicht korrekt bestätigt.");
        return ready;
    }

    public async Task<KommunikationsvorgangDto> CompleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var current = await LoadRequiredAsync(id, cancellationToken);
        if (current.Status != PreparedContractConstants.FinalReview || current.SignaturRevision != current.Revision || current.SignaturGestartetAm is null || current.FinalDokumentId is not > 0)
            throw new PreparedContractConflictException("Der Vertragsvorgang ist noch nicht zur letzten Prüfung bereit.");
        if (current.Dokumenttyp == PreparedContractConstants.MembershipApplication)
        {
            var token = await _auth.GetAccessTokenAsync() ?? throw new PreparedContractConflictException("Die Anmeldung ist abgelaufen.");
            using var client = _httpClientFactory(); using var request = new HttpRequestMessage(HttpMethod.Post, $"{_clientFactory.Url.TrimEnd('/')}/functions/v1/kgv-complete-prepared-membership-application");
            request.Headers.Add("apikey", _clientFactory.Key); request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent(JsonSerializer.Serialize(new { kommunikationsvorgang_id = id, expected_revision = current.Revision }), Encoding.UTF8, "application/json");
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode) throw new PreparedContractConflictException("Der Mitgliedsantrag kann nicht abgeschlossen werden.");
            var completedByServer = await LoadRequiredAsync(id, cancellationToken);
            if (completedByServer.Status != PreparedContractConstants.Completed) throw new PreparedContractConflictException("Der Server hat den Mitgliedsantragsabschluss nicht bestätigt.");
            return completedByServer;
        }
        var completed = Single(await PatchAsync($"kommunikationsvorgang?id=eq.{id}&status=eq.{PreparedContractConstants.FinalReview}&revision=eq.{current.Revision}&signatur_revision=eq.{current.Revision}&final_dokument_id=eq.{current.FinalDokumentId}&zugewiesen_an=eq.{CurrentUserId()}", new { status = PreparedContractConstants.Completed }, cancellationToken), "Der Vertragsvorgang kann nicht abgeschlossen werden.");
        if (completed.Id != id || completed.Status != PreparedContractConstants.Completed || completed.Revision != current.Revision || completed.FinalDokumentId != current.FinalDokumentId)
            throw new PreparedContractConflictException("Der Server hat den Vertragsabschluss nicht korrekt bestätigt.");
        return completed;
    }

    public async Task<KommunikationsvorgangDto> DiscardAsync(long id, int expectedRevision, CancellationToken cancellationToken = default)
    {
        var token = await _auth.GetAccessTokenAsync() ?? throw new PreparedContractConflictException("Die Anmeldung ist abgelaufen.");
        using var client = _httpClientFactory(); using var request = new HttpRequestMessage(HttpMethod.Post, $"{_clientFactory.Url.TrimEnd('/')}/functions/v1/kgv-discard-prepared-contract");
        request.Headers.Add("apikey", _clientFactory.Key); request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(JsonSerializer.Serialize(new { kommunikationsvorgang_id = id, expected_revision = expectedRevision }), Encoding.UTF8, "application/json");
        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) throw new PreparedContractConflictException("Der Vertragsvorgang konnte nicht verworfen werden.");
        var updated = await LoadRequiredAsync(id, cancellationToken);
        return updated.Status == PreparedContractConstants.Discarded ? updated : throw new PreparedContractConflictException("Der Server hat das Verwerfen nicht bestätigt.");
    }

    private async Task<KommunikationsvorgangDto> LoadRequiredAsync(long id, CancellationToken ct) => await LoadAsync(id, ct);
    private void EnsureEditable(KommunikationsvorgangDto item, int revision)
    {
        if (item.Revision != revision) throw new PreparedContractConflictException("Der Vertragsentwurf wurde zwischenzeitlich geändert.");
        if (item.Status != PreparedContractConstants.Prepared) throw new PreparedContractConflictException("Der Vertragsentwurf ist nicht mehr bearbeitbar.");
        if (item.SignaturGestartetAm is not null) throw new PreparedContractConflictException("Die Signatur wurde bereits gestartet.");
        if (item.ZugewiesenAn != CurrentUserId()) throw new PreparedContractConflictException("Der Vorgang ist nicht Ihnen zugewiesen.");
    }
    private static void ValidateDraft(string documentType, int memberId, int? parcelId, JsonElement draft)
    {
        var parsed = PreparedContractDraftParser.Parse(draft);
        if (documentType == PreparedContractConstants.MembershipApplication && (parsed is not PreparedMembershipApplicationDraft ma || ma.MemberId != memberId || parcelId is not null)) throw new PreparedContractValidationException("Mitgliedsantrag und Vorgang stimmen nicht überein.");
        if (documentType == PreparedContractConstants.LeaseContract && (parsed is not PreparedLeaseContractDraft pv || pv.MemberId != memberId || pv.Draft.ParcelId != parcelId)) throw new PreparedContractValidationException("Pachtvertrag und Vorgang stimmen nicht überein.");
        if (documentType == PreparedContractConstants.MaintenanceContract && (parsed is not PreparedMaintenanceContractDraft wv || wv.MemberId != memberId || parcelId is not null)) throw new PreparedContractValidationException("Wartungsvertrag und Vorgang stimmen nicht überein.");
        if (documentType is not PreparedContractConstants.MembershipApplication and not PreparedContractConstants.LeaseContract and not PreparedContractConstants.MaintenanceContract) throw new PreparedContractValidationException("Der Dokumenttyp wird nicht unterstützt.");
    }
    private Guid CurrentUserId() => Guid.TryParse(_auth.CurrentUserId, out var id) ? id : throw new PreparedContractConflictException("Es ist kein angemeldeter Benutzer vorhanden.");
    private async Task<List<KommunikationsvorgangDto>> PatchAsync(string query, object body, CancellationToken ct) => await SendAsync(HttpMethod.Patch, query, JsonSerializer.SerializeToElement(body), ct);
    private async Task<List<KommunikationsvorgangDto>> SendAsync(HttpMethod method, string relative, JsonElement? body, CancellationToken ct)
    {
        var token = await _auth.GetAccessTokenAsync() ?? throw new PreparedContractConflictException("Die Anmeldung ist abgelaufen.");
        using var client = _httpClientFactory();
        using var request = new HttpRequestMessage(method, $"{_clientFactory.Url.TrimEnd('/')}/rest/v1/{relative}");
        request.Headers.Add("apikey", _clientFactory.Key); request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token); request.Headers.Add("Prefer", "return=representation");
        if (body is { } value) request.Content = new StringContent(value.GetRawText(), Encoding.UTF8, "application/json");
        using var response = await client.SendAsync(request, ct); var content = await response.Content.ReadAsStringAsync(ct);
        if (response.StatusCode == HttpStatusCode.Conflict || response.StatusCode == HttpStatusCode.PreconditionFailed) throw new PreparedContractConflictException("Der Vorgang wurde zwischenzeitlich geändert.");
        if (!response.IsSuccessStatusCode) throw new PreparedContractRepositoryException($"Kommunikationsvorgang konnte nicht gespeichert werden ({(int)response.StatusCode}).");
        return JsonSerializer.Deserialize<List<KommunikationsvorgangDto>>(content, Json) ?? [];
    }
    private static KommunikationsvorgangDto Single(List<KommunikationsvorgangDto> records, string message) => records.Count == 1 ? records[0] : throw new PreparedContractConflictException(message);
    private sealed class FinalDocumentProvenanceRecord
    {
        [JsonPropertyName("id")] public long Id { get; init; }
        [JsonPropertyName("source_kommunikationsvorgang_id")] public long SourceKommunikationsvorgangId { get; init; }
        [JsonPropertyName("source_revision")] public int SourceRevision { get; init; }
    }
}
