namespace KGV.Core.Calendar;

public sealed class CalendarEventData
{
    public string Uid { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime Date { get; init; }
    public TimeSpan? StartTime { get; init; }
    public TimeSpan? EndTime { get; init; }
    public string? Location { get; init; }
}
