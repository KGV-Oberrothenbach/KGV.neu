using System.Collections.Generic;

namespace KGV.ReleaseManager.Models;

public sealed class VersionWriteResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string AndroidVersionCode { get; set; } = string.Empty;
    public IReadOnlyList<VersionFileBackup> Backups { get; set; } = new List<VersionFileBackup>();
    public IReadOnlyList<string> UpdatedFiles { get; set; } = new List<string>();
}
