namespace KGV.Maui.Models;

public enum PendingPhotoUploadStatus
{
    Pending = 0,
    Uploading = 1,
    Uploaded = 2,
    Failed = 3,
    Deleted = 4
}

public sealed class PendingPhotoUpload
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string OperationType { get; init; } = string.Empty;
    public string Parzelle { get; init; } = string.Empty;
    public string Medium { get; init; } = string.Empty;

    public string LocalFilePath { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = "application/octet-stream";

    public PendingPhotoUploadStatus Status { get; set; } = PendingPhotoUploadStatus.Pending;

    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    public DateTime? LastAttemptAtUtc { get; set; }
    public int AttemptCount { get; set; }

    public string? LastError { get; set; }
}
