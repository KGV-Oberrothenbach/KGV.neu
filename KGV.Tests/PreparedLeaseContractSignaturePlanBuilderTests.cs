using KGV.Core.Models;
using KGV.Core.Services;

namespace KGV.Tests;

public sealed class PreparedLeaseContractSignaturePlanBuilderTests
{
    [Fact] public void Adult_without_co_tenant_has_two_signatures() => Assert.Equal(2, PreparedLeaseContractSignaturePlanBuilder.Build(Draft(false), false, false).Count);
    [Fact] public void Adult_with_explicit_co_tenant_has_three_signatures() => Assert.Equal(3, PreparedLeaseContractSignaturePlanBuilder.Build(Draft(true), false, false).Count);
    [Fact] public void Minor_requires_representative_and_has_three_signatures() { Assert.Equal(3, PreparedLeaseContractSignaturePlanBuilder.Build(Draft(false), true, true).Count); Assert.Throws<PreparedContractValidationException>(() => PreparedLeaseContractSignaturePlanBuilder.Build(Draft(false), true, false)); }
    private static PreparedLeaseContractDraft Draft(bool secondary) => new() { MemberId = 1, Draft = new LeaseContractDraft { StartDate = "2026-01-01", ParcelId = 2, HasPreviousContract = false, IncludeSecondaryMember = secondary, HandoverDate = "2026-01-01" } };
}
