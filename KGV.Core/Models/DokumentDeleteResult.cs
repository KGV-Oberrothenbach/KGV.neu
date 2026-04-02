namespace KGV.Core.Models
{
    public sealed class DokumentDeleteResult
    {
        public bool Success { get; private init; }
        public string Message { get; private init; } = string.Empty;
        public string DiagnosticCode { get; private init; } = string.Empty;
        public string RequestId { get; private init; } = string.Empty;

        public static DokumentDeleteResult Ok(string? requestId = null)
            => new()
            {
                Success = true,
                RequestId = requestId?.Trim() ?? string.Empty,
                Message = "Dokument wurde entfernt."
            };

        public static DokumentDeleteResult Fail(string message, string diagnosticCode, string? requestId = null)
            => new()
            {
                Success = false,
                Message = message,
                DiagnosticCode = diagnosticCode,
                RequestId = requestId?.Trim() ?? string.Empty
            };
    }
}
