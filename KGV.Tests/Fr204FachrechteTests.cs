using System;
using KGV.Core.Security;
using Xunit;

namespace KGV.Tests;

public sealed class Fr204FachrechteTests
{
    private static UserContext Context(PermissionFlags legacyBits, params string[] grantedRights)
        => new(Guid.NewGuid(), UserRole.User, 42, legacyBits,
            PermissionFlags.None, PermissionFlags.None, PermissionFlags.None, grantedRights);

    [Fact]
    public void BelegungenCanBeManagedWithoutParzelleEditOrMemberCreation()
    {
        var context = Context(PermissionFlags.None, FachrechteV2.BelegungVerwalten);
        Assert.True(PermissionChecks.CanManageBelegungen(context));
        Assert.False(PermissionChecks.CanWriteParzellen(context));
        Assert.False(PermissionChecks.CanCreateMitglied(context));
        Assert.False(PermissionChecks.CanManagePachtvertraege(context));
    }

    [Fact]
    public void ParzelleEditorDoesNotAutomaticallyManageBelegungen()
    {
        var context = Context(PermissionFlags.CanWriteParzellen);
        Assert.True(PermissionChecks.CanWriteParzellen(context));
        Assert.False(PermissionChecks.CanManageBelegungen(context));
    }

    [Fact]
    public void OldMemberCreationAndDocumentBitsCannotGrantNewRights()
    {
        var context = Context(PermissionFlags.CanCreateMitglied | PermissionFlags.CanManageDocuments);
        Assert.False(PermissionChecks.CanManageBelegungen(context));
        Assert.False(PermissionChecks.CanManagePachtvertraege(context));
        Assert.False(PermissionChecks.CanManageBelegungen(null));
    }

    [Fact]
    public void PachtvertragRightsAreIndependentOfDocumentManagement()
    {
        var context = Context(PermissionFlags.None, FachrechteV2.PachtvertragVerwalten);
        Assert.True(PermissionChecks.CanManagePachtvertraege(context));
        Assert.False(PermissionChecks.CanManageBelegungen(context));
        Assert.False(PermissionChecks.CanManageDocuments(context));
    }
}
