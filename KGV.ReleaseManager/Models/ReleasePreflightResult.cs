namespace KGV.ReleaseManager.Models;

public sealed class ReleasePreflightResult
{
    public IReadOnlyList<ReleasePreflightCheckResult> Checks { get; init; } = Array.Empty<ReleasePreflightCheckResult>();

    public bool HasErrors => Checks.Any(check => check.State == ReleasePreflightCheckState.Error);
    public bool HasWarnings => Checks.Any(check => check.State == ReleasePreflightCheckState.Warning);
    public bool CanStartRelease => !HasErrors;

    public string OverallStateText => HasErrors
        ? "nicht startbar"
        : HasWarnings
            ? "eingeschränkt"
            : "bereit";

    public string SummaryMessage => HasErrors
        ? $"Systemcheck nicht startbar: {Checks.Count(check => check.State == ReleasePreflightCheckState.Error)} Fehler in {Checks.Count} Pflichtprüfungen."
        : HasWarnings
            ? $"Systemcheck eingeschränkt: {Checks.Count(check => check.State == ReleasePreflightCheckState.Warning)} Warnungen in {Checks.Count} Pflichtprüfungen."
            : $"Systemcheck bereit: {Checks.Count} Pflichtprüfungen erfolgreich.";
}
