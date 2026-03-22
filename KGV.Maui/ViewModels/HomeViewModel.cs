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
    private HomeOverviewDTO _overview = HomeOverviewFactory.Build(UserRole.User);

    public HomeViewModel(UserContextState userContextState)
    {
        _userContextState = userContextState;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<HomeAnnouncementItem> Announcements { get; } = new();
    public ObservableCollection<HomeQuickLinkItem> QuickLinks { get; } = new();

    public string Title => "Startseite";
    public string Description => _overview.Description;
    public string UserContextText => $"Kontext: {UserRoles.ToStorageValue(_userContextState.CurrentUserContext?.Role ?? UserRole.User)}";
    public string QuickLinksTitle => _overview.QuickLinksTitle;
    public string QuickLinksEmptyText => _overview.QuickLinksEmptyText;
    public string AnnouncementTitle => _overview.AnnouncementTitle;
    public string AnnouncementHintText => _overview.AnnouncementHintText;
    public string AnnouncementEmptyText => _overview.AnnouncementEmptyText;
    public bool HasQuickLinks => QuickLinks.Count > 0;
    public bool HasAnnouncements => Announcements.Count > 0;
    public bool HasSelectedAnnouncement => SelectedAnnouncement != null;
    public bool ShowAnnouncementHint => HasAnnouncements && !HasSelectedAnnouncement;
    public bool ShowAnnouncementEmptyState => !HasAnnouncements;

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
        _overview = HomeOverviewFactory.Build(_userContextState.CurrentUserContext?.Role ?? UserRole.User);

        QuickLinks.Clear();
        foreach (var item in _overview.QuickLinks)
            QuickLinks.Add(item);

        Announcements.Clear();
        SelectedAnnouncement = null;

        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(UserContextText));
        OnPropertyChanged(nameof(QuickLinksTitle));
        OnPropertyChanged(nameof(QuickLinksEmptyText));
        OnPropertyChanged(nameof(AnnouncementTitle));
        OnPropertyChanged(nameof(HasAnnouncements));
        OnPropertyChanged(nameof(HasQuickLinks));
        OnPropertyChanged(nameof(ShowAnnouncementHint));
        OnPropertyChanged(nameof(ShowAnnouncementEmptyState));
        OnPropertyChanged(nameof(AnnouncementHintText));
        OnPropertyChanged(nameof(AnnouncementEmptyText));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}