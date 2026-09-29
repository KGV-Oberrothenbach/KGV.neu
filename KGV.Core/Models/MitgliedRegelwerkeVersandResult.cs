using System;

namespace KGV.Core.Models;

/// <summary>
/// Ergebnis des nachweisbaren Versands von Satzung, Kleingartenordnung und Beitragsordnung.
/// Der Zeitpunkt wird nur gesetzt, nachdem der Maildienst die Nachricht angenommen hat.
/// </summary>
public sealed class MitgliedRegelwerkeVersandResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string DiagnosticCode { get; init; } = string.Empty;
    public DateTime? VersandtAm { get; init; }

    public static MitgliedRegelwerkeVersandResult Ok(DateTime versandtAm, string? message = null)
        => new()
        {
            Success = true,
            VersandtAm = versandtAm,
            DiagnosticCode = "REGELWERKE_VERSAND_OK",
            Message = string.IsNullOrWhiteSpace(message)
                ? "Satzung, Kleingartenordnung und Beitragsordnung wurden per E-Mail versandt."
                : message.Trim()
        };

    public static MitgliedRegelwerkeVersandResult Fail(string? message, string? diagnosticCode = null)
        => new()
        {
            Success = false,
            DiagnosticCode = string.IsNullOrWhiteSpace(diagnosticCode) ? "REGELWERKE_VERSAND_FEHLER" : diagnosticCode.Trim(),
            Message = string.IsNullOrWhiteSpace(message)
                ? "Der Versand der Vereinsregelwerke konnte nicht bestätigt werden."
                : message.Trim()
        };
}
