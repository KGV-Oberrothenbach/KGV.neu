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

    [Fact]
    public async Task HistoricalLookups_UseAuthenticatedScopedRpcsAndMapDates()
    {
        var handler = new RecordingHandler(
            Json(HttpStatusCode.OK, """[{"id":2,"mitgliedsnummer":1042,"vorname":"Party","name":"B","mitglied_seit":"1990-01-01","aktiv":true}]"""),
            Json(HttpStatusCode.OK, """[{"pachtverhaeltnisId":12,"parzelleId":12,"gartenNr":"12","anlage":"Anlage","rolle":"hauptpaechter","gueltigAb":"1998-01-01","gueltigBis":null,"urspruenglicherPachtbeginn":"1998-01-01","beendetAm":null}]"""));
        var service = Service(handler);
        var member = Assert.Single(await service.GetHistoricalContractMembersAsync());
        Assert.Equal(1042L, member.Mitgliedsnummer);
        Assert.Equal(new DateTime(1990, 1, 1), member.MitgliedSeit);
        var lease = Assert.Single(await service.GetHistoricalContractLeaseRelationshipsAsync(2));
        Assert.Equal("12", lease.Parzellennummer);
        Assert.Equal("/rest/v1/rpc/get_historical_contract_members", handler.Requests[0].PathAndQuery);
        Assert.Equal("/rest/v1/rpc/get_historical_contract_lease_relationships", handler.Requests[1].PathAndQuery);
        Assert.All(handler.Requests, request => Assert.Equal("Bearer token", request.Authorization));
        using var body = JsonDocument.Parse(handler.Requests[1].Body);
        Assert.Equal(2, body.RootElement.GetProperty("p_mitglied_id").GetInt32());
    }

    [Fact]
    public async Task HistoricalResult_UsesStrictWorkflowDownloadRatherThanGeneralDocumentFallback()
    {
        var handler = new RecordingHandler(
            Json(HttpStatusCode.OK, """[{"id":42,"titel":"Altvertrag","dateiname":"vertrag.pdf","drive_file_id":"drive-42","mime_type":"application/pdf"}]"""),
            Bytes(new byte[] { 1, 2, 3 }));
        var result = await Service(handler).LoadHistoricalContractDocumentAsync(42);
        Assert.Equal(42, result.Document.Id);
        Assert.Equal(new byte[] { 1, 2, 3 }, result.Content);
        using var body = JsonDocument.Parse(handler.Requests[1].Body);
        Assert.Equal("download_historical_contract", body.RootElement.GetProperty("action").GetString());
        Assert.All(handler.Requests, request => Assert.Equal("Bearer token", request.Authorization));
    }

    [Theory]
    [InlineData(HistoricalContractDocumentTypes.LeaseFollowUpContract)]
    [InlineData(HistoricalContractDocumentTypes.MembershipAndLeaseInitialContract)]
    [InlineData(null)]
    public async Task Upload_ForwardsExplicitHistoricalContextWithoutChangingGenericUploads(string? contractType)
    {
        var handler = new RecordingHandler(Json(HttpStatusCode.OK,
            """{"document_id":42,"drive_file_id":"drive-42","storage_path":"KGV-APP/Dokumente/Mitglieder/2/Vertrag_2026-10-30_12-00-00.pdf","dateiname":"Vertrag_2026-10-30_12-00-00.pdf","mime_type":"application/pdf","size_bytes":3}"""));
        var result = await Service(handler).CreateDokumentAsync(new DokumentUploadRequest {
            MitgliedId = 2, Titel = "Vertrag", FileName = "vertrag.pdf", MimeType = "application/pdf", FileContent = [1, 2, 3], HistoricalOperationId = contractType == null ? null : Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"), HistoricalContractType = contractType });
        Assert.True(result.Success, result.Message);
        var request = Assert.Single(handler.Requests);
        Assert.Equal("Bearer token", request.Authorization);
        if (contractType == null) { Assert.DoesNotContain("historical_contract_type", request.Body); Assert.DoesNotContain("operation_id", request.Body); }
        else { Assert.Contains("historical_contract_type", request.Body); Assert.Contains(contractType, request.Body); Assert.Contains("operation_id", request.Body); Assert.Contains("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa", request.Body); }
    }

    [Fact]
    public async Task Upload_RejectsInvalidHistoricalContextBeforeSendingAndKeepsServerDenial()
    {
        var empty = new RecordingHandler();
        var invalid = await Service(empty).CreateDokumentAsync(new DokumentUploadRequest {
            ParzelleId = 12, HistoricalOperationId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"), HistoricalContractType = HistoricalContractDocumentTypes.LeaseFollowUpContract });
        Assert.False(invalid.Success); Assert.Empty(empty.Requests);
        var handler = new RecordingHandler(Json(HttpStatusCode.Forbidden, """{"message":"Altvertragsberechtigung fehlt","error_code":"FORBIDDEN","request_id":"test-request"}"""));
        var denied = await Service(handler).CreateDokumentAsync(new DokumentUploadRequest {
            MitgliedId = 2, Titel = "Vertrag", FileName = "vertrag.pdf", FileContent = [1], HistoricalOperationId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"), HistoricalContractType = HistoricalContractDocumentTypes.LeaseFollowUpContract });
        Assert.False(denied.Success); Assert.Contains("Altvertragsberechtigung", denied.Message);
    }

    [Fact]
    public async Task HistoricalArchiveLinks_UseScopedLookupForReopeningAfterReload()
    {
        var handler = new RecordingHandler(Json(HttpStatusCode.OK, """[{"documentId":42,"title":"Altvertrag","number":80037,"date":"2010-01-01","mimeType":"application/pdf"}]"""));
        var item = Assert.Single(await Service(handler).GetHistoricalContractDocumentsAsync(2));
        Assert.Equal(42, item.DocumentId); Assert.Equal(80037, item.Number); Assert.Equal(new DateTime(2010, 1, 1), item.Date);
        Assert.Equal("/rest/v1/rpc/get_historical_contract_documents", Assert.Single(handler.Requests).PathAndQuery);
    }

    [Fact]
    public async Task HistoricalRegistration_RetrySendsTheSameOperationAndMapsStoredIdentifiers()
    {
        const string json = """[{"dokument_id":42,"dokumenttyp_id":5,"pachtverhaeltnis_id":12,"pachtvertragsfassung_id":99,"historische_vertragsnummer":80037}]""";
        var handler = new RecordingHandler(Json(HttpStatusCode.OK, json), Json(HttpStatusCode.OK, json));
        var service = Service(handler);
        var id = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
        var original = new HistoricalContractRegistrationRequest(42, HistoricalContractDocumentTypes.LeaseFollowUpContract, new DateTime(2010,1,1), true, new long[] {12}, new Dictionary<long,DateTime>(), null, null, new long[] {2}, Array.Empty<HistoricalMembershipEnd>(), Array.Empty<HistoricalActivityStatusChange>(), id);
        var restored = JsonSerializer.Deserialize<HistoricalContractRegistrationRequest>(JsonSerializer.Serialize(original))!;
        var first = Assert.Single(await service.RegisterHistoricalContractAsync(original));
        var retry = Assert.Single(await service.RegisterHistoricalContractAsync(restored));
        Assert.Equal(first, retry); Assert.Equal(80037, first.HistorischeVertragsnummer); Assert.Equal(99, first.PachtvertragsfassungId);
        Assert.Equal(handler.Requests[0].Body, handler.Requests[1].Body);
        Assert.All(handler.Requests, request => { Assert.Contains(id.ToString(), request.Body); Assert.Equal("Bearer token", request.Authorization); });
    }

    [Fact]
    public async Task HistoricalOperation_ResolvesUnknownUploadResultWithOriginalId()
    {
        var handler = new RecordingHandler(Json(HttpStatusCode.OK, "null"), Json(HttpStatusCode.OK, """{"documentId":42,"registered":true}"""));
        var service = Service(handler); var id = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
        Assert.Null(await service.GetHistoricalContractOperationAsync(id));
        Assert.Equal(new HistoricalContractOperationState(42,true), await service.GetHistoricalContractOperationAsync(id));
        Assert.All(handler.Requests, request => { Assert.Equal("/rest/v1/rpc/get_historical_contract_operation", request.PathAndQuery); Assert.Contains(id.ToString(), request.Body); });
    }

    [Fact]
    public async Task HistoricalUpload_RejectsMissingOperationIdWithoutNetworkCall()
    {
        var handler = new RecordingHandler();
        var result = await Service(handler).CreateDokumentAsync(new DokumentUploadRequest { MitgliedId=2, Titel="Original", FileName="original.pdf", FileContent=[1], HistoricalContractType=HistoricalContractDocumentTypes.LeaseFollowUpContract });
        Assert.False(result.Success); Assert.Empty(handler.Requests);
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
