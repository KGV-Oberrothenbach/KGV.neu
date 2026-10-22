using KGV.Core.Interfaces;
using KGV.Core.Models;

namespace KGV.Core.Services;

/// <summary>Persistent single-position signing orchestration for regular maintenance contracts.</summary>
public sealed class PreparedMaintenanceContractSigningService
{
    private readonly IPreparedContractRepository _repository;
    private readonly IContractGenerationClient _contracts;
    private readonly IPreparedContractSignatureService _signatures;
    private readonly PreparedContractSigningStartService _start;

    public PreparedMaintenanceContractSigningService(IPreparedContractRepository repository, IContractGenerationClient contracts, IPreparedContractSignatureService signatures, PreparedContractSigningStartService? start = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _contracts = contracts ?? throw new ArgumentNullException(nameof(contracts));
        _signatures = signatures ?? throw new ArgumentNullException(nameof(signatures));
        _start = start ?? new PreparedContractSigningStartService(_repository, _signatures);
    }

    public async Task<PreparedMaintenanceContractSignatureState> StartOrResumeAsync(KommunikationsvorgangDto contract, CancellationToken cancellationToken = default)
    { var prepared = await PrepareAsync(contract, cancellationToken); var draft = Parse(prepared); var positions = await _signatures.EnsurePositionsAsync(prepared, PreparedMaintenanceContractSignaturePlanBuilder.BuildPersistent(draft), cancellationToken); return new(prepared, draft, positions, PreparedContractSignatureProgress.From(positions)); }
    public async Task<PreparedMaintenanceContractSignatureState> GetSignatureStateAsync(KommunikationsvorgangDto contract, CancellationToken cancellationToken = default)
    { var prepared = await PrepareAsync(contract, cancellationToken); var draft = Parse(prepared); var positions = await _signatures.GetSignaturesAsync(prepared, cancellationToken); return new(prepared, draft, positions, PreparedContractSignatureProgress.From(positions)); }
    public async Task<PreparedContractSignatureDto> SaveSignatureAsync(KommunikationsvorgangDto contract, PreparedContractSignatureSaveRequest request, CancellationToken cancellationToken = default)
    { var state = await GetSignatureStateAsync(contract, cancellationToken); if (request.KommunikationsvorgangId != state.Contract.Id || request.SignaturRevision != state.Contract.SignaturRevision || !PreparedMaintenanceContractSignaturePlanBuilder.BuildPersistent(state.Draft).Any(slot => slot.Key == request.SignaturSchluessel)) throw new PreparedContractValidationException("Die Signaturposition gehört nicht zum Wartungsvertrag."); return await _signatures.SaveSignatureAsync(request, cancellationToken); }
    public async Task<PreparedContractSignatureProgress> GetProgressAsync(KommunikationsvorgangDto contract, CancellationToken cancellationToken = default)
        => (await GetSignatureStateAsync(contract, cancellationToken)).Progress;
    public async Task<bool> CanFinalizeAsync(KommunikationsvorgangDto contract, CancellationToken cancellationToken = default)
        => await _signatures.AreRequiredSignaturesCompleteAsync((await PrepareAsync(contract, cancellationToken)), cancellationToken);
    public async Task<PreparedMaintenanceContractSigningResult> FinalizeAsync(KommunikationsvorgangDto contract, CancellationToken cancellationToken = default)
    { var state = await GetSignatureStateAsync(contract, cancellationToken); if (!await _signatures.AreRequiredSignaturesCompleteAsync(state.Contract, cancellationToken)) throw new PreparedContractValidationException("Alle drei Wartungsvertragsunterschriften müssen vorliegen."); try { var result = await _contracts.FinalizeMaintenanceContractAsync(new MaintenanceContractFinalizeRequest { KommunikationsvorgangId = state.Contract.Id }, cancellationToken); if (result.DocumentId is not > 0) throw new PreparedContractValidationException("Der Finalizer lieferte keine Dokument-ID."); return new(true, result, await _repository.LoadAsync(state.Contract.Id, cancellationToken), null); } catch (Exception ex) { return new(false, null, state.Contract, ex); } }
    private async Task<KommunikationsvorgangDto> PrepareAsync(KommunikationsvorgangDto contract, CancellationToken ct) { contract = await _repository.LoadAsync(contract.Id, ct); _ = Parse(contract); if (contract.Status == PreparedContractConstants.Prepared) contract = await _start.StartAsync(contract, ct); if (contract.Status != PreparedContractConstants.ReadyForSignature || contract.SignaturRevision != contract.Revision || contract.SignaturGestartetAm is null) throw new PreparedContractValidationException("Der Wartungsvertrag ist nicht korrekt für die Signatur eingefroren."); return contract; }
    private static PreparedMaintenanceContractDraft Parse(KommunikationsvorgangDto contract) => contract.Dokumenttyp == PreparedContractConstants.MaintenanceContract && PreparedContractDraftParser.Parse(contract.EntwurfDaten) is PreparedMaintenanceContractDraft draft && draft.MemberId == contract.MitgliedId ? draft : throw new PreparedContractValidationException("Der Vorgang ist kein passender Wartungsvertrag.");
}
public sealed record PreparedMaintenanceContractSignatureState(KommunikationsvorgangDto Contract, PreparedMaintenanceContractDraft Draft, IReadOnlyList<PreparedContractSignatureDto> Positions, PreparedContractSignatureProgress Progress);
public sealed record PreparedMaintenanceContractSigningResult(bool Completed, ContractFinalizeResult? FinalizeResult, KommunikationsvorgangDto Contract, Exception? Error);
