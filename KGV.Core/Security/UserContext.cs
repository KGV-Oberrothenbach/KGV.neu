using System;

namespace KGV.Core.Security
{
    public sealed class UserContext
    {
        public Guid UserId { get; }
        public UserRole Role { get; }
        public long? MitgliedId { get; }
        public PermissionFlags Permissions { get; }
        public PermissionFlags BasePermissions { get; }
        public PermissionFlags GrantedPermissions { get; }
        public PermissionFlags RevokedPermissions { get; }

        public UserContext(Guid userId, UserRole role, long? mitgliedId, PermissionFlags permissions, PermissionFlags basePermissions, PermissionFlags grantedPermissions, PermissionFlags revokedPermissions)
        {
            UserId = userId;
            Role = role;
            MitgliedId = mitgliedId;
            Permissions = permissions;
            BasePermissions = basePermissions;
            GrantedPermissions = grantedPermissions;
            RevokedPermissions = revokedPermissions;
        }

        public bool Has(PermissionFlags permission) => (Permissions & permission) == permission;

        public override string ToString()
        {
            return $"{UserId} | {UserRoles.ToStorageValue(Role)} | MitgliedId={(MitgliedId.HasValue ? MitgliedId.Value.ToString() : "<null>")} | Effective={Permissions} | Base={BasePermissions} | Grants={GrantedPermissions} | Revocations={RevokedPermissions}";
        }
    }
}
