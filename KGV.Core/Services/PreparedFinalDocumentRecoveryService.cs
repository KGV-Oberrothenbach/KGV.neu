using KGV.Core.Interfaces;
using KGV.Core.Models;

namespace KGV.Core.Services;

/// <summary>Moves an interrupted finalized contract to final review using immutable document provenance.</summary>
public sealed class PreparedFinalDocumentRecoveryService
{
    private readonly IPreparedContractRepository _repository;

    public PreparedFinalDocumentRecoveryService(IPreparedContractRepository repository)
        => _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<KommunikationsvorgangDto?> RecoverAsync(KommunikationsvorgangDto contract, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contract);
        if (contract.Status != PreparedContractConstants.ReadyForSignature || contract.FinalDokumentId is not null || contract.Id <= 0 || contract.Revision < 1)
            return null;

        var finalDocument = await _repository.FindFinalDocumentByProvenanceAsync(contract.Id, contract.Revision, cancellationToken);
        if (finalDocument is null)
            return null;
        if (finalDocument.SourceKommunikationsvorgangId != contract.Id || finalDocument.SourceRevision != contract.Revision || finalDocument.DocumentId <= 0)
            throw new PreparedContractValidationException("Das gefundene finale Dokument passt nicht eindeutig zum Kommunikationsvorgang.");

        try
        {
            return await MarkAsync(contract, finalDocument.DocumentId, cancellationToken);
        }
        catch (PreparedContractConflictException)
        {
            var current = await _repository.LoadAsync(contract.Id, cancellationToken);
            if (current.Status == PreparedContractConstants.FinalReview && current.FinalDokumentId == finalDocument.DocumentId)
                return current;
            throw;
        }
    }

    private async Task<KommunikationsvorgangDto> MarkAsync(KommunikationsvorgangDto contract, long documentId, CancellationToken cancellationToken)
    {
        var reviewed = await _repository.MarkFinalDocumentReadyForReviewAsync(contract.Id, contract.Revision, documentId, cancellationToken);
        if (reviewed.Id != contract.Id || reviewed.Status != PreparedContractConstants.FinalReview || reviewed.FinalDokumentId != documentId)
            throw new PreparedContractValidationException("Der Server hat das vorhandene finale Dokument nicht korrekt zur letzten Prüfung übernommen.");
        return reviewed;
    }
}
