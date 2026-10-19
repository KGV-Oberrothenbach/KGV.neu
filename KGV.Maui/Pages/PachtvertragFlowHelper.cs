using System;
using System.Linq;
using System.Threading.Tasks;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Services;
using Microsoft.Maui.Controls;

namespace KGV.Maui.Pages;

internal static class PachtvertragFlowHelper
{
    public static async Task<PachtvertragFlowResult> RunAsync(
        INavigation navigation,
        ISupabaseService supabaseService,
        IAuthService authService,
        PreparedLeaseContractResumeService resumeService,
        IPreparedContractRepository preparedContractRepository,
        IContractGenerationClient contractGenerationClient,
        int mitgliedId,
        int parzelleId,
        DateTime vertragsbeginn)
    {
        ArgumentNullException.ThrowIfNull(navigation);
        ArgumentNullException.ThrowIfNull(supabaseService);
        ArgumentNullException.ThrowIfNull(authService);
        ArgumentNullException.ThrowIfNull(resumeService);
        ArgumentNullException.ThrowIfNull(preparedContractRepository);
        ArgumentNullException.ThrowIfNull(contractGenerationClient);

        if (!await supabaseService.HasSignedMitgliedsantragAsync(mitgliedId))
            throw new InvalidOperationException("Ein Pachtvertrag kann erst nach dem signierten Mitgliedsantrag erstellt werden.");
        if (!Guid.TryParse(authService.CurrentUserId, out var currentUserId) || currentUserId == Guid.Empty)
            throw new InvalidOperationException("Die aktuelle Benutzer-ID konnte nicht bestimmt werden.");
        var vereinsunterlagenService = new MitgliedsantragVereinsunterlagenService(supabaseService);

        var resume = await resumeService.ResolveAsync(mitgliedId, parzelleId, currentUserId);
        var contract = resume.Contract;
        switch (resume.Decision)
        {
            case PreparedLeaseContractResumeDecision.New:
                contract = null;
                break;
            case PreparedLeaseContractResumeDecision.ResumeOwn:
            case PreparedLeaseContractResumeDecision.ResumeClaimed:
                if (contract == null) throw new InvalidOperationException("Der vorbereitete Pachtvertrag konnte nicht geladen werden.");
                break;
            case PreparedLeaseContractResumeDecision.ForeignAssigned:
                throw new InvalidOperationException("Der vorbereitete Pachtvertrag wird bereits von einer anderen Person bearbeitet.");
            case PreparedLeaseContractResumeDecision.SignatureLocked:
                throw new InvalidOperationException("Der Pachtvertrag ist bereits zur Signatur vorbereitet oder gestartet und kann nicht mehr bearbeitet werden.");
            case PreparedLeaseContractResumeDecision.ClaimConflict:
                throw new InvalidOperationException("Der vorbereitete Pachtvertrag konnte nicht übernommen werden, da er inzwischen einer anderen Person zugewiesen wurde.");
            default:
                throw new InvalidOperationException("Der vorbereitete Pachtvertrag hat einen nicht bearbeitbaren Status.");
        }

        PachtvertragDokumentRequest? initialRequest = null;
        if (contract != null)
        {
            var parsed = PreparedContractDraftParser.Parse(contract.EntwurfDaten);
            if (parsed is not PreparedLeaseContractDraft draft
                || draft.DocumentType != PreparedContractConstants.LeaseContract
                || draft.MemberId != mitgliedId
                || draft.Draft.ParcelId != parzelleId)
                throw new PreparedContractValidationException("Der gespeicherte Pachtvertragsentwurf passt nicht zum aktuellen Mitglied oder zur aktuellen Parzelle.");

            if (!DateTime.TryParseExact(draft.Draft.StartDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var savedStartDate))
                throw new PreparedContractValidationException("Der gespeicherte Pachtvertrag enthält ein ungültiges Vertragsbeginn-Datum.");
            var resolution = await supabaseService.ResolveGesetzlicherVertreterAsync(mitgliedId, savedStartDate);
            initialRequest = PreparedLeaseContractMapper.ToDocumentRequest(draft, resolution.IstMinderjaehrig);
            vertragsbeginn = initialRequest.Vertragsbeginn;
        }

        while (true)
        {
            var request = await PromptPachtvertragRequestAsync(navigation, supabaseService, mitgliedId, parzelleId, vertragsbeginn, initialRequest, contract);
            if (request == null) return PachtvertragFlowResult.Abgebrochen;

            request.VereinsunterlagenSnapshot = await vereinsunterlagenService.CreateCurrentSnapshotAsync(mitgliedId);
            request.VereinsunterlagenNachweis = VereinsunterlagenNachweisHelper.FormatForPdf(request.VereinsunterlagenSnapshot);
            var serialized = PreparedContractDraftParser.Serialize(PreparedLeaseContractMapper.ToPreparedDraft(request));
            if (contract == null)
                contract = await preparedContractRepository.CreateAsync(PreparedContractConstants.LeaseContract, mitgliedId, parzelleId, serialized);
            else if (!PreparedContractDraftParser.AreEquivalent(contract.EntwurfDaten, serialized))
                contract = await preparedContractRepository.UpdateDraftAsync(contract.Id, contract.Revision, serialized);

            byte[] pdf;
            try { pdf = await contractGenerationClient.PreviewLeaseContractAsync(request); }
            catch (Exception ex) { throw new InvalidOperationException($"Pachtvertrag-Vorschau konnte nicht erzeugt werden: {ex.Message}", ex); }
            if (pdf.Length == 0) throw new InvalidOperationException("Pachtvertrag-Vorschau konnte nicht erzeugt werden.");

            var preview = new PachtvertragPreviewPage(
                new DokumentUploadRequest { Titel = "Pachtvertrag – Vorschau", MimeType = "application/pdf", FileName = $"pachtvertrag-vorschau-{mitgliedId}-{parzelleId}.pdf", FileContent = pdf },
                persistForSignature: false,
                preparedActionLabel: contract == null ? "Zur Signatur vorbereiten" : "Änderungen speichern");
            await navigation.PushModalAsync(new NavigationPage(preview));
            var decision = await preview.WaitForResultAsync();
            if (decision == PachtvertragPreviewDecision.BackToEditor) { initialRequest = request; continue; }
            if (decision == PachtvertragPreviewDecision.Cancel) return PachtvertragFlowResult.Abgebrochen;
            if (decision != PachtvertragPreviewDecision.SavePrepared)
                throw new InvalidOperationException("Der Pachtvertrag kann in diesem Schritt nicht zur Signatur weitergeleitet werden.");

            return PachtvertragFlowResult.Vorbereitet;
        }
    }

