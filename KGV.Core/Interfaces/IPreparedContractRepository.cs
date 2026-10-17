using System.Text.Json;
using KGV.Core.Models;

namespace KGV.Core.Interfaces;

public interface IPreparedContractRepository
{
    Task<KommunikationsvorgangDto> LoadAsync(Guid id, CancellationToken cancellationToken = default);
    Task<KommunikationsvorgangDto> CreateAsync(string documentType, int memberId, int? parcelId, JsonElement draft, CancellationToken cancellationToken = default);
    Task<KommunikationsvorgangDto> ClaimAsync(Guid id, CancellationToken cancellationToken = default);
    Task ReleaseAsync(Guid id, CancellationToken cancellationToken = default);
    Task<KommunikationsvorgangDto> UpdateDraftAsync(Guid id, int expectedRevision, JsonElement draft, CancellationToken cancellationToken = default);
    Task<KommunikationsvorgangDto> StartSignatureAsync(Guid id, int expectedRevision, CancellationToken cancellationToken = default);
    Task<KommunikationsvorgangDto> CompleteAsync(Guid id, CancellationToken cancellationToken = default);
}
