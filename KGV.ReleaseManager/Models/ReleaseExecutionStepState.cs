namespace KGV.ReleaseManager.Models;

public enum ReleaseExecutionStepState
{
    Pending,
    Successful,
    Reverted,
    Skipped,
    Failed
}
