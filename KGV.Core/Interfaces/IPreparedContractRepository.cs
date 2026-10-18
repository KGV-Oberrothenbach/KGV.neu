using System.Text.Json;
using KGV.Core.Models;

namespace KGV.Core.Interfaces;

public interface IPreparedContractRepository
{
    Task<IReadOnlyList<KommunikationsvorgangDto>> ListActiveAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
    Task<KommunikationsvorgangDto> LoadAsync(long id, CancellationToken cancellationToken = default);
    Task<KommunikationsvorgangDto?> FindActiveAsync(string documentType, int memberId, int? parcelId, CancellationToken cancellationToken = default);
    Task<KommunikationsvorgangDto> CreateAsync(string documentType, int memberId, int? parcelId, JsonElement draft, CancellationToken cancellationToken = default);
    Task<KommunikationsvorgangDto> ClaimAsync(long id, CancellationToken cancellationToken = default);
    Task ReleaseAsync(long id, CancellationToken cancellationToken = default);
    Task<KommunikationsvorgangDto> UpdateDraftAsync(long id, int expectedRevision, JsonElement draft, CancellationToken cancellationToken = default);
    Task<KommunikationsvorgangDto> StartSignatureAsync(long id, int expectedRevision, CancellationToken cancellationToken = default);
    Task<KommunikationsvorgangDto> MarkFinalDocumentReadyForReviewAsync(long id, int expectedRevision, long finalDocumentId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
    Task<KommunikationsvorgangDto> CompleteAsync(long id, CancellationToken cancellationToken = default);
}
