namespace KGV.Core.Models;

public sealed class WartungsvertragAssignmentSaveResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int RequestedCount { get; init; }
    public int AddedCount { get; init; }
    public int RemainingFreeSlots { get; init; }
}
