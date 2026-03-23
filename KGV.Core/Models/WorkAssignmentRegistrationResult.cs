namespace KGV.Core.Models;

public sealed class WorkAssignmentRegistrationResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public HomeWorkAssignmentItem? UpdatedItem { get; init; }
}
