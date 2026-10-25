using KGV.Core.Interfaces;
using KGV.Core.Models;

namespace KGV.Core.Services;

/// <summary>Orchestrates the persistent, revision-bound membership-application signature workflow.</summary>
public sealed class PreparedMembershipApplicationSigningService
{
    private readonly IPreparedContractRepository _repository;
    private readonly IContractGenerationClient _contractClient;
    private readonly MitgliedsantragVereinsunterlagenService? _vereinsunterlagenService;
    private readonly PreparedContractSigningStartService _startService;
    private readonly IPreparedContractSignatureService _signatures;

    public PreparedMembershipApplicationSigningService(
        IPreparedContractRepository repository,
        IContractGenerationClient contractClient,
        MitgliedsantragVereinsunterlagenService? vereinsunterlagenService = null,
        PreparedContractSigningStartService? startService = null,
        IPreparedContractSignatureService? persistentSignatures = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _contractClient = contractClient ?? throw new ArgumentNullException(nameof(contractClient));
        _vereinsunterlagenService = vereinsunterlagenService;
        _signatures = persistentSignatures ?? throw new ArgumentNullException(nameof(persistentSignatures), "Der persistente Signaturservice ist erforderlich.");
        _startService = startService ?? new PreparedContractSigningStartService(repository, _signatures);
    }

    public async Task<PreparedMembershipApplicationSignatureState> StartOrResumeAsync(KommunikationsvorgangDto contract, CancellationToken cancellationToken = default)
    {
        var prepared = await PrepareAsync(contract, cancellationToken);
        var draft = ParseDraft(prepared);
        var positions = await _signatures.EnsurePositionsAsync(prepared, PreparedMembershipApplicationSignaturePlanBuilder.BuildPersistent(draft), cancellationToken);
        return new(prepared, draft, positions, PreparedContractSignatureProgress.From(positions));
    }

    public async Task<PreparedMembershipApplicationSignatureState> GetSignatureStateAsync(KommunikationsvorgangDto contract, CancellationToken cancellationToken = default)
    {
        var prepared = await PrepareAsync(contract, cancellationToken);
        var draft = ParseDraft(prepared);
        var positions = await _signatures.GetSignaturesAsync(prepared, cancellationToken);
        return new(prepared, draft, positions, PreparedContractSignatureProgress.From(positions));
    }

    public async Task<PreparedContractSignatureDto> SaveSignatureAsync(KommunikationsvorgangDto contract, PreparedContractSignatureSaveRequest request, CancellationToken cancellationToken = default)
    {
        var state = await GetSignatureStateAsync(contract, cancellationToken);
        if (request.KommunikationsvorgangId != state.Contract.Id || request.SignaturRevision != state.Contract.SignaturRevision)
            throw new PreparedContractConflictException("Die Signatur gehört nicht zur aktuell eingefrorenen Mitgliedsantragsrevision.");
        if (!PreparedMembershipApplicationSignaturePlanBuilder.BuildPersistent(state.Draft).Any(slot => slot.Key == request.SignaturSchluessel))
            throw new PreparedContractValidationException("Die Signaturposition gehört nicht zum Mitgliedsantrag.");
        return await _signatures.SaveSignatureAsync(request, cancellationToken);
    }

    public async Task<PreparedContractSignatureProgress> GetProgressAsync(KommunikationsvorgangDto contract, CancellationToken cancellationToken = default)
        => (await GetSignatureStateAsync(contract, cancellationToken)).Progress;

    public async Task<bool> CanFinalizeAsync(KommunikationsvorgangDto contract, CancellationToken cancellationToken = default)
        => await _signatures.AreRequiredSignaturesCompleteAsync((await PrepareAsync(contract, cancellationToken)), cancellationToken);

    public async Task<PreparedMembershipApplicationSigningResult> FinalizeAsync(KommunikationsvorgangDto contract, CancellationToken cancellationToken = default)
    {
        var state = await GetSignatureStateAsync(contract, cancellationToken);
        if (!await _signatures.AreRequiredSignaturesCompleteAsync(state.Contract, cancellationToken))
            throw new PreparedContractValidationException("Alle erforderlichen persistenten Signaturen müssen vor der Finalisierung vorliegen.");
        try
        {
            var finalize = await _contractClient.FinalizeMembershipApplicationAsync(CreateFinalizeRequest(state), cancellationToken);
            if (finalize.DocumentId is not > 0) throw new PreparedContractValidationException("Der Finalize-Vorgang hat keine gültige Dokument-ID zurückgegeben.");
            var reviewed = await _repository.MarkFinalDocumentReadyForReviewAsync(state.Contract.Id, state.Contract.Revision, finalize.DocumentId.Value, cancellationToken);
            return new(PreparedMembershipApplicationSigningState.FinalDocumentReadyForReview, false, finalize, reviewed);
        }
        catch (Exception exception) { return new(PreparedMembershipApplicationSigningState.FinalizeFailedRetryPossible, false, null, state.Contract, exception); }
    }

