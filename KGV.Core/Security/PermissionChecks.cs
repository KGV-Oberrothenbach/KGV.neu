namespace KGV.Core.Security
{
    public static class PermissionChecks
    {
        public static bool HasPermission(UserContext? context, PermissionFlags permission)
            => context?.Has(permission) == true;

        public static bool HasOwnMemberContextAccess(UserContext? context)
            => context?.MitgliedId is > 0 && CanSeeOwnDataOnly(context);

        public static bool HasGlobalStammdatenAccess(UserContext? context)
            => CanShowStammdaten(context)
               || CanReadStammdaten(context)
               || CanWriteStammdaten(context);

        public static bool HasGlobalParzellenAccess(UserContext? context)
            => CanShowParzellen(context)
               || CanReadParzellen(context)
               || CanWriteParzellen(context);

        public static bool HasOwnStammdatenAccessForMember(UserContext? context, int? memberId)
            => IsOwnMemberContext(context, memberId);

        public static bool HasOwnParzellenAccessForMember(UserContext? context, int? memberId)
            => IsOwnMemberContext(context, memberId);

        public static bool HasOwnDocumentsAccessForMember(UserContext? context, int? memberId)
            => IsOwnMemberContext(context, memberId);

        public static bool HasOwnWorkHoursAccessForMember(UserContext? context, int? memberId)
            => IsOwnMemberContext(context, memberId);

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

        public static bool CanManageMembership(UserContext? context)
            => HasPermission(context, PermissionFlags.CanManageMembership);

        public static bool CanCreateMitglied(UserContext? context)
            => HasPermission(context, PermissionFlags.CanCreateMitglied);

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
               || HasOwnStammdatenAccessForMember(context, memberId);

        public static bool CanReadStammdatenForMember(UserContext? context, int? memberId)
            => CanReadStammdaten(context)
               || HasOwnStammdatenAccessForMember(context, memberId);

        public static bool CanWriteStammdatenForMember(UserContext? context, int? memberId)
            => CanWriteStammdaten(context)
               || HasOwnStammdatenAccessForMember(context, memberId);

        public static bool CanShowParzellen(UserContext? context)
            => HasPermission(context, PermissionFlags.CanShowParzellen)
               || CanReadParzellen(context)
               || CanWriteParzellen(context);

        public static bool CanReadParzellen(UserContext? context)
            => HasPermission(context, PermissionFlags.CanReadParzellen)
               || CanWriteParzellen(context);

        public static bool CanWriteParzellen(UserContext? context)
            => HasPermission(context, PermissionFlags.CanWriteParzellen);

        public static bool CanManageBelegungen(UserContext? context)
            => context?.HasFachrecht(FachrechteV2.BelegungVerwalten) == true;

        public static bool CanManagePachtvertraege(UserContext? context)
            => context?.HasFachrecht(FachrechteV2.PachtvertragVerwalten) == true;

        public static bool CanManageBegehungen(UserContext? context)
            => context?.HasFachrecht(FachrechteV2.BegehungVerwalten) == true;

        public static bool CanShowParzellenForMember(UserContext? context, int? memberId)
            => CanShowParzellen(context)
               || HasOwnParzellenAccessForMember(context, memberId);

        public static bool CanReadParzellenForMember(UserContext? context, int? memberId)
            => CanReadParzellen(context)
               || HasOwnParzellenAccessForMember(context, memberId);

        // FR2-08: effective Fachrechte only; administrative read grants no write.
        public static bool CanReadDocuments(UserContext? context)
            => context?.HasFachrecht(FachrechteV2.DokumentLesen) == true
               || context?.HasFachrecht("vereinsdaten.alle_lesen") == true;

        public static bool CanManageDocuments(UserContext? context)
            => context?.HasFachrecht(FachrechteV2.DokumentVerwalten) == true;

        public static bool CanArchiveDocuments(UserContext? context)
            => context?.HasFachrecht(FachrechteV2.DokumentArchivieren) == true;

        public static bool CanManageDocumentTypes(UserContext? context)
            => context?.HasFachrecht(FachrechteV2.DokumenttypVerwalten) == true;

        public static bool CanManageDocumentEffects(UserContext? context)
            => context?.HasFachrecht(FachrechteV2.DokumentwirkungVerwalten) == true;

        public static bool CanManageTemplates(UserContext? context)
            => context?.HasFachrecht(FachrechteV2.VorlageVerwalten) == true;

        public static bool CanManageClubSeasonConfiguration(UserContext? context)
            => context?.HasFachrecht(FachrechteV2.SaisonVerwalten) == true;

        // FR2-10: Vereinskonfiguration ist kein Teilrecht der Saisonverwaltung.
        public static bool CanManageClubConfiguration(UserContext? context)
            => context?.HasFachrecht(FachrechteV2.VereinKonfiguration) == true;

        public static bool CanConfigureUserMeterReadingSubmissions(UserContext? context)
            => context?.HasFachrecht(FachrechteV2.AblesungEinstellungen) == true;

        public static bool CanCaptureMeterReadings(UserContext? context)
            => context?.HasFachrecht(FachrechteV2.AblesungErfassen) == true;

        public static bool CanManageRfid(UserContext? context)
            => context?.HasFachrecht(FachrechteV2.RfidVerwalten) == true;

        public static bool CanManageExports(UserContext? context)
            => context?.HasFachrecht(FachrechteV2.AuswertungLesen) == true
               && context?.HasFachrecht(FachrechteV2.ExportAusfuehren) == true;

        public static bool CanManageClubDocuments(UserContext? context)
            => context?.HasFachrecht("vereinsunterlagen.verwalten") == true;

        public static bool CanManageClubFunctions(UserContext? context)
            => context?.HasFachrecht(FachrechteV2.VereinsfunktionVerwalten) == true;

        // FR2-09: effective Fachrechte, never a legacy permission bit.
        public static bool CanReadCommunicationWorkflows(UserContext? context)
            => context?.HasFachrecht(FachrechteV2.VorgangLesen) == true
               || context?.HasFachrecht(FachrechteV2.VereinsdatenAlleLesen) == true;

        public static bool CanManageCommunicationWorkflows(UserContext? context)
            => context?.HasFachrecht(FachrechteV2.VorgangBearbeiten) == true;

        public static bool CanReadDocumentsForMember(UserContext? context, int? memberId)
            => CanReadDocuments(context)
               || HasOwnDocumentsAccessForMember(context, memberId);

        // FR2-07: Vier unabhaengige, ausschliesslich serverseitig berechnete Fachrechte.
        public static bool CanReadWorkHours(UserContext? context)
            => context?.HasFachrecht(FachrechteV2.ArbeitsstundenLesen) == true;

        public static bool CanEditWorkHours(UserContext? context)
            => context?.HasFachrecht(FachrechteV2.ArbeitsstundenBearbeiten) == true;

        public static bool CanApproveWorkHours(UserContext? context)
            => context?.HasFachrecht(FachrechteV2.ArbeitsstundenFreigeben) == true;

        // Kompatibilitaetshelfer: Sofortgenehmigung nur mit beiden Rechten.
        // Nicht als Ersatz fuer CanEditWorkHours / CanApproveWorkHours verwenden.
        public static bool CanManageWorkHours(UserContext? context)
            => CanEditWorkHours(context) && CanApproveWorkHours(context);

        public static bool CanManageWorkAssignments(UserContext? context)
            => context?.HasFachrecht(FachrechteV2.ArbeitseinsatzVerwalten) == true;

        public static bool CanManageAppointments(UserContext? context)
            => context?.HasFachrecht(FachrechteV2.TerminVerwalten) == true;

        public static bool CanManageAnnouncements(UserContext? context)
            => context?.HasFachrecht(FachrechteV2.BekanntmachungVerwalten) == true;

        // FR2-06: Nur die benannten, zentral ermittelten Fachrechte entscheiden.
        // Alte Rollen-/Flag-Bits geben keine Finanzberechtigung.
        public static bool CanReadFinance(UserContext? context)
            => context?.HasFachrecht(FachrechteV2.FinanzenLesen) == true;

        public static bool CanManageFinanceInvoices(UserContext? context)
            => context?.HasFachrecht(FachrechteV2.FinanzenRechnungen) == true;

        public static bool CanManageFinanceCostTypes(UserContext? context)
            => context?.HasFachrecht(FachrechteV2.FinanzenKostenarten) == true;

        public static bool CanPrepareYearEndClosing(UserContext? context)
            => context?.HasFachrecht(FachrechteV2.JahresabschlussVorbereiten) == true;

        public static bool CanPerformYearEndClosing(UserContext? context)
            => context?.HasFachrecht(FachrechteV2.JahresabschlussFinalisieren) == true;

        public static bool CanCorrectYearEndClosing(UserContext? context)
            => context?.HasFachrecht(FachrechteV2.JahresabschlussKorrigieren) == true;

        public static bool HasAnyFinanceAccess(UserContext? context)
            => CanReadFinance(context)
               || CanManageFinanceInvoices(context)
               || CanManageFinanceCostTypes(context)
               || CanPrepareYearEndClosing(context)
               || CanPerformYearEndClosing(context)
               || CanCorrectYearEndClosing(context);

        public static bool CanRegisterHistoricalContracts(UserContext? context)
            => HasPermission(context, PermissionFlags.CanRegisterHistoricalContracts);

        public static bool CanReadWorkHoursForMember(UserContext? context, int? memberId)
            => CanReadWorkHours(context)
               || HasOwnWorkHoursAccessForMember(context, memberId);

        public static bool CanReadRoleManagement(UserContext? context)
            => context?.HasFachrecht(FachrechteV2.BerechtigungenLesen) == true;

        public static bool CanManageRoleManagement(UserContext? context)
            => context?.HasFachrecht(FachrechteV2.BerechtigungenVerwalten) == true;

        public static bool CanReadUsers(UserContext? context)
            => context?.HasFachrecht(FachrechteV2.BenutzerLesen) == true;

        public static bool CanManageUsers(UserContext? context)
            => context?.HasFachrecht(FachrechteV2.BenutzerVerwalten) == true;

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
               || CanCreateMitglied(context)
               || CanSeeOwnDataOnly(context);

        public static bool HasAnyStammdatenAccess(UserContext? context)
            => HasGlobalStammdatenAccess(context);

        public static bool HasAnyParzellenAccess(UserContext? context)
            => HasGlobalParzellenAccess(context);

        public static bool HasAnyMeterAccess(UserContext? context)
            => CanCaptureMeterReadings(context)
               || CanManageRfid(context)
               || CanReadMeters(context)
               || CanManageMeterChanges(context)
               || CanApproveMeterReadings(context)
               || CanSubmitOwnMeterReadings(context);

        public static bool HasAnyRoleManagementAccess(UserContext? context)
            => CanReadRoleManagement(context)
               || CanManageRoleManagement(context);

        public static bool HasAnyManagementWorkspaceAccess(UserContext? context)
            => CanSearchMembers(context)
               || CanEditAllMembers(context)
               || CanCreateMitglied(context)
               || HasGlobalStammdatenAccess(context)
               || HasGlobalParzellenAccess(context)
                || CanManageBelegungen(context)
                || CanManagePachtvertraege(context)
               || CanReadDocuments(context)
               || CanManageDocuments(context)
               || CanArchiveDocuments(context)
               || CanManageDocumentTypes(context)
               || CanManageDocumentEffects(context)
               || CanManageTemplates(context)
               || CanManageClubDocuments(context)
               || CanReadWorkHours(context)
               || CanEditWorkHours(context)
               || CanApproveWorkHours(context)
               || CanManageWorkAssignments(context)
               || CanManageAppointments(context)
               || CanManageAnnouncements(context)
               || CanReadMeters(context)
               || CanManageMeterChanges(context)
               || CanApproveMeterReadings(context)
               || CanReadUsers(context)
               || CanReadRoleManagement(context)
               || HasAnyFinanceAccess(context)
               || CanRegisterHistoricalContracts(context)
               || CanManageExports(context)
               || CanManageClubSeasonConfiguration(context)
               || CanManageClubConfiguration(context)
               || CanManageClubFunctions(context)
               || CanConfigureUserMeterReadingSubmissions(context);
    }
}
