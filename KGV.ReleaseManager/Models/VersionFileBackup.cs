namespace KGV.ReleaseManager.Models;

public sealed class VersionFileBackup
{
    public string FilePath { get; set; } = string.Empty;
    public string OriginalContent { get; set; } = string.Empty;
}
