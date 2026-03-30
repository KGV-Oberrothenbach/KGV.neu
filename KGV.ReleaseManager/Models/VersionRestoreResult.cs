namespace KGV.ReleaseManager.Models;

public sealed class VersionRestoreResult
{
    public bool Success { get; init; }
    public IReadOnlyList<string> Messages { get; init; } = Array.Empty<string>();
}
