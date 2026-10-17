using System.Text.Json;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Services;

namespace KGV.Tests;

public sealed class PreparedMembershipApplicationResumeServiceTests
{
    private static readonly Guid CurrentUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ForeignUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task New_WhenNoActiveContractExists()
    {
        var stub = new RepositoryStub();
        var result = await Resolve(stub);

        Assert.Equal(PreparedMembershipApplicationResumeDecision.New, result.Decision);
        Assert.Null(result.Contract);
        Assert.Equal(1, stub.FindActiveCalls); Assert.Equal(0, stub.ClaimCalls);
    }

    [Fact]
    public async Task ResumeOwn_WhenPreparedContractIsAssignedToCurrentUser()
    {
        var contract = Contract(PreparedContractConstants.Prepared, CurrentUserId);
        var stub = new RepositoryStub { ActiveContract = contract };
        var result = await Resolve(stub);

        Assert.Equal(PreparedMembershipApplicationResumeDecision.ResumeOwn, result.Decision);
        Assert.Same(contract, result.Contract);
        Assert.Equal(1, stub.FindActiveCalls); Assert.Equal(0, stub.ClaimCalls);
    }

    [Fact]
    public async Task ResumeClaimed_WhenUnassignedPreparedContractIsClaimed()
    {
        var contract = Contract(PreparedContractConstants.Prepared, null);
        var claimed = contract with { ZugewiesenAn = CurrentUserId };
        var stub = new RepositoryStub { ActiveContract = contract, ClaimResult = claimed };
        var result = await Resolve(stub);

        Assert.Equal(PreparedMembershipApplicationResumeDecision.ResumeClaimed, result.Decision);
        Assert.Same(claimed, result.Contract);
        Assert.Equal(1, stub.FindActiveCalls); Assert.Equal(1, stub.ClaimCalls); Assert.Equal(contract.Id, stub.ClaimedId);
    }

    [Fact]
    public async Task ClaimConflict_WhenClaimIsAlreadyTaken()
    {
        var contract = Contract(PreparedContractConstants.Prepared, null);
        var stub = new RepositoryStub { ActiveContract = contract, ClaimException = new PreparedContractConflictException("already claimed") };
        var result = await Resolve(stub);

        Assert.Equal(PreparedMembershipApplicationResumeDecision.ClaimConflict, result.Decision);
        Assert.Same(contract, result.Contract);
        Assert.Equal(1, stub.FindActiveCalls); Assert.Equal(1, stub.ClaimCalls);
    }

    [Fact]
    public async Task ForeignAssigned_WhenPreparedContractBelongsToAnotherUser()
    {
        var stub = new RepositoryStub { ActiveContract = Contract(PreparedContractConstants.Prepared, ForeignUserId) };
        var result = await Resolve(stub);

        Assert.Equal(PreparedMembershipApplicationResumeDecision.ForeignAssigned, result.Decision);
        Assert.Equal(1, stub.FindActiveCalls); Assert.Equal(0, stub.ClaimCalls);
    }

    [Fact]
    public async Task SignatureLocked_WhenContractIsReadyForSignature()
    {
        var stub = new RepositoryStub { ActiveContract = Contract(PreparedContractConstants.ReadyForSignature, ForeignUserId) };
        var result = await Resolve(stub);

        Assert.Equal(PreparedMembershipApplicationResumeDecision.SignatureLocked, result.Decision);
        Assert.Equal(1, stub.FindActiveCalls); Assert.Equal(0, stub.ClaimCalls);
    }

    [Fact]
    public async Task InvalidStatus_WhenActiveContractHasOtherStatus()
    {
        var stub = new RepositoryStub { ActiveContract = Contract(PreparedContractConstants.Completed, null) };
        var result = await Resolve(stub);

        Assert.Equal(PreparedMembershipApplicationResumeDecision.InvalidStatus, result.Decision);
        Assert.Equal(1, stub.FindActiveCalls); Assert.Equal(0, stub.ClaimCalls);
    }

