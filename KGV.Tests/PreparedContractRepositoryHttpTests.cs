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
    private static readonly Guid ItemId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task Create_UsesAuthenticatedPostgrestContractWithoutSignatures()
    {
        var handler = new RecordingHandler(Response(Item(7)));
        var repository = Repository(handler);
        await repository.CreateAsync(PreparedContractConstants.LeaseContract, 101, 22, LeaseDraft(), TestContext.Current.CancellationToken);
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
        Assert.Contains("id=eq.22222222-2222-2222-2222-222222222222&zugewiesen_an=is.null", handler.Requests[0].Uri);
        Assert.Contains("zugewiesen_an=eq.11111111-1111-1111-1111-111111111111", handler.Requests[1].Uri);
    }

    [Fact]
    public async Task UpdateAndStartSignature_UseRevisionAndServerSignatureValues()
    {
        var current = Item(4, PreparedContractConstants.Prepared, signatureRevision: null, started: null);
        var started = Item(5, PreparedContractConstants.ReadyForSignature, signatureRevision: 5, started: DateTimeOffset.Parse("2026-10-17T12:00:00Z"));
        var handler = new RecordingHandler(Response(Item(5)), Response(started), Response(started));
        var repository = Repository(handler);
        await repository.UpdateDraftAsync(ItemId, 4, ChangedLeaseDraft());
        await repository.StartSignatureAsync(ItemId, 5);
        Assert.Contains("revision=eq.4&status=eq.vorbereitet&signatur_gestartet_am=is.null", handler.Requests[0].Uri);
        Assert.Contains("revision=eq.5&status=eq.vorbereitet", handler.Requests[1].Uri); Assert.Contains("signatur_revision=is.null", handler.Requests[1].Uri);
        using var startBody = JsonDocument.Parse(handler.Requests[1].Body); Assert.Equal("zur_signatur", startBody.RootElement.GetProperty("status").GetString());
        Assert.DoesNotContain("signatur_", startBody.RootElement.GetRawText(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Complete_UsesFrozenRevisionAndZeroRowsIsConflict()
    {
        var current = Item(6, PreparedContractConstants.ReadyForSignature, 6, DateTimeOffset.Parse("2026-10-17T12:00:00Z"));
        var handler = new RecordingHandler(Response(current), Response());
        var repository = Repository(handler);
        await Assert.ThrowsAsync<PreparedContractConflictException>(() => repository.CompleteAsync(ItemId));
        Assert.Contains("status=eq.zur_signatur&signatur_revision=eq.6", handler.Requests[1].Uri);
    }

    private static PreparedContractRepository Repository(RecordingHandler handler) => new(new Factory(), new Auth(), () => new HttpClient(handler, false));
    private static JsonElement LeaseDraft() => JsonSerializer.SerializeToElement(new PreparedLeaseContractDraft { MemberId = 101, Draft = new LeaseContractDraft { StartDate = "2026-11-01", ParcelId = 22, HasPreviousContract = false, IncludeSecondaryMember = false, HandoverDate = "2026-11-01", FixedUntil = "", GardenRulesVersion = "2026", GardenRulesHandedOverAt = "2026-11-01", AdditionalAgreements = "", ClubSignatory = "" } });
    private static JsonElement MembershipDraft() => JsonSerializer.SerializeToElement(new PreparedMembershipApplicationDraft { MemberId = 101, StartDate = "2026-11-01", MemberFee = 1, IncludeSecondaryMember = false });
    private static JsonElement ChangedLeaseDraft() => JsonSerializer.SerializeToElement(new PreparedLeaseContractDraft { MemberId = 101, Draft = new LeaseContractDraft { StartDate = "2026-11-02", ParcelId = 22, HasPreviousContract = false, IncludeSecondaryMember = false, HandoverDate = "2026-11-01", FixedUntil = "", GardenRulesVersion = "2026", GardenRulesHandedOverAt = "2026-11-01", AdditionalAgreements = "", ClubSignatory = "" } });
    private static string Response(KommunikationsvorgangDto? item = null) => JsonSerializer.Serialize(item is null ? Array.Empty<KommunikationsvorgangDto>() : new[] { item });
    private static KommunikationsvorgangDto Item(int revision, string status = "vorbereitet", int? signatureRevision = null, DateTimeOffset? started = null) => new() { Id = ItemId, Vorgangstyp = "vertrag_signatur", Dokumenttyp = "pachtvertrag", Status = status, MitgliedId = 101, ParzelleId = 22, EntwurfDaten = LeaseDraft(), Revision = revision, SignaturRevision = signatureRevision, SignaturGestartetAm = started, ZugewiesenAn = UserId, ErstelltAm = DateTimeOffset.UtcNow, BearbeitetAm = DateTimeOffset.UtcNow };

    private sealed class RecordingHandler(params string[] responses) : HttpMessageHandler
    { public List<(HttpMethod Method, string Uri, string Authorization, string Prefer, string Body, string Headers)> Requests { get; } = []; private int _index;
      protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) { Requests.Add((request.Method, request.RequestUri!.PathAndQuery, request.Headers.Authorization?.ToString() ?? "", string.Join(",", request.Headers.GetValues("Prefer")), request.Content is null ? "" : await request.Content.ReadAsStringAsync(ct), string.Join(";", request.Headers.Select(x => x.Key)))); return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(responses[_index++], Encoding.UTF8, "application/json") }; } }
    private sealed class Factory : ISupabaseClientFactory { public string Url => "https://test.supabase.co"; public string Key => "anon"; public Task<Client> CreateAsync() => throw new NotSupportedException(); }
    private sealed class Auth : IAuthService { public bool IsVorstand => false; public bool IsAdmin => false; public string? CurrentUserId => UserId.ToString(); public Task<string?> GetAccessTokenAsync() => Task.FromResult<string?>("token"); public Task<Client> GetClientAsync() => throw new NotSupportedException(); public Task<bool> LoginAsync(string a,string b)=>throw new NotSupportedException(); public Task<List<AppUserDTO>> GetAppUsersAsync()=>throw new NotSupportedException(); public Task<List<MemberUserLinkStatusDto>> GetMemberUserLinkStatusesAsync()=>throw new NotSupportedException(); public Task<MemberUserLinkStatusDto?> GetMemberUserLinkStatusAsync(int x)=>throw new NotSupportedException(); public Task<InviteUserAccountResult> InviteUserAsync(AppUserDTO x)=>throw new NotSupportedException(); public Task<bool> RemoveUserAsync(AppUserDTO x)=>throw new NotSupportedException(); public Task<bool> ChangeEmailAsync(string x)=>throw new NotSupportedException(); public Task<bool> RequestEmailChangeAsync(string x)=>throw new NotSupportedException(); public Task<bool> VerifyEmailChangeOtpAsync(string a,string b)=>throw new NotSupportedException(); public Task<bool> SendPasswordResetEmailAsync(string x)=>throw new NotSupportedException(); public Task<bool> RequestOtpAsync(string x)=>throw new NotSupportedException(); public Task<bool> VerifyOtpAsync(string a,string b)=>throw new NotSupportedException(); public Task<bool> SetPasswordWithOtpAsync(string a,string b,string c)=>throw new NotSupportedException(); public Task<BiometricSessionTokens?> GetSessionTokensAsync()=>throw new NotSupportedException(); public Task<bool> RestoreSessionAsync(BiometricSessionTokens x)=>throw new NotSupportedException(); public Task LogoutAsync()=>throw new NotSupportedException(); }
}
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

