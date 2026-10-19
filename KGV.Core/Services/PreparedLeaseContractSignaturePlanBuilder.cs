using KGV.Core.Models;

namespace KGV.Core.Services;

public static class PreparedLeaseContractSignaturePlanBuilder
{
    public static IReadOnlyList<SignatureStep> Build(PreparedLeaseContractDraft draft, bool isMinor, bool hasLegalRepresentative)
    {
        ArgumentNullException.ThrowIfNull(draft);
        if (draft.MemberId <= 0) throw new PreparedContractValidationException("Für den Signaturplan fehlt das Mitglied.");
        if (isMinor && !hasLegalRepresentative) throw new PreparedContractValidationException("Für den minderjährigen Pächter fehlt ein aktiver gesetzlicher Vertreter.");
        var steps = new List<SignatureStep> { new(1, SignaturePersonTypes.Member, draft.MemberId, PreparedContractConstants.LeaseContract, "Pächter/in 1", LeaseContractSignatureKeys.Member) };
        if (isMinor) steps.Add(new(steps.Count + 1, SignaturePersonTypes.LegalRepresentative, null, PreparedContractConstants.LeaseContract, "Gesetzliche/r Vertreter/in", LeaseContractSignatureKeys.SecondParty));
        else if (draft.Draft.IncludeSecondaryMember) steps.Add(new(steps.Count + 1, SignaturePersonTypes.SecondaryMember, null, PreparedContractConstants.LeaseContract, "Pächter/in 2", LeaseContractSignatureKeys.SecondParty));
        steps.Add(new(steps.Count + 1, SignaturePersonTypes.Board, null, SignaturePurposes.Approval, "Vorstand / Verpächter", LeaseContractSignatureKeys.Board));
        return steps;
    }
}
