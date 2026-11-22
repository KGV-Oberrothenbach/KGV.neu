using System;
using KGV.Core.Security;
using Xunit;

namespace KGV.Tests;

public sealed class Fr204ProtocolFachrechtTests
{
    private static UserContext Context(PermissionFlags flags, params string[] rights)
        => new(Guid.NewGuid(), UserRole.User, 42, flags,
            PermissionFlags.None, PermissionFlags.None, PermissionFlags.None, rights);

    [Fact]
    public void BegehungRechtDoesNotRequireMemberEditing()
    {
        var context = Context(PermissionFlags.None, FachrechteV2.BegehungVerwalten);
        Assert.True(PermissionChecks.CanManageBegehungen(context));
        Assert.False(PermissionChecks.CanEditAllMembers(context));
        Assert.False(PermissionChecks.CanManagePachtvertraege(context));
    }

    [Fact]
    public void MemberEditAndContractPreparationDoNotGrantBegehung()
    {
        var context = Context(PermissionFlags.CanEditAllMembers | PermissionFlags.CanManageCommunicationWorkflows);
        Assert.False(PermissionChecks.CanManageBegehungen(context));
        Assert.False(PermissionChecks.CanManageBegehungen(null));
    }
}
