namespace KGV.Core.Models
{
    public sealed class RfidAssignmentResult
    {
        public bool Success { get; set; }
        public bool RequiresOverwriteConfirmation { get; set; }
        public string Message { get; set; } = string.Empty;
        public string NormalizedUid { get; set; } = string.Empty;
        public ParzelleRecord? UpdatedParzelle { get; set; }
    }
}
