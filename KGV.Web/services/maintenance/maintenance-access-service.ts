export type MaintenanceAccess = { canRead: boolean; canManageContracts: boolean; canManageAssignments: boolean };

// The app-wide capability decision is made by the existing workspace layer.
export const maintenanceAccess = (canManage: boolean): MaintenanceAccess => ({ canRead: true, canManageContracts: canManage, canManageAssignments: canManage });
