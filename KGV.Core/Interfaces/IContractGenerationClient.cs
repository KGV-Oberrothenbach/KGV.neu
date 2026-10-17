using KGV.Core.Models;

namespace KGV.Core.Interfaces;

public interface IContractGenerationClient
{
    Task<byte[]> PreviewMembershipApplicationAsync(
        MitgliedsantragDokumentRequest request,
        decimal admissionFee,
        CancellationToken cancellationToken = default);
}
