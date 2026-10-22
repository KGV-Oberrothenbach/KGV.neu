namespace KGV.Core.Models;

/// <summary>One ordered signature required by a prepared document.</summary>
public sealed record SignatureStep(
    int Sequence,
    string PersonType,
    int? PersonId,
    string Purpose,
    string DisplayName,
    string Key);

public static class SignaturePersonTypes
{
    public const string Member = "mitglied";
    public const string SecondaryMember = "nebenmitglied";
    public const string LegalRepresentative = "gesetzlicher_vertreter";
    public const string Board = "vorstand";
}

public static class SignaturePurposes
{
    public const string MembershipApplication = "mitgliedsantrag";
    public const string DataProtection = "datenschutz";
    public const string Approval = "freigabe";
}

public static class MembershipApplicationSignatureKeys
{
    public const string MemberApplication = "member.application";
    public const string MemberPrivacy = "member.privacy";
    public const string SecondaryApplication = "nebenmitglied.antrag";
    public const string SecondaryPrivacy = "nebenmitglied.datenschutz";
    public const string RepresentativeApplication = "gesetzliche_vertretung.antrag";
    public const string RepresentativePrivacy = "gesetzliche_vertretung.datenschutz";
    public const string BoardApproval = "board.approval";
}

public static class LeaseContractSignatureKeys
{
    public const string Member = "lease.member";
    public const string SecondParty = "lease.second_party";
    public const string Board = "lease.board";
}
