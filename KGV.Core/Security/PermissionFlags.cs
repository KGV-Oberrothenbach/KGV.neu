using System;

namespace KGV.Core.Security
{
    [Flags]
    public enum PermissionFlags : long
    {
        None = 0,

        CanSearchMembers = 1L << 0,
        CanViewMembers = 1L << 1,
        CanEditAllMembers = 1L << 2,
        CanCreateMitglied = 1L << 19,

        CanSeeOwnDataOnly = 1L << 3,

        CanManageDocuments = 1L << 4,
        CanReadMeters = 1L << 5,
        CanManageMeterChanges = 1L << 6,
        CanApproveMeterReadings = 1L << 7,

        CanManageReadings = CanReadMeters | CanManageMeterChanges | CanApproveMeterReadings,
        CanManageWorkHours = 1L << 8,
        CanManageWorkAssignments = 1L << 20,
        CanManageAppointments = 1L << 21,
        CanManageAnnouncements = 1L << 22,
        CanPerformYearEndClosing = 1L << 23,
        CanRegisterHistoricalContracts = 1L << 24,

        CanManageRoles = 1L << 9,

        CanShowStammdaten = 1L << 10,
        CanReadStammdaten = 1L << 11,
        CanWriteStammdaten = 1L << 12,

        CanShowParzellen = 1L << 13,
        CanReadParzellen = 1L << 14,
        CanWriteParzellen = 1L << 15,

        CanReadDocuments = 1L << 16,
        CanReadWorkHours = 1L << 17,
        CanReadRoles = 1L << 18,

        CanManageExports = 1L << 25,
        CanManageClubSeasonConfiguration = 1L << 26,
        CanManageDocumentTypes = 1L << 27,
        CanManageClubDocuments = 1L << 28,
        CanManageClubFunctions = 1L << 29,
        CanManageCommunicationWorkflows = 1L << 30,
        CanArchiveDocuments = 1L << 31,

        CanManageStammdaten = CanReadStammdaten | CanWriteStammdaten,
        CanManageParzellen = CanReadParzellen | CanWriteParzellen
    }
}
