using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Utilities;

namespace KGV.Core.Services;

public sealed class PreparedContractSignatureService : IPreparedContractSignatureService
{
    private readonly IPreparedContractRepository _contracts;
    private readonly IPreparedContractSignatureRepository _signatures;
    private readonly IPreparedContractSignatureStorage _storage;

    public PreparedContractSignatureService(IPreparedContractRepository contracts, IPreparedContractSignatureRepository signatures, IPreparedContractSignatureStorage storage)
    { _contracts = contracts ?? throw new ArgumentNullException(nameof(contracts)); _signatures = signatures ?? throw new ArgumentNullException(nameof(signatures)); _storage = storage ?? throw new ArgumentNullException(nameof(storage)); }

    public async Task<IReadOnlyList<PreparedContractSignatureDto>> EnsurePositionsAsync(KommunikationsvorgangDto contract, IReadOnlyList<PreparedContractSignatureSlot> slots, CancellationToken cancellationToken = default)
    {
        ValidateStarted(contract); ArgumentNullException.ThrowIfNull(slots);
        foreach (var slot in slots)
            await _signatures.EnsurePositionAsync(contract.Id, contract.SignaturRevision!.Value, slot, cancellationToken);
        return await GetSignaturesAsync(contract, cancellationToken);
    }

    public Task<IReadOnlyList<PreparedContractSignatureDto>> GetSignaturesAsync(KommunikationsvorgangDto contract, CancellationToken cancellationToken = default)
    { ValidateStarted(contract); return _signatures.ListAsync(contract.Id, contract.SignaturRevision!.Value, cancellationToken); }

    public Task<IReadOnlyList<PreparedContractAuthorizedSignatoryDto>> GetAuthorizedClubSignatoriesAsync(long kommunikationsvorgangId, int signaturRevision, CancellationToken cancellationToken = default)
        => _signatures.ListAuthorizedClubSignatoriesAsync(kommunikationsvorgangId, signaturRevision, cancellationToken);

    public async Task<PreparedContractSignatureDto> SaveSignatureAsync(PreparedContractSignatureSaveRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.PngBytes is not { Length: > 0 }) throw new PreparedContractValidationException("Die Signatur enthält keine PNG-Daten.");
        var contract = await _contracts.LoadAsync(request.KommunikationsvorgangId, cancellationToken);
        ValidateStarted(contract);
        if (contract.SignaturRevision != request.SignaturRevision) throw new PreparedContractConflictException("Die Signatur gehört nicht zur aktuell eingefrorenen Vertragsrevision.");
        var positions = await _signatures.ListAsync(request.KommunikationsvorgangId, request.SignaturRevision, cancellationToken);
        var position = positions.SingleOrDefault(x => x.SignaturSchluessel == request.SignaturSchluessel) ?? throw new PreparedContractValidationException("Die Signaturposition wurde nicht vorbereitet.");
        if (position.Status == PreparedContractSignatureStatuses.Signed) throw new PreparedContractConflictException("Diese Signaturposition ist bereits unterschrieben und darf nicht überschrieben werden.");
        var upload = await _storage.UploadAsync(new PreparedContractSignatureStorageUploadRequest(
            request.KommunikationsvorgangId,
            request.SignaturRevision,
            request.SignaturSchluessel,
            request.PngBytes), cancellationToken);
        return await _signatures.SaveAsync(request, upload.StoragePath, upload.Sha256, cancellationToken);
    }

    public async Task<bool> AreRequiredSignaturesCompleteAsync(KommunikationsvorgangDto contract, CancellationToken cancellationToken = default)
    { ValidateStarted(contract); return await _signatures.AreRequiredSignaturesCompleteAsync(contract.Id, contract.SignaturRevision!.Value, cancellationToken); }

    private static void ValidateStarted(KommunikationsvorgangDto contract)
    {
        ArgumentNullException.ThrowIfNull(contract);
        if (contract.Id <= 0 || contract.Status != PreparedContractConstants.ReadyForSignature || contract.SignaturRevision is null || contract.SignaturRevision != contract.Revision)
            throw new PreparedContractValidationException("Der Vertragsvorgang ist nicht korrekt für persistente Signaturen gestartet.");
    }
}
