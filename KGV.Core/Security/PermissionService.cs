using System;

namespace KGV.Core.Security
{
    public interface IPermissionService
    {
        PermissionFlags GetPermissions(UserRole role);
        UserContext CreateContext(Guid userId, string? role, long? mitgliedId);
    }

    public sealed class PermissionService : IPermissionService
    {
        public PermissionFlags GetPermissions(UserRole role)
        {
            return GetMemberPermissions(role)
                | GetDocumentPermissions(role)
                | GetMeterPermissions(role)
                | GetAdministrativePermissions(role);
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

        public UserContext CreateContext(Guid userId, string? role, long? mitgliedId)
        {
            var parsedRole = UserRoles.Parse(role);
            var permissions = GetPermissions(parsedRole);
            return new UserContext(userId, parsedRole, mitgliedId, permissions);
        }
    }
}
