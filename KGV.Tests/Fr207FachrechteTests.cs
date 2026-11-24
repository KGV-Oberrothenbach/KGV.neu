using System;
using KGV.Core.Security;
using Xunit;

namespace KGV.Tests;

/// <summary>
/// FR2-07: fachliche Rechte werden unabhaengig von Legacy-Masken geprueft.
/// Keine Datenbank- oder HTTP-E2E-Abnahme.
/// </summary>
public sealed class Fr207FachrechteTests
{
    private static UserContext User(PermissionFlags oldBits = PermissionFlags.None, params string[] rights)
        => new(Guid.NewGuid(), UserRole.User, 42, oldBits,
            PermissionFlags.None, PermissionFlags.None, PermissionFlags.None, rights);

    [Fact]
    public void LesenErlaubtWederBearbeitenNochFreigeben()
    {
        var context = User(PermissionFlags.None, FachrechteV2.ArbeitsstundenLesen);
        Assert.True(PermissionChecks.CanReadWorkHours(context));
        Assert.False(PermissionChecks.CanEditWorkHours(context));
        Assert.False(PermissionChecks.CanApproveWorkHours(context));
        Assert.False(PermissionChecks.CanManageWorkHours(context));
        Assert.False(PermissionChecks.CanManageWorkAssignments(context));
    }

    [Fact]
    public void BearbeitenErlaubtKeineFreigabeUndNichtImplizitLesen()
    {
        var context = User(PermissionFlags.None, FachrechteV2.ArbeitsstundenBearbeiten);
        Assert.False(PermissionChecks.CanReadWorkHours(context));
        Assert.True(PermissionChecks.CanEditWorkHours(context));
        Assert.False(PermissionChecks.CanApproveWorkHours(context));
        Assert.False(PermissionChecks.CanManageWorkHours(context));
    }

    [Fact]
    public void FreigebenErlaubtNichtAutomatischBearbeiten()
    {
        var context = User(PermissionFlags.None, FachrechteV2.ArbeitsstundenFreigeben);
        Assert.False(PermissionChecks.CanReadWorkHours(context));
        Assert.False(PermissionChecks.CanEditWorkHours(context));
        Assert.True(PermissionChecks.CanApproveWorkHours(context));
        Assert.False(PermissionChecks.CanManageWorkHours(context));
    }

    [Fact]
    public void SofortgenehmigungSetztBeideRechteVoraus()
    {
        var context = User(PermissionFlags.None,
            FachrechteV2.ArbeitsstundenBearbeiten,FachrechteV2.ArbeitsstundenFreigeben);
        Assert.True(PermissionChecks.CanManageWorkHours(context));
        Assert.False(PermissionChecks.CanReadWorkHours(context));
    }

    [Fact]
    public void ArbeitseinsatzrechtIstVonArbeitsstundenGetrennt()
    {
        var context = User(PermissionFlags.None, FachrechteV2.ArbeitseinsatzVerwalten);
        Assert.True(PermissionChecks.CanManageWorkAssignments(context));
        Assert.False(PermissionChecks.CanApproveWorkHours(context));
        Assert.False(PermissionChecks.CanEditWorkHours(context));
        Assert.False(PermissionChecks.CanReadWorkHours(context));
    }

    [Fact]
    public void AlteRollenbitsErteilenKeineFr207Rechte()
    {
        var legacy = User(PermissionFlags.CanReadWorkHours
          | PermissionFlags.CanManageWorkHours | PermissionFlags.CanManageWorkAssignments);
        Assert.False(PermissionChecks.CanReadWorkHours(legacy));
        Assert.False(PermissionChecks.CanEditWorkHours(legacy));
        Assert.False(PermissionChecks.CanApproveWorkHours(legacy));
        Assert.False(PermissionChecks.CanManageWorkHours(legacy));
        Assert.False(PermissionChecks.CanManageWorkAssignments(legacy));
    }

    [Fact]
    public void FehlenderKontextIstGeschlossen()
    {
        Assert.False(PermissionChecks.CanReadWorkHours(null));
        Assert.False(PermissionChecks.CanEditWorkHours(null));
        Assert.False(PermissionChecks.CanApproveWorkHours(null));
        Assert.False(PermissionChecks.CanManageWorkHours(null));
        Assert.False(PermissionChecks.CanManageWorkAssignments(null));
    }

    [Fact]
    public void EigenzugriffBleibtObjektbezogenUndKeinFremdzugriff()
    {
        var context=User();
        // Selbstauskunft ist bewusst vom Fachrecht arbeitsstunden.lesen getrennt.
        Assert.False(PermissionChecks.CanReadWorkHours(context));
        // Ohne die zusaetzliche normale Eigenzugriffsberechtigung hier kein Zugriff.
        Assert.False(PermissionChecks.CanReadWorkHoursForMember(context,42));
        Assert.False(PermissionChecks.CanReadWorkHoursForMember(context,43));
    }
}
