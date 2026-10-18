using KGV.Core.Models;

namespace KGV.Core.Interfaces;

public interface IContractGenerationClient
{
    Task<byte[]> PreviewMembershipApplicationAsync(
        MitgliedsantragDokumentRequest request,
        decimal admissionFee,
        CancellationToken cancellationToken = default);

    Task<byte[]> PreviewLeaseContractAsync(PachtvertragDokumentRequest request, CancellationToken cancellationToken = default);

    Task<ContractFinalizeResult> FinalizeMembershipApplicationAsync(MembershipApplicationFinalizeRequest request, CancellationToken cancellationToken = default);

    Task<ContractFinalizeResult> FinalizeLeaseContractAsync(
        LeaseContractFinalizeRequest request,
        CancellationToken cancellationToken = default);
}
