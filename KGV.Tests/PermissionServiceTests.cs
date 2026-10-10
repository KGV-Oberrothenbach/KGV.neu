using KGV.Core.Security;
using Xunit;

namespace KGV.Tests;

public sealed class PermissionServiceTests
{
    [Theory]
    [InlineData(UserRole.Admin, 2088951L)]
    [InlineData(UserRole.Vorstand, 1564151L)]
    [InlineData(UserRole.User, 10L)]
    public void RolePermissions_MatchSharedEffectivePermissionMasks(UserRole role, long expectedMask)
    {
        Assert.Equal(expectedMask, (long)PermissionService.GetRolePermissions(role));
    }

    [Theory]
    [InlineData(UserRole.Admin, true, true)]
    [InlineData(UserRole.Vorstand, true, true)]
    [InlineData(UserRole.User, false, false)]
    public void RolePermissions_KeepWorkHoursAndWorkAssignmentsAligned(UserRole role, bool canManageWorkHours, bool canManageWorkAssignments)
    {
        var permissions = PermissionService.GetRolePermissions(role);

        Assert.Equal(canManageWorkHours, permissions.HasFlag(PermissionFlags.CanManageWorkHours));
        Assert.Equal(canManageWorkAssignments, permissions.HasFlag(PermissionFlags.CanManageWorkAssignments));
    }

    [Fact]
    public void CreateContext_AppliesWorkAssignmentGrantAndRevocationToEffectivePermissions()
    {
        var service = new PermissionService();
        var grantedUser = service.CreateContext(Guid.NewGuid(), "user", null, (long)PermissionFlags.CanManageWorkAssignments);
        var revokedVorstand = service.CreateContext(Guid.NewGuid(), "vorstand", null, null, (long)PermissionFlags.CanManageWorkAssignments);

        Assert.True(grantedUser.Has(PermissionFlags.CanManageWorkAssignments));
        Assert.False(revokedVorstand.Has(PermissionFlags.CanManageWorkAssignments));
        Assert.True(revokedVorstand.Has(PermissionFlags.CanManageWorkHours));
    }

    [Theory]
    [InlineData((long)PermissionFlags.CanManageWorkAssignments, 0L, true, false)]
    [InlineData((long)PermissionFlags.CanManageWorkHours, 0L, false, true)]
    [InlineData((long)(PermissionFlags.CanManageWorkAssignments | PermissionFlags.CanManageWorkHours), 0L, true, true)]
    [InlineData((long)(PermissionFlags.CanManageWorkAssignments | PermissionFlags.CanManageWorkHours), (long)PermissionFlags.CanManageWorkAssignments, false, true)]
    public void CreateContext_KeepsWorkAssignmentAndWorkHourPermissionsIndependent(long grants, long revocations, bool canManageAssignments, bool canManageHours)
    {
        var context = new PermissionService().CreateContext(Guid.NewGuid(), "user", null, grants, revocations);

        Assert.Equal(canManageAssignments, PermissionChecks.CanManageWorkAssignments(context));
        Assert.Equal(canManageHours, PermissionChecks.CanManageWorkHours(context));
    }
}
