using System.Text.Json;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Services;

namespace KGV.Tests;

public sealed class PreparedContractSigningStartServiceTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact] public async Task Prepared_membership_contract_starts_signature() { var contract = Contract(PreparedContractConstants.MembershipApplication); var repository = new Repository { StartResult = Started(contract) }; var result = await Service(repository).StartAsync(contract); Assert.Equal(PreparedContractConstants.ReadyForSignature, result.Status); Assert.Equal(1, repository.StartCalls); Assert.Equal(0, repository.CompleteCalls); }
    [Fact] public async Task Prepared_lease_contract_starts_signature() { var contract = Contract(PreparedContractConstants.LeaseContract); var repository = new Repository { StartResult = Started(contract) }; var result = await Service(repository).StartAsync(contract); Assert.Equal(PreparedContractConstants.LeaseContract, result.Dokumenttyp); Assert.Equal(1, repository.StartCalls); }
    [Fact] public async Task Uses_exact_current_revision() { var contract = Contract(PreparedContractConstants.LeaseContract) with { Revision = 7 }; var repository = new Repository { StartResult = Started(contract) }; await Service(repository).StartAsync(contract); Assert.Equal(contract.Id, repository.StartedId); Assert.Equal(7, repository.StartedRevision); }
    [Fact] public async Task Rejects_non_prepared_status() { var repository = new Repository(); await Assert.ThrowsAsync<PreparedContractValidationException>(() => Service(repository).StartAsync(Contract(PreparedContractConstants.LeaseContract) with { Status = PreparedContractConstants.ReadyForSignature })); Assert.Equal(0, repository.StartCalls); }
    [Fact] public async Task Rejects_existing_signature_revision() { var repository = new Repository(); await Assert.ThrowsAsync<PreparedContractValidationException>(() => Service(repository).StartAsync(Contract(PreparedContractConstants.LeaseContract) with { SignaturRevision = 2 })); Assert.Equal(0, repository.StartCalls); }
    [Fact] public async Task Rejects_existing_signature_started_at() { var repository = new Repository(); await Assert.ThrowsAsync<PreparedContractValidationException>(() => Service(repository).StartAsync(Contract(PreparedContractConstants.LeaseContract) with { SignaturGestartetAm = DateTimeOffset.UtcNow })); Assert.Equal(0, repository.StartCalls); }
    [Fact] public async Task Rejects_invalid_document_type() { var repository = new Repository(); await Assert.ThrowsAsync<PreparedContractValidationException>(() => Service(repository).StartAsync(Contract("unbekannt"))); Assert.Equal(0, repository.StartCalls); }
    [Fact] public async Task Validates_returned_id() => await RejectResponse(contract => Started(contract) with { Id = 999 });
    [Fact] public async Task Validates_returned_document_type() => await RejectResponse(contract => Started(contract) with { Dokumenttyp = PreparedContractConstants.MembershipApplication });
    [Fact] public async Task Validates_returned_member() => await RejectResponse(contract => Started(contract) with { MitgliedId = 999 });
    [Fact] public async Task Validates_returned_parcel() => await RejectResponse(contract => Started(contract) with { ParzelleId = 999 });
    [Fact] public async Task Validates_returned_status() => await RejectResponse(contract => Started(contract) with { Status = PreparedContractConstants.Prepared });
    [Fact] public async Task Validates_frozen_revision() => await RejectResponse(contract => Started(contract) with { SignaturRevision = contract.Revision + 1 });
    [Fact] public async Task Validates_signature_started_at() => await RejectResponse(contract => Started(contract) with { SignaturGestartetAm = null });
    [Fact] public async Task Propagates_repository_conflict_without_retry() { var contract = Contract(PreparedContractConstants.LeaseContract); var repository = new Repository { StartException = new PreparedContractConflictException("conflict") }; await Assert.ThrowsAsync<PreparedContractConflictException>(() => Service(repository).StartAsync(contract)); Assert.Equal(1, repository.StartCalls); Assert.Equal(0, repository.CompleteCalls); }

    private static async Task RejectResponse(Func<KommunikationsvorgangDto, KommunikationsvorgangDto> response)
    {
        var contract = Contract(PreparedContractConstants.LeaseContract); var repository = new Repository { StartResult = response(contract) };
        await Assert.ThrowsAsync<PreparedContractValidationException>(() => Service(repository).StartAsync(contract)); Assert.Equal(1, repository.StartCalls); Assert.Equal(0, repository.CompleteCalls);
    }

    private static PreparedContractSigningStartService Service(Repository repository) => new(repository);
    private static KommunikationsvorgangDto Contract(string documentType) => new() { Id = 123, Vorgangstyp = PreparedContractConstants.ContractSignatureProcess, Dokumenttyp = documentType, Status = PreparedContractConstants.Prepared, MitgliedId = 101, ParzelleId = documentType == PreparedContractConstants.LeaseContract ? 202 : null, EntwurfDaten = JsonSerializer.SerializeToElement(new { }), Revision = 5, ZugewiesenAn = UserId };
    private static KommunikationsvorgangDto Started(KommunikationsvorgangDto contract) => contract with { Status = PreparedContractConstants.ReadyForSignature, SignaturRevision = contract.Revision, SignaturGestartetAm = DateTimeOffset.Parse("2026-10-18T12:00:00Z") };

    private sealed class Repository : IPreparedContractRepository
    {
        public KommunikationsvorgangDto? StartResult { get; init; }
        public Exception? StartException { get; init; }
        public int StartCalls { get; private set; }
        public int CompleteCalls { get; private set; }
        public long StartedId { get; private set; }
        public int StartedRevision { get; private set; }

        public Task<KommunikationsvorgangDto> StartSignatureAsync(long id, int expectedRevision, CancellationToken cancellationToken = default) { StartCalls++; StartedId = id; StartedRevision = expectedRevision; return StartException is null ? Task.FromResult(StartResult ?? throw new Xunit.Sdk.XunitException("Start result missing.")) : Task.FromException<KommunikationsvorgangDto>(StartException); }
        public Task<KommunikationsvorgangDto> CompleteAsync(long id, CancellationToken cancellationToken = default) { CompleteCalls++; return Task.FromException<KommunikationsvorgangDto>(new Xunit.Sdk.XunitException("CompleteAsync must not be called.")); }
        public Task<KommunikationsvorgangDto> LoadAsync(long id, CancellationToken cancellationToken = default) => Fail<KommunikationsvorgangDto>();
        public Task<KommunikationsvorgangDto?> FindActiveAsync(string documentType, int memberId, int? parcelId, CancellationToken cancellationToken = default) => Fail<KommunikationsvorgangDto?>();
        public Task<KommunikationsvorgangDto> CreateAsync(string documentType, int memberId, int? parcelId, JsonElement draft, CancellationToken cancellationToken = default) => Fail<KommunikationsvorgangDto>();
        public Task<KommunikationsvorgangDto> ClaimAsync(long id, CancellationToken cancellationToken = default) => Fail<KommunikationsvorgangDto>();
        public Task ReleaseAsync(long id, CancellationToken cancellationToken = default) => Task.FromException(new Xunit.Sdk.XunitException("Unexpected repository operation."));
        public Task<KommunikationsvorgangDto> UpdateDraftAsync(long id, int expectedRevision, JsonElement draft, CancellationToken cancellationToken = default) => Fail<KommunikationsvorgangDto>();
        private static Task<T> Fail<T>() => Task.FromException<T>(new Xunit.Sdk.XunitException("Unexpected repository operation."));
    }
}
