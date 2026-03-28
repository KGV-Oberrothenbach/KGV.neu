namespace KGV.ReleaseManager.Models;

public sealed class LogSourceStatus
{
    public bool IsAvailable { get; set; }
    public bool IsFallback { get; set; }
    public string Path { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
