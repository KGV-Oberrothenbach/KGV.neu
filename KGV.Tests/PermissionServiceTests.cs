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

    [Fact]
    public void HistoricalContractPermission_IsIndependentAndAssignableToOrdinaryUsers()
    {
        var service = new PermissionService();
        var ordinaryUser = service.CreateContext(Guid.NewGuid(), "user", null);
        var grantedUser = service.CreateContext(Guid.NewGuid(), "user", null, (long)PermissionFlags.CanRegisterHistoricalContracts);
        var admin = service.CreateContext(Guid.NewGuid(), "admin", null);

        Assert.Equal(16777216L, (long)PermissionFlags.CanRegisterHistoricalContracts);
        Assert.False(PermissionChecks.CanRegisterHistoricalContracts(ordinaryUser));
        Assert.True(PermissionChecks.CanRegisterHistoricalContracts(grantedUser));
        Assert.False(PermissionChecks.CanRegisterHistoricalContracts(admin));
    }

    [Fact]
    public void PermissionCatalog_ExposesHistoricalContractRegistrationAsAnAssignableActionArea()
    {
        Assert.Contains(PermissionCatalog.GetUserSpecificEditablePermissions(), permission => permission.Flag == PermissionFlags.CanRegisterHistoricalContracts);
        var area = Assert.Single(PermissionCatalog.GetGlobalEditablePermissionAreas(), area => area.AreaKey == "altvertraege");
        Assert.Equal(PermissionFlags.CanRegisterHistoricalContracts, area.WritePermissions);
    }

    [Theory]
    [InlineData(UserRole.Admin, 4278181879L)]
    [InlineData(UserRole.Vorstand, 1920458231L)]
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
    public void PermissionChecks_RequireServerIssuedNamedRightNotLegacyRoleBit()
    {
        var service = new PermissionService();
        var legacyAdmin = service.CreateContext(Guid.NewGuid(), "admin", null);
        var legacyGrantedVorstand = service.CreateContext(Guid.NewGuid(), "vorstand", null, (long)PermissionFlags.CanPerformYearEndClosing);
        var namedAuthorized = new UserContext(Guid.NewGuid(), UserRole.User, null,
            PermissionFlags.None, PermissionFlags.None, PermissionFlags.None, PermissionFlags.None,
            new[] { FachrechteV2.JahresabschlussFinalisieren });
        Assert.False(PermissionChecks.CanPerformYearEndClosing(legacyAdmin));
        Assert.False(PermissionChecks.CanPerformYearEndClosing(legacyGrantedVorstand));
        Assert.True(PermissionChecks.CanPerformYearEndClosing(namedAuthorized));
    }

    [Fact]
    public void PermissionChecks_KeepDocumentAndRoleManagementPermissionBased()
    {
        var service = new PermissionService();
        var documentReader = service.CreateContext(Guid.NewGuid(), "user", null, (long)PermissionFlags.CanReadDocuments);
        var documentManager = service.CreateContext(Guid.NewGuid(), "user", null, (long)PermissionFlags.CanManageDocuments);
        var memberEditor = service.CreateContext(Guid.NewGuid(), "user", null, (long)PermissionFlags.CanEditAllMembers);
        var vorstand = service.CreateContext(Guid.NewGuid(), "vorstand", null);
        var admin = service.CreateContext(Guid.NewGuid(), "admin", null);
        var revokedAdmin = service.CreateContext(Guid.NewGuid(), "admin", null, null, (long)PermissionFlags.CanManageRoles);

        Assert.True(PermissionChecks.CanReadDocuments(documentReader));
        Assert.False(PermissionChecks.CanManageDocuments(documentReader));
        Assert.True(PermissionChecks.CanReadDocuments(documentManager));
        Assert.True(PermissionChecks.CanManageDocuments(documentManager));
        Assert.False(PermissionChecks.CanManageDocuments(memberEditor));
        Assert.False(PermissionChecks.CanArchiveDocuments(vorstand));
        Assert.True(PermissionChecks.CanArchiveDocuments(admin));
        Assert.True(PermissionChecks.CanReadRoleManagement(vorstand));
        Assert.False(PermissionChecks.CanManageRoleManagement(vorstand));
        Assert.True(PermissionChecks.CanManageRoleManagement(admin));
        Assert.False(PermissionChecks.CanManageRoleManagement(revokedAdmin));
    }

    [Fact]
    public void PermissionChecks_CentralizeExplicitRoleCapabilitiesAndFailClosedWithoutContext()
    {
        var service = new PermissionService();
        var admin = service.CreateContext(Guid.NewGuid(), "admin", null);
        var vorstand = service.CreateContext(Guid.NewGuid(), "vorstand", null);
        var user = service.CreateContext(Guid.NewGuid(), "user", null);

        Assert.True(PermissionChecks.CanManageClubSeasonConfiguration(admin));
        Assert.False(PermissionChecks.CanManageClubSeasonConfiguration(vorstand));
        Assert.False(PermissionChecks.CanManageClubSeasonConfiguration(user));
        // FR2-05: legacy role bits must not authorize ablesung.einstellungen.
        Assert.False(PermissionChecks.CanConfigureUserMeterReadingSubmissions(admin));
        Assert.False(PermissionChecks.CanConfigureUserMeterReadingSubmissions(vorstand));
        Assert.False(PermissionChecks.CanConfigureUserMeterReadingSubmissions(user));
        var settingManager = new UserContext(Guid.NewGuid(), UserRole.User, null,
            PermissionFlags.None, PermissionFlags.None, PermissionFlags.None, PermissionFlags.None,
            new[] { FachrechteV2.AblesungEinstellungen });
        Assert.True(PermissionChecks.CanConfigureUserMeterReadingSubmissions(settingManager));
        Assert.False(PermissionChecks.CanPerformYearEndClosing(null));
        Assert.False(PermissionChecks.CanReadDocuments(null));
        Assert.False(PermissionChecks.CanManageDocuments(null));
        Assert.False(PermissionChecks.CanArchiveDocuments(null));
        Assert.False(PermissionChecks.CanReadRoleManagement(null));
        Assert.False(PermissionChecks.CanManageRoleManagement(null));
        Assert.False(PermissionChecks.CanManageClubSeasonConfiguration(null));
        Assert.False(PermissionChecks.CanConfigureUserMeterReadingSubmissions(null));
    }

    [Fact]
    public void HasAnyManagementWorkspaceAccess_UsesEffectiveCapabilitiesAndFailsClosed()
    {
        var service = new PermissionService();
        var ordinaryUser = service.CreateContext(Guid.NewGuid(), "user", null);
        var documentReader = service.CreateContext(Guid.NewGuid(), "user", null, (long)PermissionFlags.CanReadDocuments);
        var legacyYearEndClosingUser = service.CreateContext(Guid.NewGuid(), "user", null, (long)PermissionFlags.CanPerformYearEndClosing);
        var preparationUser = new UserContext(Guid.NewGuid(), UserRole.User, null,
            PermissionFlags.None, PermissionFlags.None, PermissionFlags.None, PermissionFlags.None,
            new[] { FachrechteV2.JahresabschlussVorbereiten });
        var revokedAdmin = service.CreateContext(Guid.NewGuid(), "admin", null, null, (long)(PermissionFlags.CanPerformYearEndClosing | PermissionFlags.CanManageRoles));

        Assert.False(PermissionChecks.HasAnyManagementWorkspaceAccess(null));
        Assert.False(PermissionChecks.HasAnyManagementWorkspaceAccess(ordinaryUser));
        Assert.True(PermissionChecks.HasAnyManagementWorkspaceAccess(documentReader));
        Assert.False(PermissionChecks.HasAnyManagementWorkspaceAccess(legacyYearEndClosingUser));
        Assert.True(PermissionChecks.HasAnyManagementWorkspaceAccess(preparationUser));
        Assert.True(PermissionChecks.HasAnyManagementWorkspaceAccess(revokedAdmin));
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
    [InlineData((long)PermissionFlags.CanManageWorkAssignments, 0L)]
    [InlineData((long)PermissionFlags.CanManageWorkHours, 0L)]
    [InlineData((long)(PermissionFlags.CanManageWorkAssignments | PermissionFlags.CanManageWorkHours), 0L)]
    [InlineData((long)(PermissionFlags.CanManageWorkAssignments | PermissionFlags.CanManageWorkHours), (long)PermissionFlags.CanManageWorkAssignments)]
    public void CreateContext_LegacyBitsGrantNoWorkRights(long grants, long revocations)
    {
        var context = new PermissionService().CreateContext(Guid.NewGuid(), "user", null, grants, revocations);

        // FR2-07: Uebergebene Legacy-Grants sind keine Fachrechtszuweisungen.
        Assert.False(PermissionChecks.CanManageWorkAssignments(context));
        Assert.False(PermissionChecks.CanManageWorkHours(context));
        Assert.False(PermissionChecks.CanEditWorkHours(context));
        Assert.False(PermissionChecks.CanApproveWorkHours(context));
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
