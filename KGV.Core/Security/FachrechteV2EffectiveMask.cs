using System;
using System.Collections.Generic;
using System.Linq;

namespace KGV.Core.Security;

/// <summary>
/// AP-02.4K: Transitional compatibility adapter for existing MAUI UI flags.
/// Only the complete, server-evaluated 55 named rights are authoritative.
/// The old role/grant/revocation bit fields are never an authorization source.
/// </summary>
public static class FachrechteV2EffectiveMask
{
    public static PermissionFlags FromNamedRights(IEnumerable<string> grantedRights, long? memberId)
    {
        if (grantedRights is null) throw new ArgumentNullException(nameof(grantedRights));
        var rights = new HashSet<string>(grantedRights, StringComparer.Ordinal);
        var mask = memberId is > 0 ? PermissionFlags.CanSeeOwnDataOnly : PermissionFlags.None;

        void Allow(PermissionFlags flag, params string[] required)
        {
            if (required.All(rights.Contains)) mask |= flag;
        }

        Allow(PermissionFlags.CanSearchMembers, "mitglied.lesen");
        Allow(PermissionFlags.CanViewMembers, "mitglied.lesen");
        Allow(PermissionFlags.CanEditAllMembers, "mitglied.bearbeiten");
        Allow(PermissionFlags.CanCreateMitglied, "mitglied.anlegen");
        Allow(PermissionFlags.CanManageDocuments, "dokument.verwalten");
        Allow(PermissionFlags.CanReadMeters, "zaehler.lesen");
        Allow(PermissionFlags.CanManageMeterChanges, "zaehlerwechsel.verwalten");
        Allow(PermissionFlags.CanApproveMeterReadings, "ablesung.freigeben");
        Allow(PermissionFlags.CanManageWorkHours, "arbeitsstunden.bearbeiten", "arbeitsstunden.freigeben");
        Allow(PermissionFlags.CanManageRoles, "berechtigungen.verwalten");
        Allow(PermissionFlags.CanShowStammdaten, "mitglied.lesen");
        Allow(PermissionFlags.CanReadStammdaten, "mitglied.lesen");
        Allow(PermissionFlags.CanWriteStammdaten, "mitglied.bearbeiten");
        Allow(PermissionFlags.CanShowParzellen, "parzelle.lesen");
        Allow(PermissionFlags.CanReadParzellen, "parzelle.lesen");
        Allow(PermissionFlags.CanWriteParzellen, "parzelle.bearbeiten");
        Allow(PermissionFlags.CanReadDocuments, "dokument.lesen");
        Allow(PermissionFlags.CanReadWorkHours, "arbeitsstunden.lesen");
        Allow(PermissionFlags.CanReadRoles, "berechtigungen.lesen");
        Allow(PermissionFlags.CanManageWorkAssignments, "arbeitseinsatz.verwalten");
        Allow(PermissionFlags.CanManageAppointments, "termin.verwalten");
        Allow(PermissionFlags.CanManageAnnouncements, "bekanntmachung.verwalten");
        Allow(PermissionFlags.CanPerformYearEndClosing, "jahresabschluss.finalisieren");
        Allow(PermissionFlags.CanRegisterHistoricalContracts, "historische_vertraege.erfassen");
        Allow(PermissionFlags.CanManageExports, "export.ausfuehren");
        Allow(PermissionFlags.CanManageClubSeasonConfiguration, "saison.verwalten");
        Allow(PermissionFlags.CanManageDocumentTypes, "dokumenttyp.verwalten");
        Allow(PermissionFlags.CanManageClubDocuments, "vereinsunterlagen.verwalten");
        Allow(PermissionFlags.CanManageClubFunctions, "vereinsfunktion.verwalten");
        Allow(PermissionFlags.CanManageCommunicationWorkflows, "vorgang.bearbeiten");
        Allow(PermissionFlags.CanArchiveDocuments, "dokument.archivieren");
        return mask;
    }
}