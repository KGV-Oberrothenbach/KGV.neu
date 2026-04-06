namespace KGV.Core.Models;

public sealed class MembershipEndResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public MitgliedRecord? UpdatedMainMember { get; init; }
    public MitgliedRecord? UpdatedSecondaryMember { get; init; }
    public MembershipEndDecision? AppliedDecision { get; init; }

    public static MembershipEndResult Failure(string message)
        => new() { Success = false, Message = message };

    public static MembershipEndResult SuccessResult(
        string message,
        MitgliedRecord? updatedMainMember,
        MitgliedRecord? updatedSecondaryMember,
        MembershipEndDecision? appliedDecision)
        => new()
        {
            Success = true,
            Message = message,
            UpdatedMainMember = updatedMainMember,
            UpdatedSecondaryMember = updatedSecondaryMember,
            AppliedDecision = appliedDecision
        };
}
