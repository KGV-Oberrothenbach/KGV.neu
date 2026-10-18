using System.Net;
using System.Text;
using System.Text.Json;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Infrastructure.Services;
using Supabase;

namespace KGV.Tests;

public sealed class SupabaseDocumentLoadingHttpTests
{
    [Fact]
    public async Task LoadDokumentById_LoadsExactMetadataThenUsesExistingDriveDownload()
    {
        var handler = new RecordingHandler(
            Json(HttpStatusCode.OK, "[{\"id\":42,\"titel\":\"Finaler Antrag\",\"dateiname\":\"antrag.pdf\",\"drive_file_id\":\"drive-42\",\"mime_type\":\"application/pdf\",\"size_bytes\":3}]"),
            Bytes(new byte[] { 1, 2, 3 }));

        var result = await Service(handler).LoadDokumentByIdAsync(42);

        Assert.Equal(42, result.Document.Id);
        Assert.Equal("Finaler Antrag", result.Document.Title);
        Assert.Equal("antrag.pdf", result.Document.Dateiname);
        Assert.Equal("drive-42", result.Document.DriveFileId);
        Assert.Equal(new byte[] { 1, 2, 3 }, result.Content);
        Assert.Equal(2, handler.Requests.Count);
        var metadataRequest = handler.Requests[0];
        Assert.Equal(HttpMethod.Get, metadataRequest.Method);
        Assert.Equal("/rest/v1/dokument?id=eq.42&select=*", metadataRequest.PathAndQuery);
        Assert.Equal("Bearer token", metadataRequest.Authorization);
        Assert.Contains("apikey", metadataRequest.Headers, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("service_role", metadataRequest.Headers, StringComparison.OrdinalIgnoreCase);
        var downloadRequest = handler.Requests[1];
        Assert.Equal(HttpMethod.Post, downloadRequest.Method);
        Assert.Equal("/functions/v1/kgv-upload-document", downloadRequest.PathAndQuery);
        using var body = JsonDocument.Parse(downloadRequest.Body);
        Assert.Equal("download", body.RootElement.GetProperty("action").GetString());
        Assert.Equal(42, body.RootElement.GetProperty("document_id").GetInt64());
    }

    [Fact]
    public async Task LoadDokumentById_RejectsNonPositiveIdWithoutHttpCall()
    {
        var handler = new RecordingHandler();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Service(handler).LoadDokumentByIdAsync(0));
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task LoadDokumentById_ReportsNotFoundWithoutDownloadFallback()
    {
        var handler = new RecordingHandler(Json(HttpStatusCode.OK, "[]"));
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => Service(handler).LoadDokumentByIdAsync(42));
        Assert.Contains("42", exception.Message);
        Assert.Single(handler.Requests);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("[{\"id\":42},{\"id\":42}]")]
    [InlineData("[{\"id\":41}]")]
    public async Task LoadDokumentById_RejectsMalformedOrUnexpectedMetadataResponse(string responseBody)
    {
        var handler = new RecordingHandler(Json(HttpStatusCode.OK, responseBody));
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => Service(handler).LoadDokumentByIdAsync(42));
        Assert.Contains("Ungültige Serverantwort", exception.Message);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task LoadDokumentById_LeavesServerFailuresTechnical()
    {
        var handler = new RecordingHandler(Json(HttpStatusCode.InternalServerError, "{\"message\":\"Fehler\"}"));
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => Service(handler).LoadDokumentByIdAsync(42));
        Assert.Contains("500", exception.Message);
        Assert.Single(handler.Requests);
    }

    private static SupabaseService Service(RecordingHandler handler) => new(
        new Factory(), null, null, new Auth(), new TemplateProvider(), "https://test.supabase.co", "anon", () => new HttpClient(handler, false));

    private static HttpResponseMessage Json(HttpStatusCode statusCode, string content) => new(statusCode)
    {
        Content = new StringContent(content, Encoding.UTF8, "application/json")
    };

    private static HttpResponseMessage Bytes(byte[] content) => new(HttpStatusCode.OK)
    {
        Content = new ByteArrayContent(content)
    };

    private sealed class RecordingHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private int _index;
        public List<(HttpMethod Method, string PathAndQuery, string Authorization, string Headers, string Body)> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add((request.Method, request.RequestUri!.PathAndQuery, request.Headers.Authorization?.ToString() ?? string.Empty, string.Join(";", request.Headers.Select(x => x.Key)), request.Content == null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken)));
            return responses[_index++];
        }
    }

    private sealed class Factory : ISupabaseClientFactory
    {
        public string Url => "https://test.supabase.co";
        public string Key => "anon";
        public Task<Client> CreateAsync() => throw new NotSupportedException();
    }

    private sealed class TemplateProvider : IPdfTemplateProvider
    {
        public Task<PdfTemplateAsset> GetActiveAsync(PdfTemplateRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class Auth : IAuthService
    {
        public bool IsVorstand => false;
        public bool IsAdmin => false;
        public string? CurrentUserId => null;
        public Task<string?> GetAccessTokenAsync() => Task.FromResult<string?>("token");
        public Task<Client> GetClientAsync() => throw new NotSupportedException();
        public Task<bool> LoginAsync(string email, string password) => throw new NotSupportedException();
        public Task<List<AppUserDTO>> GetAppUsersAsync() => throw new NotSupportedException();
        public Task<List<MemberUserLinkStatusDto>> GetMemberUserLinkStatusesAsync() => throw new NotSupportedException();
        public Task<MemberUserLinkStatusDto?> GetMemberUserLinkStatusAsync(int mitgliedId) => throw new NotSupportedException();
        public Task<InviteUserAccountResult> InviteUserAsync(AppUserDTO dto) => throw new NotSupportedException();
        public Task<bool> RemoveUserAsync(AppUserDTO dto) => throw new NotSupportedException();
        public Task<bool> ChangeEmailAsync(string newEmail) => throw new NotSupportedException();
        public Task<bool> RequestEmailChangeAsync(string newEmail) => throw new NotSupportedException();
        public Task<bool> VerifyEmailChangeOtpAsync(string newEmail, string token) => throw new NotSupportedException();
        public Task<bool> SendPasswordResetEmailAsync(string email) => throw new NotSupportedException();
        public Task<bool> RequestOtpAsync(string email) => throw new NotSupportedException();
        public Task<bool> VerifyOtpAsync(string email, string token) => throw new NotSupportedException();
        public Task<bool> SetPasswordWithOtpAsync(string email, string token, string newPassword) => throw new NotSupportedException();
        public Task<BiometricSessionTokens?> GetSessionTokensAsync() => throw new NotSupportedException();
        public Task<bool> RestoreSessionAsync(BiometricSessionTokens tokens) => throw new NotSupportedException();
        public Task LogoutAsync() => throw new NotSupportedException();
    }
}
