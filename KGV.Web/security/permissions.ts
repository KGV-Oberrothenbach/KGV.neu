export type AppUserRole = "admin" | "vorstand" | "user";

const PermissionBits = {
  CanSearchMembers: 1 << 0,
  CanViewMembers: 1 << 1,
  CanEditAllMembers: 1 << 2,
  CanSeeOwnDataOnly: 1 << 3,
  CanManageDocuments: 1 << 4,
  CanReadMeters: 1 << 5,
  CanManageMeterChanges: 1 << 6,
  CanApproveMeterReadings: 1 << 7,
  CanManageWorkHours: 1 << 8,
  CanManageRoles: 1 << 9,
  CanShowStammdaten: 1 << 10,
  CanReadStammdaten: 1 << 11,
  CanWriteStammdaten: 1 << 12,
  CanShowParzellen: 1 << 13,
  CanReadParzellen: 1 << 14,
  CanWriteParzellen: 1 << 15,
  CanReadDocuments: 1 << 16,
  CanReadWorkHours: 1 << 17,
  CanReadRoles: 1 << 18,
  CanCreateMitglied: 1 << 19,
  CanManageWorkAssignments: 1 << 20,
  CanManageAppointments: 1 << 21,
  CanManageAnnouncements: 1 << 22,
  CanPerformYearEndClosing: 1 << 23,
  CanRegisterHistoricalContracts: 1 << 24,
  CanManageExports: 2 ** 25,
  CanManageClubSeasonConfiguration: 2 ** 26,
  CanManageDocumentTypes: 2 ** 27,
  CanManageClubDocuments: 2 ** 28,
  CanManageClubFunctions: 2 ** 29,
  CanManageCommunicationWorkflows: 2 ** 30,
  CanArchiveDocuments: 2 ** 31,
} as const;

// Camel-case aliases preserve the existing Web call sites while the Pascal-case
// members mirror KGV.Core's permission contract exactly.
export const Permission = {
  ...PermissionBits,
  searchMembers: PermissionBits.CanSearchMembers,
  viewMembers: PermissionBits.CanViewMembers,
  editAllMembers: PermissionBits.CanEditAllMembers,
  seeOwnData: PermissionBits.CanSeeOwnDataOnly,
  manageDocuments: PermissionBits.CanManageDocuments,
  readMeters: PermissionBits.CanReadMeters,
  manageMeterChanges: PermissionBits.CanManageMeterChanges,
  approveMeterReadings: PermissionBits.CanApproveMeterReadings,
  manageWorkHours: PermissionBits.CanManageWorkHours,
  manageRoles: PermissionBits.CanManageRoles,
  showStammdaten: PermissionBits.CanShowStammdaten,
  readStammdaten: PermissionBits.CanReadStammdaten,
  writeStammdaten: PermissionBits.CanWriteStammdaten,
  showParzellen: PermissionBits.CanShowParzellen,
  readParzellen: PermissionBits.CanReadParzellen,
  writeParzellen: PermissionBits.CanWriteParzellen,
  readDocuments: PermissionBits.CanReadDocuments,
  readWorkHours: PermissionBits.CanReadWorkHours,
  readRoles: PermissionBits.CanReadRoles,
  createMember: PermissionBits.CanCreateMitglied,
  manageWorkAssignments: PermissionBits.CanManageWorkAssignments,
  manageAppointments: PermissionBits.CanManageAppointments,
  manageAnnouncements: PermissionBits.CanManageAnnouncements,
  performYearEndClosing: PermissionBits.CanPerformYearEndClosing,
  registerHistoricalContracts: PermissionBits.CanRegisterHistoricalContracts,
  manageExports: PermissionBits.CanManageExports,
  manageClubSeasonConfiguration: PermissionBits.CanManageClubSeasonConfiguration,
  manageDocumentTypes: PermissionBits.CanManageDocumentTypes,
  manageClubDocuments: PermissionBits.CanManageClubDocuments,
  manageClubFunctions: PermissionBits.CanManageClubFunctions,
  manageCommunicationWorkflows: PermissionBits.CanManageCommunicationWorkflows,
  archiveDocuments: PermissionBits.CanArchiveDocuments,
} as const;

const AllPermissionBits = Object.values(PermissionBits);
const addPermission = (mask: number, permission: number) => hasPermission(mask, permission) ? mask : mask + permission;
const removePermission = (mask: number, permission: number) => hasPermission(mask, permission) ? mask - permission : mask;
const combinePermissions = (...permissions: number[]) => permissions.reduce((mask, permission) => addPermission(mask, permission), 0);

const UserPermissions = Permission.CanViewMembers | Permission.CanSeeOwnDataOnly;
const VorstandPermissions = combinePermissions(Permission.CanSearchMembers, Permission.CanViewMembers, Permission.CanEditAllMembers, Permission.CanManageDocuments, Permission.CanReadMeters, Permission.CanManageMeterChanges, Permission.CanApproveMeterReadings, Permission.CanManageWorkHours, Permission.CanShowStammdaten, Permission.CanReadStammdaten, Permission.CanWriteStammdaten, Permission.CanReadParzellen, Permission.CanWriteParzellen, Permission.CanReadDocuments, Permission.CanReadWorkHours, Permission.CanReadRoles, Permission.CanManageWorkAssignments, Permission.CanManageAppointments, Permission.CanManageAnnouncements, Permission.CanManageExports, Permission.CanManageClubDocuments, Permission.CanManageClubFunctions, Permission.CanManageCommunicationWorkflows);
const AdminPermissions = combinePermissions(VorstandPermissions, Permission.CanManageRoles, Permission.CanCreateMitglied, Permission.CanPerformYearEndClosing, Permission.CanManageClubSeasonConfiguration, Permission.CanManageDocumentTypes, Permission.CanArchiveDocuments);

