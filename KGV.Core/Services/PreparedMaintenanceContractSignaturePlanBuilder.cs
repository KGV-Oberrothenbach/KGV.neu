using KGV.Core.Models;

namespace KGV.Core.Services;

/// <summary>The regular maintenance contract has exactly one private party: its assignment's principal member.</summary>
public static class PreparedMaintenanceContractSignaturePlanBuilder
{
    public const string Member = "mitglied";

    public static IReadOnlyList<PreparedContractSignatureSlot> BuildPersistent(PreparedMaintenanceContractDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);
        if (draft.MemberId <= 0 || draft.WartungsvertragZuordnungId <= 0 || draft.WartungsvertragId <= 0)
            throw new PreparedContractValidationException("Die Wartungsvertragszuordnung ist für den Signaturplan unvollständig.");
        return [
            new(Member, 10, true, SignaturePersonTypes.Member, "Mitglied / Vertragspartner", draft.MemberId),
            new(PreparedContractBoardSignatureKeys.Board1, 90, true, "vereinsunterzeichner", "Vorstand 1"),
            new(PreparedContractBoardSignatureKeys.Board2, 100, true, "vereinsunterzeichner", "Vorstand 2")
        ];
    }
}
