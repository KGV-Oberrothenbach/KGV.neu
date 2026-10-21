namespace KGV.Core.Models;

public sealed class WartungsvertragAssignmentSaveResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public long? ZuordnungId { get; init; }
    public string? DokumentStatus { get; init; }
    public string? DokumentMessage { get; init; }
    public int RequestedCount { get; init; }
    public int AddedCount { get; init; }
    public int RemainingFreeSlots { get; init; }
    public List<WartungsvertragAssignmentSaveItem> Items { get; init; } = new();
    public int FailedCount => Items.Count(x => !x.Success);
}

public sealed record WartungsvertragAssignmentSaveItem
{
    public int MitgliedId { get; init; }
    public string MitgliedName { get; init; } = string.Empty;
    public long WartungsvertragId { get; init; }
    public string WartungsvertragTitel { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public long? ZuordnungId { get; init; }
    public string? DokumentStatus { get; init; }
    public string? DokumentMessage { get; init; }
    public string AnzeigeText => string.IsNullOrWhiteSpace(DokumentMessage)
        ? $"{MitgliedName} – {WartungsvertragTitel}: {Message}"
        : $"{MitgliedName} – {WartungsvertragTitel}: {Message} {DokumentMessage}";
}
