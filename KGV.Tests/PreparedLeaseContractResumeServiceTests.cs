using System.Reflection;
using System.Text.Json;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Services;

namespace KGV.Tests;

public sealed class PreparedLeaseContractResumeServiceTests
{
    private const int MemberId = 101;
    private const int ParcelId = 202;
    private static readonly Guid CurrentUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ForeignUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task No_active_contract_returns_New()
    {
        var repository = RepositoryProxy.Create();

        var result = await Resolve(repository);

        Assert.Equal(PreparedLeaseContractResumeDecision.New, result.Decision);
        Assert.Null(result.Contract);
        Assert.Equal(1, repository.FindActiveCalls);
        Assert.Equal(0, repository.ClaimCalls);
        Assert.Equal(PreparedContractConstants.LeaseContract, repository.FindDocumentType);
        Assert.Equal(MemberId, repository.FindMemberId);
        Assert.Equal(ParcelId, repository.FindParcelId);
    }

    [Fact]
    public async Task Own_prepared_contract_returns_ResumeOwn()
    {
        var contract = Contract(PreparedContractConstants.Prepared, CurrentUserId);
        var repository = RepositoryProxy.Create();
        repository.ActiveContract = contract;

        var result = await Resolve(repository);

        Assert.Equal(PreparedLeaseContractResumeDecision.ResumeOwn, result.Decision);
        Assert.Same(contract, result.Contract);
        Assert.Equal(0, repository.ClaimCalls);
    }

    [Fact]
    public async Task Unassigned_prepared_contract_is_claimed()
    {
        var contract = Contract(PreparedContractConstants.Prepared, null);
        var claimed = contract with { ZugewiesenAn = CurrentUserId };
        var repository = RepositoryProxy.Create();
        repository.ActiveContract = contract;
        repository.ClaimResult = claimed;

        var result = await Resolve(repository);

        Assert.Equal(PreparedLeaseContractResumeDecision.ResumeClaimed, result.Decision);
        Assert.Same(claimed, result.Contract);
        Assert.Equal(1, repository.ClaimCalls);
        Assert.Equal(contract.Id, repository.ClaimedId);
    }

    [Fact]
    public async Task Foreign_assignment_returns_ForeignAssigned()
    {
        var repository = RepositoryProxy.Create();
        repository.ActiveContract = Contract(PreparedContractConstants.Prepared, ForeignUserId);

        var result = await Resolve(repository);

        Assert.Equal(PreparedLeaseContractResumeDecision.ForeignAssigned, result.Decision);
        Assert.Equal(0, repository.ClaimCalls);
    }

    [Fact]
    public async Task Ready_for_signature_returns_SignatureLocked()
    {
        var repository = RepositoryProxy.Create();
        repository.ActiveContract = Contract(PreparedContractConstants.ReadyForSignature, ForeignUserId);

        var result = await Resolve(repository);

        Assert.Equal(PreparedLeaseContractResumeDecision.SignatureLocked, result.Decision);
        Assert.Equal(0, repository.ClaimCalls);
    }

    [Fact]
    public async Task Unexpected_status_returns_InvalidStatus()
    {
        var repository = RepositoryProxy.Create();
        repository.ActiveContract = Contract(PreparedContractConstants.Completed, null);

        var result = await Resolve(repository);

        Assert.Equal(PreparedLeaseContractResumeDecision.InvalidStatus, result.Decision);
        Assert.Equal(0, repository.ClaimCalls);
    }

    [Fact]
    public async Task Claim_conflict_returns_ClaimConflict()
    {
        var contract = Contract(PreparedContractConstants.Prepared, null);
        var repository = RepositoryProxy.Create();
        repository.ActiveContract = contract;
        repository.ClaimException = new PreparedContractConflictException("already claimed");

        var result = await Resolve(repository);

        Assert.Equal(PreparedLeaseContractResumeDecision.ClaimConflict, result.Decision);
        Assert.Same(contract, result.Contract);
        Assert.Equal(1, repository.ClaimCalls);
    }

    [Fact]
    public async Task Claim_wrong_id_throws()
    {
        await AssertInvalidClaim(Contract(PreparedContractConstants.Prepared, CurrentUserId, id: 124));
    }

