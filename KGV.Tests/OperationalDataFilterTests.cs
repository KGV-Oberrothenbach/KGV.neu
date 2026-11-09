using KGV.Core.Models;
using Xunit;

namespace KGV.Tests;

public sealed class OperationalDataFilterTests
{
    [Fact]
    public void IsOperationalMember_UsesAdmissionStatus_NotNameOrEmailMarkers()
    {
        var member = new MitgliedRecord
        {
            Id = 1,
            Vorname = "Demo",
            Name = "Testfall",
            Email = "demo.user@example.com",
            AufnahmeStatus = "aufgenommen"
        };

        Assert.True(OperationalDataFilter.IsOperationalMember(member));
    }

    [Fact]
    public void IsOperationalMember_ReturnsFalse_ForPendingApplicant()
        => Assert.False(OperationalDataFilter.IsOperationalMember(
            new MitgliedRecord { Id = 2, AufnahmeStatus = "antrag_offen" }));

    [Fact]
    public void IsOperationalAppUser_WithoutMember_DoesNotInferScopeFromText()
        => Assert.True(OperationalDataFilter.IsOperationalAppUser(
            member: null,
            displayName: "Play Store Tester",
            email: "tester@example.com"));

    [Fact]
    public void IsOperationalAppUser_UsesMemberAdmissionStatus()
        => Assert.False(OperationalDataFilter.IsOperationalAppUser(
            new MitgliedRecord { AufnahmeStatus = "antrag_offen" },
            displayName: "Anna",
            email: "anna@example.com"));

    [Fact]
    public void ProjectLocalRecords_AreOperational_WhenPresent()
    {
        Assert.True(OperationalDataFilter.IsOperationalArbeitseinsatz(new ArbeitseinsatzRecord()));
        Assert.True(OperationalDataFilter.IsOperationalTermin(new TerminRecord()));
        Assert.True(OperationalDataFilter.IsOperationalBekanntmachung(new BekanntmachungRecord()));
    }

    [Fact]
    public void MissingRecords_AreNotOperational()
    {
        Assert.False(OperationalDataFilter.IsOperationalArbeitseinsatz(null));
        Assert.False(OperationalDataFilter.IsOperationalTermin(null));
        Assert.False(OperationalDataFilter.IsOperationalBekanntmachung(null));
    }
}
