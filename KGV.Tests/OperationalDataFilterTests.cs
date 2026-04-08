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
            Email = "demo.user@example.com",
            IsDemo = false
        };

        var result = OperationalDataFilter.IsOperationalMember(member);

        Assert.False(result);
    }

    [Fact]
    public void IsOperationalMember_ReturnsFalse_ForDemoFlag()
    {
        var member = new MitgliedRecord
        {
            Id = 2,
            Vorname = "Max",
            Name = "Mustermann",
            Email = "max.mustermann@kgv-oberrothenbach.de",
            IsDemo = true
        };

        var result = OperationalDataFilter.IsOperationalMember(member);

        Assert.False(result);
    }

    [Fact]
    public void IsOperationalMember_ReturnsTrue_ForRealMemberData()
    {
        var member = new MitgliedRecord
        {
            Id = 3,
            Vorname = "Max",
            Name = "Mustermann",
            Email = "max.mustermann@kgv-oberrothenbach.de",
            IsDemo = false
        };

        var result = OperationalDataFilter.IsOperationalMember(member);

        Assert.True(result);
    }

    [Fact]
    public void IsOperationalAppUser_ReturnsFalse_WhenDisplayNameContainsPlayStoreMarker()
    {
        var result = OperationalDataFilter.IsOperationalAppUser(
            member: null,
            displayName: "Play Store Tester",
            email: "tester@kgv.de");

        Assert.False(result);
    }

    [Fact]
    public void IsOperationalAppUser_UsesMemberData_WhenMemberIsAvailable()
    {
        var member = new MitgliedRecord
        {
            Id = 4,
            Vorname = "Anna",
            Name = "Testfall",
            Email = "anna@kgv-oberrothenbach.de",
            IsDemo = false
        };

        var result = OperationalDataFilter.IsOperationalAppUser(
            member,
            displayName: "Anna Müller",
            email: "anna@kgv-oberrothenbach.de");

        Assert.False(result);
    }

    [Fact]
    public void IsOperationalArbeitseinsatz_ReturnsFalse_ForDemoRecord()
    {
        var item = new ArbeitseinsatzRecord
        {
            Id = 1,
            Titel = "Frühjahrsdienst",
            Beschreibung = "Produktiv",
            Treffpunkt = "Tor",
            IsDemo = true
        };

        var result = OperationalDataFilter.IsOperationalArbeitseinsatz(item);

        Assert.False(result);
    }

    [Fact]
    public void IsOperationalArbeitseinsatz_ReturnsTrue_ForRealRecord()
    {
        var item = new ArbeitseinsatzRecord
        {
            Id = 2,
            Titel = "Frühjahrsdienst",
            Beschreibung = "Beete pflegen",
            Treffpunkt = "Vereinsheim",
            IsDemo = false
        };

        var result = OperationalDataFilter.IsOperationalArbeitseinsatz(item);

        Assert.True(result);
    }

    [Fact]
    public void IsOperationalTermin_ReturnsFalse_ForDemoFlag()
    {
        var item = new TerminRecord
        {
            Id = 3,
            Titel = "Mitgliederversammlung",
            Beschreibung = "Regulärer Termin",
            IsDemo = true
        };

        var result = OperationalDataFilter.IsOperationalTermin(item);

        Assert.False(result);
    }

    [Fact]
    public void IsOperationalTermin_ReturnsFalse_ForDemoMarker()
    {
        var item = new TerminRecord
        {
            Id = 4,
            Titel = "Demo-Termin",
            Beschreibung = "Nur Test",
            IsDemo = false
        };

        var result = OperationalDataFilter.IsOperationalTermin(item);

        Assert.False(result);
    }

    [Fact]
    public void IsOperationalTermin_ReturnsTrue_ForRealRecord()
    {
        var item = new TerminRecord
        {
            Id = 5,
            Titel = "Mitgliederversammlung",
            Beschreibung = "Einladung an alle Mitglieder",
            IsDemo = false
        };

        var result = OperationalDataFilter.IsOperationalTermin(item);

        Assert.True(result);
    }

    [Fact]
    public void IsOperationalBekanntmachung_ReturnsFalse_ForDemoFlag()
    {
        var item = new BekanntmachungRecord
        {
            Id = 6,
            Titel = "Hinweis",
            InhaltHtml = "<p>Dies ist ein produktiver Text.</p>",
            IsDemo = true
        };

        var result = OperationalDataFilter.IsOperationalBekanntmachung(item);

        Assert.False(result);
    }

    [Fact]
    public void IsOperationalBekanntmachung_ReturnsTrue_ForRealRecord()
    {
        var item = new BekanntmachungRecord
        {
            Id = 7,
            Titel = "Wasser abstellen",
            InhaltHtml = "<p>Bitte Haupthahn schließen.</p>",
            IsDemo = false
        };

        var result = OperationalDataFilter.IsOperationalBekanntmachung(item);

        Assert.True(result);
    }
}