using System.ComponentModel;
using KGV.Maui.Models;
using KGV.Core.Interfaces;
using KGV.Maui.State;

namespace KGV.Maui.Services.PendingPhotos;

public sealed class PendingPhotoMenuState : INotifyPropertyChanged
{
    private readonly PendingPhotoQueue _queue;
    private readonly IVereinskontext _vereinskontext;
    private readonly UserContextState _userContextState;
    private int _openCount;

    public PendingPhotoMenuState(PendingPhotoQueue queue, IVereinskontext vereinskontext, UserContextState userContextState)
    {
        _queue = queue;
        _vereinskontext = vereinskontext;
        _userContextState = userContextState;
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
        var vereinId = _vereinskontext.Aktuell?.VereinId;
        var ownerUserId = _userContextState.CurrentUserId;
        OpenCount = vereinId.HasValue && ownerUserId.HasValue
            ? _queue.CountOpenForOwner(vereinId.Value, ownerUserId.Value)
            : 0;
    }
}
