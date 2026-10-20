using KGV.Core.Interfaces;
using KGV.Core.Models;

namespace KGV.Core.Services;

public sealed class PreparedLeaseContractSigningService
{
    private readonly IPreparedContractRepository _repository;
    private readonly IContractGenerationClient _contractClient;
    private readonly ISupabaseService _supabaseService;
    private readonly PreparedLeaseContractVereinsunterlagenService _unterlagen;
    private readonly PreparedContractSigningStartService _start;
    private readonly PreparedFinalDocumentRecoveryService _recovery;

    public PreparedLeaseContractSigningService(IPreparedContractRepository repository, IContractGenerationClient contractClient, ISupabaseService supabaseService, PreparedLeaseContractVereinsunterlagenService unterlagen, PreparedContractSigningStartService? start = null)
    { _repository = repository; _contractClient = contractClient; _supabaseService = supabaseService; _unterlagen = unterlagen; _start = start ?? new(repository); _recovery = new(repository); }

    public async Task<KommunikationsvorgangDto> StartAsync(KommunikationsvorgangDto contract, CancellationToken cancellationToken = default)
    {
        contract = await _repository.LoadAsync(contract.Id, cancellationToken);
        var draft = Parse(contract);
        if (contract.Status == PreparedContractConstants.Prepared)
        {
            await _unterlagen.RevalidateBeforeStartAsync(contract, cancellationToken);
            contract = await _start.StartAsync(contract, cancellationToken);
        }
        ValidateStarted(contract);
        await PlanAsync(draft, cancellationToken);
        return contract;
    }

    public async Task<KommunikationsvorgangDto> FinalizeAsync(KommunikationsvorgangDto contract, IPreparedSignatureCapture capture, CancellationToken cancellationToken = default)
    {
        contract = await _repository.LoadAsync(contract.Id, cancellationToken);
        var recovered = await _recovery.RecoverAsync(contract, cancellationToken);
        if (recovered is not null) return recovered;
        ValidateStarted(contract); var draft = Parse(contract); var plan = await PlanAsync(draft, cancellationToken);
        var values = new Dictionary<string, string>();
        foreach (var step in plan) { var captured = await capture.CaptureAsync(step, cancellationToken); if (captured is null) throw new OperationCanceledException("Die Signaturerfassung wurde abgebrochen."); values[step.Key] = DigitalSignatureEncoder.EncodePngDataUrl(captured); }
        contract = await _repository.LoadAsync(contract.Id, cancellationToken);
        recovered = await _recovery.RecoverAsync(contract, cancellationToken); if (recovered is not null) return recovered;
        ValidateStarted(contract);
        var minor = (await ResolveAsync(draft, cancellationToken)).IstMinderjaehrig;
        var result = await _contractClient.FinalizeLeaseContractAsync(new LeaseContractFinalizeRequest { Contract = PreparedLeaseContractMapper.ToDocumentRequest(draft, minor), MemberSignature = values[LeaseContractSignatureKeys.Member], SecondarySignature = values.GetValueOrDefault(LeaseContractSignatureKeys.SecondParty), BoardSignature = values[LeaseContractSignatureKeys.Board] }, cancellationToken);
        if (result.DocumentId is not > 0) throw new PreparedContractValidationException("Der Finalize-Vorgang hat keine gültige Dokument-ID zurückgegeben.");
        return await _repository.MarkFinalDocumentReadyForReviewAsync(contract.Id, contract.Revision, result.DocumentId.Value, cancellationToken);
    }

    private async Task<IReadOnlyList<SignatureStep>> PlanAsync(PreparedLeaseContractDraft draft, CancellationToken ct) { var resolution = await ResolveAsync(draft, ct); var relationship = await _supabaseService.GetAktivesPachtverhaeltnisStatusAsync(draft.Draft.ParcelId ?? 0); ValidateFrozenDraft(draft, resolution.IstMinderjaehrig, relationship); return PreparedLeaseContractSignaturePlanBuilder.Build(draft, resolution.IstMinderjaehrig, resolution.HatAktivenGesetzlichenVertreter); }
    private Task<GesetzlicherVertreterAufloesung> ResolveAsync(PreparedLeaseContractDraft draft, CancellationToken ct) => _supabaseService.ResolveGesetzlicherVertreterAsync(draft.MemberId, DateTime.ParseExact(draft.Draft.StartDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
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
    private static void ValidateStarted(KommunikationsvorgangDto c) { if (c.Status != PreparedContractConstants.ReadyForSignature || c.SignaturRevision != c.Revision || c.SignaturGestartetAm is null) throw new PreparedContractValidationException("Der Pachtvertrag ist nicht korrekt für die Signatur eingefroren."); }
}
