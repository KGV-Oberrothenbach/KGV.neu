using System.Collections.Generic;

namespace KGV.ReleaseManager.Models;

public sealed class ReleaseExecutionResult
{
    public bool Success { get; set; }
    public bool RolledBack { get; set; }
    public bool PreventedByPreflight { get; set; }
    public bool MarkerWritten { get; set; }
    public bool CommitExecuted { get; set; }
    public bool PushExecuted { get; set; }
    public ReleaseExecutionOverallState OverallState { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ReleaseFolderPath { get; set; } = string.Empty;
    public IReadOnlyList<string> Messages { get; set; } = new List<string>();
    public IReadOnlyList<string> ArtifactPaths { get; set; } = new List<string>();
    public IReadOnlyList<ReleaseExecutionStepResult> Steps { get; set; } = new List<ReleaseExecutionStepResult>();

    public string OverallStateText => OverallState switch
    {
        ReleaseExecutionOverallState.Successful => "erfolgreich",
        ReleaseExecutionOverallState.FailedRollbackSuccessful => "fehlgeschlagen, rollback erfolgreich",
        ReleaseExecutionOverallState.FailedRollbackIncomplete => "fehlgeschlagen, rollback unvollständig",
        _ => "fehlgeschlagen"
    };
}
