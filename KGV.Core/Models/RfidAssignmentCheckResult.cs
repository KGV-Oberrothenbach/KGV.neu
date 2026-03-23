namespace KGV.Core.Models
{
    public sealed class RfidAssignmentCheckResult
    {
        public bool IsValid { get; set; }
        public bool RequiresOverwriteConfirmation { get; set; }
        public bool AlreadyAssignedToTarget { get; set; }
        public string Message { get; set; } = string.Empty;
        public string NormalizedUid { get; set; } = string.Empty;
        public string CurrentTargetRfid { get; set; } = string.Empty;
        public int? ConflictParzelleId { get; set; }
        public string ConflictGartenNr { get; set; } = string.Empty;
        public string ConflictAnlage { get; set; } = string.Empty;
        public string ConflictMedium { get; set; } = string.Empty;

        public string ConflictDisplayName => string.IsNullOrWhiteSpace(ConflictAnlage)
            ? ConflictGartenNr
            : $"{ConflictGartenNr} - {ConflictAnlage}";
    }
}
