using KGV.Core.Models;
using KGV.Core.Services;

namespace KGV.Tests;

public sealed class PreparedLeaseContractSignaturePlanBuilderTests
{
    [Fact] public void Adult_without_co_tenant_has_two_signatures() => Assert.Equal(2, PreparedLeaseContractSignaturePlanBuilder.Build(Draft(false), false, false).Count);
    [Fact] public void Adult_with_explicit_co_tenant_has_three_signatures() => Assert.Equal(3, PreparedLeaseContractSignaturePlanBuilder.Build(Draft(true), false, false).Count);
    [Fact] public void Minor_requires_representative_and_has_three_signatures() { Assert.Equal(3, PreparedLeaseContractSignaturePlanBuilder.Build(Draft(false), true, true).Count); Assert.Throws<PreparedContractValidationException>(() => PreparedLeaseContractSignaturePlanBuilder.Build(Draft(false), true, false)); }
    [Fact]
    public void Persistent_plan_has_two_required_board_slots()
    {
        var plan = PreparedLeaseContractSignaturePlanBuilder.BuildPersistent(Draft(true), false, false);
        Assert.Equal(new[] { LeaseContractSignatureKeys.Member, LeaseContractSignatureKeys.SecondParty, PreparedContractBoardSignatureKeys.Board1, PreparedContractBoardSignatureKeys.Board2 }, plan.Select(x => x.Key));
        Assert.Equal(new short[] { 10, 20, 90, 100 }, plan.Select(x => x.SortOrder));
        Assert.All(plan, x => Assert.True(x.Required));
    }
    private static PreparedLeaseContractDraft Draft(bool secondary) => new() { MemberId = 1, Draft = new LeaseContractDraft { StartDate = "2026-01-01", ParcelId = 2, HasPreviousContract = false, IncludeSecondaryMember = secondary, HandoverDate = "2026-01-01" } };
}
