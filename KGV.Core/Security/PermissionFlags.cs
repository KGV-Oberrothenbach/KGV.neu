using System;

namespace KGV.Core.Security
{
    [Flags]
    public enum PermissionFlags
    {
        None = 0,

        CanSearchMembers = 1 << 0,
        CanViewMembers = 1 << 1,
        CanEditAllMembers = 1 << 2,

        CanSeeOwnDataOnly = 1 << 3,

        CanManageDocuments = 1 << 4,
        CanReadMeters = 1 << 5,
        CanManageMeterChanges = 1 << 6,
        CanApproveMeterReadings = 1 << 7,

        CanManageReadings = CanReadMeters | CanManageMeterChanges | CanApproveMeterReadings,
        CanManageWorkHours = 1 << 8,

        CanManageRoles = 1 << 9
    }
}
