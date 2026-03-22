using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Maui.State;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace KGV.Maui.ViewModels;

public sealed class HomeViewModel : INotifyPropertyChanged
{
    private readonly ISupabaseService _supabaseService;
    private readonly UserContextState _userContextState;
    private HomeAnnouncementItem? _selectedAnnouncement;
    private HomeOverviewDTO _overview = HomeOverviewFactory.Build(UserRole.User);

    public HomeViewModel(ISupabaseService supabaseService, UserContextState userContextState)
    {
        _supabaseService = supabaseService;
        _userContextState = userContextState;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<HomeAnnouncementItem> Announcements { get; } = new();
    public ObservableCollection<HomeQuickLinkItem> QuickLinks { get; } = new();
    public ObservableCollection<HomeOperationalItem> OperationalItems { get; } = new();

    public string Title => "Startseite";
    public string Description => _overview.Description;
    public string UserContextText => $"Kontext: {UserRoles.ToStorageValue(_userContextState.CurrentUserContext?.Role ?? UserRole.User)}";
    public string QuickLinksTitle => _overview.QuickLinksTitle;
    public string QuickLinksEmptyText => _overview.QuickLinksEmptyText;
    public string OperationalTitle => _overview.OperationalTitle;
    public string OperationalEmptyText => _overview.OperationalEmptyText;
    public string AnnouncementTitle => _overview.AnnouncementTitle;
    public string AnnouncementHintText => _overview.AnnouncementHintText;
    public string AnnouncementEmptyText => _overview.AnnouncementEmptyText;
    public bool HasQuickLinks => QuickLinks.Count > 0;
    public bool HasOperationalItems => OperationalItems.Count > 0;
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

    public async Task InitializeAsync()
    {
        _overview = await _supabaseService.GetHomeOverviewAsync(
            _userContextState.CurrentUserContext?.Role ?? UserRole.User,
            ToInt32(_userContextState.CurrentMitgliedId));

        FillCollection(QuickLinks, _overview.QuickLinks);
        FillCollection(OperationalItems, _overview.OperationalItems);
        FillCollection(Announcements, _overview.Announcements);
        SelectedAnnouncement = null;

        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(UserContextText));
        OnPropertyChanged(nameof(QuickLinksTitle));
        OnPropertyChanged(nameof(QuickLinksEmptyText));
        OnPropertyChanged(nameof(OperationalTitle));
        OnPropertyChanged(nameof(OperationalEmptyText));
        OnPropertyChanged(nameof(AnnouncementTitle));
        OnPropertyChanged(nameof(HasAnnouncements));
        OnPropertyChanged(nameof(HasQuickLinks));
        OnPropertyChanged(nameof(HasOperationalItems));
        OnPropertyChanged(nameof(ShowAnnouncementHint));
        OnPropertyChanged(nameof(ShowAnnouncementEmptyState));
        OnPropertyChanged(nameof(AnnouncementHintText));
        OnPropertyChanged(nameof(AnnouncementEmptyText));
    }

    private static void FillCollection<T>(ObservableCollection<T> target, System.Collections.Generic.IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source)
            target.Add(item);
    }

    private static int? ToInt32(long? value)
    {
        return value is > 0 and <= int.MaxValue ? (int)value.Value : null;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}