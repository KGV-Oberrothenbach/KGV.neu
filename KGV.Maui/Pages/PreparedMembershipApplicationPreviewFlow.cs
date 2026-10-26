using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Services;
using Microsoft.Maui.Controls;

namespace KGV.Maui.Pages;

public enum PreparedMembershipApplicationPreviewAction
{
    Cancel,
    StartSigning,
    ContinueSigning
}

public sealed record PreparedMembershipApplicationPreviewResult(
    PreparedMembershipApplicationPreviewAction Action,
    KommunikationsvorgangDto Contract);

/// <summary>
/// Opens a prepared membership application as a transient, full PDF preview.
/// The caller decides what to do with the returned signing action.
/// </summary>
public sealed class PreparedMembershipApplicationPreviewFlow
{
    private readonly Page _hostPage;
    private readonly IPreparedContractRepository _repository;
    private readonly IContractGenerationClient _contractClient;
    private readonly MitgliedsantragVereinsunterlagenService _vereinsunterlagenService;

    public PreparedMembershipApplicationPreviewFlow(
        Page hostPage,
        IPreparedContractRepository repository,
        IContractGenerationClient contractClient,
        MitgliedsantragVereinsunterlagenService vereinsunterlagenService)
    {
        _hostPage = hostPage ?? throw new ArgumentNullException(nameof(hostPage));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _contractClient = contractClient ?? throw new ArgumentNullException(nameof(contractClient));
        _vereinsunterlagenService = vereinsunterlagenService ?? throw new ArgumentNullException(nameof(vereinsunterlagenService));
    }

    public async Task<PreparedMembershipApplicationPreviewResult> OpenAsync(long contractId, CancellationToken cancellationToken = default)
    {
        if (contractId <= 0)
            throw new ArgumentOutOfRangeException(nameof(contractId));

        var contract = await _repository.LoadAsync(contractId, cancellationToken);
        var draft = ValidateAndParse(contract);
        if (!VereinsunterlagenNachweisHelper.IsComplete(draft.VereinsunterlagenNachweis))
            throw new PreparedContractValidationException("Der vorbereitete Mitgliedsantrag enthält noch keinen Vereinsunterlagen-Snapshot. Bitte erstellen Sie zuerst eine neue Vorschau.");
        var currentSnapshot = await _vereinsunterlagenService.CreateCurrentSnapshotAsync(contract.MitgliedId, cancellationToken);
        if (!VereinsunterlagenNachweisHelper.Equivalent(draft.VereinsunterlagenNachweis, currentSnapshot))
        {
            if (contract.Status != PreparedContractConstants.Prepared)
                throw new PreparedContractValidationException("Die Pflichtunterlagen haben sich seit der Vorschau geändert. Die Signatur darf nicht gestartet werden; bitte erstellen Sie eine neue Vorschau.");
            var updatedDraft = draft with { VereinsunterlagenNachweis = currentSnapshot };
            contract = await _repository.UpdateDraftAsync(contract.Id, contract.Revision, PreparedContractDraftParser.Serialize(updatedDraft), cancellationToken);
            draft = updatedDraft;
        }
        var isMinor = draft.Representative is not null;
        var application = PreparedMembershipApplicationMapper.ToDocumentRequest(draft, isMinor);
        application.NebenmitgliedEinbeziehen = !isMinor && draft.IncludeSecondaryMember;
        if (contract.Status == PreparedContractConstants.ReadyForSignature)
            application.KommunikationsvorgangId = contract.Id;

        var pdf = await _contractClient.PreviewMembershipApplicationAsync(
            application,
            draft.MemberFee,
            cancellationToken);
        if (pdf.Length == 0)
            throw new InvalidOperationException("Für den vorbereiteten Mitgliedsantrag wurde keine PDF-Vorschau erzeugt.");

        var preview = new MitgliedsantragPreviewPage(
            new DokumentUploadRequest
            {
                MitgliedId = contract.MitgliedId,
                Titel = "Mitgliedsantrag – Vorschau",
                FileName = "Mitgliedsantrag_Vorschau.pdf",
                MimeType = "application/pdf",
                FileContent = pdf
            },
            persistForSignature: false,
            preparedActionLabel: contract.Status == PreparedContractConstants.Prepared ? "Signatur starten" : "Signatur fortsetzen");

        await _hostPage.Navigation.PushModalAsync(new NavigationPage(preview));
        var decision = await preview.WaitForResultAsync();
        return new(ToAction(contract.Status, decision), contract);
    }

    private static PreparedMembershipApplicationDraft ValidateAndParse(KommunikationsvorgangDto contract)
    {
        ArgumentNullException.ThrowIfNull(contract);
        if (contract.Id <= 0 || contract.MitgliedId <= 0 || contract.Revision < 1)
            throw new PreparedContractValidationException("Der vorbereitete Mitgliedsantrag enthält ungültige Basisdaten.");
        if (contract.Dokumenttyp != PreparedContractConstants.MembershipApplication)
            throw new PreparedContractValidationException("Der Vorgang ist kein Mitgliedsantrag.");
        if (contract.Status is not PreparedContractConstants.Prepared and not PreparedContractConstants.ReadyForSignature)
            throw new PreparedContractValidationException("Der Mitgliedsantrag kann in seinem aktuellen Status nicht signiert werden.");
        if (contract.Status == PreparedContractConstants.ReadyForSignature &&
            (contract.SignaturRevision is null || contract.SignaturRevision != contract.Revision || contract.SignaturGestartetAm is null))
            throw new PreparedContractValidationException("Der Mitgliedsantrag ist nicht korrekt für die Signatur eingefroren.");
        if (PreparedContractDraftParser.Parse(contract.EntwurfDaten) is not PreparedMembershipApplicationDraft draft ||
            draft.MemberId != contract.MitgliedId)
            throw new PreparedContractValidationException("Der gespeicherte Vertragsentwurf passt nicht zum Mitgliedsantrag.");
        return draft;
    }

    private static PreparedMembershipApplicationPreviewAction ToAction(string status, MitgliedsantragPreviewDecision decision) => decision == MitgliedsantragPreviewDecision.SavePrepared
        ? status == PreparedContractConstants.Prepared
            ? PreparedMembershipApplicationPreviewAction.StartSigning
            : PreparedMembershipApplicationPreviewAction.ContinueSigning
        : PreparedMembershipApplicationPreviewAction.Cancel;
}
