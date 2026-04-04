using System;

namespace KGV.Core.Security
{
    public sealed class UserPermissionSettings
    {
        public Guid? AuthUserId { get; set; }
        public int? MitgliedId { get; set; }
        public string Role { get; set; } = UserRoles.User;
        public PermissionFlags GrantedPermissions { get; set; }
        public PermissionFlags RevokedPermissions { get; set; }

        public bool HasLinkedUser => AuthUserId.HasValue;
        public UserRole ParsedRole => UserRoles.Parse(Role);
        public PermissionFlags BasePermissions => PermissionService.GetRolePermissions(ParsedRole);
        public PermissionFlags EffectivePermissions => PermissionService.ApplyOverrides(BasePermissions, GrantedPermissions, RevokedPermissions);
    }
}
