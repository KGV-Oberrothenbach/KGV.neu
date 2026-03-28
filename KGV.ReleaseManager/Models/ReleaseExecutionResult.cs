using System.Collections.Generic;

namespace KGV.ReleaseManager.Models;

public sealed class ReleaseExecutionResult
{
    public bool Success { get; set; }
    public bool RolledBack { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ReleaseFolderPath { get; set; } = string.Empty;
    public IReadOnlyList<string> Messages { get; set; } = new List<string>();
    public IReadOnlyList<string> ArtifactPaths { get; set; } = new List<string>();
}
