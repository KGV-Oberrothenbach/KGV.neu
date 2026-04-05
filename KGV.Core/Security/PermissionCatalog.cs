using System.Collections.Generic;
using System.Linq;

namespace KGV.Core.Security
{
    public readonly record struct PermissionDefinition(PermissionFlags Flag, string DisplayName);

    public static class PermissionCatalog
    {
        private static readonly PermissionDefinition[] AllPermissions =
        {
            new(PermissionFlags.CanSearchMembers, "Mitglieder suchen"),
            new(PermissionFlags.CanViewMembers, "Mitglieder sehen"),
            new(PermissionFlags.CanEditAllMembers, "Mitglieder bearbeiten"),
            new(PermissionFlags.CanSeeOwnDataOnly, "Eigene Daten sehen"),
            new(PermissionFlags.CanShowStammdaten, "Stammdaten anzeigen"),
            new(PermissionFlags.CanReadStammdaten, "Stammdaten lesen"),
            new(PermissionFlags.CanWriteStammdaten, "Stammdaten bearbeiten"),
            new(PermissionFlags.CanShowParzellen, "Parzellen anzeigen"),
            new(PermissionFlags.CanReadParzellen, "Parzellen lesen"),
            new(PermissionFlags.CanWriteParzellen, "Parzellen bearbeiten"),
            new(PermissionFlags.CanReadDocuments, "Dokumente lesen"),
            new(PermissionFlags.CanManageDocuments, "Dokumente verwalten"),
            new(PermissionFlags.CanReadWorkHours, "Arbeitsstunden lesen"),
            new(PermissionFlags.CanReadMeters, "Zähler lesen"),
            new(PermissionFlags.CanManageMeterChanges, "Zählerwechsel verwalten"),
            new(PermissionFlags.CanApproveMeterReadings, "Ablesungen freigeben"),
            new(PermissionFlags.CanManageWorkHours, "Arbeitsstunden verwalten"),
            new(PermissionFlags.CanReadRoles, "Rollen/Rechte sehen"),
            new(PermissionFlags.CanManageRoles, "Rollen verwalten")
        };

        private static readonly PermissionDefinition[] UserSpecificEditablePermissions =
        {
            new(PermissionFlags.CanShowStammdaten, "Stammdaten anzeigen"),
            new(PermissionFlags.CanReadStammdaten, "Stammdaten lesen"),
            new(PermissionFlags.CanWriteStammdaten, "Stammdaten bearbeiten"),
            new(PermissionFlags.CanShowParzellen, "Parzellen anzeigen"),
            new(PermissionFlags.CanReadParzellen, "Parzellen lesen"),
            new(PermissionFlags.CanWriteParzellen, "Parzellen bearbeiten"),
            new(PermissionFlags.CanReadDocuments, "Dokumente lesen"),
            new(PermissionFlags.CanReadWorkHours, "Arbeitsstunden lesen"),
            new(PermissionFlags.CanReadMeters, "Zähler lesen"),
            new(PermissionFlags.CanManageMeterChanges, "Zählerwechsel verwalten"),
            new(PermissionFlags.CanApproveMeterReadings, "Ablesungen freigeben"),
            new(PermissionFlags.CanManageDocuments, "Dokumente verwalten"),
            new(PermissionFlags.CanManageWorkHours, "Arbeitsstunden verwalten"),
            new(PermissionFlags.CanReadRoles, "Rollen/Rechte sehen"),
            new(PermissionFlags.CanManageRoles, "Rollen verwalten")
        };

        public static IReadOnlyList<PermissionDefinition> GetAllPermissions() => AllPermissions;

        public static IReadOnlyList<PermissionDefinition> GetUserSpecificEditablePermissions() => UserSpecificEditablePermissions;

        public static PermissionFlags GetKnownPermissionMask()
        {
            var mask = PermissionFlags.None;
            foreach (var permission in AllPermissions)
                mask |= permission.Flag;

            return mask;
        }

        public static string GetDisplayName(PermissionFlags permission)
        {
            var match = AllPermissions.FirstOrDefault(x => x.Flag == permission);
            return string.IsNullOrWhiteSpace(match.DisplayName)
                ? permission.ToString()
                : match.DisplayName;
        }

        public static string FormatPermissions(PermissionFlags permissions, string emptyText = "Keine Fachrechte aktiv.")
        {
            var names = AllPermissions
                .Where(x => x.Flag != PermissionFlags.None && permissions.HasFlag(x.Flag))
                .Select(x => x.DisplayName)
                .ToList();

            return names.Count == 0
                ? emptyText
                : string.Join(", ", names);
        }

        public static string FormatGrantedPermissions(PermissionFlags permissions)
            => FormatPermissions(permissions, "Keine zusätzlichen Fachrechte aktiv.");
    }
}
