// File: Core/Interfaces/ISupabaseService.cs
using Supabase;
using KGV.Core.Models;
using KGV.Core.Security;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KGV.Core.Interfaces
{
    public interface ISupabaseService
    {
        Client Client { get; }
        Task InitializeAsync();

        Task<List<string>> GetSeasonsAsync();
        Task<List<MitgliedRecord>> GetMitgliederAsync();
        Task<ImpressumInfo> GetImpressumInfoAsync();

        // ✅ Vollständige Stammdaten
        Task<MitgliedRecord?> GetMitgliedByIdAsync(int mitgliedId);
        Task<MitgliedRecord?> CreateMitgliedAsync(MemberDTO dto);
        Task<bool> UpdateMitgliedAsync(MemberDTO dto, string userId);
        Task<MembershipEndResult> EndMembershipAsync(int mainMemberId, DateTime endDate, MembershipEndDecision? secondaryDecision, string userId, int timeoutMinutes = 10);

        Task<ParzelleRecord?> GetParzelleByNumberAsync(string gartenNr);
        Task<List<ParzelleRecord>> GetAllParzellenAsync();
        Task<ParzelleDetailDTO?> GetParzelleDetailAsync(int parzelleId);
        Task<bool> UpdateParzelleStammdatenAsync(ParzelleRecord record);
        Task<List<RfidMediumOption>> GetAvailableRfidMediumOptionsForParzelleAsync(int parzelleId);
        Task<RfidAssignmentCheckResult> CheckParzelleRfidAssignmentAsync(int parzelleId, string medium, string uid);
        Task<RfidAssignmentResult> AssignParzelleRfidAsync(int parzelleId, string medium, string uid, bool overwriteExisting = false);
        Task<List<ZaehlerEichstatusRecord>> GetZaehlerEichstatusAsync();
        Task<RfidScanContextResult> ResolveRfidScanContextAsync(string uid);

        Task<ParzellenBelegungRecord?> GetCurrentBelegungForParzelleAsync(int parzelleId);

        // ✅ Belegungen für Parzellen-Zuweisung
        Task<List<ParzellenBelegungRecord>> GetBelegungenForMitgliedAsync(int mitgliedId);
        Task<List<ParzellenBelegungRecord>> GetAllParzellenBelegungenAsync();
        Task<bool> AssignParzelleToMitgliedAsync(int mitgliedId, int parzelleId, DateTime startDatum);
        Task<bool> EndParzellenBelegungAsync(int belegungId, DateTime bisDatum);

        Task<List<ZaehlerAblesungDTO>> GetStromAblesungenAsync(int parzelleId);
        Task<List<ZaehlerAblesungDTO>> GetWasserAblesungenAsync(int parzelleId);

        Task<StromzaehlerRecord?> GetActiveStromzaehlerAsync(int parzelleId, DateTime onDate);
        Task<WasserzaehlerRecord?> GetActiveWasserzaehlerAsync(int parzelleId, DateTime onDate);

        Task<bool> AddStromzaehlerAsync(StromzaehlerInsertRecord request);
        Task<bool> AddWasserzaehlerAsync(WasserzaehlerInsertRecord request);

        Task<ZaehlerInsertResult> TryAddStromzaehlerAsync(StromzaehlerInsertRecord request);
        Task<ZaehlerInsertResult> TryAddWasserzaehlerAsync(WasserzaehlerInsertRecord request);

        Task<bool> SetStromzaehlerAusgebautAmAsync(long stromzaehlerId, DateTime ausgebautAm);
        Task<bool> SetWasserzaehlerAusgebautAmAsync(long wasserzaehlerId, DateTime ausgebautAm);

        Task<bool> AddAblesungAsync(AblesungInsertRecord request);
        Task<bool> UpdateAblesungAsync(long ablesungId, DateTime ablesedatum, decimal stand, string? fotoPfad);
        Task<List<AblesungReviewItem>> GetOffeneAblesungenZurFreigabeAsync();
        Task<bool> UpdateAblesungPruefstatusAsync(long ablesungId, string pruefstatus, string? pruefkommentar, int? geprueftVon, DateTime? geprueftAm = null);
        Task<bool> CorrectAblesungImPruefprozessAsync(long ablesungId, DateTime ablesedatum, decimal stand, string korrekturkommentar, int geprueftVon, DateTime? geprueftAm = null);
        Task<bool> RemoveAblesungImPruefprozessAsync(long ablesungId, string begruendung, int geprueftVon, DateTime? geprueftAm = null);
        Task<bool> GetAllowUserMeterReadingSubmissionsAsync();
        Task<bool> SetAllowUserMeterReadingSubmissionsAsync(bool allowed);
        Task<UserPermissionSettings?> GetUserPermissionSettingsAsync(int mitgliedId);
        Task<bool> SetAppUserRoleAsync(int mitgliedId, string role);
        Task<bool> SetUserPermissionSettingsAsync(int mitgliedId, string role, long grantedPermissions, long revokedPermissions);

        // =========================
        // Nebenmitglied
        // =========================
        Task<MitgliedRecord?> GetNebenmitgliedByHauptmitgliedIdAsync(int hauptmitgliedId);
        Task<MitgliedRecord?> CreateNebenmitgliedAsync(NebenmitgliedCreateDTO request);
        Task<MitgliedGesetzlicherVertreterRecord?> GetAktivenGesetzlichenVertreterAsync(int minderjaehrigesMitgliedId, DateTime? stichtag = null);
        Task<MitgliedGesetzlicherVertreterRecord?> SaveGesetzlichenVertreterAsync(GesetzlicherVertreterSaveRequest request);
        Task<GesetzlicherVertreterAufloesung> ResolveGesetzlicherVertreterAsync(int mitgliedId, DateTime? stichtag = null);
        Task<VereinskonfigurationRecord?> GetAktiveVereinskonfigurationAsync();
        Task<VereinskonfigurationRecord?> SaveAktiveVereinskonfigurationAsync(VereinskonfigurationRecord vereinskonfiguration);

        // =========================
        // Arbeitsstunden
        // =========================
        Task<List<SaisonRecord>> GetSaisonRecordsAsync();
        Task<SaisonRecord?> SaveSaisonAsync(SaisonRecord saison);
        Task<MitgliedRecord?> GetMitgliedByAuthUserIdAsync(Guid authUserId);
        Task<MitgliedRecord?> GetMitgliedByAuthUserIdAsync(string authUserId);

        Task<bool> UpdateOwnContactAsync(int mitgliedId, string? telefon, string? handy, string? adresse, string? plz, string? ort);
        Task<bool> UpdateOwnContactAsync(int mitgliedId, string? telefon, string? handy, string? adresse, string? plz, string? ort, string? email, DateTime? geburtsdatum, DateTime? mitgliedSeit, bool whatsappEinwilligung);
        Task<List<ArbeitsstundeDTO>> GetArbeitsstundenAsync(params int[] mitgliedIds);
        Task<List<ArbeitsstundeDTO>> GetOffeneArbeitsstundenZurFreigabeAsync();
        Task<List<ArbeitsstundenPruefverlaufItem>> GetArbeitsstundenPruefverlaufAsync(int arbeitsstundeId);
        Task<bool> AddArbeitsstundeAsync(ArbeitsstundeInsertRecord request);
        Task<bool> UpdateArbeitsstundeAsync(ArbeitsstundeRecord record);
        Task<bool> DeleteArbeitsstundeAsync(int arbeitsstundeId);
        Task<bool> ApproveArbeitsstundeImPruefprozessAsync(int arbeitsstundeId, string begruendung, int geprueftVon, DateTime? geprueftAm = null);
        Task<bool> RejectArbeitsstundeImPruefprozessAsync(int arbeitsstundeId, string begruendung, int geprueftVon, DateTime? geprueftAm = null);
        Task<bool> CorrectArbeitsstundeImPruefprozessAsync(ArbeitsstundenPruefkorrekturRequest request);
        Task<bool> DeleteArbeitsstundeImPruefprozessAsync(int arbeitsstundeId, string begruendung, int geprueftVon, DateTime? geprueftAm = null);
        Task<List<(int MitgliedId, string Vorname, string Nachname, int Count)>> GetUnapprovedArbeitsstundenByMitgliedAsync();
        Task<ArbeitsstundenReviewLockResult> TryAcquireArbeitsstundenReviewLockAsync(string userId, int timeoutMinutes = 10);
        Task<bool> RefreshArbeitsstundenReviewLockAsync(string userId, int timeoutMinutes = 10);
        Task<bool> ReleaseArbeitsstundenReviewLockAsync(string userId, bool force = false);

        Task<bool> TryLockMitgliedAsync(int mitgliedId, string userId, int timeoutMinutes = 10);
        Task<bool> ReleaseLockMitgliedAsync(int mitgliedId, string userId, bool force = false);

        Task<bool> TryLockArbeitsstundeAsync(int arbeitsstundeId, string userId, int timeoutMinutes = 10);
        Task<bool> ReleaseLockArbeitsstundeAsync(int arbeitsstundeId, string userId, bool force = false);

        // =========================
        // Dokumente (Google Drive / Storage-kompatibler Fallback)
        // =========================
        Task<List<DocumentInfo>> GetMitgliedDokumenteAsync(int mitgliedId);
        Task<List<DocumentInfo>> GetParzelleDokumenteAsync(int parzelleId);
        Task<DokumentUploadResult> CreateMitgliedsantragDokumentAsync(int mitgliedId, string status = FormularDokumentStatus.Unsigniert);
        Task<DokumentUploadResult> CreateMitgliedsantragDokumentAsync(MitgliedsantragDokumentRequest request);
        Task<DokumentUploadRequest?> BuildMitgliedsantragPreviewAsync(MitgliedsantragDokumentRequest request);
        Task<DokumentUploadResult> CreateSignedMitgliedsantragDokumentAsync(MitgliedsantragDokumentRequest request, DigitalSignatureCapture signatureCapture, DigitalSignatureCapture? gesetzlicherVertreterSignatureCapture = null);
        Task<DokumentUploadResult> CreateMitgliedsvertragDokumentAsync(int mitgliedId, string status = FormularDokumentStatus.Unsigniert);
        Task<DokumentUploadResult> CreatePachtvertragDokumentAsync(int mitgliedId, int parzelleId, DateTime vertragsbeginn, string status = FormularDokumentStatus.Unsigniert);
        Task<DokumentUploadRequest?> BuildPachtvertragPreviewAsync(int mitgliedId, int parzelleId, DateTime vertragsbeginn);
        Task<DokumentUploadRequest?> BuildPachtvertragPreviewAsync(PachtvertragDokumentRequest request);
        Task<DokumentUploadResult> CreatePachtvertragDokumentAsync(PachtvertragDokumentRequest request);
        Task<DokumentUploadResult> CreateSignedPachtvertragDokumentAsync(int mitgliedId, int parzelleId, DateTime vertragsbeginn, DigitalSignatureCapture signatureCapture);
        Task<DokumentUploadResult> CreateSignedPachtvertragDokumentAsync(PachtvertragDokumentRequest request, DigitalSignatureCapture signatureCapture, DigitalSignatureCapture? gesetzlicherVertreterSignatureCapture = null);
        Task<DokumentUploadResult> CreateSignedVertragsdokumentAsync(int mitgliedId, DocumentInfo sourceDocument, DigitalSignatureCapture signatureCapture);
        Task<DokumentUploadResult> UploadSignedVertragsdokumentAsync(int mitgliedId, DocumentInfo sourceDocument, byte[] fileContent, string originalFileName, string mimeType = "application/pdf");
        Task<DokumentUploadResult> CreateDokumentAsync(DokumentUploadRequest request);
        Task<DokumentDeleteResult> DeleteDokumentAsync(DocumentInfo? document);
        Task<string?> CreateDokumentSignedUrlAsync(string storagePath, int expiresInSeconds = 3600);
        Task<string?> ResolveDokumentOpenUrlAsync(DocumentInfo? document, int expiresInSeconds = 3600);
        Task<string?> ResolveAblesungFotoOpenUrlAsync(string? fotoPfad, string? fotoDriveFileId, int expiresInSeconds = 3600);
        Task<PflichtstundenUebersichtRecord?> GetPflichtstundenUebersichtForMitgliedAsync(int mitgliedId);
        Task<WartungsvertragRecord?> GetWartungsvertragByIdAsync(long wartungsvertragId);
        Task<List<WartungsvertragOverviewItem>> GetWartungsvertraegeOverviewAsync();
        Task<WartungsvertragDetailItem?> GetWartungsvertragDetailAsync(long wartungsvertragId);
        Task<List<MemberWartungsvertragItem>> GetWartungsvertraegeForMitgliedAsync(int mitgliedId);
        Task<List<WartungsvertragOverviewItem>> GetAssignableWartungsvertraegeForMitgliedAsync(int mitgliedId);
        Task<WartungsvertragRecord?> CreateWartungsvertragAsync(WartungsvertragInsertRecord request);
        Task<bool> UpdateWartungsvertragAsync(WartungsvertragRecord record);
        Task<WartungsvertragAssignmentSaveResult> AssignMitgliederToWartungsvertragAsync(long wartungsvertragId, DateTime gueltigAb, IReadOnlyCollection<int> mitgliedIds);
        Task<WartungsvertragAssignmentSaveResult> AssignWartungsvertraegeToMitgliedAsync(int mitgliedId, DateTime gueltigAb, IReadOnlyCollection<long> wartungsvertragIds);
        Task<bool> EndWartungsvertragZuordnungAsync(long wartungsvertragZuordnungId, DateTime gueltigBis);

        Task<HomeOverviewDTO> GetHomeOverviewAsync(KGV.Core.Security.UserRole role, int? mitgliedId);
        Task<List<HomeWorkAssignmentItem>> GetStartseiteArbeitseinsaetzeAsync();
        Task<HomeWorkAssignmentItem?> GetStartseiteArbeitseinsatzByIdAsync(int arbeitseinsatzId);
        Task<List<WorkAssignmentParticipantItem>> GetArbeitseinsatzParticipantsAsync(int arbeitseinsatzId);
        Task<WorkAssignmentRegistrationResult> SignUpForArbeitseinsatzAsync(int arbeitseinsatzId, int mitgliedId);
        Task<WorkAssignmentRegistrationResult> SignOffFromArbeitseinsatzAsync(int arbeitseinsatzId, int mitgliedId);
        Task<List<HomeAppointmentItem>> GetStartseiteTermineAsync();
        Task<List<HomeAnnouncementItem>> GetStartseiteBekanntmachungenAsync();
        Task<List<ArbeitseinsatzRecord>> GetArbeitseinsaetzeVerwaltungAsync();
        Task<ArbeitseinsatzRecord?> CreateArbeitseinsatzAsync(ArbeitseinsatzInsertRecord request);
        Task<bool> UpdateArbeitseinsatzAsync(ArbeitseinsatzRecord record);
        Task<bool> DeleteArbeitseinsatzAsync(long id);
        Task<List<TerminRecord>> GetTermineVerwaltungAsync();
        Task<TerminRecord?> CreateTerminAsync(TerminInsertRecord request);
        Task<bool> UpdateTerminAsync(TerminRecord record);
        Task<bool> DeleteTerminAsync(long id);
        Task<List<BekanntmachungRecord>> GetBekanntmachungenVerwaltungAsync();
        Task<BekanntmachungRecord?> CreateBekanntmachungAsync(BekanntmachungInsertRecord request);
        Task<bool> UpdateBekanntmachungAsync(BekanntmachungRecord record);
        Task<bool> DeleteBekanntmachungAsync(long id);
    }
}