    private async Task<KommunikationsvorgangDto> PrepareAsync(KommunikationsvorgangDto contract, CancellationToken cancellationToken)
    {
        if (contract.Dokumenttyp != PreparedContractConstants.MembershipApplication || contract.Vorgangstyp != PreparedContractConstants.ContractSignatureProcess)
            throw new PreparedContractValidationException("Der Vertragsvorgang ist kein Mitgliedsantrag.");
        if (contract.Status == PreparedContractConstants.Prepared)
        {
            await RevalidateVereinsunterlagenBeforeStartAsync(contract, cancellationToken);
            return await _startService.StartAsync(contract, cancellationToken);
        }
        if (contract.Status != PreparedContractConstants.ReadyForSignature || contract.SignaturRevision is null || contract.SignaturRevision != contract.Revision || contract.SignaturGestartetAm is null || contract.Id <= 0 || contract.MitgliedId <= 0)
            throw new PreparedContractValidationException("Der Vertragsvorgang ist nicht korrekt für die Signatur eingefroren.");
        return contract;
    }

    private async Task RevalidateVereinsunterlagenBeforeStartAsync(KommunikationsvorgangDto contract, CancellationToken cancellationToken)
    {
        var draft = ParseDraft(contract);
        if (!VereinsunterlagenNachweisHelper.IsComplete(draft.VereinsunterlagenNachweis, PreparedContractConstants.MembershipApplication)) throw new PreparedContractValidationException("Der vorbereitete Mitgliedsantrag enthält noch keinen Satzungsnachweis. Bitte erstellen Sie zuerst eine neue Vorschau.");
        if (_vereinsunterlagenService is null) throw new InvalidOperationException("Der Vereinsunterlagenservice ist für den Signaturstart nicht registriert.");
        var current = await _vereinsunterlagenService.CreateCurrentSnapshotAsync(contract.MitgliedId, PreparedContractConstants.MembershipApplication, cancellationToken);
        if (VereinsunterlagenNachweisHelper.Equivalent(draft.VereinsunterlagenNachweis, current, PreparedContractConstants.MembershipApplication)) return;
        await _repository.UpdateDraftAsync(contract.Id, contract.Revision, PreparedContractDraftParser.Serialize(draft with { VereinsunterlagenNachweis = current }), cancellationToken);
        throw new PreparedContractValidationException("Die Pflichtunterlagen haben sich seit der Vorschau geändert. Der aktualisierte Entwurf wurde gespeichert; bitte erstellen Sie eine neue Vorschau.");
    }

    private static PreparedMembershipApplicationDraft ParseDraft(KommunikationsvorgangDto contract)
    {
        if (PreparedContractDraftParser.Parse(contract.EntwurfDaten) is not PreparedMembershipApplicationDraft draft || draft.MemberId != contract.MitgliedId)
            throw new PreparedContractValidationException("Der gespeicherte Mitgliedsantrag passt nicht zum Vorgang.");
        return draft;
    }

    private static MembershipApplicationFinalizeRequest CreateFinalizeRequest(PreparedMembershipApplicationSignatureState state)
    {
        var isMinor = state.Draft.Representative is not null;
        var application = PreparedMembershipApplicationMapper.ToDocumentRequest(state.Draft, isMinor);
        application.NebenmitgliedEinbeziehen = !isMinor && state.Draft.IncludeSecondaryMember;
        return new() { Application = application, KommunikationsvorgangId = state.Contract.Id, SourceKommunikationsvorgangId = state.Contract.Id, SourceRevision = state.Contract.SignaturRevision!.Value, AdmissionFee = state.Draft.MemberFee };
    }
}

public sealed record PreparedMembershipApplicationSignatureState(KommunikationsvorgangDto Contract, PreparedMembershipApplicationDraft Draft, IReadOnlyList<PreparedContractSignatureDto> Positions, PreparedContractSignatureProgress Progress);
