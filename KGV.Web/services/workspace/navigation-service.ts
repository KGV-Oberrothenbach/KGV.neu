import { type NavigationGroup, type NavigationItem } from "../../features/navigation/Navigation";
import { navigationConfig } from "../../features/navigation/NavigationConfig";

export type NavigationCapabilities = {
  season: number;
  creatingMember: boolean;
  hasMemberContext: boolean;
  canReadStammdaten: boolean;
  canReadMemberDocuments: boolean;
  canEditMemberProtocols: boolean;
  canReadMemberGardens: boolean;
  canManageMemberRoles: boolean;
  canReadMemberWorkHours: boolean;
  canAccessMeters: boolean;
  canReadMeterPhotos: boolean;
  canAccessMeterChanges: boolean;
  canApproveMeterReadings: boolean;
  canAccessParcels: boolean;
  canManageMaintenance: boolean;
  canApproveWorkHours: boolean;
  canExport: boolean;
  canManageAdministration: boolean;
  canManageProcesses: boolean;
  canAccessCommunication: boolean;
  openProcessCount: number;
  canManageSeasonConfiguration: boolean;
  canPerformYearEndClosing: boolean;
  canManageClubConfiguration: boolean;
  canManageClubDocuments: boolean;
  canReadDocumentTypes: boolean;
  canManageClubFunctions: boolean;
  canRegisterHistoricalContracts: boolean;
  canSearchMembers: boolean;
};

export type WorkspaceNavigation = { memberItems: NavigationItem[]; navigationGroups: NavigationGroup[] };

export function buildNavigation(capabilities: NavigationCapabilities): WorkspaceNavigation {
  const memberItems: NavigationItem[] = capabilities.creatingMember
    ? (capabilities.canReadStammdaten ? [navigationConfig.memberItems.newMasterData] : [])
    : capabilities.hasMemberContext
      ? [
          ...(capabilities.canReadStammdaten ? [navigationConfig.memberItems.masterData] : []),
          ...(capabilities.canReadMemberDocuments ? [navigationConfig.memberItems.documents] : []),
          ...(capabilities.canEditMemberProtocols ? [navigationConfig.memberItems.protocols] : []),
          navigationConfig.memberItems.maintenance,
          ...(capabilities.canReadStammdaten ? [navigationConfig.memberItems.secondaryMember] : []),
          ...(capabilities.canReadMemberGardens ? [navigationConfig.memberItems.gardens] : []),
          ...(capabilities.canManageMemberRoles ? [navigationConfig.memberItems.administration] : []),
          ...(capabilities.canReadMemberWorkHours ? [navigationConfig.memberItems.workHours] : []),
        ]
      : [];

  const navigationGroups = ([
    { ...navigationConfig.groups.home, items: [], target: { ...navigationConfig.groups.home.target, detail: `${navigationConfig.groups.home.target.detail} ${capabilities.season}` } },
    { ...navigationConfig.groups.imprint, items: [] },
    {
      ...navigationConfig.groups.processes,
      items: capabilities.canAccessCommunication ? Object.values(navigationConfig.groups.processes.items) : [],
    },
    {
      ...navigationConfig.groups.meters,
      items: [
        ...(capabilities.canAccessMeters ? [navigationConfig.groups.meters.items.capture, navigationConfig.groups.meters.items.annualCapture] : []),
        ...(capabilities.canAccessMeterChanges ? [navigationConfig.groups.meters.items.meterChanges, navigationConfig.groups.meters.items.rfidSetup] : []),
        ...(capabilities.canApproveMeterReadings ? [navigationConfig.groups.meters.items.approve] : []),
      ],
    },
    {
      ...navigationConfig.groups.administration,
      items: [
        ...(capabilities.canAccessParcels ? [navigationConfig.groups.administration.items.parcels] : []),
        ...(capabilities.canManageMaintenance ? [navigationConfig.groups.administration.items.maintenance] : []),
        ...(capabilities.canManageAdministration ? [
          navigationConfig.groups.administration.items.seasons,
          navigationConfig.groups.administration.items.annualClosing,
          navigationConfig.groups.administration.items.club,
          navigationConfig.groups.administration.items.clubDocuments,
          navigationConfig.groups.administration.items.clubFunctions,
          navigationConfig.groups.administration.items.documentTypes,
        ] : []),
        ...(capabilities.canRegisterHistoricalContracts ? [navigationConfig.groups.administration.items.backfillContracts] : []),
      ],
    },
    { ...navigationConfig.groups.workhours, items: [], target: capabilities.canApproveWorkHours ? navigationConfig.groups.workhours.target : undefined },
    { ...navigationConfig.groups.export, items: [], target: capabilities.canExport ? navigationConfig.groups.export.target : undefined },
    { ...navigationConfig.groups.members, items: memberItems, target: capabilities.canSearchMembers ? navigationConfig.groups.members.target : undefined },
  ] as NavigationGroup[]).filter((group) => group.kind === "item" ? Boolean(group.target) : group.items.length > 0);

  return { memberItems, navigationGroups };
}
