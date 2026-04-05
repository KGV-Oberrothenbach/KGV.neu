namespace KGV.Core.Security
{
    public static class PermissionChecks
    {
        public static bool HasPermission(UserContext? context, PermissionFlags permission)
            => context?.Has(permission) == true;

        public static bool CanShowStammdaten(UserContext? context)
            => HasPermission(context, PermissionFlags.CanShowStammdaten);

        public static bool CanReadStammdaten(UserContext? context)
            => HasPermission(context, PermissionFlags.CanReadStammdaten);

        public static bool CanWriteStammdaten(UserContext? context)
            => HasPermission(context, PermissionFlags.CanWriteStammdaten);

        public static bool CanShowParzellen(UserContext? context)
            => HasPermission(context, PermissionFlags.CanShowParzellen);

        public static bool CanReadParzellen(UserContext? context)
            => HasPermission(context, PermissionFlags.CanReadParzellen);

        public static bool CanWriteParzellen(UserContext? context)
            => HasPermission(context, PermissionFlags.CanWriteParzellen);

        public static bool CanReadDocuments(UserContext? context)
            => HasPermission(context, PermissionFlags.CanReadDocuments)
               || CanManageDocuments(context);

        public static bool CanManageDocuments(UserContext? context)
            => HasPermission(context, PermissionFlags.CanManageDocuments);

        public static bool CanReadWorkHours(UserContext? context)
            => HasPermission(context, PermissionFlags.CanReadWorkHours)
               || HasPermission(context, PermissionFlags.CanManageWorkHours);

        public static bool CanManageWorkHours(UserContext? context)
            => HasPermission(context, PermissionFlags.CanManageWorkHours);

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
            => context?.MitgliedId is > 0 && HasPermission(context, PermissionFlags.CanSeeOwnDataOnly);

        public static bool HasAnyMeterAccess(UserContext? context)
            => CanReadMeters(context)
               || CanManageMeterChanges(context)
               || CanApproveMeterReadings(context);
    }
}
