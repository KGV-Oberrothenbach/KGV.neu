using KGV.Core.Interfaces;
using KGV.Core.Models;

namespace KGV.Core.Services;

public sealed class PreparedMembershipApplicationResumeService
{
    private readonly IPreparedContractRepository _repository;

    public PreparedMembershipApplicationResumeService(IPreparedContractRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<PreparedMembershipApplicationResumeResult> ResolveAsync(
        int memberId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (currentUserId == Guid.Empty)
            throw new ArgumentException("Eine gültige Benutzer-ID ist erforderlich.", nameof(currentUserId));

        var contract = await _repository.FindActiveAsync(
            PreparedContractConstants.MembershipApplication,
            memberId,
            null,
            cancellationToken);

        if (contract is null)
            return new(PreparedMembershipApplicationResumeDecision.New, null);

        if (contract.Status == PreparedContractConstants.ReadyForSignature)
            return new(PreparedMembershipApplicationResumeDecision.SignatureLocked, contract);

        if (contract.Status != PreparedContractConstants.Prepared)
            return new(PreparedMembershipApplicationResumeDecision.InvalidStatus, contract);

        if (contract.ZugewiesenAn == currentUserId)
            return new(PreparedMembershipApplicationResumeDecision.ResumeOwn, contract);

        if (contract.ZugewiesenAn is not null)
            return new(PreparedMembershipApplicationResumeDecision.ForeignAssigned, contract);

        try
        {
            var claimed = await _repository.ClaimAsync(contract.Id, cancellationToken);
            ValidateClaim(contract.Id, currentUserId, claimed);
            return new(PreparedMembershipApplicationResumeDecision.ResumeClaimed, claimed);
        }
        catch (PreparedContractConflictException)
        {
            return new(PreparedMembershipApplicationResumeDecision.ClaimConflict, contract);
        }
    }

    private static void ValidateClaim(long expectedId, Guid currentUserId, KommunikationsvorgangDto claimed)
    {
        if (claimed.Id != expectedId)
            throw new PreparedContractValidationException("Die Claim-Antwort gehört zu einem anderen Vertragsvorgang.");
        if (claimed.Status != PreparedContractConstants.Prepared)
            throw new PreparedContractValidationException("Die Claim-Antwort bestätigt keinen vorbereiteten Vertragsvorgang.");
        if (claimed.ZugewiesenAn != currentUserId)
            throw new PreparedContractValidationException("Die Claim-Antwort bestätigt keine Zuweisung an den aktuellen Benutzer.");
    }
}
