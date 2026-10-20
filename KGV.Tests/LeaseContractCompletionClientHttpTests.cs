using System.Net;
using System.Text;
using System.Text.Json;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Infrastructure.Services;
using Supabase;

namespace KGV.Tests;

public sealed class LeaseContractCompletionClientHttpTests
{
    [Fact]
    public async Task Complete_SendsAuthenticatedRequestAndAcceptsAlreadyCompletedResponse()
    {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"success\":true,\"pachtverhaeltnis_id\":7,\"pachtvertragsfassung_id\":9,\"bereits_abgeschlossen\":true}", Encoding.UTF8, "application/json") });

        var result = await Client(handler).CompleteAsync(42, 3);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method); Assert.Equal("/functions/v1/kgv-complete-prepared-lease-contract", request.Path);
        Assert.Equal("Bearer token", request.Authorization); Assert.Equal("anon", request.ApiKey); Assert.DoesNotContain("service_role", request.Headers, StringComparison.OrdinalIgnoreCase);
        using var body = JsonDocument.Parse(request.Body); Assert.Equal(42, body.RootElement.GetProperty("kommunikationsvorgang_id").GetInt64()); Assert.Equal(3, body.RootElement.GetProperty("expected_revision").GetInt32());
        Assert.Equal(7, result.PachtverhaeltnisId); Assert.Equal(9, result.PachtvertragsfassungId); Assert.True(result.BereitsAbgeschlossen);
    }

    [Fact]
    public async Task Complete_ReportsEdgeErrorAndDoesNotUseServiceRole()
    {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.Conflict) { Content = new StringContent("{\"message\":\"Revision veraltet\"}", Encoding.UTF8, "application/json") });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => Client(handler).CompleteAsync(42, 3));

        Assert.Contains("Revision veraltet", exception.Message); Assert.Single(handler.Requests);
    }

    private static LeaseContractCompletionClient Client(RecordingHandler handler) => new(new Factory(), new Auth(), () => new HttpClient(handler, false));
    private sealed class RecordingHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        public List<(HttpMethod Method, string Path, string Authorization, string ApiKey, string Headers, string Body)> Requests { get; } = [];
        private int _index;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add((request.Method, request.RequestUri!.PathAndQuery, request.Headers.Authorization?.ToString() ?? "", request.Headers.TryGetValues("apikey", out var key) ? key.Single() : "", string.Join(';', request.Headers.Select(x => x.Key)), request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken)));
            return responses[_index++];
        }
    }
    private sealed class Factory : ISupabaseClientFactory { public string Url => "https://test.supabase.co"; public string Key => "anon"; public Task<Client> CreateAsync() => throw new NotSupportedException(); }
    private sealed class Auth : IAuthService
    {
        public bool IsVorstand => false; public bool IsAdmin => false; public string? CurrentUserId => null; public Task<string?> GetAccessTokenAsync() => Task.FromResult<string?>("token"); public Task<Client> GetClientAsync() => throw new NotSupportedException(); public Task<bool> LoginAsync(string a, string b) => throw new NotSupportedException(); public Task<List<AppUserDTO>> GetAppUsersAsync() => throw new NotSupportedException(); public Task<List<MemberUserLinkStatusDto>> GetMemberUserLinkStatusesAsync() => throw new NotSupportedException(); public Task<MemberUserLinkStatusDto?> GetMemberUserLinkStatusAsync(int x) => throw new NotSupportedException(); public Task<InviteUserAccountResult> InviteUserAsync(AppUserDTO x) => throw new NotSupportedException(); public Task<bool> RemoveUserAsync(AppUserDTO x) => throw new NotSupportedException(); public Task<bool> ChangeEmailAsync(string x) => throw new NotSupportedException(); public Task<bool> RequestEmailChangeAsync(string x) => throw new NotSupportedException(); public Task<bool> VerifyEmailChangeOtpAsync(string a, string b) => throw new NotSupportedException(); public Task<bool> SetPasswordWithOtpAsync(string a, string b, string c) => throw new NotSupportedException(); public Task<bool> SendPasswordResetEmailAsync(string x) => throw new NotSupportedException(); public Task<bool> RequestOtpAsync(string x) => throw new NotSupportedException(); public Task<bool> VerifyOtpAsync(string a, string b) => throw new NotSupportedException(); public Task<BiometricSessionTokens?> GetSessionTokensAsync() => throw new NotSupportedException(); public Task<bool> RestoreSessionAsync(BiometricSessionTokens x) => throw new NotSupportedException(); public Task LogoutAsync() => throw new NotSupportedException();
    }
}
