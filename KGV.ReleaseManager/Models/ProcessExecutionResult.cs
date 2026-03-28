using System;
using System.Collections.Generic;
using System.Linq;

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
        => GetUserFacingMessage(Array.Empty<string>());

    public string GetUserFacingMessage(params string[] sensitiveValues)
    {
        if (!string.IsNullOrWhiteSpace(ExceptionMessage))
        {
            return $"{StepName} konnte nicht gestartet werden: {Redact(ExceptionMessage, sensitiveValues)}";
        }

        if (Success)
        {
            return $"{StepName} erfolgreich abgeschlossen.";
        }

        var detail = string.IsNullOrWhiteSpace(StandardError)
            ? StandardOutput
            : StandardError;

        detail = string.IsNullOrWhiteSpace(detail) ? "Keine weitere Prozessausgabe verfügbar." : detail.Trim();
        return $"{StepName} fehlgeschlagen (ExitCode {ExitCode}). {Redact(detail, sensitiveValues)}";
    }

    private static string Redact(string text, IReadOnlyList<string> sensitiveValues)
    {
        var sanitized = text;
        foreach (var sensitiveValue in sensitiveValues.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal))
        {
            sanitized = sanitized.Replace(sensitiveValue, "***", StringComparison.Ordinal);
        }

        return sanitized;
    }
}
