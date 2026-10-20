using KGV.Core.Models;

namespace KGV.Core.Interfaces;

public interface ILeaseContractCompletionClient
{
    Task<LeaseContractCompletionResult> CompleteAsync(long kommunikationsvorgangId, int expectedRevision, CancellationToken cancellationToken = default);
}
