namespace KGV.Core.Security
{
    public static class PermissionChecks
    {
        public static bool HasPermission(UserContext? context, PermissionFlags permission)
            => context?.Has(permission) == true;

        public static bool CanManageDocuments(UserContext? context)
            => HasPermission(context, PermissionFlags.CanManageDocuments);

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
