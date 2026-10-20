namespace KGV.Core.Models;

public sealed record LeaseContractCompletionResult(long? PachtverhaeltnisId, long? PachtvertragsfassungId, bool BereitsAbgeschlossen);
