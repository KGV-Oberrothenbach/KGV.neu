namespace KGV.ReleaseManager.Models;

public enum ReleaseExecutionOverallState
{
    Successful,
    Failed,
    FailedRollbackSuccessful,
    FailedRollbackIncomplete
}
