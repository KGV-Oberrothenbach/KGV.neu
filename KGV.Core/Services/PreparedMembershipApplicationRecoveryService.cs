using KGV.Core.Interfaces;
using KGV.Core.Models;

namespace KGV.Core.Services;

/// <summary>
/// Compatibility facade for callers from before provenance-based recovery.
/// </summary>
public sealed class PreparedMembershipApplicationRecoveryService
{
    private readonly PreparedFinalDocumentRecoveryService _recovery;

    public PreparedMembershipApplicationRecoveryService(IPreparedContractRepository repository)
    {
        _recovery = new PreparedFinalDocumentRecoveryService(repository);
    }

    public async Task<KommunikationsvorgangDto?> RecoverAsync(
        KommunikationsvorgangDto contract,
        IEnumerable<DocumentInfo> memberDocuments,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(memberDocuments); // Kept for source compatibility; recovery no longer uses document heuristics.
        return contract.Dokumenttyp == PreparedContractConstants.MembershipApplication
            ? await _recovery.RecoverAsync(contract, cancellationToken)
            : null;
    }
}
