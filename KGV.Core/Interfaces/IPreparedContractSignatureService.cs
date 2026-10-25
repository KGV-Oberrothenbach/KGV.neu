using KGV.Core.Models;

namespace KGV.Core.Interfaces;

public interface IPreparedContractSignatureService
{
    Task<IReadOnlyList<PreparedContractSignatureDto>> EnsurePositionsAsync(KommunikationsvorgangDto contract, IReadOnlyList<PreparedContractSignatureSlot> slots, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PreparedContractSignatureDto>> GetSignaturesAsync(KommunikationsvorgangDto contract, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PreparedContractAuthorizedSignatoryDto>> GetAuthorizedClubSignatoriesAsync(long kommunikationsvorgangId, int signaturRevision, CancellationToken cancellationToken = default);
    Task<PreparedContractSignatureDto> SaveSignatureAsync(PreparedContractSignatureSaveRequest request, CancellationToken cancellationToken = default);
    Task<bool> AreRequiredSignaturesCompleteAsync(KommunikationsvorgangDto contract, CancellationToken cancellationToken = default);
}
