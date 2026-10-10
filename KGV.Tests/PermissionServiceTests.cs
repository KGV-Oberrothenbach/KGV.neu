using KGV.Core.Security;
using Xunit;

namespace KGV.Tests;

public sealed class PermissionServiceTests
{
    [Fact]
    public void AppointmentAndAnnouncementPermissionBits_AreStable()
    {
        Assert.Equal(2097152L, (long)PermissionFlags.CanManageAppointments);
        Assert.Equal(4194304L, (long)PermissionFlags.CanManageAnnouncements);
    }
    [Theory]
    [InlineData(UserRole.Admin, 8380407L)]
    [InlineData(UserRole.Vorstand, 7855607L)]
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

    [Theory]
    [InlineData(UserRole.Admin, true, true)]
    [InlineData(UserRole.Vorstand, true, true)]
    [InlineData(UserRole.User, false, false)]
    public void RolePermissions_KeepAppointmentAndAnnouncementPermissionsIndependent(UserRole role, bool appointments, bool announcements)
    {
        var permissions = PermissionService.GetRolePermissions(role);
        Assert.Equal(appointments, permissions.HasFlag(PermissionFlags.CanManageAppointments));
        Assert.Equal(announcements, permissions.HasFlag(PermissionFlags.CanManageAnnouncements));
    }

    [Fact]
    public void CreateContext_AppliesAppointmentAndAnnouncementGrantsAndRevocationsIndependently()
    {
        var service = new PermissionService();
        var appointmentUser = service.CreateContext(Guid.NewGuid(), "user", null, (long)PermissionFlags.CanManageAppointments);
        var announcementUser = service.CreateContext(Guid.NewGuid(), "user", null, (long)PermissionFlags.CanManageAnnouncements);
        var revokedVorstand = service.CreateContext(Guid.NewGuid(), "vorstand", null, null, (long)PermissionFlags.CanManageAppointments);
        var revokedAdmin = service.CreateContext(Guid.NewGuid(), "admin", null, null, (long)PermissionFlags.CanManageAnnouncements);
        Assert.True(PermissionChecks.CanManageAppointments(appointmentUser));
        Assert.False(PermissionChecks.CanManageAnnouncements(appointmentUser));
        Assert.True(PermissionChecks.CanManageAnnouncements(announcementUser));
        Assert.False(PermissionChecks.CanManageAppointments(announcementUser));
        Assert.False(PermissionChecks.CanManageAppointments(revokedVorstand));
        Assert.True(PermissionChecks.CanManageAnnouncements(revokedVorstand));
        Assert.True(PermissionChecks.CanManageAppointments(revokedAdmin));
        Assert.False(PermissionChecks.CanManageAnnouncements(revokedAdmin));
    }
}
