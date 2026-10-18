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
    public const string SecondaryApplication = "secondary.application";
    public const string SecondaryPrivacy = "secondary.privacy";
    public const string RepresentativeApplication = "representative.application";
    public const string RepresentativePrivacy = "representative.privacy";
    public const string BoardApproval = "board.approval";
}
