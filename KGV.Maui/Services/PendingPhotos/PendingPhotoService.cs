using KGV.Maui.Models;

namespace KGV.Maui.Services.PendingPhotos;

public sealed class PendingPhotoService
{
    private readonly PendingPhotoQueue _queue;

    public PendingPhotoService(PendingPhotoQueue queue)
    {
        _queue = queue;
    }

    public PendingPhotoUpload SaveAndEnqueue(
        byte[] content,
        string operationType,
        string parzelle,
        string medium,
        string contentType,
        DateTimeOffset? now = null)
    {
        if (content is not { Length: > 0 })
            throw new ArgumentException("Foto-Inhalt fehlt.", nameof(content));

        var id = Guid.NewGuid();
        var fileName = PendingPhotoFileNameFactory.Create(operationType, parzelle, medium, now);
        var filePath = PendingPhotoStorage.GetPendingFilePath($"{id:N}_{fileName}");

        File.WriteAllBytes(filePath, content);

        var item = new PendingPhotoUpload
        {
            Id = id,
            OperationType = operationType,
            Parzelle = parzelle,
            Medium = medium,
            LocalFilePath = filePath,
            FileName = Path.GetFileName(filePath),
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim(),
            Status = PendingPhotoUploadStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        _queue.Enqueue(item);
        return item;
    }

    public bool TryLoadContent(PendingPhotoUpload item, out byte[]? content)
    {
        content = null;

        try
        {
            if (item == null || string.IsNullOrWhiteSpace(item.LocalFilePath) || !File.Exists(item.LocalFilePath))
                return false;

            content = File.ReadAllBytes(item.LocalFilePath);
            return content is { Length: > 0 };
        }
        catch
        {
            return false;
        }
    }

    public void MarkFailed(PendingPhotoUpload item, string? error)
    {
        item.Status = PendingPhotoUploadStatus.Failed;
        item.AttemptCount += 1;
        item.LastAttemptAtUtc = DateTime.UtcNow;
        item.LastError = string.IsNullOrWhiteSpace(error) ? null : error.Trim();
        _queue.Update(item);
    }

    public void MarkUploadedAndDeleteLocal(PendingPhotoUpload item)
    {
        item.Status = PendingPhotoUploadStatus.Uploaded;
        item.LastAttemptAtUtc = DateTime.UtcNow;
        item.LastError = null;
        _queue.Update(item);

        try
        {
            if (!string.IsNullOrWhiteSpace(item.LocalFilePath) && File.Exists(item.LocalFilePath))
                File.Delete(item.LocalFilePath);
        }
        catch
        {
        }

        _queue.Remove(item.Id);
    }
}
