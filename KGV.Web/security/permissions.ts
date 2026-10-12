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
} as const;

const UserPermissions = Permission.CanViewMembers | Permission.CanSeeOwnDataOnly;
const VorstandPermissions = Permission.CanSearchMembers | Permission.CanViewMembers | Permission.CanEditAllMembers | Permission.CanManageDocuments | Permission.CanReadMeters | Permission.CanManageMeterChanges | Permission.CanApproveMeterReadings | Permission.CanManageWorkHours | Permission.CanShowStammdaten | Permission.CanReadStammdaten | Permission.CanWriteStammdaten | Permission.CanReadParzellen | Permission.CanWriteParzellen | Permission.CanReadDocuments | Permission.CanReadWorkHours | Permission.CanReadRoles | Permission.CanManageWorkAssignments | Permission.CanManageAppointments | Permission.CanManageAnnouncements;
const AdminPermissions = VorstandPermissions | Permission.CanManageRoles | Permission.CanCreateMitglied | Permission.CanPerformYearEndClosing;

export function getRolePermissions(role: AppUserRole): number {
  if (role === "admin") return AdminPermissions;
  if (role === "vorstand") return VorstandPermissions;
  return UserPermissions;
}

export function applyPermissionOverrides(base: number, grants: number, revocations: number): number {
  return (base | grants) & ~revocations;
}

export function getEffectivePermissions(role: AppUserRole, grants: number, revocations: number): number {
  return applyPermissionOverrides(getRolePermissions(role), grants, revocations);
}

export function hasPermission(mask: number, permission: number): boolean {
  return (mask & permission) === permission;
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
] as const;

export const ControllablePermissionMask = PermissionAreas.reduce((mask, area) => mask | area.all, 0);

export function preserveUncontrolledPermissions(existingPermissions: number, calculatedPermissions: number): number {
  return (existingPermissions & ~ControllablePermissionMask) | (calculatedPermissions & ControllablePermissionMask);
}
