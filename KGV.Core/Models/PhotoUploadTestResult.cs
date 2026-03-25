namespace KGV.Core.Models
{
    public sealed class PhotoUploadTestResult
    {
        public bool Success { get; set; }
        public int? HttpStatusCode { get; set; }
        public string HttpStatusText { get; set; } = string.Empty;
        public string RawResponseBody { get; set; } = string.Empty;
        public string ExceptionMessage { get; set; } = string.Empty;
        public string FileId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public string ErrorSummary => !string.IsNullOrWhiteSpace(ExceptionMessage)
            ? ExceptionMessage
            : HttpStatusCode.HasValue
                ? $"HTTP {(int)HttpStatusCode.Value} {HttpStatusText}".Trim()
                : string.Empty;
    }
}
