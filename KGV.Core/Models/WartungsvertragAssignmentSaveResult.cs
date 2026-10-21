namespace KGV.Core.Models;

public sealed class WartungsvertragAssignmentSaveResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int RequestedCount { get; init; }
    public int AddedCount { get; init; }
    public int RemainingFreeSlots { get; init; }
    public List<WartungsvertragAssignmentSaveItem> Items { get; init; } = new();
    public int FailedCount => Items.Count(x => !x.Success);
}

public sealed class WartungsvertragAssignmentSaveItem
{
    public int MitgliedId { get; init; }
    public string MitgliedName { get; init; } = string.Empty;
    public long WartungsvertragId { get; init; }
    public string WartungsvertragTitel { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string AnzeigeText => $"{MitgliedName} – {WartungsvertragTitel}: {Message}";
}
