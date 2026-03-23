namespace KGV.Core.Models;

public sealed class WorkAssignmentParticipantItem
{
    public int MitgliedId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string StatusText { get; init; } = string.Empty;
}
