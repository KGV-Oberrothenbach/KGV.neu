using KGV.Core.Interfaces;
using KGV.Core.Models;

namespace KGV.Core.Services;

public sealed class PreparedLeaseContractResumeService
{
    private readonly IPreparedContractRepository _repository;
    public PreparedLeaseContractResumeService(IPreparedContractRepository repository) => _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<PreparedLeaseContractResumeResult> ResolveAsync(int memberId, int parcelId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (currentUserId == Guid.Empty) throw new ArgumentException("Eine gültige Benutzer-ID ist erforderlich.", nameof(currentUserId));
        var contract = await _repository.FindActiveAsync(PreparedContractConstants.LeaseContract, memberId, parcelId, cancellationToken);
        if (contract is null) return new(PreparedLeaseContractResumeDecision.New, null);
        if (contract.Status == PreparedContractConstants.ReadyForSignature) return new(PreparedLeaseContractResumeDecision.SignatureLocked, contract);
        if (contract.Status != PreparedContractConstants.Prepared) return new(PreparedLeaseContractResumeDecision.InvalidStatus, contract);
        if (contract.ZugewiesenAn == currentUserId) return new(PreparedLeaseContractResumeDecision.ResumeOwn, contract);
        if (contract.ZugewiesenAn is not null) return new(PreparedLeaseContractResumeDecision.ForeignAssigned, contract);
        try
        {
            var claimed = await _repository.ClaimAsync(contract.Id, cancellationToken);
            if (claimed.Id != contract.Id || claimed.Dokumenttyp != PreparedContractConstants.LeaseContract || claimed.MitgliedId != memberId || claimed.ParzelleId != parcelId || claimed.Status != PreparedContractConstants.Prepared || claimed.ZugewiesenAn != currentUserId)
                throw new PreparedContractValidationException("Die Claim-Antwort bestätigt keinen passenden vorbereiteten Pachtvertrag.");
            return new(PreparedLeaseContractResumeDecision.ResumeClaimed, claimed);
        }
        catch (PreparedContractConflictException) { return new(PreparedLeaseContractResumeDecision.ClaimConflict, contract); }
    }
}
