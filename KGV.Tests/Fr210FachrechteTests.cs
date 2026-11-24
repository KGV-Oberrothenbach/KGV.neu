using System;
using KGV.Core.Security;
using Xunit;

namespace KGV.Tests;

/// <summary>
/// FR2-10: fünf voneinander unabhängige serverseitig ermittelte Fachrechte.
/// Statischer/Unit-Vertrag, keine authentifizierte HTTP-E2E-Abnahme.
/// </summary>
public sealed class Fr210FachrechteTests
{
    private static UserContext Context(PermissionFlags oldBits = PermissionFlags.None, params string[] rights)
        => new(Guid.NewGuid(), UserRole.User, 42, oldBits,
            PermissionFlags.None, PermissionFlags.None, PermissionFlags.None, rights);

    [Fact]
    public void AlleFuenfRechteSindVoneinanderUnabhaengig()
    {
        var cases = new (string Right, Func<UserContext, bool> Check)[]
        {
            (FachrechteV2.TerminVerwalten, PermissionChecks.CanManageAppointments),
            (FachrechteV2.BekanntmachungVerwalten, PermissionChecks.CanManageAnnouncements),
            (FachrechteV2.VereinsfunktionVerwalten, PermissionChecks.CanManageClubFunctions),
            (FachrechteV2.VereinKonfiguration, PermissionChecks.CanManageClubConfiguration),
            (FachrechteV2.SaisonVerwalten, PermissionChecks.CanManageClubSeasonConfiguration)
        };
        for (var grantedIndex = 0; grantedIndex < cases.Length; grantedIndex++)
        {
            var current = Context(PermissionFlags.None, cases[grantedIndex].Right);
            for (var checkedIndex = 0; checkedIndex < cases.Length; checkedIndex++)
                Assert.Equal(grantedIndex == checkedIndex, cases[checkedIndex].Check(current));
        }
    }

    [Fact]
    public void LegacyFlagsGebenKeinesDerFuenfFachrechte()
    {
        var old = Context(PermissionFlags.CanManageAppointments
            | PermissionFlags.CanManageAnnouncements
            | PermissionFlags.CanManageClubFunctions
            | PermissionFlags.CanManageClubSeasonConfiguration);
        Assert.False(PermissionChecks.CanManageAppointments(old));
        Assert.False(PermissionChecks.CanManageAnnouncements(old));
        Assert.False(PermissionChecks.CanManageClubFunctions(old));
        Assert.False(PermissionChecks.CanManageClubConfiguration(old));
        Assert.False(PermissionChecks.CanManageClubSeasonConfiguration(old));
    }

    [Fact]
    public void VereinskonfigurationImpliziertKeineSaisonUndUmgekehrt()
    {
        var configuration = Context(PermissionFlags.None, FachrechteV2.VereinKonfiguration);
        var season = Context(PermissionFlags.None, FachrechteV2.SaisonVerwalten);
        Assert.True(PermissionChecks.CanManageClubConfiguration(configuration));
        Assert.False(PermissionChecks.CanManageClubSeasonConfiguration(configuration));
        Assert.False(PermissionChecks.CanManageClubConfiguration(season));
        Assert.True(PermissionChecks.CanManageClubSeasonConfiguration(season));
    }

    [Fact]
    public void NullKontextFailClosed()
    {
        Assert.False(PermissionChecks.CanManageAppointments(null));
        Assert.False(PermissionChecks.CanManageAnnouncements(null));
        Assert.False(PermissionChecks.CanManageClubFunctions(null));
        Assert.False(PermissionChecks.CanManageClubConfiguration(null));
        Assert.False(PermissionChecks.CanManageClubSeasonConfiguration(null));
    }

    [Fact]
    public void EinzelnesRechtOeffnetVerwaltungsnavigation()
    {
        foreach (var right in new[] {
            FachrechteV2.TerminVerwalten,
            FachrechteV2.BekanntmachungVerwalten,
            FachrechteV2.VereinsfunktionVerwalten,
            FachrechteV2.VereinKonfiguration,
            FachrechteV2.SaisonVerwalten
        })
            Assert.True(PermissionChecks.HasAnyManagementWorkspaceAccess(Context(PermissionFlags.None, right)));
    }
}
