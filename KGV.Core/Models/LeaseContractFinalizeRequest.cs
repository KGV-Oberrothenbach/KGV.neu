namespace KGV.Core.Models;

public sealed class LeaseContractFinalizeRequest
{
    public required PachtvertragDokumentRequest Contract { get; init; }
    public required string MemberSignature { get; init; }
    public string? SecondarySignature { get; init; }
    public required string BoardSignature { get; init; }
}
