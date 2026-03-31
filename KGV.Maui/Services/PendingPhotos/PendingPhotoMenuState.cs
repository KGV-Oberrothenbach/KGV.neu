using System.ComponentModel;
using KGV.Maui.Models;

namespace KGV.Maui.Services.PendingPhotos;

public sealed class PendingPhotoMenuState : INotifyPropertyChanged
{
    private readonly PendingPhotoQueue _queue;
    private int _openCount;

    public PendingPhotoMenuState(PendingPhotoQueue queue)
    {
        _queue = queue;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public int OpenCount
    {
        get => _openCount;
        private set
        {
            if (_openCount == value)
                return;

            _openCount = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OpenCount)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasOpenItems)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MenuTitle)));
        }
    }

    public bool HasOpenItems => OpenCount > 0;

    public string MenuTitle => $"↳ Foto-Upload ({OpenCount})";

    public void Refresh()
    {
        var items = _queue.GetAll();
        OpenCount = items.Count(x => x.Status is PendingPhotoUploadStatus.Pending or PendingPhotoUploadStatus.Failed or PendingPhotoUploadStatus.Uploading);
    }
}
