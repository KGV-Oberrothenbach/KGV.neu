using KGV.Core.Interfaces;
using KGV.Core.Models;

namespace KGV.Core.Services;

public sealed class PreparedMembershipApplicationSigningService
{
    private readonly IPreparedContractRepository _repository;
    private readonly IContractGenerationClient _contractClient;
    private readonly PreparedContractSigningStartService _startService;
    private KommunikationsvorgangDto? _contract;
    private PreparedMembershipApplicationDraft? _draft;
    private Dictionary<string, string>? _signatures;
    private ContractFinalizeResult? _finalizeResult;
    private bool _finalDocumentCreated;
    private bool _finalDocumentReadyForReview;

    public PreparedMembershipApplicationSigningService(
        IPreparedContractRepository repository,
        IContractGenerationClient contractClient,
        PreparedContractSigningStartService? startService = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _contractClient = contractClient ?? throw new ArgumentNullException(nameof(contractClient));
        _startService = startService ?? new PreparedContractSigningStartService(repository);
    }

    public async Task<PreparedMembershipApplicationSigningResult> RunAsync(
        KommunikationsvorgangDto contract,
        IPreparedSignatureCapture capture,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(capture);

        if (_finalDocumentReadyForReview)
            return ReadyForReviewResult();

        if (_finalDocumentCreated)
            return await RetryAsync(cancellationToken);

        if (_signatures is not null)
            return await FinalizeAndMarkReadyForReviewAsync(cancellationToken);

        _contract = await PrepareAsync(contract, cancellationToken);
        _draft = ParseDraft(_contract);
        var captures = await CaptureAllAsync(capture, _draft, cancellationToken);
        _signatures = captures.Signatures;
        if (_signatures is null)
            return new(PreparedMembershipApplicationSigningState.Cancelled, captures.DiscardedCapturedSignatures);

        return await FinalizeAndMarkReadyForReviewAsync(cancellationToken);
    }

    public async Task<PreparedMembershipApplicationSigningResult> RetryAsync(CancellationToken cancellationToken = default)
    {
        if (_contract is null || _draft is null || _finalizeResult is null && _signatures is null)
            throw new InvalidOperationException("Es gibt keine Signatursitzung zum Wiederholen.");

        if (_finalDocumentReadyForReview)
            return ReadyForReviewResult();

        return _finalDocumentCreated
            ? await MarkReadyForReviewAsync(cancellationToken)
            : await FinalizeAndMarkReadyForReviewAsync(cancellationToken);
    }

    private async Task<KommunikationsvorgangDto> PrepareAsync(KommunikationsvorgangDto contract, CancellationToken cancellationToken)
    {
        if (contract.Dokumenttyp != PreparedContractConstants.MembershipApplication)
            throw new PreparedContractValidationException("Der Vertragsvorgang ist kein Mitgliedsantrag.");

        if (contract.Status == PreparedContractConstants.Prepared)
            return await _startService.StartAsync(contract, cancellationToken);

        if (contract.Status != PreparedContractConstants.ReadyForSignature ||
            contract.SignaturRevision is null ||
            contract.SignaturRevision != contract.Revision ||
            contract.SignaturGestartetAm is null)
            throw new PreparedContractValidationException("Der Vertragsvorgang ist nicht korrekt für die Signatur eingefroren.");

        if (contract.Id <= 0 || contract.Revision < 1 || contract.MitgliedId <= 0)
            throw new PreparedContractValidationException("Der eingefrorene Vertragsvorgang enthält ungültige Basisdaten.");

        return contract;
    }

    private static PreparedMembershipApplicationDraft ParseDraft(KommunikationsvorgangDto contract)
    {
        if (PreparedContractDraftParser.Parse(contract.EntwurfDaten) is not PreparedMembershipApplicationDraft draft)
            throw new PreparedContractValidationException("Der gespeicherte Vertragsentwurf ist kein Mitgliedsantrag.");
        if (draft.MemberId != contract.MitgliedId)
            throw new PreparedContractValidationException("Der gespeicherte Mitgliedsantrag gehört zu einem anderen Mitglied.");
        return draft;
    }

