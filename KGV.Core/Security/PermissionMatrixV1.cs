using System.Collections.Generic;

namespace KGV.Core.Security
{
    public readonly record struct PermissionAreaMatrixV1(
        string AreaKey,
        string DisplayName,
        PermissionFlags ShowPermission,
        PermissionFlags ReadPermission,
        PermissionFlags WritePermission);

    public static class PermissionMatrixV1
    {
        private static readonly PermissionAreaMatrixV1[] Areas =
        {
            new("stammdaten", "Stammdaten", PermissionFlags.CanShowStammdaten, PermissionFlags.CanReadStammdaten, PermissionFlags.CanWriteStammdaten),
            new("parzellen", "Parzellen", PermissionFlags.CanShowParzellen, PermissionFlags.CanReadParzellen, PermissionFlags.CanWriteParzellen),
            new("dokumente", "Dokumente", PermissionFlags.None, PermissionFlags.CanReadDocuments, PermissionFlags.CanManageDocuments),
            new("arbeitsstunden", "Arbeitsstunden", PermissionFlags.None, PermissionFlags.CanReadWorkHours, PermissionFlags.CanManageWorkHours),
            new("ablesen", "Ablesen", PermissionFlags.None, PermissionFlags.CanReadMeters, PermissionFlags.CanReadMeters),
            new("zaehlerwechsel", "Zählerwechsel", PermissionFlags.None, PermissionFlags.CanReadMeters, PermissionFlags.CanManageMeterChanges),
            new("freigaben", "Freigaben", PermissionFlags.None, PermissionFlags.CanApproveMeterReadings, PermissionFlags.CanApproveMeterReadings),
            new("rollen_rechte", "Rollen-/Rechteverwaltung", PermissionFlags.None, PermissionFlags.CanReadRoles, PermissionFlags.CanManageRoles)
        };

        public static IReadOnlyList<PermissionAreaMatrixV1> GetAreas() => Areas;
    }
}
