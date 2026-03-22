using KGV.Core.Models;
using Xunit;

namespace KGV.Tests;

public sealed class OperationalDataFilterTests
{
    [Fact]
    public void IsOperationalMember_ReturnsFalse_ForDemoMarkersInMemberData()
    {
        var member = new MitgliedRecord
        {
            Id = 1,
            Vorname = "Demo",
            Name = "Mitglied",
            Email = "demo.user@example.com"
        };

        var result = OperationalDataFilter.IsOperationalMember(member);

        Assert.False(result);
    }

    [Fact]
    public void IsOperationalMember_ReturnsTrue_ForRealMemberData()
    {
        var member = new MitgliedRecord
        {
            Id = 2,
            Vorname = "Max",
            Name = "Mustermann",
            Email = "max.mustermann@kgv-oberrothenbach.de"
        };

        var result = OperationalDataFilter.IsOperationalMember(member);

        Assert.True(result);
    }

    [Fact]
    public void IsOperationalAppUser_ReturnsFalse_WhenDisplayNameContainsPlayStoreMarker()
    {
        var result = OperationalDataFilter.IsOperationalAppUser(member: null, displayName: "Play Store Tester", email: "tester@kgv.de");

        Assert.False(result);
    }

    [Fact]
    public void IsOperationalAppUser_UsesMemberData_WhenMemberIsAvailable()
    {
        var member = new MitgliedRecord
        {
            Id = 3,
            Vorname = "Anna",
            Name = "Testfall",
            Email = "anna@kgv-oberrothenbach.de"
        };

        var result = OperationalDataFilter.IsOperationalAppUser(member, displayName: "Anna Müller", email: "anna@kgv-oberrothenbach.de");

        Assert.False(result);
    }
}
