using KGV.Core.Interfaces;
using KGV.Core.Models;

namespace KGV.Core.Services;

/// <summary>Orchestrates the persistent, revision-bound lease-contract signature workflow.</summary>
public sealed class PreparedLeaseContractSigningService
{
    private readonly IPreparedContractRepository _repository;
    private readonly IContractGenerationClient _contractClient;
    private readonly ISupabaseService _supabaseService;
    private readonly PreparedLeaseContractVereinsunterlagenService _unterlagen;
    private readonly PreparedContractSigningStartService _start;
    private readonly IPreparedContractSignatureService _signatures;

    public PreparedLeaseContractSigningService(IPreparedContractRepository repository, IContractGenerationClient contractClient, ISupabaseService supabaseService, PreparedLeaseContractVereinsunterlagenService unterlagen, PreparedContractSigningStartService? start = null, IPreparedContractSignatureService? persistentSignatures = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _contractClient = contractClient ?? throw new ArgumentNullException(nameof(contractClient));
        _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        _unterlagen = unterlagen ?? throw new ArgumentNullException(nameof(unterlagen));
        _signatures = persistentSignatures ?? throw new ArgumentNullException(nameof(persistentSignatures), "Der persistente Signaturservice ist erforderlich.");
        _start = start ?? new PreparedContractSigningStartService(repository, _signatures);
    }

    public async Task<PreparedLeaseContractSignatureState> StartOrResumeAsync(KommunikationsvorgangDto contract, CancellationToken cancellationToken = default)
    {
        var prepared = await PrepareAsync(contract, cancellationToken);
        var (draft, resolution) = await ValidateAndResolveAsync(prepared, cancellationToken);
        var positions = await _signatures.EnsurePositionsAsync(prepared, PreparedLeaseContractSignaturePlanBuilder.BuildPersistent(draft, resolution.IstMinderjaehrig, resolution.HatAktivenGesetzlichenVertreter), cancellationToken);
        return new(prepared, draft, resolution.IstMinderjaehrig, positions, PreparedContractSignatureProgress.From(positions));
    }

    public async Task<PreparedLeaseContractSignatureState> GetSignatureStateAsync(KommunikationsvorgangDto contract, CancellationToken cancellationToken = default)
    {
        var prepared = await PrepareAsync(contract, cancellationToken);
        var (draft, resolution) = await ValidateAndResolveAsync(prepared, cancellationToken);
        var positions = await _signatures.GetSignaturesAsync(prepared, cancellationToken);
        return new(prepared, draft, resolution.IstMinderjaehrig, positions, PreparedContractSignatureProgress.From(positions));
    }

    public async Task<PreparedContractSignatureDto> SaveSignatureAsync(KommunikationsvorgangDto contract, PreparedContractSignatureSaveRequest request, CancellationToken cancellationToken = default)
    {
        var state = await GetSignatureStateAsync(contract, cancellationToken);
        if (request.KommunikationsvorgangId != state.Contract.Id || request.SignaturRevision != state.Contract.SignaturRevision)
            throw new PreparedContractConflictException("Die Signatur gehört nicht zur aktuell eingefrorenen Pachtvertragsrevision.");
        if (!PreparedLeaseContractSignaturePlanBuilder.BuildPersistent(state.Draft, state.IsMinor, true).Any(slot => slot.Key == request.SignaturSchluessel))
            throw new PreparedContractValidationException("Die Signaturposition gehört nicht zum Pachtvertrag.");
        return await _signatures.SaveSignatureAsync(request, cancellationToken);
    }

    public async Task<PreparedLeaseContractSigningResult> FinalizeAsync(KommunikationsvorgangDto contract, CancellationToken cancellationToken = default)
    {
        var state = await GetSignatureStateAsync(contract, cancellationToken);
        if (!await _signatures.AreRequiredSignaturesCompleteAsync(state.Contract, cancellationToken))
            throw new PreparedContractValidationException("Alle erforderlichen persistenten Signaturen müssen vor der Finalisierung vorliegen.");
        try
        {
            var result = await _contractClient.FinalizeLeaseContractAsync(new LeaseContractFinalizeRequest { Contract = PreparedLeaseContractMapper.ToDocumentRequest(state.Draft, state.IsMinor), KommunikationsvorgangId = state.Contract.Id, SourceRevision = state.Contract.SignaturRevision!.Value }, cancellationToken);
            if (result.DocumentId is not > 0) throw new PreparedContractValidationException("Der Finalize-Vorgang hat keine gültige Dokument-ID zurückgegeben.");
            var reviewed = await _repository.MarkFinalDocumentReadyForReviewAsync(state.Contract.Id, state.Contract.Revision, result.DocumentId.Value, cancellationToken);
            return new(true, result, reviewed, null);
        }
        catch (Exception exception) { return new(false, null, state.Contract, exception); }
    }

