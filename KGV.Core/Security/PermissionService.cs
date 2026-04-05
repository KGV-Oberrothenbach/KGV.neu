using System;

namespace KGV.Core.Security
{
    public interface IPermissionService
    {
        PermissionFlags GetPermissions(UserRole role);
        UserContext CreateContext(Guid userId, string? role, long? mitgliedId, long? grantedPermissions = null, long? revokedPermissions = null);
    }

    public sealed class PermissionService : IPermissionService
    {
        public PermissionFlags GetPermissions(UserRole role)
            => GetRolePermissions(role);

        public static PermissionFlags GetRolePermissions(UserRole role)
        {
            return role switch
            {
                UserRole.Admin => GetAdminPermissions(),
                UserRole.Vorstand => GetVorstandPermissions(),
                _ => GetUserPermissions()
            };
        }

        public static PermissionFlags NormalizeStoredPermissions(long? permissionMask)
        {
            if (!permissionMask.HasValue || permissionMask.Value <= 0)
                return PermissionFlags.None;

            return (PermissionFlags)permissionMask.Value & PermissionCatalog.GetKnownPermissionMask();
        }

        public static PermissionFlags ApplyOverrides(PermissionFlags basePermissions, PermissionFlags grantedPermissions, PermissionFlags revokedPermissions)
        {
            var knownMask = PermissionCatalog.GetKnownPermissionMask();
            var normalizedBasePermissions = basePermissions & knownMask;
            var normalizedGrantedPermissions = grantedPermissions & knownMask;
            var normalizedRevokedPermissions = revokedPermissions & knownMask;

            var effectivePermissions = normalizedBasePermissions | normalizedGrantedPermissions;
            effectivePermissions &= ~normalizedRevokedPermissions;
            return effectivePermissions;
        }

        private static PermissionFlags GetUserPermissions()
            => PermissionFlags.CanViewMembers
               | PermissionFlags.CanSeeOwnDataOnly;

        private static PermissionFlags GetVorstandPermissions()
            => PermissionFlags.CanSearchMembers
               | PermissionFlags.CanViewMembers
               | PermissionFlags.CanEditAllMembers
               | PermissionFlags.CanShowStammdaten
               | PermissionFlags.CanReadStammdaten
               | PermissionFlags.CanWriteStammdaten
               | PermissionFlags.CanShowParzellen
               | PermissionFlags.CanReadParzellen
               | PermissionFlags.CanWriteParzellen
               | PermissionFlags.CanReadDocuments
               | PermissionFlags.CanManageDocuments
               | PermissionFlags.CanReadWorkHours
               | PermissionFlags.CanManageWorkHours
               | PermissionFlags.CanReadMeters
               | PermissionFlags.CanManageMeterChanges
               | PermissionFlags.CanApproveMeterReadings
               | PermissionFlags.CanReadRoles;

        private static PermissionFlags GetAdminPermissions()
            => GetVorstandPermissions()
               | PermissionFlags.CanManageRoles;

        public UserContext CreateContext(Guid userId, string? role, long? mitgliedId, long? grantedPermissions = null, long? revokedPermissions = null)
        {
            var parsedRole = UserRoles.Parse(role);
            var basePermissions = GetPermissions(parsedRole);
            var normalizedGrantedPermissions = NormalizeStoredPermissions(grantedPermissions);
            var normalizedRevokedPermissions = NormalizeStoredPermissions(revokedPermissions);
            var permissions = ApplyOverrides(basePermissions, normalizedGrantedPermissions, normalizedRevokedPermissions);
            return new UserContext(userId, parsedRole, mitgliedId, permissions, basePermissions, normalizedGrantedPermissions, normalizedRevokedPermissions);
        }
    }
}
