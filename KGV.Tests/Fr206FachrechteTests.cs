using System;
using KGV.Core.Security;
using Xunit;

namespace KGV.Tests;

public sealed class Fr206FachrechteTests
{
    private static UserContext Context(PermissionFlags legacyBits = PermissionFlags.None, params string[] rights)
        => new(Guid.NewGuid(), UserRole.User, 42, legacyBits,
            PermissionFlags.None, PermissionFlags.None, PermissionFlags.None, rights);

    [Fact]
    public void VorbereitungUndRechnungenErfordernKeineFinalisierung()
    {
        var context = Context(PermissionFlags.None,
            FachrechteV2.JahresabschlussVorbereiten, FachrechteV2.FinanzenRechnungen);
        Assert.True(PermissionChecks.CanPrepareYearEndClosing(context));
        Assert.True(PermissionChecks.CanManageFinanceInvoices(context));
        Assert.True(PermissionChecks.HasAnyFinanceAccess(context));
        Assert.False(PermissionChecks.CanPerformYearEndClosing(context));
        Assert.False(PermissionChecks.CanCorrectYearEndClosing(context));
        Assert.False(PermissionChecks.CanManageFinanceCostTypes(context));
    }

    [Fact]
    public void FinanzrechteSindVoneinanderUnabhaengig()
    {
        var lesen = Context(PermissionFlags.None, FachrechteV2.FinanzenLesen);
        var kosten = Context(PermissionFlags.None, FachrechteV2.FinanzenKostenarten);
        Assert.True(PermissionChecks.CanReadFinance(lesen));
        Assert.False(PermissionChecks.CanManageFinanceInvoices(lesen));
        Assert.False(PermissionChecks.CanPrepareYearEndClosing(lesen));
        Assert.True(PermissionChecks.CanManageFinanceCostTypes(kosten));
        Assert.False(PermissionChecks.CanReadFinance(kosten));
        Assert.False(PermissionChecks.CanManageFinanceInvoices(kosten));
    }

    [Fact]
    public void AlteRollenBitsErteilenKeineFinanzrechte()
    {
        var legacy = Context(PermissionFlags.CanPerformYearEndClosing | PermissionFlags.CanManageRoles);
        Assert.False(PermissionChecks.CanReadFinance(legacy));
        Assert.False(PermissionChecks.CanManageFinanceInvoices(legacy));
        Assert.False(PermissionChecks.CanManageFinanceCostTypes(legacy));
        Assert.False(PermissionChecks.CanPrepareYearEndClosing(legacy));
        Assert.False(PermissionChecks.CanPerformYearEndClosing(legacy));
        Assert.False(PermissionChecks.CanCorrectYearEndClosing(legacy));
        Assert.False(PermissionChecks.HasAnyFinanceAccess(legacy));
    }

    [Fact]
    public void FachrechtePruefungenSindBeiFehlendemKontextGeschlossen()
    {
        Assert.False(PermissionChecks.HasAnyFinanceAccess(null));
        Assert.False(PermissionChecks.CanReadFinance(null));
        Assert.False(PermissionChecks.CanPrepareYearEndClosing(null));
        Assert.False(PermissionChecks.CanPerformYearEndClosing(null));
    }
}