    private async Task<KommunikationsvorgangDto> PrepareAsync(KommunikationsvorgangDto contract, CancellationToken cancellationToken)
    {
        contract = await _repository.LoadAsync(contract.Id, cancellationToken);
        Parse(contract);
        if (contract.Status == PreparedContractConstants.Prepared)
        {
            await _unterlagen.RevalidateBeforeStartAsync(contract, cancellationToken);
            contract = await _start.StartAsync(contract, cancellationToken);
        }
        if (contract.Status != PreparedContractConstants.ReadyForSignature || contract.SignaturRevision != contract.Revision || contract.SignaturGestartetAm is null)
            throw new PreparedContractValidationException("Der Pachtvertrag ist nicht korrekt für die Signatur eingefroren.");
        return contract;
    }

    private async Task<(PreparedLeaseContractDraft Draft, GesetzlicherVertreterAufloesung Resolution)> ValidateAndResolveAsync(KommunikationsvorgangDto contract, CancellationToken cancellationToken)
    {
        var draft = Parse(contract);
        var resolution = await _supabaseService.ResolveGesetzlicherVertreterAsync(draft.MemberId, DateTime.ParseExact(draft.Draft.StartDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
        var relationship = await _supabaseService.GetAktivesPachtverhaeltnisStatusAsync(draft.Draft.ParcelId ?? 0);
        ValidateFrozenDraft(draft, resolution.IstMinderjaehrig, relationship);
        return (draft, resolution);
    }

    private static PreparedLeaseContractDraft Parse(KommunikationsvorgangDto contract) => contract.Dokumenttyp == PreparedContractConstants.LeaseContract && PreparedContractDraftParser.Parse(contract.EntwurfDaten) is PreparedLeaseContractDraft draft && draft.MemberId == contract.MitgliedId ? draft : throw new PreparedContractValidationException("Der gespeicherte Vertragsentwurf ist kein passender Pachtvertrag.");
    private static void ValidateFrozenDraft(PreparedLeaseContractDraft draft, bool isMinor, AktivesPachtverhaeltnisStatus relationship)
    {
        if (isMinor && (draft.Draft.IncludeSecondaryMember || draft.Draft.SecondaryMemberId is not null || !string.IsNullOrWhiteSpace(draft.Draft.EffectiveDate))) throw new PreparedContractValidationException("Ein minderjähriger Pächter darf weder Pächter 2 noch ein Wirksamkeitsdatum enthalten.");
        if (!draft.Draft.IncludeSecondaryMember && draft.Draft.SecondaryMemberId is not null) throw new PreparedContractValidationException("Ohne Pächter 2 darf kein Nebenmitglied gespeichert sein.");
        if (draft.Draft.IncludeSecondaryMember && draft.Draft.SecondaryMemberId is not > 0) throw new PreparedContractValidationException("Der vorbereitete Pachtvertrag enthält kein Nebenmitglied für Pächter 2. Bitte eine neue Vorschau erstellen.");
        if (!relationship.HatAktivesPachtverhaeltnis && !string.IsNullOrWhiteSpace(draft.Draft.EffectiveDate)) throw new PreparedContractValidationException("Ein Erstvertrag darf kein Wirksamkeitsdatum einer Neufassung enthalten.");
        if (relationship.HatAktivesPachtverhaeltnis && relationship.Vertragsfassungen == 0) throw new PreparedContractValidationException("Das aktive Pachtverhältnis besitzt keine Vertragsfassung. Eine Neufassung kann nicht gestartet werden.");
        if (relationship.HatAktivesPachtverhaeltnis && !DateTime.TryParseExact(draft.Draft.EffectiveDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _)) throw new PreparedContractValidationException("Für die Neufassung fehlt ein gültiges Wirksamkeitsdatum. Bitte eine neue Vorschau erstellen.");
    }
}

public sealed record PreparedLeaseContractSignatureState(KommunikationsvorgangDto Contract, PreparedLeaseContractDraft Draft, bool IsMinor, IReadOnlyList<PreparedContractSignatureDto> Positions, PreparedContractSignatureProgress Progress);
public sealed record PreparedLeaseContractSigningResult(bool ReadyForReview, ContractFinalizeResult? FinalizeResult, KommunikationsvorgangDto Contract, Exception? Error);
