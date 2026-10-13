using System.Text.Json;
using KGV.Maui.Models;

namespace KGV.Maui.Services.PendingPhotos;

public sealed class PendingPhotoQueue
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly object _sync = new();
    private readonly string _queueFilePath;

    public PendingPhotoQueue()
    {
        _queueFilePath = Path.Combine(PendingPhotoStorage.GetPendingRootDirectory(), "pending-photo-queue.json");
    }

    public IReadOnlyList<PendingPhotoUpload> GetAll()
    {
        lock (_sync)
        {
            return LoadInternal();
        }
    }

    public IReadOnlyList<PendingPhotoUpload> GetForOwner(Guid vereinId, Guid ownerUserId)
    {
        lock (_sync)
        {
            return LoadInternal().Where(x => x.VereinId == vereinId && x.OwnerUserId == ownerUserId).ToList();
        }
    }

    public IReadOnlyList<PendingPhotoUpload> GetOpenForOwner(Guid vereinId, Guid ownerUserId)
        => GetForOwner(vereinId, ownerUserId)
            .Where(x => x.Status is PendingPhotoUploadStatus.Pending or PendingPhotoUploadStatus.Failed or PendingPhotoUploadStatus.Uploading)
            .ToList();

    public int CountOpenForOwner(Guid vereinId, Guid ownerUserId)
        => GetOpenForOwner(vereinId, ownerUserId).Count;

    public int CountLegacyForVerein(Guid vereinId)
    {
        lock (_sync)
        {
            return LoadInternal().Count(x => x.VereinId == vereinId && x.OwnerUserId == null);
        }
    }

    public PendingPhotoUpload Enqueue(PendingPhotoUpload item)
    {
        lock (_sync)
        {
            var items = LoadInternal();
            items.Add(item);
            SaveInternal(items);
            return item;
        }
    }

    public bool TryGet(Guid id, out PendingPhotoUpload? item)
    {
        lock (_sync)
        {
            var items = LoadInternal();
            item = items.FirstOrDefault(x => x.Id == id);
            return item != null;
        }
    }

    public void Update(PendingPhotoUpload item)
    {
        lock (_sync)
        {
            var items = LoadInternal();
            var index = items.FindIndex(x => x.Id == item.Id);
            if (index < 0)
            {
                items.Add(item);
            }
            else
            {
                items[index] = item;
            }

            SaveInternal(items);
        }
    }

    public bool Remove(Guid id)
    {
        lock (_sync)
        {
            var items = LoadInternal();
            var removed = items.RemoveAll(x => x.Id == id) > 0;
            if (removed)
            {
                SaveInternal(items);
            }

            return removed;
        }
    }

    public bool TryRemoveForOwner(Guid id, Guid vereinId, Guid ownerUserId, out PendingPhotoUpload? removedItem)
    {
        lock (_sync)
        {
            var items = LoadInternal();
            var index = items.FindIndex(x => x.Id == id && x.VereinId == vereinId && x.OwnerUserId == ownerUserId);
            if (index < 0)
            {
                removedItem = null;
                return false;
            }

            removedItem = items[index];
            items.RemoveAt(index);
            SaveInternal(items);
            return true;
        }
    }

    public IReadOnlyList<PendingPhotoUpload> RemoveLegacyForVerein(Guid vereinId)
    {
        lock (_sync)
        {
            var items = LoadInternal();
            var removed = items.Where(x => x.VereinId == vereinId && x.OwnerUserId == null).ToList();
            if (removed.Count > 0)
            {
                items.RemoveAll(x => x.VereinId == vereinId && x.OwnerUserId == null);
                SaveInternal(items);
            }

            return removed;
        }
    }

    private List<PendingPhotoUpload> LoadInternal()
    {
        try
        {
            if (!File.Exists(_queueFilePath))
                return new List<PendingPhotoUpload>();

            var json = File.ReadAllText(_queueFilePath);
            if (string.IsNullOrWhiteSpace(json))
                return new List<PendingPhotoUpload>();

            var items = JsonSerializer.Deserialize<List<PendingPhotoUpload>>(json, JsonOptions);
            return items ?? new List<PendingPhotoUpload>();
        }
        catch
        {
            return new List<PendingPhotoUpload>();
        }
    }

    private void SaveInternal(List<PendingPhotoUpload> items)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_queueFilePath)!);
        var json = JsonSerializer.Serialize(items, JsonOptions);
        File.WriteAllText(_queueFilePath, json);
    }
}
