namespace KGV.Core.Models;

public sealed class MembershipApplicationFinalizeRequest
{
    public required MitgliedsantragDokumentRequest Application { get; init; }
    public long SourceKommunikationsvorgangId { get; init; }
    public int SourceRevision { get; init; }
    public decimal AdmissionFee { get; init; }
    public string? ApplicationMemberSignature { get; init; }
    public string? PrivacyMemberSignature { get; init; }
    public string? ApplicationRepresentativeSignature { get; init; }
    public string? PrivacyRepresentativeSignature { get; init; }
    public string? ApplicationSecondarySignature { get; init; }
    public string? PrivacySecondarySignature { get; init; }
    public string? BoardSignature { get; init; }
}

public sealed record ContractFinalizeResult(int? DocumentId, string? Message);
