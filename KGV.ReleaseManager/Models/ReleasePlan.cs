namespace KGV.ReleaseManager.Models;

public sealed class ReleasePlan
{
    public string CurrentVersion { get; set; } = string.Empty;
    public string NextVersion { get; set; } = string.Empty;
    public ReleaseTargetSelection Targets { get; set; } = new();
    public string ReleaseFolderPath { get; set; } = string.Empty;
    public string NotesExportPath { get; set; } = string.Empty;
}
