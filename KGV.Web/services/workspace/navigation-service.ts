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
  canAccessParcels: boolean;
  canManageMaintenance: boolean;
  canApproveWorkHours: boolean;
  canExport: boolean;
  canManageAdministration: boolean;
  canSearchMembers: boolean;
};

export type WorkspaceNavigation = {
  memberItems: NavigationItem[];
  navigationGroups: NavigationGroup[];
};

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
      ...navigationConfig.groups.meters,
      target: capabilities.canAccessMeters ? navigationConfig.groups.meters.target : undefined,
      items: [
        ...(capabilities.canReadMeterPhotos ? [navigationConfig.groups.meters.items.photoUploads] : []),
        ...(capabilities.canAccessMeterChanges ? [navigationConfig.groups.meters.items.meterChanges] : []),
      ],
    },
    { ...navigationConfig.groups.parcels, items: [], target: capabilities.canAccessParcels ? navigationConfig.groups.parcels.target : undefined },
    { ...navigationConfig.groups.maintenance, items: [], target: capabilities.canManageMaintenance ? navigationConfig.groups.maintenance.target : undefined },
    { ...navigationConfig.groups.workhours, items: [], target: capabilities.canApproveWorkHours ? navigationConfig.groups.workhours.target : undefined },
    { ...navigationConfig.groups.export, items: [], target: capabilities.canExport ? navigationConfig.groups.export.target : undefined },
    {
      ...navigationConfig.groups.administration,
      items: capabilities.canManageAdministration
        ? [
            navigationConfig.groups.administration.items.seasons,
            navigationConfig.groups.administration.items.annualClosing,
            navigationConfig.groups.administration.items.club,
          ]
        : [],
    },
    { ...navigationConfig.groups.members, target: capabilities.canSearchMembers ? navigationConfig.groups.members.target : undefined, items: memberItems },
  ] as NavigationGroup[]).filter((group) => Boolean(group.target) || group.items.length > 0);

  return { memberItems, navigationGroups };
}
