using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Maui.State;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KGV.Maui.ViewModels;

public sealed class HomeViewModel : INotifyPropertyChanged
{
    private readonly UserContextState _userContextState;
    private HomeAnnouncementItem? _selectedAnnouncement;

    public HomeViewModel(UserContextState userContextState)
    {
        _userContextState = userContextState;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<HomeAnnouncementItem> Announcements { get; } = new();

    public string Title => "Startseite";
    public string UserContextText => $"Kontext: {UserRoles.ToStorageValue(_userContextState.CurrentUserContext?.Role ?? UserRole.User)}";
    public string AnnouncementHintText => "Bitte eine Bekanntmachung aus der Liste auswählen.";
    public string AnnouncementEmptyText => "Aktuell sind keine Bekanntmachungen vorhanden.";
    public bool HasAnnouncements => Announcements.Count > 0;
    public bool HasSelectedAnnouncement => SelectedAnnouncement != null;
    public bool ShowAnnouncementHint => HasAnnouncements && !HasSelectedAnnouncement;
    public bool ShowAnnouncementEmptyState => !HasAnnouncements;
    public bool CanEditAnnouncements => (_userContextState.CurrentUserContext?.Role ?? UserRole.User) is UserRole.Admin or UserRole.Vorstand;

    public HomeAnnouncementItem? SelectedAnnouncement
    {
        get => _selectedAnnouncement;
        set
        {
            if (_selectedAnnouncement == value)
                return;

            _selectedAnnouncement = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelectedAnnouncement));
            OnPropertyChanged(nameof(ShowAnnouncementHint));
        }
    }

    public void Initialize()
    {
        Announcements.Clear();
        SelectedAnnouncement = null;

        OnPropertyChanged(nameof(UserContextText));
        OnPropertyChanged(nameof(HasAnnouncements));
        OnPropertyChanged(nameof(ShowAnnouncementHint));
        OnPropertyChanged(nameof(ShowAnnouncementEmptyState));
        OnPropertyChanged(nameof(CanEditAnnouncements));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}