namespace KGV.ReleaseManager.Models;

public sealed class ProcessExecutionResult
{
    public string StepName { get; set; } = string.Empty;
    public int ExitCode { get; set; }
    public string StandardOutput { get; set; } = string.Empty;
    public string StandardError { get; set; } = string.Empty;
    public string ExceptionMessage { get; set; } = string.Empty;

    public bool Success => ExitCode == 0 && string.IsNullOrWhiteSpace(ExceptionMessage);

    public string GetUserFacingMessage()
    {
        if (!string.IsNullOrWhiteSpace(ExceptionMessage))
        {
            return $"{StepName} konnte nicht gestartet werden: {ExceptionMessage}";
        }

        if (Success)
        {
            return $"{StepName} erfolgreich abgeschlossen.";
        }

        var detail = string.IsNullOrWhiteSpace(StandardError)
            ? StandardOutput
            : StandardError;

        detail = string.IsNullOrWhiteSpace(detail) ? "Keine weitere Prozessausgabe verfügbar." : detail.Trim();
        return $"{StepName} fehlgeschlagen (ExitCode {ExitCode}). {detail}";
    }
}
