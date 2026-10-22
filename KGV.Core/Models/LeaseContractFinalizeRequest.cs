namespace KGV.Core.Models;

public sealed class LeaseContractFinalizeRequest
{
    public required PachtvertragDokumentRequest Contract { get; init; }
    /// <summary>Persistent PV-v4 workflow identity; when set, signatures are resolved server-side.</summary>
    public long KommunikationsvorgangId { get; init; }
    public int SourceRevision { get; init; }
    public string? MemberSignature { get; init; }
    public string? SecondarySignature { get; init; }
    public string? BoardSignature { get; init; }
}
