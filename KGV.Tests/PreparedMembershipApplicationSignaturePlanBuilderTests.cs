using KGV.Core.Models;
using KGV.Core.Services;

namespace KGV.Tests;

public sealed class PreparedMembershipApplicationSignaturePlanBuilderTests
{
    [Fact]
    public void Adult_without_secondary_has_three_ordered_steps()
    {
        var plan = Build();
        AssertPlan(plan, MembershipApplicationSignatureKeys.MemberApplication, MembershipApplicationSignatureKeys.MemberPrivacy, MembershipApplicationSignatureKeys.BoardApproval);
        Assert.Equal(7, plan[0].PersonId);
        Assert.Equal(SignaturePurposes.DataProtection, plan[1].Purpose);
    }

    [Fact]
    public void Adult_with_secondary_has_five_ordered_steps()
    {
        var plan = Build(includeSecondary: true);
        AssertPlan(plan, MembershipApplicationSignatureKeys.MemberApplication, MembershipApplicationSignatureKeys.MemberPrivacy, MembershipApplicationSignatureKeys.SecondaryApplication, MembershipApplicationSignatureKeys.SecondaryPrivacy, MembershipApplicationSignatureKeys.BoardApproval);
        Assert.Equal(SignaturePersonTypes.SecondaryMember, plan[2].PersonType);
    }

    [Fact]
    public void Minor_with_representative_has_five_ordered_steps_and_no_secondary()
    {
        var plan = Build(representative: new LegalRepresentativeDraft { Mode = "existing", MemberId = 8 });
        AssertPlan(plan, MembershipApplicationSignatureKeys.MemberApplication, MembershipApplicationSignatureKeys.MemberPrivacy, MembershipApplicationSignatureKeys.RepresentativeApplication, MembershipApplicationSignatureKeys.RepresentativePrivacy, MembershipApplicationSignatureKeys.BoardApproval);
        Assert.Equal(8, plan[2].PersonId);
        Assert.DoesNotContain(plan, step => step.PersonType == SignaturePersonTypes.SecondaryMember);
    }

    [Fact]
    public void Minor_with_secondary_is_rejected()
        => Assert.Throws<PreparedContractValidationException>(() => Build(includeSecondary: true, representative: new LegalRepresentativeDraft { Mode = "manual", Vorname = "Erika", Nachname = "Muster" }));

    private static IReadOnlyList<SignatureStep> Build(bool includeSecondary = false, LegalRepresentativeDraft? representative = null)
        => PreparedMembershipApplicationSignaturePlanBuilder.Build(new PreparedMembershipApplicationDraft { MemberId = 7, IncludeSecondaryMember = includeSecondary, Representative = representative });

    private static void AssertPlan(IReadOnlyList<SignatureStep> plan, params string[] keys)
    {
        Assert.Equal(keys, plan.Select(step => step.Key));
        Assert.Equal(Enumerable.Range(1, keys.Length), plan.Select(step => step.Sequence));
    }
}
