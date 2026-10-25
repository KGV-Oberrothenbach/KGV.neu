using KGV.Core.Models;
using KGV.Core.Services;

namespace KGV.Tests;

public sealed class VereinsunterlagenNachweisHelperTests
{
    [Fact]
    public void Required_sets_are_contract_specific()
    {
        Assert.Equal(new[] { "satzung" }, VereinsunterlagenNachweisHelper.RequiredFor(PreparedContractConstants.MembershipApplication));
        Assert.Equal(new[] { "kleingartenordnung", "beitragsordnung", "arbeitsordnung" }, VereinsunterlagenNachweisHelper.RequiredFor(PreparedContractConstants.LeaseContract));
    }

    [Fact]
    public void Paper_without_version_is_valid_but_digital_requires_frozen_version()
    {
        var paper = new VereinsunterlagenNachweisSnapshotItem { Dokumenttyp = "satzung", Bereitstellungsart = "papier", BereitgestelltAm = DateTime.UtcNow };
        var incompleteDigital = paper with { Bereitstellungsart = "digital" };
        Assert.True(VereinsunterlagenNachweisHelper.IsComplete(new[] { paper }, PreparedContractConstants.MembershipApplication));
        Assert.False(VereinsunterlagenNachweisHelper.IsComplete(new[] { incompleteDigital }, PreparedContractConstants.MembershipApplication));
    }
}
