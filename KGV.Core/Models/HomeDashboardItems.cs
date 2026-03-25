namespace KGV.Core.Models;

public sealed class HomeWorkHoursSummary
{
    public int Year { get; init; }
    public decimal? RequiredHours { get; init; }
    public decimal? WorkedHours { get; init; }
    public decimal? OpenHours { get; init; }
    public bool HasMaintenanceContract { get; init; }
    public bool IsAgeExempt { get; init; }
    public bool IsExempt { get; init; }
    public string RuleReason { get; init; } = string.Empty;
}

public sealed class HomeWorkAssignmentItem
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;
    public string StartTimeText { get; init; } = string.Empty;
    public string EndTimeText { get; init; } = string.Empty;
    public string TimeText => BuildTimeText(StartTimeText, EndTimeText);
    public bool HasTimeText => !string.IsNullOrWhiteSpace(TimeText);
    public string Details { get; init; } = string.Empty;
    public string DetailInfo { get; init; } = string.Empty;
    public string RegistrationInfo { get; init; } = string.Empty;
    public bool HasRegistrationInfo => !string.IsNullOrWhiteSpace(RegistrationInfo);
    public bool CanRegister { get; init; }
    public bool CanSignOff { get; init; }

    private static string BuildTimeText(string? start, string? end)
    {
        var hasStart = !string.IsNullOrWhiteSpace(start);
        var hasEnd = !string.IsNullOrWhiteSpace(end);

        if (!hasStart && !hasEnd)
            return string.Empty;
        if (!hasEnd)
            return start ?? string.Empty;
        if (!hasStart)
            return end ?? string.Empty;

        return $"{start} - {end}";
    }
}

public sealed class HomeAppointmentItem
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;
    public string StartTimeText { get; init; } = string.Empty;
    public string EndTimeText { get; init; } = string.Empty;
    public string TimeText => BuildTimeText(StartTimeText, EndTimeText);
    public bool HasTimeText => !string.IsNullOrWhiteSpace(TimeText);
    public string Details { get; init; } = string.Empty;
    public string DetailInfo { get; init; } = string.Empty;

    private static string BuildTimeText(string? start, string? end)
    {
        var hasStart = !string.IsNullOrWhiteSpace(start);
        var hasEnd = !string.IsNullOrWhiteSpace(end);

        if (!hasStart && !hasEnd)
            return string.Empty;
        if (!hasEnd)
            return start ?? string.Empty;
        if (!hasStart)
            return end ?? string.Empty;

        return $"{start} - {end}";
    }
}
