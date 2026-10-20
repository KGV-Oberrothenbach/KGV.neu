using System.Text.Json;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Services;

namespace KGV.Tests;

public sealed class PreparedFinalDocumentReviewServiceTests
{
    [Fact]
    public async Task Open_LoadsExactlyPersistedFinalDocumentIdWithoutCompleting()
    {
        var context = Create();
        var result = await context.Service.OpenAsync(context.Contract);

        Assert.Equal(PreparedFinalDocumentReviewState.FinalDocumentLoaded, result.State);
        Assert.Equal(new long[] { 77 }, context.Loader.RequestedIds);
        Assert.Equal(0, context.Repository.CompleteCalls);
        Assert.Equal(77, result.Document!.Document.Id);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Open_RejectsMissingOrInvalidFinalDocumentIdWithoutFallbackOrComplete(long documentId)
    {
        var context = Create(documentId);

        await Assert.ThrowsAsync<PreparedContractValidationException>(() => context.Service.OpenAsync(context.Contract));

        Assert.Empty(context.Loader.RequestedIds);
        Assert.Equal(0, context.Repository.CompleteCalls);
    }

    [Fact]
    public async Task Reload_UsesTheSameFinalDocumentIdAgain()
    {
        var context = Create();
        await context.Service.OpenAsync(context.Contract);
        await context.Service.ReloadAsync();

        Assert.Equal(new long[] { 77, 77 }, context.Loader.RequestedIds);
        Assert.Equal(0, context.Repository.CompleteCalls);
    }

    [Fact]
    public async Task Complete_AfterSuccessfulLoad_CompletesOnceWithServerResult()
    {
        var context = Create();
        await context.Service.OpenAsync(context.Contract);
        context.Repository.CompleteResult = context.Contract with { Status = PreparedContractConstants.Completed };

        var result = await context.Service.CompleteAsync();

        Assert.Equal(PreparedFinalDocumentReviewState.Completed, result.State);
        Assert.Equal(1, context.Repository.CompleteCalls);
        Assert.Equal(PreparedContractConstants.Completed, result.Contract.Status);
        Assert.Equal(77, result.Contract.FinalDokumentId);
    }

    [Fact]
    public async Task Complete_LeaseUsesCompletionClientAndReloadsTheCompletedContract()
    {
        var context = Create(documentType: PreparedContractConstants.LeaseContract);
        await context.Service.OpenAsync(context.Contract);
        context.Repository.LoadResult = context.Contract with { Status = PreparedContractConstants.Completed };

        var result = await context.Service.CompleteAsync();

        Assert.Equal(PreparedFinalDocumentReviewState.Completed, result.State);
        Assert.Equal(0, context.Repository.CompleteCalls);
        Assert.Equal([(11L, 2)], context.LeaseCompletion.Calls);
    }

    [Fact]
    public async Task CompleteFailure_KeepsLoadedDocumentAndAllowsRetry()
    {
        var context = Create();
        await context.Service.OpenAsync(context.Contract);
        context.Repository.CompleteException = new PreparedContractConflictException("conflict");

        var failed = await context.Service.CompleteAsync();

        Assert.Equal(PreparedFinalDocumentReviewState.CompleteFailed, failed.State);
        Assert.Equal(77, failed.Contract.FinalDokumentId);
        Assert.NotNull(failed.Document);
        Assert.True(context.Service.CanComplete);
        context.Repository.CompleteException = null;
        context.Repository.CompleteResult = context.Contract with { Status = PreparedContractConstants.Completed };
        Assert.Equal(PreparedFinalDocumentReviewState.Completed, (await context.Service.CompleteAsync()).State);
        Assert.Equal(2, context.Repository.CompleteCalls);
        Assert.Equal(new long[] { 77 }, context.Loader.RequestedIds);
    }

    [Fact]
    public async Task Resume_ExistingFinalReviewLoadsFinalDocumentWithoutDraftOrSigningOperations()
    {
        var context = Create();

        var result = await context.Service.OpenAsync(context.Contract);

        Assert.Equal(PreparedFinalDocumentReviewState.FinalDocumentLoaded, result.State);
        Assert.Equal(new long[] { 77 }, context.Loader.RequestedIds);
        Assert.Equal(0, context.Repository.LoadCalls);
        Assert.Equal(0, context.Repository.StartCalls);
        Assert.Equal(0, context.Repository.CompleteCalls);
    }

    [Fact]
    public async Task LoadFailure_DisablesCompleteAndPreservesReviewContract()
    {
        var context = Create();
        context.Loader.Exception = new InvalidOperationException("download");

        var result = await context.Service.OpenAsync(context.Contract);

        Assert.Equal(PreparedFinalDocumentReviewState.FinalDocumentLoadFailed, result.State);
        Assert.Equal(77, result.Contract.FinalDokumentId);
        Assert.False(context.Service.CanComplete);
        Assert.Equal(0, context.Repository.CompleteCalls);
    }

    private static Context Create(long documentId = 77, string documentType = PreparedContractConstants.MembershipApplication)
    {
        var contract = new KommunikationsvorgangDto
        {
            Id = 11,
            Dokumenttyp = documentType,
            Status = PreparedContractConstants.FinalReview,
            MitgliedId = 7,
            Revision = 2,
            SignaturRevision = 2,
            SignaturGestartetAm = DateTimeOffset.UtcNow,
            FinalDokumentId = documentId,
            EntwurfDaten = JsonSerializer.SerializeToElement(new { })
        };
        var repository = new Repository();
        var loader = new Loader();
        var leaseCompletion = new LeaseCompletion();
        return new(contract, repository, loader, leaseCompletion, new PreparedFinalDocumentReviewService(repository, loader, leaseCompletion));
    }

    private sealed record Context(KommunikationsvorgangDto Contract, Repository Repository, Loader Loader, LeaseCompletion LeaseCompletion, PreparedFinalDocumentReviewService Service);

    private sealed class Loader : IFinalDocumentLoader
    {
        public List<long> RequestedIds { get; } = [];
        public Exception? Exception { get; set; }

        public Task<LoadedDocument> LoadDokumentByIdAsync(long documentId)
        {
            RequestedIds.Add(documentId);
            if (Exception is not null)
                return Task.FromException<LoadedDocument>(Exception);
            return Task.FromResult(new LoadedDocument(
                new DocumentInfo { Id = documentId, Title = "Finaldokument" },
                [(byte)'%', (byte)'P', (byte)'D', (byte)'F', (byte)'-', 1]));
        }
    }

    private sealed class Repository : IPreparedContractRepository
    {
        public int LoadCalls { get; private set; }
        public int StartCalls { get; private set; }
        public int CompleteCalls { get; private set; }
        public KommunikationsvorgangDto? CompleteResult { get; set; }
        public KommunikationsvorgangDto? LoadResult { get; set; }
        public Exception? CompleteException { get; set; }

        public Task<KommunikationsvorgangDto> CompleteAsync(long id, CancellationToken cancellationToken = default)
        {
            CompleteCalls++;
            if (CompleteException is not null)
                return Task.FromException<KommunikationsvorgangDto>(CompleteException);
            return Task.FromResult(CompleteResult ?? throw new Xunit.Sdk.XunitException("Complete result missing."));
        }

        public Task<KommunikationsvorgangDto> LoadAsync(long id, CancellationToken cancellationToken = default) { LoadCalls++; return Task.FromResult(LoadResult ?? throw new Xunit.Sdk.XunitException("LoadAsync must not be used for review resume.")); }
        public Task<KommunikationsvorgangDto> StartSignatureAsync(long id, int expectedRevision, CancellationToken cancellationToken = default) { StartCalls++; throw new Xunit.Sdk.XunitException("StartSignatureAsync must not be used for review resume."); }
        public Task<KommunikationsvorgangDto?> FindActiveAsync(string documentType, int memberId, int? parcelId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<KommunikationsvorgangDto> CreateAsync(string documentType, int memberId, int? parcelId, JsonElement draft, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<KommunikationsvorgangDto> ClaimAsync(long id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task ReleaseAsync(long id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<KommunikationsvorgangDto> UpdateDraftAsync(long id, int expectedRevision, JsonElement draft, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class LeaseCompletion : ILeaseContractCompletionClient
    {
        public List<(long Id, int Revision)> Calls { get; } = [];
        public Task<LeaseContractCompletionResult> CompleteAsync(long kommunikationsvorgangId, int expectedRevision, CancellationToken cancellationToken = default)
        {
            Calls.Add((kommunikationsvorgangId, expectedRevision));
            return Task.FromResult(new LeaseContractCompletionResult(1, 1, false));
        }
    }
}
