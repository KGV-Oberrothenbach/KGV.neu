namespace KGV.Core.Security
{
    public static class PermissionChecks
    {
        public static bool HasPermission(UserContext? context, PermissionFlags permission)
            => context?.Has(permission) == true;

        public static bool HasOwnMemberContextAccess(UserContext? context)
            => context?.MitgliedId is > 0 && CanSeeOwnDataOnly(context);

        public static bool IsOwnMemberContext(UserContext? context, int? memberId)
            => HasOwnMemberContextAccess(context)
               && memberId is > 0
               && context!.MitgliedId == memberId;

        public static bool CanSearchMembers(UserContext? context)
            => HasPermission(context, PermissionFlags.CanSearchMembers);

        public static bool CanViewMembers(UserContext? context)
            => HasPermission(context, PermissionFlags.CanViewMembers);

        public static bool CanEditAllMembers(UserContext? context)
            => HasPermission(context, PermissionFlags.CanEditAllMembers);

        public static bool CanSeeOwnDataOnly(UserContext? context)
            => HasPermission(context, PermissionFlags.CanSeeOwnDataOnly);

        public static bool CanShowStammdaten(UserContext? context)
            => HasPermission(context, PermissionFlags.CanShowStammdaten)
               || CanReadStammdaten(context)
               || CanWriteStammdaten(context);

        public static bool CanReadStammdaten(UserContext? context)
            => HasPermission(context, PermissionFlags.CanReadStammdaten)
               || CanWriteStammdaten(context);

        public static bool CanWriteStammdaten(UserContext? context)
            => HasPermission(context, PermissionFlags.CanWriteStammdaten);

        public static bool CanShowStammdatenForMember(UserContext? context, int? memberId)
            => CanShowStammdaten(context)
               || IsOwnMemberContext(context, memberId);

        public static bool CanReadStammdatenForMember(UserContext? context, int? memberId)
            => CanReadStammdaten(context)
               || IsOwnMemberContext(context, memberId);

        public static bool CanWriteStammdatenForMember(UserContext? context, int? memberId)
            => CanWriteStammdaten(context)
               || IsOwnMemberContext(context, memberId);

        public static bool CanShowParzellen(UserContext? context)
            => HasPermission(context, PermissionFlags.CanShowParzellen)
               || CanReadParzellen(context)
               || CanWriteParzellen(context);

        public static bool CanReadParzellen(UserContext? context)
            => HasPermission(context, PermissionFlags.CanReadParzellen)
               || CanWriteParzellen(context);

        public static bool CanWriteParzellen(UserContext? context)
            => HasPermission(context, PermissionFlags.CanWriteParzellen);

        public static bool CanShowParzellenForMember(UserContext? context, int? memberId)
            => CanShowParzellen(context)
               || IsOwnMemberContext(context, memberId);

        public static bool CanReadParzellenForMember(UserContext? context, int? memberId)
            => CanReadParzellen(context)
               || IsOwnMemberContext(context, memberId);

        public static bool CanReadDocuments(UserContext? context)
            => HasPermission(context, PermissionFlags.CanReadDocuments)
               || CanManageDocuments(context);

        public static bool CanManageDocuments(UserContext? context)
            => HasPermission(context, PermissionFlags.CanManageDocuments);

        public static bool CanReadDocumentsForMember(UserContext? context, int? memberId)
            => CanReadDocuments(context)
               || IsOwnMemberContext(context, memberId);

        public static bool CanReadWorkHours(UserContext? context)
            => HasPermission(context, PermissionFlags.CanReadWorkHours)
               || HasPermission(context, PermissionFlags.CanManageWorkHours);

        public static bool CanManageWorkHours(UserContext? context)
            => HasPermission(context, PermissionFlags.CanManageWorkHours);

        public static bool CanReadWorkHoursForMember(UserContext? context, int? memberId)
            => CanReadWorkHours(context)
               || IsOwnMemberContext(context, memberId);

        public static bool CanReadRoleManagement(UserContext? context)
            => HasPermission(context, PermissionFlags.CanReadRoles)
               || CanManageRoleManagement(context);

        public static bool CanManageRoleManagement(UserContext? context)
            => HasPermission(context, PermissionFlags.CanManageRoles);

        public static bool CanReadMeters(UserContext? context)
            => HasPermission(context, PermissionFlags.CanReadMeters);

        public static bool CanManageMeterChanges(UserContext? context)
            => HasPermission(context, PermissionFlags.CanManageMeterChanges);

        public static bool CanApproveMeterReadings(UserContext? context)
            => HasPermission(context, PermissionFlags.CanApproveMeterReadings);

        public static bool CanSubmitOwnMeterReadings(UserContext? context)
            => context?.MitgliedId is > 0 && CanSeeOwnDataOnly(context);

        public static bool HasAnyMemberAccess(UserContext? context)
            => CanSearchMembers(context)
               || CanViewMembers(context)
               || CanEditAllMembers(context)
               || CanSeeOwnDataOnly(context);

        public static bool HasAnyStammdatenAccess(UserContext? context)
            => CanShowStammdaten(context)
               || CanReadStammdaten(context)
               || CanWriteStammdaten(context);

        public static bool HasAnyParzellenAccess(UserContext? context)
            => CanShowParzellen(context)
               || CanReadParzellen(context)
               || CanWriteParzellen(context);

        public static bool HasAnyMeterAccess(UserContext? context)
            => CanReadMeters(context)
               || CanManageMeterChanges(context)
               || CanApproveMeterReadings(context)
               || CanSubmitOwnMeterReadings(context);

        public static bool HasAnyRoleManagementAccess(UserContext? context)
            => CanReadRoleManagement(context)
               || CanManageRoleManagement(context);
    }
}