    private static async Task<PachtvertragDokumentRequest?> PromptPachtvertragRequestAsync(
        INavigation navigation, ISupabaseService supabaseService, int mitgliedId, int parzelleId, DateTime vertragsbeginn,
        PachtvertragDokumentRequest? initialRequest, KommunikationsvorgangDto? contract)
    {
        var member = await supabaseService.GetMitgliedByIdAsync(mitgliedId) ?? throw new InvalidOperationException("Mitglied konnte nicht geladen werden.");
        var parzelle = (await supabaseService.GetAllParzellenAsync()).FirstOrDefault(x => x.Id == parzelleId) ?? throw new InvalidOperationException("Parzelle konnte nicht geladen werden.");
        var representative = await supabaseService.ResolveGesetzlicherVertreterAsync(mitgliedId, vertragsbeginn);
        if (representative.IstMinderjaehrig && !representative.HatAktivenGesetzlichenVertreter)
            throw new InvalidOperationException("Für dieses minderjährige Mitglied ist im signierten Mitgliedsantrag kein gesetzlicher Vertreter hinterlegt.");
        var nebenmitglied = await supabaseService.GetNebenmitgliedByHauptmitgliedIdAsync(mitgliedId);
        Func<PachtvertragDokumentRequest?, bool>? unsaved = contract == null ? null : request =>
        {
            if (request == null) return true;
            try { return !PreparedContractDraftParser.AreEquivalent(contract.EntwurfDaten, PreparedContractDraftParser.Serialize(PreparedLeaseContractMapper.ToPreparedDraft(request))); }
            catch { return true; }
        };
        var dialog = new PachtvertragDialogPage(member, parzelle, vertragsbeginn, representative, nebenmitglied, initialRequest, unsaved);
        await navigation.PushModalAsync(new NavigationPage(dialog));
        return await dialog.WaitForResultAsync();
    }
}

internal enum PachtvertragFlowResult
{
    Abgebrochen,
    Vorbereitet,
    Gespeichert
}
