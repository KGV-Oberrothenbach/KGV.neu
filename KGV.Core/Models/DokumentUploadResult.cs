namespace KGV.Core.Models
{
    public sealed class DokumentUploadResult
    {
        public bool Success { get; private init; }
        public string Message { get; private init; } = string.Empty;
        public string DiagnosticCode { get; private init; } = string.Empty;
        public string RequestId { get; private init; } = string.Empty;
        public DocumentInfo? Document { get; private init; }

        public static DokumentUploadResult Ok(DocumentInfo document, string? requestId = null)
            => new()
            {
                Success = true,
                Document = document,
                RequestId = requestId?.Trim() ?? string.Empty,
                Message = "Dokument wurde erfolgreich hochgeladen."
            };

        public static DokumentUploadResult Fail(string message, string diagnosticCode, string? requestId = null)
            => new()
            {
                Success = false,
                Message = message,
                DiagnosticCode = diagnosticCode,
                RequestId = requestId?.Trim() ?? string.Empty
            };
    }
}
