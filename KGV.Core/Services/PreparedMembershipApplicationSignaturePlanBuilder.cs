using KGV.Core.Models;

namespace KGV.Core.Services;

/// <summary>Defines the complete, ordered signature plan for a membership application.</summary>
public static class PreparedMembershipApplicationSignaturePlanBuilder
{
    public static IReadOnlyList<SignatureStep> Build(PreparedMembershipApplicationDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);
        if (draft.MemberId <= 0)
            throw new PreparedContractValidationException("Für den Signaturplan fehlt das Mitglied.");
        if (draft.Representative is not null && draft.IncludeSecondaryMember)
            throw new PreparedContractValidationException("Ein minderjähriges Mitglied kann kein Nebenmitglied im Mitgliedsantrag haben.");

        var steps = new List<SignatureStep>();
        Add(steps, SignaturePersonTypes.Member, draft.MemberId, SignaturePurposes.MembershipApplication, "Mitglied – Mitgliedsantrag", MembershipApplicationSignatureKeys.MemberApplication);
        Add(steps, SignaturePersonTypes.Member, draft.MemberId, SignaturePurposes.DataProtection, "Mitglied – Datenschutz", MembershipApplicationSignatureKeys.MemberPrivacy);

        if (draft.Representative is not null)
        {
            var representativeId = draft.Representative.Mode == "existing" ? draft.Representative.MemberId : null;
            Add(steps, SignaturePersonTypes.LegalRepresentative, representativeId, SignaturePurposes.MembershipApplication, "Gesetzlicher Vertreter – Mitgliedsantrag", MembershipApplicationSignatureKeys.RepresentativeApplication);
            Add(steps, SignaturePersonTypes.LegalRepresentative, representativeId, SignaturePurposes.DataProtection, "Gesetzlicher Vertreter – Datenschutz", MembershipApplicationSignatureKeys.RepresentativePrivacy);
        }
        else if (draft.IncludeSecondaryMember)
        {
            Add(steps, SignaturePersonTypes.SecondaryMember, null, SignaturePurposes.MembershipApplication, "Nebenmitglied – Mitgliedsantrag", MembershipApplicationSignatureKeys.SecondaryApplication);
            Add(steps, SignaturePersonTypes.SecondaryMember, null, SignaturePurposes.DataProtection, "Nebenmitglied – Datenschutz", MembershipApplicationSignatureKeys.SecondaryPrivacy);
        }

        Add(steps, SignaturePersonTypes.Board, null, SignaturePurposes.Approval, "Vorstand – Freigabe", MembershipApplicationSignatureKeys.BoardApproval);
        return steps;
    }

    private static void Add(List<SignatureStep> steps, string personType, int? personId, string purpose, string displayName, string key)
        => steps.Add(new SignatureStep(steps.Count + 1, personType, personId, purpose, displayName, key));
}