    private static async Task<(Dictionary<string, string>? Signatures, bool DiscardedCapturedSignatures)> CaptureAllAsync(
        IPreparedSignatureCapture capture,
        PreparedMembershipApplicationDraft draft,
        CancellationToken cancellationToken)
    {
        var plan = PreparedMembershipApplicationSignaturePlanBuilder.Build(draft);
        var result = new Dictionary<string, string>();
        foreach (var step in plan)
        {
            var signature = await capture.CaptureAsync(step, cancellationToken);
            if (signature is null)
                return (null, result.Count > 0);
            result[step.Key] = DigitalSignatureEncoder.EncodePngDataUrl(signature);
        }
        return (result, false);
    }

    private async Task<PreparedMembershipApplicationSigningResult> FinalizeAndMarkReadyForReviewAsync(CancellationToken cancellationToken)
    {
        try
        {
            var request = CreateFinalizeRequest();
            _finalizeResult = await _contractClient.FinalizeMembershipApplicationAsync(request, cancellationToken);
            _finalDocumentCreated = true;
        }
        catch (Exception exception)
        {
            return new(PreparedMembershipApplicationSigningState.FinalizeFailedRetryPossible, false, null, null, exception);
        }

        return await MarkReadyForReviewAsync(cancellationToken);
    }

    private async Task<PreparedMembershipApplicationSigningResult> MarkReadyForReviewAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_finalizeResult?.DocumentId is not > 0)
                throw new PreparedContractValidationException("Der Finalize-Vorgang hat keine gültige Dokument-ID zurückgegeben.");
            var reviewed = await _repository.MarkFinalDocumentReadyForReviewAsync(
                _contract!.Id,
                _contract.Revision,
                _finalizeResult.DocumentId.Value,
                cancellationToken);
            if (reviewed.Status != PreparedContractConstants.FinalReview || reviewed.FinalDokumentId != _finalizeResult.DocumentId ||
                reviewed.Id != _contract.Id || reviewed.Revision != _contract.Revision)
                throw new PreparedContractValidationException("Der Server hat den finalen Mitgliedsantrag nicht korrekt zur Prüfung vorgemerkt.");
            _contract = reviewed;
            _finalDocumentReadyForReview = true;
            return ReadyForReviewResult();
        }
        catch (Exception exception)
        {
            return new(PreparedMembershipApplicationSigningState.FinalDocumentReviewPersistenceFailed, false, _finalizeResult, _contract, exception);
        }
    }

    private PreparedMembershipApplicationSigningResult ReadyForReviewResult()
        => new(PreparedMembershipApplicationSigningState.FinalDocumentReadyForReview, false, _finalizeResult, _contract);

    private MembershipApplicationFinalizeRequest CreateFinalizeRequest()
    {
        var draft = _draft!;
        var isMinor = draft.Representative is not null;
        var application = PreparedMembershipApplicationMapper.ToDocumentRequest(draft, isMinor);
        application.NebenmitgliedEinbeziehen = !isMinor && draft.IncludeSecondaryMember;
        return new MembershipApplicationFinalizeRequest
        {
            Application = application,
            SourceKommunikationsvorgangId = _contract!.Id,
            SourceRevision = _contract.Revision,
            AdmissionFee = draft.MemberFee,
            ApplicationMemberSignature = Signature(MembershipApplicationSignatureKeys.MemberApplication),
            PrivacyMemberSignature = Signature(MembershipApplicationSignatureKeys.MemberPrivacy),
            ApplicationRepresentativeSignature = isMinor ? Signature(MembershipApplicationSignatureKeys.RepresentativeApplication) : null,
            PrivacyRepresentativeSignature = isMinor ? Signature(MembershipApplicationSignatureKeys.RepresentativePrivacy) : null,
            ApplicationSecondarySignature = !isMinor && draft.IncludeSecondaryMember ? Signature(MembershipApplicationSignatureKeys.SecondaryApplication) : null,
            PrivacySecondarySignature = !isMinor && draft.IncludeSecondaryMember ? Signature(MembershipApplicationSignatureKeys.SecondaryPrivacy) : null,
            BoardSignature = Signature(MembershipApplicationSignatureKeys.BoardApproval)
        };
    }

    private string Signature(string key) => _signatures![key];
}