    [Fact]
    public async Task Claim_wrong_document_type_throws()
    {
        await AssertInvalidClaim(Contract(PreparedContractConstants.Prepared, CurrentUserId) with { Dokumenttyp = PreparedContractConstants.MembershipApplication });
    }

    [Fact]
    public async Task Claim_wrong_member_throws()
    {
        await AssertInvalidClaim(Contract(PreparedContractConstants.Prepared, CurrentUserId) with { MitgliedId = MemberId + 1 });
    }

    [Fact]
    public async Task Claim_wrong_parcel_throws()
    {
        await AssertInvalidClaim(Contract(PreparedContractConstants.Prepared, CurrentUserId) with { ParzelleId = ParcelId + 1 });
    }

    [Fact]
    public async Task Claim_wrong_status_throws()
    {
        await AssertInvalidClaim(Contract(PreparedContractConstants.ReadyForSignature, CurrentUserId));
    }

    [Fact]
    public async Task Claim_wrong_assignment_throws()
    {
        await AssertInvalidClaim(Contract(PreparedContractConstants.Prepared, ForeignUserId));
    }

    [Fact]
    public async Task Invalid_current_user_id_throws()
    {
        var repository = RepositoryProxy.Create();

        await Assert.ThrowsAsync<ArgumentException>(() => new PreparedLeaseContractResumeService(repository).ResolveAsync(MemberId, ParcelId, Guid.Empty));

        Assert.Equal(0, repository.FindActiveCalls);
        Assert.Equal(0, repository.ClaimCalls);
    }

    private static async Task AssertInvalidClaim(KommunikationsvorgangDto claimed)
    {
        var repository = RepositoryProxy.Create();
        repository.ActiveContract = Contract(PreparedContractConstants.Prepared, null);
        repository.ClaimResult = claimed;

        await Assert.ThrowsAsync<PreparedContractValidationException>(() => Resolve(repository));

        Assert.Equal(1, repository.FindActiveCalls);
        Assert.Equal(1, repository.ClaimCalls);
    }

    private static Task<PreparedLeaseContractResumeResult> Resolve(RepositoryProxy repository)
        => new PreparedLeaseContractResumeService(repository).ResolveAsync(MemberId, ParcelId, CurrentUserId, CancellationToken.None);

    private static KommunikationsvorgangDto Contract(string status, Guid? assignedUser, long id = 123) => new()
    {
        Id = id,
        Dokumenttyp = PreparedContractConstants.LeaseContract,
        MitgliedId = MemberId,
        ParzelleId = ParcelId,
        Status = status,
        ZugewiesenAn = assignedUser,
        EntwurfDaten = JsonSerializer.SerializeToElement(new { })
    };

    private sealed class RepositoryProxy : DispatchProxy
    {
        public KommunikationsvorgangDto? ActiveContract { get; set; }
        public KommunikationsvorgangDto? ClaimResult { get; set; }
        public Exception? ClaimException { get; set; }
        public int FindActiveCalls { get; private set; }
        public int ClaimCalls { get; private set; }
        public string? FindDocumentType { get; private set; }
        public int? FindMemberId { get; private set; }
        public int? FindParcelId { get; private set; }
        public long? ClaimedId { get; private set; }

        public static RepositoryProxy Create() => (RepositoryProxy)(object)DispatchProxy.Create<IPreparedContractRepository, RepositoryProxy>();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            return targetMethod!.Name switch
            {
                nameof(IPreparedContractRepository.FindActiveAsync) => FindActive(args!),
                nameof(IPreparedContractRepository.ClaimAsync) => Claim(args!),
                _ => throw new Xunit.Sdk.XunitException("Unexpected repository operation.")
            };
        }

        private Task<KommunikationsvorgangDto?> FindActive(object?[] args)
        {
            FindActiveCalls++;
            FindDocumentType = (string)args[0]!;
            FindMemberId = (int)args[1]!;
            FindParcelId = (int?)args[2];
            return Task.FromResult(ActiveContract);
        }

        private Task<KommunikationsvorgangDto> Claim(object?[] args)
        {
            ClaimCalls++;
            ClaimedId = (long)args[0]!;
            if (ClaimException is not null) return Task.FromException<KommunikationsvorgangDto>(ClaimException);
            return Task.FromResult(ClaimResult ?? throw new Xunit.Sdk.XunitException("ClaimAsync requires a configured result."));
        }
    }
}
