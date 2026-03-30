namespace KGV.ReleaseManager.Models;

public sealed class ReleaseExecutionStepResult
{
    public string Name { get; set; } = string.Empty;
    public ReleaseExecutionStepState State { get; set; }
    public string Message { get; set; } = string.Empty;

    public string StateText => State switch
    {
        ReleaseExecutionStepState.Successful => "Erfolgreich",
        ReleaseExecutionStepState.Skipped => "Übersprungen",
        ReleaseExecutionStepState.Failed => "Fehlgeschlagen",
        _ => "Ausstehend"
    };
}
