/**
 * Dashboard-only destinations are intentionally absent from the sidebar.
 * Their access rights still need to be honoured by the workspace resolver.
 */
export type DashboardManagementAccess = {
  canManageWorkAssignments: boolean;
  canManageAppointments: boolean;
  canManageAnnouncements: boolean;
};

export type DashboardManagementTarget = {
  id: "arbeitseinsaetze" | "termine" | "bekanntmachungen";
  label: string;
  detail: string;
};

export function getDashboardManagementTargets(access: DashboardManagementAccess): DashboardManagementTarget[] {
  const targets: DashboardManagementTarget[] = [];
  if (access.canManageWorkAssignments) targets.push({
    id: "arbeitseinsaetze", label: "Arbeitseinsätze", detail: "Arbeitseinsätze und Teilnehmer verwalten",
  });
  if (access.canManageAppointments) targets.push({
    id: "termine", label: "Termine", detail: "Vereinstermine verwalten",
  });
  if (access.canManageAnnouncements) targets.push({
    id: "bekanntmachungen", label: "Bekanntmachungen", detail: "Bekanntmachungen verwalten",
  });
  return targets;
}
