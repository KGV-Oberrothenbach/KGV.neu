namespace KGV.Core.Models;

public enum PreparedMembershipApplicationResumeDecision
{
    New,
    ResumeOwn,
    ResumeClaimed,
    ClaimConflict,
    ForeignAssigned,
    SignatureLocked,
    InvalidStatus
}

public sealed record PreparedMembershipApplicationResumeResult(
    PreparedMembershipApplicationResumeDecision Decision,
    KommunikationsvorgangDto? Contract);
