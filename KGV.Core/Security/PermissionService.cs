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
        {
            return GetRolePermissions(role)
                | GetDocumentPermissions(role)
                | GetMeterPermissions(role)
                | GetAdministrativePermissions(role);
        }

        public static PermissionFlags GetRolePermissions(UserRole role)
        {
            return GetMemberPermissions(role)
                | GetDocumentPermissions(role)
                | GetMeterPermissions(role)
                | GetAdministrativePermissions(role);
        }

        public static PermissionFlags NormalizeStoredPermissions(long? permissionMask)
        {
            if (!permissionMask.HasValue || permissionMask.Value <= 0)
                return PermissionFlags.None;

            return (PermissionFlags)permissionMask.Value & PermissionCatalog.GetKnownPermissionMask();
        }

        public static PermissionFlags ApplyOverrides(PermissionFlags basePermissions, PermissionFlags grantedPermissions, PermissionFlags revokedPermissions)
        {
            var effectivePermissions = basePermissions | grantedPermissions;
            effectivePermissions &= ~revokedPermissions;
            return effectivePermissions;
        }

        private static PermissionFlags GetMemberPermissions(UserRole role)
        {
            return role switch
            {
                UserRole.Admin =>
                    PermissionFlags.CanSearchMembers |
                    PermissionFlags.CanViewMembers |
                    PermissionFlags.CanEditAllMembers,

                UserRole.Vorstand =>
                    PermissionFlags.CanSearchMembers |
                    PermissionFlags.CanViewMembers |
                    PermissionFlags.CanEditAllMembers,

                _ =>
                    PermissionFlags.CanViewMembers |
                    PermissionFlags.CanSeeOwnDataOnly
            };
        }

        private static PermissionFlags GetDocumentPermissions(UserRole role)
        {
            return role switch
            {
                UserRole.Admin or UserRole.Vorstand => PermissionFlags.CanManageDocuments,
                _ => PermissionFlags.None
            };
        }

        private static PermissionFlags GetMeterPermissions(UserRole role)
        {
            return role switch
            {
                UserRole.Admin or UserRole.Vorstand =>
                    PermissionFlags.CanReadMeters |
                    PermissionFlags.CanManageMeterChanges |
                    PermissionFlags.CanApproveMeterReadings,
                _ => PermissionFlags.None
            };
        }

        private static PermissionFlags GetAdministrativePermissions(UserRole role)
        {
            return role switch
            {
                UserRole.Admin =>
                    PermissionFlags.CanManageWorkHours |
                    PermissionFlags.CanManageRoles,

                UserRole.Vorstand =>
                    PermissionFlags.CanManageWorkHours,

                _ => PermissionFlags.None
            };
        }

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
