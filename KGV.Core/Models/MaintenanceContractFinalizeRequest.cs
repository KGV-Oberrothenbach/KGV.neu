namespace KGV.Core.Models;

/// <summary>Persistent Wartungsvertrag identity. Signature images remain exclusively in prepared-contract storage.</summary>
public sealed class MaintenanceContractFinalizeRequest
{
    public long KommunikationsvorgangId { get; init; }
}
