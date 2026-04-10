using System.Collections.Generic;
using System.Linq;

namespace KGV.Core.Security
{
    public readonly record struct PermissionDefinition(PermissionFlags Flag, string DisplayName);
    public readonly record struct PermissionAreaDefinition(
        string AreaKey,
        string DisplayName,
        PermissionFlags AllPermissions,
        PermissionFlags ReadPermissions,
        PermissionFlags WritePermissions);

    public enum PermissionAreaAccessLevel
    {
        None = 0,
        Read = 1,
        Write = 2
    }

    public static class PermissionCatalog
    {
        private static readonly PermissionDefinition[] AllPermissions =
        {
            new(PermissionFlags.CanSearchMembers, "Mitglieder suchen"),
            new(PermissionFlags.CanViewMembers, "Mitglieder sehen"),
            new(PermissionFlags.CanEditAllMembers, "Mitglieder bearbeiten"),
            new(PermissionFlags.CanCreateMitglied, "Mitglieder aufnehmen / verpachten"),
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

        private static readonly PermissionFlags[] UserSpecificEditablePermissionFlags =
        {
            PermissionFlags.CanCreateMitglied,
            PermissionFlags.CanShowStammdaten,
            PermissionFlags.CanReadStammdaten,
            PermissionFlags.CanWriteStammdaten,
            PermissionFlags.CanShowParzellen,
            PermissionFlags.CanReadParzellen,
            PermissionFlags.CanWriteParzellen,
            PermissionFlags.CanReadDocuments,
            PermissionFlags.CanManageDocuments,
            PermissionFlags.CanReadWorkHours,
            PermissionFlags.CanManageWorkHours,
            PermissionFlags.CanReadMeters,
            PermissionFlags.CanManageMeterChanges,
            PermissionFlags.CanApproveMeterReadings,
            PermissionFlags.CanReadRoles,
            PermissionFlags.CanManageRoles
        };

        private static readonly PermissionDefinition[] UserSpecificEditablePermissions = AllPermissions
            .Where(x => UserSpecificEditablePermissionFlags.Contains(x.Flag))
            .ToArray();

        private static readonly PermissionAreaDefinition[] GlobalEditablePermissionAreas =
        {
            new(
                "mitgliedaufnahme",
                "Mitglieder aufnehmen / verpachten",
                PermissionFlags.CanCreateMitglied,
                PermissionFlags.CanCreateMitglied,
                PermissionFlags.CanCreateMitglied),
            new(
                "stammdaten",
                "Stammdaten",
                PermissionFlags.CanShowStammdaten | PermissionFlags.CanReadStammdaten | PermissionFlags.CanWriteStammdaten,
                PermissionFlags.CanReadStammdaten,
                PermissionFlags.CanWriteStammdaten),
            new(
                "parzellen",
                "Parzellen",
                PermissionFlags.CanShowParzellen | PermissionFlags.CanReadParzellen | PermissionFlags.CanWriteParzellen,
                PermissionFlags.CanReadParzellen,
                PermissionFlags.CanWriteParzellen),
            new(
                "dokumente",
                "Dokumente",
                PermissionFlags.CanReadDocuments | PermissionFlags.CanManageDocuments,
                PermissionFlags.CanReadDocuments,
                PermissionFlags.CanManageDocuments),
            new(
                "arbeitsstunden",
                "Arbeitsstunden",
                PermissionFlags.CanReadWorkHours | PermissionFlags.CanManageWorkHours,
                PermissionFlags.CanReadWorkHours,
                PermissionFlags.CanManageWorkHours),
            new(
                "zaehlerwechsel",
                "Zählerwechsel",
                PermissionFlags.CanReadMeters | PermissionFlags.CanManageMeterChanges,
                PermissionFlags.CanReadMeters,
                PermissionFlags.CanManageMeterChanges),
            new(
                "ablesungsfreigaben",
                "Ablesungsfreigaben",
                PermissionFlags.CanReadMeters | PermissionFlags.CanApproveMeterReadings,
                PermissionFlags.CanReadMeters,
                PermissionFlags.CanApproveMeterReadings),
            new(
                "rollen_rechte",
                "Rollen/Rechte",
                PermissionFlags.CanReadRoles | PermissionFlags.CanManageRoles,
                PermissionFlags.CanReadRoles,
                PermissionFlags.CanManageRoles)
        };

        public static IReadOnlyList<PermissionDefinition> GetAllPermissions() => AllPermissions;

        public static IReadOnlyList<PermissionDefinition> GetUserSpecificEditablePermissions() => UserSpecificEditablePermissions;

        public static IReadOnlyList<PermissionAreaDefinition> GetGlobalEditablePermissionAreas() => GlobalEditablePermissionAreas;

        public static PermissionFlags GetGlobalEditablePermissionMask()
        {
            var mask = PermissionFlags.None;
            foreach (var area in GlobalEditablePermissionAreas)
                mask |= area.AllPermissions;

            return mask;
        }

        public static PermissionAreaAccessLevel GetAccessLevel(PermissionFlags permissions, PermissionAreaDefinition area)
        {
            if (area.WritePermissions != PermissionFlags.None && permissions.HasFlag(area.WritePermissions))
                return PermissionAreaAccessLevel.Write;

            if (area.ReadPermissions != PermissionFlags.None && permissions.HasFlag(area.ReadPermissions))
                return PermissionAreaAccessLevel.Read;

            return PermissionAreaAccessLevel.None;
        }

        public static PermissionFlags GetRequiredPermissions(PermissionAreaDefinition area, PermissionAreaAccessLevel level)
            => level switch
            {
                PermissionAreaAccessLevel.Write => area.WritePermissions,
                PermissionAreaAccessLevel.Read => area.ReadPermissions,
                _ => PermissionFlags.None
            };

        public static string FormatAccessLevel(PermissionAreaAccessLevel level)
            => level switch
            {
                PermissionAreaAccessLevel.Write => "Bearbeiten",
                PermissionAreaAccessLevel.Read => "Lesen",
                _ => "Aus"
            };

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
