namespace KGV.Core.Models;

public enum PreparedLeaseContractResumeDecision { New, ResumeOwn, ResumeClaimed, ClaimConflict, ForeignAssigned, SignatureLocked, InvalidStatus }
public sealed record PreparedLeaseContractResumeResult(PreparedLeaseContractResumeDecision Decision, KommunikationsvorgangDto? Contract);
