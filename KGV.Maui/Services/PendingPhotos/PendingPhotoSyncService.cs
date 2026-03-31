using System.Collections.Concurrent;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Maui.Models;

namespace KGV.Maui.Services.PendingPhotos;

public sealed class PendingPhotoSyncService
{
    private static readonly ConcurrentDictionary<Guid, byte> InFlight = new();

    private readonly PendingPhotoQueue _queue;
    private readonly PendingPhotoService _pendingPhotoService;
    private readonly IPhotoUploadTestService _uploadService;

    public PendingPhotoSyncService(
        PendingPhotoQueue queue,
        PendingPhotoService pendingPhotoService,
        IPhotoUploadTestService uploadService)
    {
        _queue = queue;
        _pendingPhotoService = pendingPhotoService;
        _uploadService = uploadService;
    }

    public async Task<PendingPhotoSyncResult> TrySyncOnceAsync(CancellationToken cancellationToken = default)
    {
        var result = new PendingPhotoSyncResult();

        if (!PendingPhotoUploadDecision.CanUploadNow(out var reason))
        {
            result.SkippedReason = reason;
            return result;
        }

        var items = _queue
            .GetAll()
            .Where(x => x.Status is PendingPhotoUploadStatus.Pending or PendingPhotoUploadStatus.Failed)
            .OrderBy(x => x.CreatedAtUtc)
            .ToList();

        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            result.TotalConsidered++;

            if (!InFlight.TryAdd(item.Id, 0))
            {
                result.AlreadyInFlight++;
                continue;
            }

            try
            {
                if (!_queue.TryGet(item.Id, out var latest) || latest == null)
                {
                    result.MissingFromQueue++;
                    continue;
                }

                if (latest.Status == PendingPhotoUploadStatus.Uploading)
                {
                    result.AlreadyInFlight++;
                    continue;
                }

                if (!_pendingPhotoService.TryLoadContent(latest, out var content) || content is not { Length: > 0 })
                {
                    _pendingPhotoService.MarkFailed(latest, "Lokale Foto-Datei fehlt.");
                    result.FileMissing++;
                    continue;
                }

                latest.Status = PendingPhotoUploadStatus.Uploading;
                latest.LastAttemptAtUtc = DateTime.UtcNow;
                _queue.Update(latest);

                PhotoUploadTestResult uploadResult;
                try
                {
                    uploadResult = await _uploadService.UploadAsync(new PhotoUploadTestRequest
                    {
                        FileName = latest.FileName,
                        ContentType = latest.ContentType,
                        FileContent = content,
                        Kind = latest.OperationType,
                        Medium = latest.Medium,
                        Anlage = string.Empty,
                        Garten = latest.Parzelle,
                        Datum = DateTime.Today
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PendingPhotoSync] UploadAsync failed for {latest.Id}: {ex}");
                    _pendingPhotoService.MarkFailed(latest, "Upload fehlgeschlagen. Erneuter Versuch möglich.");
                    result.Failed++;
                    continue;
                }

                if (uploadResult.Success)
                {
                    _pendingPhotoService.MarkUploadedAndDeleteLocal(latest);
                    result.Uploaded++;
                }
                else
                {
                    var error = string.IsNullOrWhiteSpace(uploadResult.RequestId)
                        ? uploadResult.ErrorSummary
                        : $"{uploadResult.ErrorSummary} (Support-ID: {uploadResult.RequestId})";

                    _pendingPhotoService.MarkFailed(latest, error);
                    result.Failed++;
                }
            }
            finally
            {
                InFlight.TryRemove(item.Id, out _);
            }
        }

        return result;
    }
}

public sealed class PendingPhotoSyncResult
{
    public string SkippedReason { get; set; } = string.Empty;
    public int TotalConsidered { get; set; }
    public int Uploaded { get; set; }
    public int Failed { get; set; }
    public int AlreadyInFlight { get; set; }
    public int MissingFromQueue { get; set; }
    public int FileMissing { get; set; }
}
