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
    public string Title { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;
    public string Details { get; init; } = string.Empty;
    public string RegistrationInfo { get; init; } = string.Empty;
    public bool CanRegister { get; init; }
}

public sealed class HomeAppointmentItem
{
    public string Title { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;
    public string Details { get; init; } = string.Empty;
}
