using KGV.Core.Models;

namespace KGV.Core.Services;

public static class PreparedLeaseContractSignaturePlanBuilder
{
    /// <summary>Persistent slots for the new workflow; Build remains the legacy capture plan until the UI is migrated.</summary>
    public static IReadOnlyList<PreparedContractSignatureSlot> BuildPersistent(PreparedLeaseContractDraft draft, bool isMinor, bool hasLegalRepresentative)
    {
        ArgumentNullException.ThrowIfNull(draft);
        if (draft.MemberId <= 0) throw new PreparedContractValidationException("Für den Signaturplan fehlt das Mitglied.");
        if (isMinor && !hasLegalRepresentative) throw new PreparedContractValidationException("Für den minderjährigen Pächter fehlt ein aktiver gesetzlicher Vertreter.");
        var slots = new List<PreparedContractSignatureSlot>
        {
            new(LeaseContractSignatureKeys.Member, 10, true, SignaturePersonTypes.Member, "Pächter/in 1", draft.MemberId)
        };
        if (isMinor) slots.Add(new(LeaseContractSignatureKeys.SecondParty, 20, true, SignaturePersonTypes.LegalRepresentative, "Gesetzliche/r Vertreter/in"));
        else if (draft.Draft.IncludeSecondaryMember) slots.Add(new(LeaseContractSignatureKeys.SecondParty, 20, true, SignaturePersonTypes.SecondaryMember, "Pächter/in 2", draft.Draft.SecondaryMemberId));
        slots.Add(new(PreparedContractBoardSignatureKeys.Board1, 90, true, "vereinsunterzeichner", "Vorstand 1"));
        slots.Add(new(PreparedContractBoardSignatureKeys.Board2, 100, true, "vereinsunterzeichner", "Vorstand 2"));
        return slots;
    }

    public static IReadOnlyList<SignatureStep> Build(PreparedLeaseContractDraft draft, bool isMinor, bool hasLegalRepresentative)
    {
        ArgumentNullException.ThrowIfNull(draft);
        if (draft.MemberId <= 0) throw new PreparedContractValidationException("Für den Signaturplan fehlt das Mitglied.");
        if (isMinor && !hasLegalRepresentative) throw new PreparedContractValidationException("Für den minderjährigen Pächter fehlt ein aktiver gesetzlicher Vertreter.");
        var steps = new List<SignatureStep> { new(1, SignaturePersonTypes.Member, draft.MemberId, PreparedContractConstants.LeaseContract, "Pächter/in 1", LeaseContractSignatureKeys.Member) };
        if (isMinor) steps.Add(new(steps.Count + 1, SignaturePersonTypes.LegalRepresentative, null, PreparedContractConstants.LeaseContract, "Gesetzliche/r Vertreter/in", LeaseContractSignatureKeys.SecondParty));
        else if (draft.Draft.IncludeSecondaryMember) steps.Add(new(steps.Count + 1, SignaturePersonTypes.SecondaryMember, draft.Draft.SecondaryMemberId, PreparedContractConstants.LeaseContract, "Pächter/in 2", LeaseContractSignatureKeys.SecondParty));
        steps.Add(new(steps.Count + 1, SignaturePersonTypes.Board, null, SignaturePurposes.Approval, "Vorstand / Verpächter", LeaseContractSignatureKeys.Board));
        return steps;
    }
}
