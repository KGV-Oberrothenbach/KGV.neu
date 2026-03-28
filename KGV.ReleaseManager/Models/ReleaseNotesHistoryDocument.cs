namespace KGV.ReleaseManager.Models;

public sealed class ReleaseNotesHistoryDocument
{
    public List<ReleaseNotesHistoryEntry> Entries { get; set; } = new();
}