export function getRolePermissions(role: AppUserRole): number {
  if (role === "admin") return AdminPermissions;
  if (role === "vorstand") return VorstandPermissions;
  return UserPermissions;
}

export function applyPermissionOverrides(base: number, grants: number, revocations: number): number {
  return AllPermissionBits.reduce((effective, permission) => {
    const granted = hasPermission(grants, permission);
    const revoked = hasPermission(revocations, permission);
    return revoked ? removePermission(effective, permission) : granted ? addPermission(effective, permission) : effective;
  }, base);
}

export function getEffectivePermissions(role: AppUserRole, grants: number, revocations: number): number {
  return applyPermissionOverrides(getRolePermissions(role), grants, revocations);
}

export function hasPermission(mask: number, permission: number): boolean {
  return Number.isSafeInteger(mask) && permission > 0 && Math.floor(mask / permission) % 2 === 1;
}

export const PermissionAreas = [
  { key: "mitgliedaufnahme", label: "Mitglieder aufnehmen / verpachten", all: Permission.CanCreateMitglied, read: Permission.CanCreateMitglied, write: Permission.CanCreateMitglied },
  { key: "stammdaten", label: "Stammdaten", all: Permission.CanShowStammdaten | Permission.CanReadStammdaten | Permission.CanWriteStammdaten, read: Permission.CanReadStammdaten, write: Permission.CanWriteStammdaten },
  { key: "parzellen", label: "Parzellen", all: Permission.CanShowParzellen | Permission.CanReadParzellen | Permission.CanWriteParzellen, read: Permission.CanReadParzellen, write: Permission.CanWriteParzellen },
  { key: "dokumente", label: "Dokumente", all: Permission.CanReadDocuments | Permission.CanManageDocuments, read: Permission.CanReadDocuments, write: Permission.CanManageDocuments },
  { key: "arbeitsstunden", label: "Arbeitsstunden", all: Permission.CanReadWorkHours | Permission.CanManageWorkHours, read: Permission.CanReadWorkHours, write: Permission.CanManageWorkHours },
  { key: "zaehlerwechsel", label: "Zählerwechsel", all: Permission.CanReadMeters | Permission.CanManageMeterChanges, read: Permission.CanReadMeters, write: Permission.CanManageMeterChanges },
  { key: "ablesungsfreigaben", label: "Ablesungsfreigaben", all: Permission.CanReadMeters | Permission.CanApproveMeterReadings, read: Permission.CanReadMeters, write: Permission.CanApproveMeterReadings },
  { key: "rollen", label: "Rollen / Rechte", all: Permission.CanReadRoles | Permission.CanManageRoles, read: Permission.CanReadRoles, write: Permission.CanManageRoles },
  { key: "jahresabschluss", label: "Jahresabschluss", all: Permission.CanPerformYearEndClosing, read: Permission.CanPerformYearEndClosing, write: Permission.CanPerformYearEndClosing },
  { key: "altvertraege", label: "Historische Verträge", all: Permission.CanRegisterHistoricalContracts, read: Permission.CanRegisterHistoricalContracts, write: Permission.CanRegisterHistoricalContracts },
  { key: "exporte", label: "Exporte", all: Permission.CanManageExports, read: Permission.CanManageExports, write: Permission.CanManageExports },
  { key: "vereins_saison", label: "Vereins- / Saisonkonfiguration", all: Permission.CanManageClubSeasonConfiguration, read: Permission.CanManageClubSeasonConfiguration, write: Permission.CanManageClubSeasonConfiguration },
  { key: "dokumenttypen", label: "Dokumenttypen", all: Permission.CanManageDocumentTypes, read: Permission.CanManageDocumentTypes, write: Permission.CanManageDocumentTypes },
  { key: "vereinsunterlagen", label: "Vereinsunterlagen", all: Permission.CanManageClubDocuments, read: Permission.CanManageClubDocuments, write: Permission.CanManageClubDocuments },
  { key: "vereinsfunktionen", label: "Vereinsfunktionen", all: Permission.CanManageClubFunctions, read: Permission.CanManageClubFunctions, write: Permission.CanManageClubFunctions },
  { key: "kommunikation", label: "Kommunikationsvorgänge", all: Permission.CanManageCommunicationWorkflows, read: Permission.CanManageCommunicationWorkflows, write: Permission.CanManageCommunicationWorkflows },
  { key: "dokumentarchiv", label: "Dokumentarchivierung", all: Permission.CanArchiveDocuments, read: Permission.CanArchiveDocuments, write: Permission.CanArchiveDocuments },
] as const;

export const ControllablePermissionMask = AllPermissionBits.reduce((mask, permission) => mask + permission, 0);

export function preserveUncontrolledPermissions(existingPermissions: number, calculatedPermissions: number): number {
  return AllPermissionBits.reduce((result, permission) => !hasPermission(ControllablePermissionMask, permission)
    ? result
    : hasPermission(calculatedPermissions, permission)
      ? addPermission(result, permission)
      : removePermission(result, permission), existingPermissions);
}

export function derivePermissionOverrides(base: number, required: number): { grants: number; revocations: number } {
  return AllPermissionBits.reduce((result, permission) => !hasPermission(ControllablePermissionMask, permission) ? result : ({
    grants: hasPermission(required, permission) && !hasPermission(base, permission) ? addPermission(result.grants, permission) : result.grants,
    revocations: hasPermission(base, permission) && !hasPermission(required, permission) ? addPermission(result.revocations, permission) : result.revocations,
  }), { grants: 0, revocations: 0 });
}