    [Fact]
    public async Task EmptyUserId_IsRejectedWithoutRepositoryCall()
    {
        var stub = new RepositoryStub();

        await Assert.ThrowsAsync<ArgumentException>(() => new PreparedMembershipApplicationResumeService(stub).ResolveAsync(101, Guid.Empty));

        Assert.Equal(0, stub.FindActiveCalls); Assert.Equal(0, stub.ClaimCalls);
    }

    [Theory]
    [InlineData(124L, "vorbereitet", "11111111-1111-1111-1111-111111111111")]
    [InlineData(123L, "zur_signatur", "11111111-1111-1111-1111-111111111111")]
    [InlineData(123L, "vorbereitet", "22222222-2222-2222-2222-222222222222")]
    public async Task InvalidClaimResponse_IsRejected(long id, string status, string assignedUser)
    {
        var stub = new RepositoryStub
        {
            ActiveContract = Contract(PreparedContractConstants.Prepared, null),
            ClaimResult = Contract(status, Guid.Parse(assignedUser), id)
        };

        await Assert.ThrowsAsync<PreparedContractValidationException>(() => Resolve(stub));

        Assert.Equal(1, stub.FindActiveCalls); Assert.Equal(1, stub.ClaimCalls);
    }

    private static Task<PreparedMembershipApplicationResumeResult> Resolve(RepositoryStub stub)
        => new PreparedMembershipApplicationResumeService(stub).ResolveAsync(101, CurrentUserId, CancellationToken.None);

    private static KommunikationsvorgangDto Contract(string status, Guid? assignedUser, long id = 123) => new()
    {
        Id = id,
        Status = status,
        ZugewiesenAn = assignedUser,
        EntwurfDaten = JsonSerializer.SerializeToElement(new { })
    };

    private sealed class RepositoryStub : IPreparedContractRepository
    {
        public KommunikationsvorgangDto? ActiveContract { get; init; }
        public KommunikationsvorgangDto? ClaimResult { get; init; }
        public Exception? ClaimException { get; init; }
        public int FindActiveCalls { get; private set; }
        public int ClaimCalls { get; private set; }
        public long? ClaimedId { get; private set; }

        public Task<KommunikationsvorgangDto?> FindActiveAsync(string documentType, int memberId, int? parcelId, CancellationToken cancellationToken = default)
        {
            FindActiveCalls++;
            Assert.Equal(PreparedContractConstants.MembershipApplication, documentType); Assert.Equal(101, memberId); Assert.Null(parcelId);
            return Task.FromResult(ActiveContract);
        }

        public Task<KommunikationsvorgangDto> ClaimAsync(long id, CancellationToken cancellationToken = default)
        {
            ClaimCalls++; ClaimedId = id;
            if (ClaimException is not null) return Task.FromException<KommunikationsvorgangDto>(ClaimException);
            return Task.FromResult(ClaimResult ?? throw new Xunit.Sdk.XunitException("ClaimAsync requires a configured result."));
        }

        public Task<KommunikationsvorgangDto> LoadAsync(long id, CancellationToken cancellationToken = default) => Fail<KommunikationsvorgangDto>();
        public Task<KommunikationsvorgangDto> CreateAsync(string documentType, int memberId, int? parcelId, JsonElement draft, CancellationToken cancellationToken = default) => Fail<KommunikationsvorgangDto>();
        public Task ReleaseAsync(long id, CancellationToken cancellationToken = default) => Fail();
        public Task<KommunikationsvorgangDto> UpdateDraftAsync(long id, int expectedRevision, JsonElement draft, CancellationToken cancellationToken = default) => Fail<KommunikationsvorgangDto>();
        public Task<KommunikationsvorgangDto> StartSignatureAsync(long id, int expectedRevision, CancellationToken cancellationToken = default) => Fail<KommunikationsvorgangDto>();
        public Task<KommunikationsvorgangDto> CompleteAsync(long id, CancellationToken cancellationToken = default) => Fail<KommunikationsvorgangDto>();

        private static Task Fail() => Task.FromException(new Xunit.Sdk.XunitException("Unexpected repository write."));
        private static Task<T> Fail<T>() => Task.FromException<T>(new Xunit.Sdk.XunitException("Unexpected repository operation."));
    }
}
