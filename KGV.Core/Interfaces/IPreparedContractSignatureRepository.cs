using KGV.Core.Models;

namespace KGV.Core.Interfaces;

public interface IPreparedContractSignatureRepository
{
    Task<IReadOnlyList<PreparedContractSignatureDto>> ListAsync(long kommunikationsvorgangId, int signaturRevision, CancellationToken cancellationToken = default);
    Task<PreparedContractSignatureDto> EnsurePositionAsync(long kommunikationsvorgangId, int signaturRevision, PreparedContractSignatureSlot slot, CancellationToken cancellationToken = default);
    Task<PreparedContractSignatureDto> SaveAsync(PreparedContractSignatureSaveRequest request, string storagePath, string sha256, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PreparedContractAuthorizedSignatoryDto>> ListAuthorizedClubSignatoriesAsync(long kommunikationsvorgangId, int signaturRevision, CancellationToken cancellationToken = default);
    Task<bool> AreRequiredSignaturesCompleteAsync(long kommunikationsvorgangId, int signaturRevision, CancellationToken cancellationToken = default);
}
