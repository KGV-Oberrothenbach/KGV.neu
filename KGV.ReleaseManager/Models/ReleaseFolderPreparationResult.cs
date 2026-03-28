namespace KGV.ReleaseManager.Models;

public sealed class ReleaseFolderPreparationResult
{
    public bool Success { get; set; }
    public bool ExistedBefore { get; set; }
    public string VersionFolderPath { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
