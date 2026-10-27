using System.Net;
using System.Text;
using System.Text.Json;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Infrastructure.Services;
using Supabase;

namespace KGV.Tests;

public sealed class ContractGenerationClientHttpTests
{
    [Fact]
    public async Task AdultPreview_SendsExpectedAuthenticatedSnakeCasePayload()
    {
        var handler = new RecordingHandler(Pdf());
        var result = await Client(handler).PreviewMembershipApplicationAsync(Request(), 12.5m);
        var sent = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, sent.Method); Assert.Equal("/functions/v1/kgv-generate-contract", sent.Uri);
        Assert.Equal("Bearer token", sent.Authorization); Assert.Equal("application/json", sent.ContentType); Assert.Contains("apikey", sent.Headers, StringComparison.OrdinalIgnoreCase); Assert.DoesNotContain("service_role", sent.Headers, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(new byte[] { 1, 2, 3 }, result);
        using var body = JsonDocument.Parse(sent.Body); var root = body.RootElement;
        Assert.Equal("preview", root.GetProperty("action").GetString()); Assert.Equal("mitgliedsantrag", root.GetProperty("type").GetString()); Assert.Equal(101, root.GetProperty("member_id").GetInt32());
        Assert.Equal("2026-11-01", root.GetProperty("start_date").GetString()); Assert.Equal(42.5m, root.GetProperty("member_fee").GetDecimal()); Assert.Equal(12.5m, root.GetProperty("admission_fee").GetDecimal()); Assert.True(root.GetProperty("include_secondary_member").GetBoolean());
        Assert.False(root.TryGetProperty("representative", out _)); Assert.False(sent.Body.Contains("signature", StringComparison.OrdinalIgnoreCase)); Assert.False(sent.Body.Contains("schema_version", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task MinorExistingRepresentative_MapsExistingAndDisablesSecondaryMember()
    {
        var handler = new RecordingHandler(Pdf()); var request = Request(); request.IstMinderjaehrig = true; request.GesetzlicherVertreterAusBestehendemMitglied = true; request.GesetzlicherVertreterMitgliedId = 77;
        await Client(handler).PreviewMembershipApplicationAsync(request, 0);
        using var body = JsonDocument.Parse(handler.Requests.Single().Body); var rep = body.RootElement.GetProperty("representative");
        Assert.Equal("existing", rep.GetProperty("mode").GetString()); Assert.Equal(77, rep.GetProperty("member_id").GetInt32()); Assert.False(rep.GetProperty("adresse_abweichend").GetBoolean()); Assert.False(body.RootElement.GetProperty("include_secondary_member").GetBoolean());
    }

    [Fact]
    public async Task MinorManualRepresentative_MapsManualAddress()
    {
        var handler = new RecordingHandler(Pdf()); var request = Request(); request.IstMinderjaehrig = true; request.GesetzlicherVertreterAdresseAbweichend = true; request.GesetzlicherVertreterSnapshot = new() { Vorname = "Ada", Nachname = "Lovelace", Adresse = "Str. 1", Plz = "12345", Ort = "Ort" };
        await Client(handler).PreviewMembershipApplicationAsync(request, 0);
        using var body = JsonDocument.Parse(handler.Requests.Single().Body); var rep = body.RootElement.GetProperty("representative");
        Assert.Equal("manual", rep.GetProperty("mode").GetString()); Assert.Equal("Ada", rep.GetProperty("vorname").GetString()); Assert.Equal("Lovelace", rep.GetProperty("nachname").GetString()); Assert.True(rep.GetProperty("adresse_abweichend").GetBoolean()); Assert.Equal("Str. 1", rep.GetProperty("adresse").GetString()); Assert.Equal("12345", rep.GetProperty("plz").GetString()); Assert.Equal("Ort", rep.GetProperty("ort").GetString());
    }

    [Fact]
    public async Task MissingToken_DoesNotSendRequest()
    {
        var handler = new RecordingHandler(Pdf());
        await Assert.ThrowsAsync<InvalidOperationException>(() => Client(handler, null).PreviewMembershipApplicationAsync(Request(), 0));
        Assert.Empty(handler.Requests);
    }

    [Theory]
    [InlineData(0, "application/pdf", "keine PDF-Daten")]
    [InlineData(3, "application/json", "unerwarteten Inhaltstyp")]
    public async Task EmptyOrWrongContent_IsRejected(int bytes, string contentType, string message)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[bytes]) };
        response.Content.Headers.ContentType = new(contentType);
        var handler = new RecordingHandler(response);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Client(handler).PreviewMembershipApplicationAsync(Request(), 0)); Assert.Contains(message, ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task JsonServerError_IncludesMessage()
    {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("{\"message\":\"Ungültiger Antrag\"}", Encoding.UTF8, "application/json") });
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Client(handler).PreviewMembershipApplicationAsync(Request(), 0)); Assert.Contains("400", ex.Message); Assert.Contains("Ungültiger Antrag", ex.Message);
    }

    [Fact]
    public async Task Lease_preview_sends_complete_payload()
    {
        var handler = new RecordingHandler(Pdf());

        await Client(handler).PreviewLeaseContractAsync(LeaseRequest());

        using var body = JsonDocument.Parse(Assert.Single(handler.Requests).Body); var root = body.RootElement;
        Assert.Equal("preview", root.GetProperty("action").GetString()); Assert.Equal("pachtvertrag", root.GetProperty("type").GetString());
        Assert.Equal(101, root.GetProperty("member_id").GetInt32()); Assert.Equal(202, root.GetProperty("parcel_id").GetInt32()); Assert.Equal("2026-11-01", root.GetProperty("start_date").GetString());
        Assert.True(root.GetProperty("has_previous_contract").GetBoolean()); Assert.Equal("2020-02-03", root.GetProperty("previous_contract_date").GetString()); Assert.True(root.GetProperty("include_secondary_member").GetBoolean());
        Assert.Equal("2026-11-04", root.GetProperty("handover_date").GetString()); Assert.Equal("2027-05-06", root.GetProperty("fixed_until").GetString()); Assert.Equal("Fassung 2026", root.GetProperty("garden_rules_version").GetString());
        Assert.Equal("2026-07-08", root.GetProperty("garden_rules_handed_over_at").GetString()); Assert.Equal("Zusatz", root.GetProperty("additional_agreements").GetString());
        Assert.True(root.GetProperty("attachment_1").GetBoolean()); Assert.False(root.GetProperty("attachment_2").GetBoolean()); Assert.True(root.GetProperty("attachment_3").GetBoolean()); Assert.True(root.GetProperty("attachment_4").GetBoolean()); Assert.Equal("Vorstand", root.GetProperty("club_signatory").GetString());
    }

    [Fact]
    public async Task Lease_preview_sendsExistingProcessOnlyWhenAvailableForFachnummerLookup()
    {
        var handler = new RecordingHandler(Pdf()); var request = LeaseRequest(); request.KommunikationsvorgangId = 4711;

        await Client(handler).PreviewLeaseContractAsync(request);

        using var body = JsonDocument.Parse(handler.Requests.Single().Body);
        Assert.Equal(4711, body.RootElement.GetProperty("kommunikationsvorgang_id").GetInt64());
    }

    [Fact]
    public async Task Lease_without_previous_contract_omits_previous_contract_date()
    {
        var handler = new RecordingHandler(Pdf()); var request = LeaseRequest(); request.HasPreviousContract = false; request.AltvertragDatum = new DateTime(2020, 2, 3);

        await Client(handler).PreviewLeaseContractAsync(request);

        using var body = JsonDocument.Parse(handler.Requests.Single().Body); var root = body.RootElement;
        Assert.False(root.GetProperty("has_previous_contract").GetBoolean()); Assert.False(root.TryGetProperty("previous_contract_date", out _));
    }

    [Fact]
    public async Task Lease_empty_optional_values_are_omitted()
    {
        var handler = new RecordingHandler(Pdf()); var request = LeaseRequest(); request.UebergabeDatum = null; request.BefristetBis = null; request.KleingartenordnungFassung = null; request.KleingartenordnungAusgehaendigtAm = null; request.WeitereVereinbarungen = null; request.VereinsUnterzeichner = null;

        await Client(handler).PreviewLeaseContractAsync(request);

        using var body = JsonDocument.Parse(handler.Requests.Single().Body); var root = body.RootElement;
        Assert.Equal("2026-11-01", root.GetProperty("handover_date").GetString());
        foreach (var name in new[] { "fixed_until", "garden_rules_version", "garden_rules_handed_over_at", "additional_agreements", "club_signatory" }) Assert.False(root.TryGetProperty(name, out _));
    }

    [Fact]
    public async Task Lease_minor_forces_secondary_member_false()
    {
        var handler = new RecordingHandler(Pdf()); var request = LeaseRequest(); request.IstMinderjaehrig = true; request.IncludeSecondaryMember = true;

        await Client(handler).PreviewLeaseContractAsync(request);

        using var body = JsonDocument.Parse(handler.Requests.Single().Body); Assert.False(body.RootElement.GetProperty("include_secondary_member").GetBoolean());
    }

    [Fact]
    public async Task Lease_preview_uses_apikey_and_bearer_token()
    {
        var handler = new RecordingHandler(Pdf());

        await Client(handler).PreviewLeaseContractAsync(LeaseRequest());

        var sent = Assert.Single(handler.Requests);
        Assert.Equal("anon", sent.ApiKey); Assert.Equal("Bearer token", sent.Authorization); Assert.DoesNotContain("service_role", sent.Headers, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Lease_preview_without_token_does_not_send_request()
    {
        var handler = new RecordingHandler(Pdf());

        await Assert.ThrowsAsync<InvalidOperationException>(() => Client(handler, null).PreviewLeaseContractAsync(LeaseRequest()));

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Lease_preview_rejects_empty_pdf()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([]) }; response.Content.Headers.ContentType = new("application/pdf");
        var handler = new RecordingHandler(response);

        await Assert.ThrowsAsync<InvalidOperationException>(() => Client(handler).PreviewLeaseContractAsync(LeaseRequest()));
    }

    [Fact]
    public async Task Lease_preview_rejects_wrong_content_type()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([1]) }; response.Content.Headers.ContentType = new("application/json");
        var handler = new RecordingHandler(response);

        await Assert.ThrowsAsync<InvalidOperationException>(() => Client(handler).PreviewLeaseContractAsync(LeaseRequest()));
    }

    [Fact]
    public async Task Lease_preview_reads_json_error_message()
    {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("{\"message\":\"Pachtvertrag ungültig.\"}", Encoding.UTF8, "application/json") });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => Client(handler).PreviewLeaseContractAsync(LeaseRequest()));

        Assert.Contains("Pachtvertrag ungültig.", exception.Message);
    }

    [Fact]
    public async Task Lease_preview_contains_no_signature_fields()
    {
        var handler = new RecordingHandler(Pdf());

        await Client(handler).PreviewLeaseContractAsync(LeaseRequest());

        using var body = JsonDocument.Parse(handler.Requests.Single().Body);
        Assert.DoesNotContain(PropertyNames(body.RootElement), name => name.Contains("signature", StringComparison.OrdinalIgnoreCase) || name.Contains("signatur", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Membership_finalize_sends_exact_payload()
    {
        var handler = new RecordingHandler(Json("{\"documentId\":7,\"message\":\"ok\"}"));
        var result = await Client(handler).FinalizeMembershipApplicationAsync(FinalizeRequest());
        using var body = JsonDocument.Parse(handler.Requests.Single().Body); var root = body.RootElement;
        Assert.Equal("finalize", root.GetProperty("action").GetString()); Assert.Equal("mitgliedsantrag", root.GetProperty("type").GetString()); Assert.Equal(101, root.GetProperty("member_id").GetInt32()); Assert.Equal("member-app", root.GetProperty("signature_application_member").GetString()); Assert.Equal("member-privacy", root.GetProperty("signature_privacy_member").GetString()); Assert.Equal("board", root.GetProperty("signature_board").GetString()); Assert.Equal(7, result.DocumentId); Assert.Equal("ok", result.Message);
    }

    [Fact]
    public async Task Membership_finalize_sends_representative_signatures_for_minor()
    {
        var handler = new RecordingHandler(Json("{}")); var finalize = FinalizeRequest(); finalize.Application.IstMinderjaehrig = true; finalize.Application.GesetzlicherVertreterAusBestehendemMitglied = true; finalize.Application.GesetzlicherVertreterMitgliedId = 77;
        await Client(handler).FinalizeMembershipApplicationAsync(finalize);
        using var body = JsonDocument.Parse(handler.Requests.Single().Body); var root = body.RootElement; Assert.Equal("rep-app", root.GetProperty("signature_application_representative").GetString()); Assert.Equal("rep-privacy", root.GetProperty("signature_privacy_representative").GetString()); Assert.False(root.TryGetProperty("signature_application_secondary", out _));
    }

    [Fact]
    public async Task Membership_finalize_uses_auth_and_reports_errors()
    {
        var handler = new RecordingHandler(Json("{}")); await Client(handler).FinalizeMembershipApplicationAsync(FinalizeRequest()); var sent = handler.Requests.Single(); Assert.Equal("anon", sent.ApiKey); Assert.Equal("Bearer token", sent.Authorization);
        var noToken = new RecordingHandler(Json("{}")); await Assert.ThrowsAsync<InvalidOperationException>(() => Client(noToken, null).FinalizeMembershipApplicationAsync(FinalizeRequest())); Assert.Empty(noToken.Requests);
        var error = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("{\"message\":\"Finale ungültig\"}", Encoding.UTF8, "application/json") }); var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Client(error).FinalizeMembershipApplicationAsync(FinalizeRequest())); Assert.Contains("Finale ungültig", ex.Message);
    }

    [Fact] public async Task Membership_finalize_always_sends_board_signature() { var h = new RecordingHandler(Json("{}")); await Client(h).FinalizeMembershipApplicationAsync(FinalizeRequest()); using var b = JsonDocument.Parse(h.Requests.Single().Body); Assert.Equal("board", b.RootElement.GetProperty("signature_board").GetString()); }
    [Fact] public async Task Membership_finalize_omits_irrelevant_signature_fields() { var h = new RecordingHandler(Json("{}")); var r = FinalizeRequest(); r.Application.NebenmitgliedEinbeziehen = false; await Client(h).FinalizeMembershipApplicationAsync(r); using var b = JsonDocument.Parse(h.Requests.Single().Body); Assert.False(b.RootElement.TryGetProperty("signature_application_secondary", out _)); Assert.False(b.RootElement.TryGetProperty("signature_application_representative", out _)); }
    [Fact] public async Task Membership_finalize_without_token_sends_no_request() { var h = new RecordingHandler(Json("{}")); await Assert.ThrowsAsync<InvalidOperationException>(() => Client(h, null).FinalizeMembershipApplicationAsync(FinalizeRequest())); Assert.Empty(h.Requests); }
    [Fact] public async Task Membership_finalize_reads_json_error_message() { var h = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("{\"message\":\"Fehler\"}", Encoding.UTF8, "application/json") }); var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Client(h).FinalizeMembershipApplicationAsync(FinalizeRequest())); Assert.Contains("Fehler", ex.Message); }
    [Fact] public async Task Membership_finalize_reads_success_json() { var h = new RecordingHandler(Json("{\"documentId\":9,\"message\":\"fertig\"}")); var result = await Client(h).FinalizeMembershipApplicationAsync(FinalizeRequest()); Assert.Equal(9, result.DocumentId); Assert.Equal("fertig", result.Message); }

    [Fact]
    public async Task Lease_finalize_sends_exact_payload()
    {
        var handler = new RecordingHandler(Json("{\"documentId\":7,\"message\":\"ok\"}"));
        await Client(handler).FinalizeLeaseContractAsync(LeaseFinalizeRequest());
        using var body = JsonDocument.Parse(handler.Requests.Single().Body); var root = body.RootElement;
        Assert.Equal("finalize", root.GetProperty("action").GetString()); Assert.Equal("pachtvertrag", root.GetProperty("type").GetString());
        Assert.Equal(101, root.GetProperty("member_id").GetInt32()); Assert.Equal(202, root.GetProperty("parcel_id").GetInt32()); Assert.Equal("2026-11-01", root.GetProperty("start_date").GetString()); Assert.True(root.GetProperty("has_previous_contract").GetBoolean()); Assert.Equal("2020-02-03", root.GetProperty("previous_contract_date").GetString()); Assert.True(root.GetProperty("include_secondary_member").GetBoolean()); Assert.Equal("2026-11-04", root.GetProperty("handover_date").GetString());
        Assert.Equal("2027-05-06", root.GetProperty("fixed_until").GetString()); Assert.Equal("Fassung 2026", root.GetProperty("garden_rules_version").GetString()); Assert.Equal("2026-07-08", root.GetProperty("garden_rules_handed_over_at").GetString()); Assert.Equal("Zusatz", root.GetProperty("additional_agreements").GetString()); Assert.True(root.GetProperty("attachment_1").GetBoolean()); Assert.False(root.GetProperty("attachment_2").GetBoolean()); Assert.True(root.GetProperty("attachment_3").GetBoolean()); Assert.True(root.GetProperty("attachment_4").GetBoolean()); Assert.Equal("Vorstand", root.GetProperty("club_signatory").GetString());
        Assert.Equal("member", root.GetProperty("signature_member").GetString()); Assert.Equal("secondary", root.GetProperty("signature_secondary").GetString()); Assert.Equal("board", root.GetProperty("signature_board").GetString());
    }

    [Fact]
    public async Task Lease_finalize_preserves_all_v1_fields()
    {
        var handler = new RecordingHandler(Json("{}")); await Client(handler).FinalizeLeaseContractAsync(LeaseFinalizeRequest());
        using var body = JsonDocument.Parse(handler.Requests.Single().Body); var root = body.RootElement;
        Assert.Equal("2026-11-01", root.GetProperty("start_date").GetString()); Assert.Equal("2020-02-03", root.GetProperty("previous_contract_date").GetString()); Assert.Equal("2026-11-04", root.GetProperty("handover_date").GetString()); Assert.Equal("2027-05-06", root.GetProperty("fixed_until").GetString()); Assert.Equal("2026-07-08", root.GetProperty("garden_rules_handed_over_at").GetString()); Assert.Equal("Fassung 2026", root.GetProperty("garden_rules_version").GetString()); Assert.Equal("Zusatz", root.GetProperty("additional_agreements").GetString()); Assert.Equal("Vorstand", root.GetProperty("club_signatory").GetString());
        Assert.Equal(101, root.GetProperty("member_id").GetInt32()); Assert.Equal(202, root.GetProperty("parcel_id").GetInt32()); Assert.True(root.GetProperty("has_previous_contract").GetBoolean()); Assert.True(root.GetProperty("include_secondary_member").GetBoolean()); Assert.True(root.GetProperty("attachment_1").GetBoolean()); Assert.False(root.GetProperty("attachment_2").GetBoolean()); Assert.True(root.GetProperty("attachment_3").GetBoolean()); Assert.True(root.GetProperty("attachment_4").GetBoolean());
    }

    [Fact] public async Task Lease_finalize_without_previous_contract_omits_date() { var h = new RecordingHandler(Json("{}")); var r = LeaseFinalizeRequest(); r.Contract.HasPreviousContract = false; r.Contract.AltvertragDatum = new DateTime(2020, 2, 3); await Client(h).FinalizeLeaseContractAsync(r); using var b = JsonDocument.Parse(h.Requests.Single().Body); Assert.False(b.RootElement.GetProperty("has_previous_contract").GetBoolean()); Assert.False(b.RootElement.TryGetProperty("previous_contract_date", out _)); }
    [Fact] public async Task Lease_finalize_sends_secondary_when_enabled() { var h = new RecordingHandler(Json("{}")); await Client(h).FinalizeLeaseContractAsync(LeaseFinalizeRequest()); using var b = JsonDocument.Parse(h.Requests.Single().Body); Assert.Equal("secondary", b.RootElement.GetProperty("signature_secondary").GetString()); }
    [Fact] public async Task Lease_finalize_requires_secondary_when_enabled() { var h = new RecordingHandler(Json("{}")); await Assert.ThrowsAsync<ArgumentException>(() => Client(h).FinalizeLeaseContractAsync(LeaseFinalizeRequest(secondarySignature: " "))); Assert.Empty(h.Requests); }
    [Fact] public async Task Lease_finalize_omits_secondary_when_disabled() { var h = new RecordingHandler(Json("{}")); var r = LeaseFinalizeRequest(); r.Contract.IncludeSecondaryMember = false; await Client(h).FinalizeLeaseContractAsync(r); using var b = JsonDocument.Parse(h.Requests.Single().Body); Assert.False(b.RootElement.TryGetProperty("signature_secondary", out _)); }
    [Fact] public async Task Lease_finalize_contains_no_representative_fields() { var h = new RecordingHandler(Json("{}")); await Client(h).FinalizeLeaseContractAsync(LeaseFinalizeRequest()); using var b = JsonDocument.Parse(h.Requests.Single().Body); Assert.DoesNotContain(PropertyNames(b.RootElement), name => name.Contains("representative", StringComparison.OrdinalIgnoreCase)); }
    [Fact] public async Task Lease_finalize_requires_member_signature() { var h = new RecordingHandler(Json("{}")); await Assert.ThrowsAsync<ArgumentException>(() => Client(h).FinalizeLeaseContractAsync(LeaseFinalizeRequest(memberSignature: " "))); Assert.Empty(h.Requests); }
    [Fact] public async Task Lease_finalize_requires_board_signature() { var h = new RecordingHandler(Json("{}")); await Assert.ThrowsAsync<ArgumentException>(() => Client(h).FinalizeLeaseContractAsync(LeaseFinalizeRequest(boardSignature: " "))); Assert.Empty(h.Requests); }
    [Fact] public async Task Lease_finalize_uses_apikey_and_bearer() { var h = new RecordingHandler(Json("{}")); await Client(h).FinalizeLeaseContractAsync(LeaseFinalizeRequest()); var sent = h.Requests.Single(); Assert.Equal("anon", sent.ApiKey); Assert.Equal("Bearer token", sent.Authorization); Assert.DoesNotContain("service_role", sent.Headers, StringComparison.OrdinalIgnoreCase); }
    [Fact] public async Task Lease_finalize_without_token_sends_no_request() { var h = new RecordingHandler(Json("{}")); await Assert.ThrowsAsync<InvalidOperationException>(() => Client(h, null).FinalizeLeaseContractAsync(LeaseFinalizeRequest())); Assert.Empty(h.Requests); }
    [Fact] public async Task Lease_finalize_reads_success_json() { var h = new RecordingHandler(Json("{\"documentId\":9,\"message\":\"fertig\"}")); var result = await Client(h).FinalizeLeaseContractAsync(LeaseFinalizeRequest()); Assert.Equal(9, result.DocumentId); Assert.Equal("fertig", result.Message); }
    [Fact] public async Task Lease_finalize_reads_json_error_message() { var h = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("{\"message\":\"Pachtvertrag konnte nicht erstellt werden.\"}", Encoding.UTF8, "application/json") }); var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Client(h).FinalizeLeaseContractAsync(LeaseFinalizeRequest())); Assert.Contains("Pachtvertrag konnte nicht erstellt werden.", ex.Message); }
    [Fact] public async Task Lease_finalize_omits_empty_optional_fields() { var h = new RecordingHandler(Json("{}")); var r = LeaseFinalizeRequest(); r.Contract.UebergabeDatum = null; r.Contract.BefristetBis = null; r.Contract.KleingartenordnungFassung = " "; r.Contract.KleingartenordnungAusgehaendigtAm = null; r.Contract.WeitereVereinbarungen = ""; r.Contract.VereinsUnterzeichner = null; await Client(h).FinalizeLeaseContractAsync(r); using var b = JsonDocument.Parse(h.Requests.Single().Body); Assert.Equal("2026-11-01", b.RootElement.GetProperty("handover_date").GetString()); foreach (var name in new[] { "fixed_until", "garden_rules_version", "garden_rules_handed_over_at", "additional_agreements", "club_signatory" }) Assert.False(b.RootElement.TryGetProperty(name, out _)); }

    private static ContractGenerationClient Client(RecordingHandler handler, string? token = "token") => new(new Factory(), new Auth(token), () => new HttpClient(handler, false));
    private static MitgliedsantragDokumentRequest Request() => new() { MitgliedId = 101, BeginnDatum = new DateTime(2026, 11, 1), Mitgliedsbeitrag = 42.5m, NebenmitgliedEinbeziehen = true };
    private static PachtvertragDokumentRequest LeaseRequest() => new() { MitgliedId = 101, ParzelleId = 202, Vertragsbeginn = new DateTime(2026, 11, 1, 12, 0, 0), HasPreviousContract = true, AltvertragDatum = new DateTime(2020, 2, 3, 12, 0, 0), UebergabeDatum = new DateTime(2026, 11, 4, 12, 0, 0), BefristetBis = new DateTime(2027, 5, 6, 12, 0, 0), KleingartenordnungFassung = "Fassung 2026", KleingartenordnungAusgehaendigtAm = new DateTime(2026, 7, 8, 12, 0, 0), WeitereVereinbarungen = "Zusatz", Anlage1 = true, Anlage2 = false, Anlage3 = true, Anlage4 = true, VereinsUnterzeichner = "Vorstand", IncludeSecondaryMember = true, SecondaryMemberId = 102, IstMinderjaehrig = false };
    private static MembershipApplicationFinalizeRequest FinalizeRequest() => new() { Application = Request(), SourceKommunikationsvorgangId = 77, SourceRevision = 3, AdmissionFee = 12.5m, ApplicationMemberSignature = "member-app", PrivacyMemberSignature = "member-privacy", ApplicationRepresentativeSignature = "rep-app", PrivacyRepresentativeSignature = "rep-privacy", ApplicationSecondarySignature = "secondary-app", PrivacySecondarySignature = "secondary-privacy", BoardSignature = "board" };
    private static LeaseContractFinalizeRequest LeaseFinalizeRequest(string memberSignature = "member", string? secondarySignature = "secondary", string boardSignature = "board") => new() { Contract = LeaseRequest(), MemberSignature = memberSignature, SecondarySignature = secondarySignature, BoardSignature = boardSignature };
    private static HttpResponseMessage Pdf() => new(HttpStatusCode.OK) { Content = new ByteArrayContent([1, 2, 3]) { Headers = { ContentType = new("application/pdf") } } };
    private static HttpResponseMessage Json(string value) => new(HttpStatusCode.OK) { Content = new StringContent(value, Encoding.UTF8, "application/json") };
    private static IEnumerable<string> PropertyNames(JsonElement value) => value.ValueKind == JsonValueKind.Object ? value.EnumerateObject().SelectMany(property => new[] { property.Name }.Concat(PropertyNames(property.Value))) : value.ValueKind == JsonValueKind.Array ? value.EnumerateArray().SelectMany(PropertyNames) : [];
    private sealed class RecordingHandler(params HttpResponseMessage[] responses) : HttpMessageHandler { public List<(HttpMethod Method, string Uri, string Authorization, string ApiKey, string Headers, string ContentType, string Body)> Requests { get; } = []; private int _index; protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) { Requests.Add((request.Method, request.RequestUri!.PathAndQuery, request.Headers.Authorization?.ToString() ?? "", request.Headers.TryGetValues("apikey", out var apiKey) ? apiKey.Single() : "", string.Join(';', request.Headers.Select(x => x.Key)), request.Content?.Headers.ContentType?.MediaType ?? "", request.Content is null ? "" : await request.Content.ReadAsStringAsync(ct))); return responses[_index++]; } }
    private sealed class Factory : ISupabaseClientFactory { public string Url => "https://test.supabase.co"; public string Key => "anon"; public Task<Client> CreateAsync() => throw new NotSupportedException(); }
    private sealed class Auth(string? token) : IAuthService { public bool IsVorstand => false; public bool IsAdmin => false; public string? CurrentUserId => null; public Task<string?> GetAccessTokenAsync() => Task.FromResult(token); public Task<Client> GetClientAsync()=>throw new NotSupportedException(); public Task<bool> LoginAsync(string a,string b)=>throw new NotSupportedException(); public Task<List<AppUserDTO>> GetAppUsersAsync()=>throw new NotSupportedException(); public Task<List<MemberUserLinkStatusDto>> GetMemberUserLinkStatusesAsync()=>throw new NotSupportedException(); public Task<MemberUserLinkStatusDto?> GetMemberUserLinkStatusAsync(int x)=>throw new NotSupportedException(); public Task<InviteUserAccountResult> InviteUserAsync(AppUserDTO x)=>throw new NotSupportedException(); public Task<bool> RemoveUserAsync(AppUserDTO x)=>throw new NotSupportedException(); public Task<bool> ChangeEmailAsync(string x)=>throw new NotSupportedException(); public Task<bool> RequestEmailChangeAsync(string x)=>throw new NotSupportedException(); public Task<bool> VerifyEmailChangeOtpAsync(string a,string b)=>throw new NotSupportedException(); public Task<bool> SendPasswordResetEmailAsync(string x)=>throw new NotSupportedException(); public Task<bool> RequestOtpAsync(string x)=>throw new NotSupportedException(); public Task<bool> VerifyOtpAsync(string a,string b)=>throw new NotSupportedException(); public Task<bool> SetPasswordWithOtpAsync(string a,string b,string c)=>throw new NotSupportedException(); public Task<BiometricSessionTokens?> GetSessionTokensAsync()=>throw new NotSupportedException(); public Task<bool> RestoreSessionAsync(BiometricSessionTokens x)=>throw new NotSupportedException(); public Task LogoutAsync()=>throw new NotSupportedException(); }
}
