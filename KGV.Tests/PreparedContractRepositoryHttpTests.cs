using System.Net;
using System.Text;
using System.Text.Json;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Infrastructure.Services;
using Supabase;

namespace KGV.Tests;

public sealed class PreparedContractRepositoryHttpTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private const long ItemId = 123;

    [Fact]
    public async Task Create_UsesAuthenticatedPostgrestContractWithoutSignatures()
    {
        var handler = new RecordingHandler(Response(Item(7)));
        var repository = Repository(handler);
        await repository.CreateAsync(PreparedContractConstants.LeaseContract, 101, 22, LeaseDraft(), CancellationToken.None);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method); Assert.Contains("kommunikationsvorgang", request.Uri);
        Assert.Equal("Bearer token", request.Authorization); Assert.Equal("return=representation", request.Prefer);
        Assert.DoesNotContain("service_role", request.Headers, StringComparison.OrdinalIgnoreCase);
        using var body = JsonDocument.Parse(request.Body); var root = body.RootElement;
        Assert.Equal("vertrag_signatur", root.GetProperty("vorgangstyp").GetString()); Assert.Equal("vorbereitet", root.GetProperty("status").GetString());
        Assert.Equal(UserId, root.GetProperty("zugewiesen_an").GetGuid()); Assert.Equal(22, root.GetProperty("parzelle_id").GetInt32());
        Assert.False(root.GetProperty("entwurf_daten").GetRawText().Contains("signature", StringComparison.OrdinalIgnoreCase));
    }
    [Fact]
    public async Task ClaimAndRelease_AreAtomicallyFiltered()
    {
        var handler = new RecordingHandler(Response(Item(2)), Response(Item(3)));
        var repository = Repository(handler);
        await repository.ClaimAsync(ItemId); await repository.ReleaseAsync(ItemId);
        Assert.Contains("id=eq.123&zugewiesen_an=is.null", handler.Requests[0].Uri);
        Assert.Contains("zugewiesen_an=eq.11111111-1111-1111-1111-111111111111", handler.Requests[1].Uri);
    }

    [Theory]
    [InlineData(CommunicationArea.Signatures, "vorgangstyp=eq.vertrag_signatur", "status=in.(vorbereitet,zur_signatur,letzte_pruefung)")]
    [InlineData(CommunicationArea.Documents, "vorgangstyp=eq.dokument_erzeugung", "status=in.(vorbereitet,fehler)")]
    [InlineData(CommunicationArea.MailOutbox, "vorgangstyp=eq.mail", "status=in.(mail_bereit,wird_versendet,fehler)")]
    [InlineData(CommunicationArea.Sent, "vorgangstyp=eq.mail", "versendet_am=not.is.null")]
    [InlineData(CommunicationArea.Print, "vorgangstyp=eq.druck", "status=in.(druck_bereit,fehler)")]
    public async Task ListForArea_Uses_the_existing_status_filter_for_each_open_area(CommunicationArea area, string processFilter, string statusFilter)
    {
        var handler = new RecordingHandler(Response());

        await Repository(handler).ListForAreaAsync(area);

        var request = Assert.Single(handler.Requests);
        Assert.Contains(processFilter, request.Uri);
        Assert.Contains(statusFilter, request.Uri);
    }

    [Fact]
    public async Task History_defaults_to_ninety_days_but_can_include_all_items()
    {
        var recentHandler = new RecordingHandler(Response());
        await Repository(recentHandler).ListForAreaAsync(CommunicationArea.History);
        Assert.Contains("status=in.(erledigt,abgebrochen,verworfen)", recentHandler.Requests.Single().Uri);
        Assert.Contains("bearbeitet_am=gte.", recentHandler.Requests.Single().Uri);

        var allHandler = new RecordingHandler(Response());
        await Repository(allHandler).ListForAreaAsync(CommunicationArea.History, includeAllHistory: true);
        Assert.DoesNotContain("bearbeitet_am=gte.", allHandler.Requests.Single().Uri);
    }

    [Fact]
    public async Task CompletePrint_Uses_the_existing_print_completion_transition()
    {
        var handler = new RecordingHandler(Response(Item(2, PreparedContractConstants.Completed)));

        await Repository(handler).CompletePrintAsync(ItemId);

        var request = Assert.Single(handler.Requests);
        Assert.Contains("vorgangstyp=eq.druck&status=eq.druck_bereit", request.Uri);
        using var body = JsonDocument.Parse(request.Body);
        Assert.Equal(PreparedContractConstants.Completed, body.RootElement.GetProperty("status").GetString());
        Assert.True(body.RootElement.TryGetProperty("gedruckt_am", out _));
    }

    [Fact]
    public async Task UpdateAndStartSignature_UseRevisionAndServerSignatureValues()
    {
        var started = Item(5, PreparedContractConstants.ReadyForSignature, signatureRevision: 5, started: DateTimeOffset.Parse("2026-10-17T12:00:00Z"));
        var handler = new RecordingHandler(Response(Item(4)), Response(Item(5)), Response(started), Response(started));
        var repository = Repository(handler);
        await repository.UpdateDraftAsync(ItemId, 4, ChangedLeaseDraft());
        await repository.StartSignatureAsync(ItemId, 5);
        Assert.Contains("revision=eq.4&status=eq.vorbereitet&signatur_gestartet_am=is.null", handler.Requests[1].Uri);
        using var updateBody = JsonDocument.Parse(handler.Requests[1].Body); Assert.Equal(5, updateBody.RootElement.GetProperty("revision").GetInt32());
        Assert.Contains("rpc/start_prepared_contract_signature", handler.Requests[2].Uri);
        using var startBody = JsonDocument.Parse(handler.Requests[2].Body); Assert.Equal(ItemId, startBody.RootElement.GetProperty("p_kommunikationsvorgang_id").GetInt64()); Assert.Equal(5, startBody.RootElement.GetProperty("p_expected_revision").GetInt32());
        Assert.True(startBody.RootElement.TryGetProperty("p_assigned_to", out _));
    }

    [Fact]
    public async Task MarkFinalDocumentReadyForReview_StoresDocumentAndUsesFrozenRevision()
    {
        var current = Item(6, PreparedContractConstants.ReadyForSignature, 6, DateTimeOffset.Parse("2026-10-17T12:00:00Z"));
        var reviewed = current with { Status = PreparedContractConstants.FinalReview, FinalDokumentId = 77 };
        var handler = new RecordingHandler(Response(reviewed));
        var repository = Repository(handler);
        var result = await repository.MarkFinalDocumentReadyForReviewAsync(ItemId, 6, 77);
        Assert.Equal(PreparedContractConstants.FinalReview, result.Status); Assert.Equal(77, result.FinalDokumentId);
        Assert.Contains("status=eq.zur_signatur&revision=eq.6&signatur_revision=eq.6", handler.Requests[0].Uri);
        Assert.Contains("final_dokument_id=is.null", handler.Requests[0].Uri);
        using var body = JsonDocument.Parse(handler.Requests[0].Body); Assert.Equal("letzte_pruefung", body.RootElement.GetProperty("status").GetString()); Assert.Equal(77, body.RootElement.GetProperty("final_dokument_id").GetInt64());
    }

    [Fact]
    public async Task MarkFinalDocumentReadyForReview_RejectsMissingOrWrongServerResponse()
    {
        await Assert.ThrowsAsync<PreparedContractConflictException>(() => Repository(new RecordingHandler(Response())).MarkFinalDocumentReadyForReviewAsync(ItemId, 6, 77));
        var wrong = Item(6, PreparedContractConstants.ReadyForSignature, 6, DateTimeOffset.UtcNow) with { FinalDokumentId = 77 };
        await Assert.ThrowsAsync<PreparedContractConflictException>(() => Repository(new RecordingHandler(Response(wrong))).MarkFinalDocumentReadyForReviewAsync(ItemId, 6, 77));
    }

    [Fact]
    public async Task Complete_RequiresLastReviewAndFinalDocument()
    {
        var reviewed = Item(6, PreparedContractConstants.FinalReview, 6, DateTimeOffset.Parse("2026-10-17T12:00:00Z")) with { FinalDokumentId = 77 };
        var completed = reviewed with { Status = PreparedContractConstants.Completed };
        var handler = new RecordingHandler(Response(reviewed), Response(completed));
        var result = await Repository(handler).CompleteAsync(ItemId);
        Assert.Equal(PreparedContractConstants.Completed, result.Status);
        Assert.Contains("status=eq.letzte_pruefung&revision=eq.6&signatur_revision=eq.6&final_dokument_id=eq.77", handler.Requests[1].Uri);
    }

    [Fact]
    public async Task Complete_RejectsInvalidTransitionOrMissingServerConfirmation()
    {
        var signed = Item(6, PreparedContractConstants.ReadyForSignature, 6, DateTimeOffset.UtcNow) with { FinalDokumentId = 77 };
        await Assert.ThrowsAsync<PreparedContractConflictException>(() => Repository(new RecordingHandler(Response(signed))).CompleteAsync(ItemId));
        var reviewed = signed with { Status = PreparedContractConstants.FinalReview };
        await Assert.ThrowsAsync<PreparedContractConflictException>(() => Repository(new RecordingHandler(Response(reviewed), Response())).CompleteAsync(ItemId));
    }

    [Fact]
    public async Task FindActive_UsesServerFiltersAndReadsRawNumericResponse()
    {
        var handler = new RecordingHandler(RawMembershipRow());
        var result = await Repository(handler).FindActiveAsync("mitgliedsantrag", 101, null);
        var request = Assert.Single(handler.Requests);
        Assert.NotNull(result); Assert.Equal(123L, result.Id); Assert.Equal("Bearer token", request.Authorization);
        Assert.Contains("vorgangstyp=eq.vertrag_signatur", request.Uri); Assert.Contains("dokumenttyp=eq.mitgliedsantrag", request.Uri); Assert.Contains("mitglied_id=eq.101", request.Uri); Assert.Contains("parzelle_id=is.null", request.Uri); Assert.Contains("vorbereitet", request.Uri); Assert.Contains("zur_signatur", request.Uri);
        Assert.Contains("apikey", request.Headers, StringComparison.OrdinalIgnoreCase); Assert.DoesNotContain("service_role", request.Headers, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FindActive_EmptyAndMultipleRows_AreHandledSafely()
    {
        Assert.Null(await Repository(new RecordingHandler("[]")).FindActiveAsync("mitgliedsantrag", 101, null));
        await Assert.ThrowsAsync<PreparedContractConflictException>(() => Repository(new RecordingHandler("[" + RawMembershipRow()[1..^1] + "," + RawMembershipRow()[1..^1] + "]")).FindActiveAsync("mitgliedsantrag", 101, null));
    }

    [Fact]
    public async Task FindActive_ReadsPreparedAndSignatureRows()
    {
        var prepared = await Repository(new RecordingHandler(RawMembershipRow())).FindActiveAsync("mitgliedsantrag", 101, null);
        Assert.NotNull(prepared); Assert.Equal(123L, prepared.Id); Assert.Equal(101, prepared.MitgliedId); Assert.Null(prepared.ParzelleId); Assert.Equal("vorbereitet", prepared.Status); Assert.Equal(2, prepared.Revision);
        var signed = await Repository(new RecordingHandler(RawMembershipRow().Replace("\"status\":\"vorbereitet\",\"mitglied_id\"", "\"status\":\"zur_signatur\",\"mitglied_id\"").Replace("\"revision\":2,\"signatur_revision\":null,\"signatur_gestartet_am\":null", "\"revision\":3,\"signatur_revision\":3,\"signatur_gestartet_am\":\"2026-10-17T12:00:00Z\""))).FindActiveAsync("mitgliedsantrag", 101, null);
        Assert.Equal("zur_signatur", signed!.Status); Assert.Equal(3, signed.SignaturRevision);
    }

    [Fact]
    public async Task FindActive_ReadsFinalReviewRowsToPreventDuplicateContracts()
    {
        var finalReview = await Repository(new RecordingHandler(RawMembershipRow()
            .Replace("\"status\":\"vorbereitet\",\"mitglied_id\"", "\"status\":\"letzte_pruefung\",\"mitglied_id\"")))
            .FindActiveAsync("mitgliedsantrag", 101, null);

        Assert.NotNull(finalReview);
        Assert.Equal(PreparedContractConstants.FinalReview, finalReview!.Status);
    }

    [Theory]
    [InlineData("kgv-prepared-contract/v1", "mitgliedsantrag", "101", "null", "vertrag_signatur", false)]
    [InlineData("unknown", "mitgliedsantrag", "101", "null", "vertrag_signatur", true)]
    [InlineData("kgv-prepared-contract/v1", "pachtvertrag", "101", "null", "vertrag_signatur", true)]
    [InlineData("kgv-prepared-contract/v1", "mitgliedsantrag", "999", "null", "vertrag_signatur", true)]
    [InlineData("kgv-prepared-contract/v1", "mitgliedsantrag", "101", "22", "vertrag_signatur", true)]
    [InlineData("kgv-prepared-contract/v1", "mitgliedsantrag", "101", "null", "mail", true)]
    public async Task FindActive_RejectsInvalidRawServerData(string schema, string draftType, string draftMember, string parcel, string process, bool invalid)
    {
        var json = RawMembershipRow().Replace("kgv-prepared-contract/v1", schema).Replace("\"document_type\":\"mitgliedsantrag\"", $"\"document_type\":\"{draftType}\"").Replace("\"memberId\":101", $"\"memberId\":{draftMember}").Replace("\"parzelle_id\":null", $"\"parzelle_id\":{parcel}").Replace("\"vorgangstyp\":\"vertrag_signatur\"", $"\"vorgangstyp\":\"{process}\"");
        if (invalid) await Assert.ThrowsAsync<PreparedContractValidationException>(() => Repository(new RecordingHandler(json)).FindActiveAsync("mitgliedsantrag", 101, null));
    }

    private static PreparedContractRepository Repository(RecordingHandler handler) => new(new Factory(), new Auth(), () => new HttpClient(handler, false));
    private static JsonElement LeaseDraft() => JsonSerializer.SerializeToElement(new PreparedLeaseContractDraft { MemberId = 101, Draft = new LeaseContractDraft { StartDate = "2026-11-01", ParcelId = 22, HasPreviousContract = false, IncludeSecondaryMember = false, HandoverDate = "2026-11-01", FixedUntil = "", GardenRulesVersion = "2026", GardenRulesHandedOverAt = "2026-11-01", AdditionalAgreements = "", ClubSignatory = "" } });
    private static JsonElement MembershipDraft() => JsonSerializer.SerializeToElement(new PreparedMembershipApplicationDraft { MemberId = 101, StartDate = "2026-11-01", MemberFee = 1, IncludeSecondaryMember = false });
    private static JsonElement ChangedLeaseDraft() => JsonSerializer.SerializeToElement(new PreparedLeaseContractDraft { MemberId = 101, Draft = new LeaseContractDraft { StartDate = "2026-11-02", ParcelId = 22, HasPreviousContract = false, IncludeSecondaryMember = false, HandoverDate = "2026-11-01", FixedUntil = "", GardenRulesVersion = "2026", GardenRulesHandedOverAt = "2026-11-01", AdditionalAgreements = "", ClubSignatory = "" } });
    private static string Response(KommunikationsvorgangDto? item = null) => JsonSerializer.Serialize(item is null ? Array.Empty<KommunikationsvorgangDto>() : new[] { item });
    private static string RawMembershipRow() => "[{\"id\":123,\"vorgangstyp\":\"vertrag_signatur\",\"dokumenttyp\":\"mitgliedsantrag\",\"status\":\"vorbereitet\",\"mitglied_id\":101,\"parzelle_id\":null,\"entwurf_daten\":{\"schema_version\":\"kgv-prepared-contract/v1\",\"document_type\":\"mitgliedsantrag\",\"memberId\":101,\"startDate\":\"2026-11-01\",\"memberFee\":42.5,\"representative\":null,\"includeSecondaryMember\":false},\"revision\":2,\"signatur_revision\":null,\"signatur_gestartet_am\":null,\"zugewiesen_an\":null,\"erstellt_am\":\"2026-10-17T12:00:00Z\",\"bearbeitet_am\":\"2026-10-17T12:00:00Z\"}]";
    private static KommunikationsvorgangDto Item(int revision, string status = "vorbereitet", int? signatureRevision = null, DateTimeOffset? started = null) => new() { Id = ItemId, Vorgangstyp = "vertrag_signatur", Dokumenttyp = "pachtvertrag", Status = status, MitgliedId = 101, ParzelleId = 22, EntwurfDaten = LeaseDraft(), Revision = revision, SignaturRevision = signatureRevision, SignaturGestartetAm = started, ZugewiesenAn = UserId, ErstelltAm = DateTimeOffset.UtcNow, BearbeitetAm = DateTimeOffset.UtcNow };

    private sealed class RecordingHandler(params string[] responses) : HttpMessageHandler
    { public List<(HttpMethod Method, string Uri, string Authorization, string Prefer, string Body, string Headers)> Requests { get; } = []; private int _index;
      protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) { Requests.Add((request.Method, request.RequestUri!.PathAndQuery, request.Headers.Authorization?.ToString() ?? "", string.Join(",", request.Headers.GetValues("Prefer")), request.Content is null ? "" : await request.Content.ReadAsStringAsync(ct), string.Join(";", request.Headers.Select(x => x.Key)))); return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(responses[_index++], Encoding.UTF8, "application/json") }; } }
    private sealed class Factory : ISupabaseClientFactory { public string Url => "https://test.supabase.co"; public string Key => "anon"; public Task<Client> CreateAsync() => throw new NotSupportedException(); }
    private sealed class Auth : IAuthService { public bool IsVorstand => false; public bool IsAdmin => false; public string? CurrentUserId => UserId.ToString(); public Task<string?> GetAccessTokenAsync() => Task.FromResult<string?>("token"); public Task<Client> GetClientAsync() => throw new NotSupportedException(); public Task<bool> LoginAsync(string a,string b)=>throw new NotSupportedException(); public Task<List<AppUserDTO>> GetAppUsersAsync()=>throw new NotSupportedException(); public Task<List<MemberUserLinkStatusDto>> GetMemberUserLinkStatusesAsync()=>throw new NotSupportedException(); public Task<MemberUserLinkStatusDto?> GetMemberUserLinkStatusAsync(int x)=>throw new NotSupportedException(); public Task<InviteUserAccountResult> InviteUserAsync(AppUserDTO x)=>throw new NotSupportedException(); public Task<bool> RemoveUserAsync(AppUserDTO x)=>throw new NotSupportedException(); public Task<bool> ChangeEmailAsync(string x)=>throw new NotSupportedException(); public Task<bool> RequestEmailChangeAsync(string x)=>throw new NotSupportedException(); public Task<bool> VerifyEmailChangeOtpAsync(string a,string b)=>throw new NotSupportedException(); public Task<bool> SendPasswordResetEmailAsync(string x)=>throw new NotSupportedException(); public Task<bool> RequestOtpAsync(string x)=>throw new NotSupportedException(); public Task<bool> VerifyOtpAsync(string a,string b)=>throw new NotSupportedException(); public Task<bool> SetPasswordWithOtpAsync(string a,string b,string c)=>throw new NotSupportedException(); public Task<BiometricSessionTokens?> GetSessionTokensAsync()=>throw new NotSupportedException(); public Task<bool> RestoreSessionAsync(BiometricSessionTokens x)=>throw new NotSupportedException(); public Task LogoutAsync()=>throw new NotSupportedException(); }
    [Fact]
    public async Task Load_ValidatesDraftAndUsesBearerAuthorization()
    {
        var handler = new RecordingHandler(Response(Item(2)));
        var item = await Repository(handler).LoadAsync(ItemId);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method); Assert.Contains($"id=eq.{ItemId}", request.Uri);
        Assert.Equal("Bearer token", request.Authorization); Assert.Equal(ItemId, item.Id);
    }

    [Fact]
    public async Task Load_EmptyResultIsNotFound()
        => await Assert.ThrowsAsync<PreparedContractNotFoundException>(() => Repository(new RecordingHandler(Response())).LoadAsync(ItemId));

    [Fact]
    public async Task Load_InvalidOrMismatchedServerDraftIsRejected()
    {
        var invalid = Item(2) with { EntwurfDaten = JsonSerializer.SerializeToElement(new { schema_version = "unknown" }) };
        await Assert.ThrowsAsync<PreparedContractValidationException>(() => Repository(new RecordingHandler(Response(invalid))).LoadAsync(ItemId));
        var mismatch = Item(2) with { EntwurfDaten = MembershipDraft() };
        await Assert.ThrowsAsync<PreparedContractValidationException>(() => Repository(new RecordingHandler(Response(mismatch))).LoadAsync(ItemId));
    }
}

