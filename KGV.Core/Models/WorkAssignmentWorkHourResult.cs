namespace KGV.Core.Models;

public sealed class WorkAssignmentWorkHourResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public ArbeitsstundeRecord? WorkHour { get; init; }
}
