using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Services;

namespace KGV.Infrastructure.Services;

/// <summary>Authenticated preview client for the server-owned contract generator.</summary>
public sealed class ContractGenerationClient : IContractGenerationClient
{
    private readonly ISupabaseClientFactory _clientFactory;
    private readonly IAuthService _auth;
    private readonly Func<HttpClient> _httpClientFactory;

    public ContractGenerationClient(ISupabaseClientFactory clientFactory, IAuthService auth)
        : this(clientFactory, auth, () => new HttpClient()) { }

    public ContractGenerationClient(ISupabaseClientFactory clientFactory, IAuthService auth, Func<HttpClient> httpClientFactory)
    {
        _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        _auth = auth ?? throw new ArgumentNullException(nameof(auth));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    public async Task<byte[]> PreviewMembershipApplicationAsync(MitgliedsantragDokumentRequest request, decimal admissionFee, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.MitgliedId <= 0) throw new ArgumentException("Für den Mitgliedsantrag ist eine gültige Mitglieds-ID erforderlich.", nameof(request));
        if (request.Mitgliedsbeitrag < 0) throw new ArgumentException("Der Mitgliedsbeitrag darf nicht negativ sein.", nameof(request));
        if (admissionFee < 0) throw new ArgumentOutOfRangeException(nameof(admissionFee), "Die Aufnahmegebühr darf nicht negativ sein.");

        var draft = PreparedMembershipApplicationMapper.ToPreparedDraft(request, request.IstMinderjaehrig);
        var payload = new Dictionary<string, object?>
        {
            ["action"] = "preview",
            ["type"] = "mitgliedsantrag",
            ["member_id"] = draft.MemberId,
            ["start_date"] = draft.StartDate,
            ["member_fee"] = draft.MemberFee,
            ["admission_fee"] = admissionFee,
            ["include_secondary_member"] = request.IstMinderjaehrig ? false : request.NebenmitgliedEinbeziehen
        };

        if (request.IstMinderjaehrig && draft.Representative is { } representative)
        {
            payload["representative"] = representative.Mode == "existing"
                ? new Dictionary<string, object?> { ["mode"] = "existing", ["member_id"] = representative.MemberId, ["adresse_abweichend"] = false }
                : new Dictionary<string, object?>
                {
                    ["mode"] = "manual", ["vorname"] = representative.Vorname, ["nachname"] = representative.Nachname,
                    ["adresse_abweichend"] = representative.AdresseAbweichend, ["adresse"] = representative.Adresse,
                    ["plz"] = representative.Plz, ["ort"] = representative.Ort
                };
        }

        var token = await _auth.GetAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(token)) throw new InvalidOperationException("Für die Vertragsvorschau ist eine gültige Anmeldung erforderlich.");

        using var client = _httpClientFactory();
        using var message = new HttpRequestMessage(HttpMethod.Post, $"{_clientFactory.Url.TrimEnd('/')}/functions/v1/kgv-generate-contract");
        message.Headers.Add("apikey", _clientFactory.Key);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        message.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var response = await client.SendAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Die Vertragsvorschau konnte nicht erzeugt werden ({(int)response.StatusCode}): {await ReadErrorMessageAsync(response, cancellationToken)}");

        var contentType = response.Content.Headers.ContentType?.MediaType;
        if (!string.IsNullOrWhiteSpace(contentType) && !string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Die Vertragsvorschau lieferte einen unerwarteten Inhaltstyp '{contentType}'.");
        var pdf = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        return pdf.Length > 0 ? pdf : throw new InvalidOperationException("Die Vertragsvorschau hat keine PDF-Daten geliefert.");
    }

