using KGV.Core.Security;
using Xunit;

namespace KGV.Tests;

public sealed class PermissionServiceTests
{
    [Theory]
    [InlineData(PermissionFlags.CanSearchMembers, 1L)]
    [InlineData(PermissionFlags.CanViewMembers, 2L)]
    [InlineData(PermissionFlags.CanEditAllMembers, 4L)]
    [InlineData(PermissionFlags.CanSeeOwnDataOnly, 8L)]
    [InlineData(PermissionFlags.CanManageDocuments, 16L)]
    [InlineData(PermissionFlags.CanReadMeters, 32L)]
    [InlineData(PermissionFlags.CanManageMeterChanges, 64L)]
    [InlineData(PermissionFlags.CanApproveMeterReadings, 128L)]
    [InlineData(PermissionFlags.CanManageWorkHours, 256L)]
    [InlineData(PermissionFlags.CanManageRoles, 512L)]
    [InlineData(PermissionFlags.CanShowStammdaten, 1024L)]
    [InlineData(PermissionFlags.CanReadStammdaten, 2048L)]
    [InlineData(PermissionFlags.CanWriteStammdaten, 4096L)]
    [InlineData(PermissionFlags.CanShowParzellen, 8192L)]
    [InlineData(PermissionFlags.CanReadParzellen, 16384L)]
    [InlineData(PermissionFlags.CanWriteParzellen, 32768L)]
    [InlineData(PermissionFlags.CanReadDocuments, 65536L)]
    [InlineData(PermissionFlags.CanReadWorkHours, 131072L)]
    [InlineData(PermissionFlags.CanReadRoles, 262144L)]
    [InlineData(PermissionFlags.CanCreateMitglied, 524288L)]
    [InlineData(PermissionFlags.CanManageWorkAssignments, 1048576L)]
    [InlineData(PermissionFlags.CanManageAppointments, 2097152L)]
    [InlineData(PermissionFlags.CanManageAnnouncements, 4194304L)]
    public void ExistingPermissionBits_AreStable(PermissionFlags permission, long expectedValue)
    {
        Assert.Equal(expectedValue, (long)permission);
    }

    [Fact]
    public void YearEndClosingPermissionBit_Is8388608()
        => Assert.Equal(8388608L, (long)PermissionFlags.CanPerformYearEndClosing);

    [Theory]
    [InlineData(UserRole.Admin, 16769015L)]
    [InlineData(UserRole.Vorstand, 7855607L)]
    [InlineData(UserRole.User, 10L)]
    public void RolePermissions_MatchSharedEffectivePermissionMasks(UserRole role, long expectedMask)
    {
        Assert.Equal(expectedMask, (long)PermissionService.GetRolePermissions(role));
    }

    [Theory]
    [InlineData(UserRole.Admin, true)]
    [InlineData(UserRole.Vorstand, false)]
    [InlineData(UserRole.User, false)]
    public void RolePermissions_ApplyYearEndClosingOnlyToAdminsByDefault(UserRole role, bool expected)
        => Assert.Equal(expected, PermissionService.GetRolePermissions(role).HasFlag(PermissionFlags.CanPerformYearEndClosing));

    [Fact]
    public void CreateContext_AppliesYearEndClosingGrantsAndRevocationsWithRevocationWinning()
    {
        var service = new PermissionService();
        var vorstandWithGrant = service.CreateContext(Guid.NewGuid(), "vorstand", null, (long)PermissionFlags.CanPerformYearEndClosing);
        var adminWithRevocation = service.CreateContext(Guid.NewGuid(), "admin", null, null, (long)PermissionFlags.CanPerformYearEndClosing);
        var userWithGrantAndRevocation = service.CreateContext(
            Guid.NewGuid(),
            "user",
            null,
            (long)PermissionFlags.CanPerformYearEndClosing,
            (long)PermissionFlags.CanPerformYearEndClosing);

        Assert.True(vorstandWithGrant.Has(PermissionFlags.CanPerformYearEndClosing));
        Assert.False(adminWithRevocation.Has(PermissionFlags.CanPerformYearEndClosing));
        Assert.False(userWithGrantAndRevocation.Has(PermissionFlags.CanPerformYearEndClosing));
    }

    [Fact]
    public void PermissionCatalog_ContainsYearEndClosingInKnownAndUserEditablePermissions()
    {
        Assert.True(PermissionCatalog.GetKnownPermissionMask().HasFlag(PermissionFlags.CanPerformYearEndClosing));
        Assert.Contains(
            PermissionCatalog.GetUserSpecificEditablePermissions(),
            permission => permission.Flag == PermissionFlags.CanPerformYearEndClosing
                          && permission.DisplayName == "Jahresabschluss durchführen");
    }

    [Fact]
    public void PermissionCatalog_ExposesYearEndClosingAsAnActionArea()
    {
        var area = Assert.Single(PermissionCatalog.GetGlobalEditablePermissionAreas(), area => area.AreaKey == "jahresabschluss");

        Assert.Equal("Jahresabschluss", area.DisplayName);
        Assert.Equal(PermissionFlags.CanPerformYearEndClosing, area.AllPermissions);
        Assert.Equal(PermissionFlags.CanPerformYearEndClosing, area.ReadPermissions);
        Assert.Equal(PermissionFlags.CanPerformYearEndClosing, area.WritePermissions);
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
