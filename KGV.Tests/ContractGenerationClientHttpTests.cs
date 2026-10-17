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

    private static ContractGenerationClient Client(RecordingHandler handler, string? token = "token") => new(new Factory(), new Auth(token), () => new HttpClient(handler, false));
    private static MitgliedsantragDokumentRequest Request() => new() { MitgliedId = 101, BeginnDatum = new DateTime(2026, 11, 1), Mitgliedsbeitrag = 42.5m, NebenmitgliedEinbeziehen = true };
    private static HttpResponseMessage Pdf() => new(HttpStatusCode.OK) { Content = new ByteArrayContent([1, 2, 3]) { Headers = { ContentType = new("application/pdf") } } };
    private sealed class RecordingHandler(params HttpResponseMessage[] responses) : HttpMessageHandler { public List<(HttpMethod Method, string Uri, string Authorization, string Headers, string ContentType, string Body)> Requests { get; } = []; private int _index; protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) { Requests.Add((request.Method, request.RequestUri!.PathAndQuery, request.Headers.Authorization?.ToString() ?? "", string.Join(';', request.Headers.Select(x => x.Key)), request.Content?.Headers.ContentType?.MediaType ?? "", request.Content is null ? "" : await request.Content.ReadAsStringAsync(ct))); return responses[_index++]; } }
    private sealed class Factory : ISupabaseClientFactory { public string Url => "https://test.supabase.co"; public string Key => "anon"; public Task<Client> CreateAsync() => throw new NotSupportedException(); }
    private sealed class Auth(string? token) : IAuthService { public bool IsVorstand => false; public bool IsAdmin => false; public string? CurrentUserId => null; public Task<string?> GetAccessTokenAsync() => Task.FromResult(token); public Task<Client> GetClientAsync()=>throw new NotSupportedException(); public Task<bool> LoginAsync(string a,string b)=>throw new NotSupportedException(); public Task<List<AppUserDTO>> GetAppUsersAsync()=>throw new NotSupportedException(); public Task<List<MemberUserLinkStatusDto>> GetMemberUserLinkStatusesAsync()=>throw new NotSupportedException(); public Task<MemberUserLinkStatusDto?> GetMemberUserLinkStatusAsync(int x)=>throw new NotSupportedException(); public Task<InviteUserAccountResult> InviteUserAsync(AppUserDTO x)=>throw new NotSupportedException(); public Task<bool> RemoveUserAsync(AppUserDTO x)=>throw new NotSupportedException(); public Task<bool> ChangeEmailAsync(string x)=>throw new NotSupportedException(); public Task<bool> RequestEmailChangeAsync(string x)=>throw new NotSupportedException(); public Task<bool> VerifyEmailChangeOtpAsync(string a,string b)=>throw new NotSupportedException(); public Task<bool> SendPasswordResetEmailAsync(string x)=>throw new NotSupportedException(); public Task<bool> RequestOtpAsync(string x)=>throw new NotSupportedException(); public Task<bool> VerifyOtpAsync(string a,string b)=>throw new NotSupportedException(); public Task<bool> SetPasswordWithOtpAsync(string a,string b,string c)=>throw new NotSupportedException(); public Task<BiometricSessionTokens?> GetSessionTokensAsync()=>throw new NotSupportedException(); public Task<bool> RestoreSessionAsync(BiometricSessionTokens x)=>throw new NotSupportedException(); public Task LogoutAsync()=>throw new NotSupportedException(); }
}
