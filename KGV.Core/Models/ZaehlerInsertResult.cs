namespace KGV.Core.Models;

public sealed record ZaehlerInsertResult(
    bool Success,
    string UserMessage,
    string? DiagnosticCode = null,
    string? DiagnosticDetail = null)
{
    public static ZaehlerInsertResult Ok()
        => new(true, string.Empty);

    public static ZaehlerInsertResult Fail(string userMessage, string? diagnosticCode = null, string? diagnosticDetail = null)
        => new(false, userMessage, diagnosticCode, diagnosticDetail);
}