    public async Task<byte[]> PreviewLeaseContractAsync(PachtvertragDokumentRequest request, CancellationToken cancellationToken = default)
    {
        var draft = PreparedLeaseContractMapper.ToPreparedDraft(request).Draft;
        var payload = new Dictionary<string, object?>
        {
            ["action"] = "preview", ["type"] = "pachtvertrag", ["member_id"] = request.MitgliedId, ["parcel_id"] = request.ParzelleId,
            ["start_date"] = draft.StartDate, ["has_previous_contract"] = draft.HasPreviousContract,
            ["include_secondary_member"] = draft.IncludeSecondaryMember, ["handover_date"] = draft.HandoverDate,
            ["attachment_1"] = draft.Attachment1, ["attachment_2"] = draft.Attachment2, ["attachment_3"] = draft.Attachment3, ["attachment_4"] = draft.Attachment4
        };
        if (draft.HasPreviousContract == true) payload["previous_contract_date"] = draft.PreviousContractDate;
        if (!string.IsNullOrWhiteSpace(draft.FixedUntil)) payload["fixed_until"] = draft.FixedUntil;
        if (!string.IsNullOrWhiteSpace(draft.GardenRulesVersion)) payload["garden_rules_version"] = draft.GardenRulesVersion;
        if (!string.IsNullOrWhiteSpace(draft.GardenRulesHandedOverAt)) payload["garden_rules_handed_over_at"] = draft.GardenRulesHandedOverAt;
        if (!string.IsNullOrWhiteSpace(draft.AdditionalAgreements)) payload["additional_agreements"] = draft.AdditionalAgreements;
        if (!string.IsNullOrWhiteSpace(draft.ClubSignatory)) payload["club_signatory"] = draft.ClubSignatory;
        return await SendPreviewAsync(payload, cancellationToken);
    }

