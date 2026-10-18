using System.Text.Json;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Services;

namespace KGV.Tests;

public sealed class PreparedMembershipApplicationRecoveryServiceTests
{
    [Fact]
    public async Task Signed_membership_application_is_moved_to_final_review_without_starting_signatures()
    {
        var repository = new Repository { Document = new(42, 7, 3) };
        var service = new PreparedMembershipApplicationRecoveryService(repository);

        var result = await service.RecoverAsync(ReadyContract(), []);

        Assert.NotNull(result);
        Assert.Equal(PreparedContractConstants.FinalReview, result.Status);
        Assert.Equal(42, result.FinalDokumentId);
        Assert.Equal(1, repository.MarkCalls);
        Assert.Equal(0, repository.StartCalls);
    }

    [Fact]
    public async Task No_signed_document_leaves_contract_unchanged()
    {
        var repository = new Repository();
        var service = new PreparedMembershipApplicationRecoveryService(repository);

        var result = await service.RecoverAsync(ReadyContract(), []);

        Assert.Null(result);
        Assert.Equal(0, repository.MarkCalls);
        Assert.Equal(0, repository.StartCalls);
    }

    [Fact]
    public async Task Existing_final_document_is_not_recovered_again()
    {
        var repository = new Repository { Document = new(42, 7, 3) };
        var service = new PreparedMembershipApplicationRecoveryService(repository);

        var result = await service.RecoverAsync(ReadyContract() with { FinalDokumentId = 41 }, []);

        Assert.Null(result);
        Assert.Equal(0, repository.MarkCalls);
    }

    private static KommunikationsvorgangDto ReadyContract() => new()
    {
        Id = 7,
        Dokumenttyp = PreparedContractConstants.MembershipApplication,
        Status = PreparedContractConstants.ReadyForSignature,
        MitgliedId = 9,
        Revision = 3,
        SignaturRevision = 3,
        SignaturGestartetAm = DateTimeOffset.UtcNow
    };

    private sealed class Repository : IPreparedContractRepository
    {
        public FinalDocumentProvenance? Document { get; init; }
        public int MarkCalls { get; private set; }
        public int StartCalls { get; private set; }
        public Task<KommunikationsvorgangDto> MarkFinalDocumentReadyForReviewAsync(long id, int expectedRevision, long finalDocumentId, CancellationToken cancellationToken = default)
        {
            MarkCalls++;
            return Task.FromResult(ReadyContract() with { Id = id, Revision = expectedRevision, Status = PreparedContractConstants.FinalReview, FinalDokumentId = finalDocumentId });
        }
        public Task<KommunikationsvorgangDto> StartSignatureAsync(long id, int expectedRevision, CancellationToken cancellationToken = default) { StartCalls++; throw new InvalidOperationException(); }
        public Task<FinalDocumentProvenance?> FindFinalDocumentByProvenanceAsync(long sourceKommunikationsvorgangId, int sourceRevision, CancellationToken cancellationToken = default) => Task.FromResult(Document);
        public Task<KommunikationsvorgangDto> LoadAsync(long id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<KommunikationsvorgangDto?> FindActiveAsync(string documentType, int memberId, int? parcelId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<KommunikationsvorgangDto> CreateAsync(string documentType, int memberId, int? parcelId, JsonElement draft, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<KommunikationsvorgangDto> ClaimAsync(long id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task ReleaseAsync(long id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<KommunikationsvorgangDto> UpdateDraftAsync(long id, int expectedRevision, JsonElement draft, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<KommunikationsvorgangDto> CompleteAsync(long id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
