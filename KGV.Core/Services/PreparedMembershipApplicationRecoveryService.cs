using KGV.Core.Interfaces;
using KGV.Core.Models;

namespace KGV.Core.Services;

/// <summary>
/// Repairs an interrupted membership-application finalization after the final
/// signed document was persisted but the communication status was not.
/// </summary>
public sealed class PreparedMembershipApplicationRecoveryService
{
    private readonly IPreparedContractRepository _repository;

    public PreparedMembershipApplicationRecoveryService(IPreparedContractRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<KommunikationsvorgangDto?> RecoverAsync(
        KommunikationsvorgangDto contract,
        IEnumerable<DocumentInfo> memberDocuments,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(memberDocuments);

        if (contract.Dokumenttyp != PreparedContractConstants.MembershipApplication ||
            contract.Status != PreparedContractConstants.ReadyForSignature ||
            contract.FinalDokumentId is not null)
            return null;

        var finalDocument = memberDocuments
            .Where(document => document.Id > 0 &&
                string.Equals(document.FormularDokumentTypKey, FormularDokumentTyp.Mitgliedsantrag, StringComparison.Ordinal) &&
                string.Equals(document.FormularDokumentStatusKey, FormularDokumentStatus.Signiert, StringComparison.Ordinal))
            .OrderByDescending(document => document.UpdatedAt ?? document.CreatedAt ?? DateTime.MinValue)
            .FirstOrDefault();
        if (finalDocument is null)
            return null;

        var reviewed = await _repository.MarkFinalDocumentReadyForReviewAsync(
            contract.Id,
            contract.Revision,
            finalDocument.Id,
            cancellationToken);
        if (reviewed.Id != contract.Id ||
            reviewed.Status != PreparedContractConstants.FinalReview ||
            reviewed.FinalDokumentId != finalDocument.Id)
            throw new PreparedContractValidationException("Der Server hat den vorhandenen signierten Mitgliedsantrag nicht korrekt zur letzten Prüfung übernommen.");

        return reviewed;
    }
}