    public async Task<ContractFinalizeResult> FinalizeMembershipApplicationAsync(MembershipApplicationFinalizeRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request); ArgumentNullException.ThrowIfNull(request.Application);
        var application = request.Application;
        var draft = PreparedMembershipApplicationMapper.ToPreparedDraft(application, application.IstMinderjaehrig);
        var payload = new Dictionary<string, object?> { ["action"] = "finalize", ["type"] = "mitgliedsantrag", ["member_id"] = draft.MemberId, ["start_date"] = draft.StartDate, ["member_fee"] = draft.MemberFee, ["admission_fee"] = request.AdmissionFee, ["include_secondary_member"] = !application.IstMinderjaehrig && application.NebenmitgliedEinbeziehen };
        if (application.IstMinderjaehrig && draft.Representative is { } representative)
            payload["representative"] = representative.Mode == "existing" ? new Dictionary<string, object?> { ["mode"] = "existing", ["member_id"] = representative.MemberId, ["adresse_abweichend"] = false } : new Dictionary<string, object?> { ["mode"] = "manual", ["vorname"] = representative.Vorname, ["nachname"] = representative.Nachname, ["adresse_abweichend"] = representative.AdresseAbweichend, ["adresse"] = representative.Adresse, ["plz"] = representative.Plz, ["ort"] = representative.Ort };
        Add(payload, "signature_application_member", request.ApplicationMemberSignature); Add(payload, "signature_privacy_member", request.PrivacyMemberSignature); Add(payload, "signature_board", request.BoardSignature);
        if (application.IstMinderjaehrig) { Add(payload, "signature_application_representative", request.ApplicationRepresentativeSignature); Add(payload, "signature_privacy_representative", request.PrivacyRepresentativeSignature); }
        else if (application.NebenmitgliedEinbeziehen) { Add(payload, "signature_application_secondary", request.ApplicationSecondarySignature); Add(payload, "signature_privacy_secondary", request.PrivacySecondarySignature); }
        return await SendFinalizeAsync(payload, cancellationToken);
    }

    public async Task<ContractFinalizeResult> FinalizeLeaseContractAsync(LeaseContractFinalizeRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request); ArgumentNullException.ThrowIfNull(request.Contract);
        if (string.IsNullOrWhiteSpace(request.MemberSignature)) throw new ArgumentException("Die Unterschrift des Mitglieds ist erforderlich.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.BoardSignature)) throw new ArgumentException("Die Unterschrift des Vorstands ist erforderlich.", nameof(request));
        if (request.Contract.IncludeSecondaryMember == true && string.IsNullOrWhiteSpace(request.SecondarySignature)) throw new ArgumentException("Die Unterschrift des Nebenmitglieds ist erforderlich.", nameof(request));

        var contract = request.Contract;
        var draft = PreparedLeaseContractMapper.ToPreparedDraft(contract).Draft;
        var payload = new Dictionary<string, object?>
        {
            ["action"] = "finalize", ["type"] = "pachtvertrag", ["member_id"] = contract.MitgliedId, ["parcel_id"] = contract.ParzelleId,
            ["start_date"] = draft.StartDate, ["has_previous_contract"] = draft.HasPreviousContract,
            ["include_secondary_member"] = draft.IncludeSecondaryMember, ["handover_date"] = draft.HandoverDate,
            ["attachment_1"] = draft.Attachment1, ["attachment_2"] = draft.Attachment2, ["attachment_3"] = draft.Attachment3, ["attachment_4"] = draft.Attachment4,
            ["signature_member"] = request.MemberSignature, ["signature_board"] = request.BoardSignature
        };
        if (draft.HasPreviousContract == true) payload["previous_contract_date"] = draft.PreviousContractDate;
        if (!string.IsNullOrWhiteSpace(draft.FixedUntil)) payload["fixed_until"] = draft.FixedUntil;
        if (!string.IsNullOrWhiteSpace(draft.GardenRulesVersion)) payload["garden_rules_version"] = draft.GardenRulesVersion;
        if (!string.IsNullOrWhiteSpace(draft.GardenRulesHandedOverAt)) payload["garden_rules_handed_over_at"] = draft.GardenRulesHandedOverAt;
        if (!string.IsNullOrWhiteSpace(draft.AdditionalAgreements)) payload["additional_agreements"] = draft.AdditionalAgreements;
        if (!string.IsNullOrWhiteSpace(draft.ClubSignatory)) payload["club_signatory"] = draft.ClubSignatory;
        if (contract.IncludeSecondaryMember == true) payload["signature_secondary"] = request.SecondarySignature;
        return await SendFinalizeAsync(payload, cancellationToken);
    }

    private async Task<byte[]> SendPreviewAsync(object payload, CancellationToken cancellationToken)
    {
        var token = await _auth.GetAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(token)) throw new InvalidOperationException("Für die Vertragsvorschau ist eine gültige Anmeldung erforderlich.");
        using var client = _httpClientFactory(); using var message = new HttpRequestMessage(HttpMethod.Post, $"{_clientFactory.Url.TrimEnd('/')}/functions/v1/kgv-generate-contract");
        message.Headers.Add("apikey", _clientFactory.Key); message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token); message.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var response = await client.SendAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Die Vertragsvorschau konnte nicht erzeugt werden ({(int)response.StatusCode}): {await ReadErrorMessageAsync(response, cancellationToken)}");
        var contentType = response.Content.Headers.ContentType?.MediaType;
        if (!string.IsNullOrWhiteSpace(contentType) && !string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException($"Die Vertragsvorschau lieferte einen unerwarteten Inhaltstyp '{contentType}'.");
        var pdf = await response.Content.ReadAsByteArrayAsync(cancellationToken); return pdf.Length > 0 ? pdf : throw new InvalidOperationException("Die Vertragsvorschau hat keine PDF-Daten geliefert.");
    }

    private async Task<ContractFinalizeResult> SendFinalizeAsync(object payload, CancellationToken cancellationToken)
    {
        var token = await _auth.GetAccessTokenAsync(); if (string.IsNullOrWhiteSpace(token)) throw new InvalidOperationException("Für die Vertragsfinalisierung ist eine gültige Anmeldung erforderlich.");
        using var client = _httpClientFactory(); using var message = new HttpRequestMessage(HttpMethod.Post, $"{_clientFactory.Url.TrimEnd('/')}/functions/v1/kgv-generate-contract");
        message.Headers.Add("apikey", _clientFactory.Key); message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token); message.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var response = await client.SendAsync(message, cancellationToken); if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Die Vertragsfinalisierung konnte nicht erzeugt werden ({(int)response.StatusCode}): {await ReadErrorMessageAsync(response, cancellationToken)}");
        var content = await response.Content.ReadAsStringAsync(cancellationToken); if (string.IsNullOrWhiteSpace(content)) throw new InvalidOperationException("Die Vertragsfinalisierung lieferte keine JSON-Antwort.");
        try { using var json = JsonDocument.Parse(content); var root = json.RootElement; return new(root.TryGetProperty("documentId", out var id) && id.TryGetInt32(out var documentId) ? documentId : null, root.TryGetProperty("message", out var messageValue) && messageValue.ValueKind == JsonValueKind.String ? messageValue.GetString() : null); }
        catch (JsonException ex) { throw new InvalidOperationException("Die Vertragsfinalisierung lieferte keine gültige JSON-Antwort.", ex); }
    }

    private static void Add(Dictionary<string, object?> payload, string name, string? value) { if (!string.IsNullOrWhiteSpace(value)) payload[name] = value; }

    private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(content)) return "Keine Fehlermeldung vom Server erhalten.";
        try
        {
            using var json = JsonDocument.Parse(content);
            if (json.RootElement.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(message.GetString()))
                return message.GetString()!;
        }
        catch (JsonException) { }
        return content;
    }
}
