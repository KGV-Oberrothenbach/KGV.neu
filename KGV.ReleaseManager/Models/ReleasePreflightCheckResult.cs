namespace KGV.ReleaseManager.Models;

public sealed class ReleasePreflightCheckResult
{
    public ReleasePreflightCheckState State { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;

    public string StateText => State switch
    {
        ReleasePreflightCheckState.Ok => "OK",
        ReleasePreflightCheckState.Warning => "Warnung",
        _ => "Fehler"
    };
}
