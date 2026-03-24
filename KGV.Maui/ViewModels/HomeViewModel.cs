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
    public ObservableCollection<HomeWorkAssignmentItem> WorkAssignments { get; } = new();
    public ObservableCollection<HomeAppointmentItem> Appointments { get; } = new();

    public string Title => "Startseite";
    public string Description => _overview.Description;
    public string UserContextText => $"Kontext: {UserRoles.ToStorageValue(_userContextState.CurrentUserContext?.Role ?? UserRole.User)}";
    public string QuickLinksTitle => _overview.QuickLinksTitle;
    public string QuickLinksEmptyText => _overview.QuickLinksEmptyText;
    public string OperationalTitle => "Arbeitsstunden";
    public string OperationalEmptyText => "Aktuell liegen keine zusätzlichen Hinweise zu Arbeitsstunden vor.";
    public string AnnouncementTitle => _overview.AnnouncementTitle;
    public string AnnouncementEmptyText => _overview.AnnouncementEmptyText;
    public string WorkAssignmentsTitle => "Arbeitseinsätze";
    public string WorkAssignmentsEmptyText => _overview.WorkAssignmentsEmptyText;
    public string AppointmentsTitle => "Termine";
    public string AppointmentsEmptyText => _overview.AppointmentsEmptyText;
    public string ManagementTitle => "Verwaltung";
    public string ManagementHintText => "Admin/Vorstand erreichen hier mobil die produktiven Verwaltungsoberflächen für Arbeitseinsätze, Termine und Bekanntmachungen auf den bestehenden Shared-Servicepfaden.";
    public bool HasQuickLinks => QuickLinks.Count > 0;
    public bool HasOperationalItems => OperationalItems.Count > 0;
    public bool HasAnnouncements => Announcements.Count > 0;
    public bool HasWorkAssignments => WorkAssignments.Count > 0;
    public bool HasAppointments => Appointments.Count > 0;
    public bool ShowAnnouncementDetail => HasAnnouncements;
    public bool HasSelectedAnnouncement => SelectedAnnouncement != null;
    public bool ShowAnnouncementHint => HasAnnouncements && !HasSelectedAnnouncement;
    public bool ShowAnnouncementEmptyState => !HasAnnouncements;
    public bool ShowWorkAssignmentsEmptyState => !HasWorkAssignments;
    public bool ShowAppointmentsEmptyState => !HasAppointments;
    public bool IsAdminContext => _userContextState.CurrentUserContext?.Role is UserRole.Admin or UserRole.Vorstand;
    public bool ShowManagementSection => IsAdminContext;

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
        FillCollection(WorkAssignments, _overview.WorkAssignments);
        FillCollection(Appointments, _overview.Appointments);
        SelectedAnnouncement = null;

        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(UserContextText));
        OnPropertyChanged(nameof(QuickLinksTitle));
        OnPropertyChanged(nameof(QuickLinksEmptyText));
        OnPropertyChanged(nameof(OperationalTitle));
        OnPropertyChanged(nameof(OperationalEmptyText));
        OnPropertyChanged(nameof(AnnouncementTitle));
        OnPropertyChanged(nameof(AnnouncementEmptyText));
        OnPropertyChanged(nameof(WorkAssignmentsTitle));
        OnPropertyChanged(nameof(WorkAssignmentsEmptyText));
        OnPropertyChanged(nameof(AppointmentsTitle));
        OnPropertyChanged(nameof(AppointmentsEmptyText));
        OnPropertyChanged(nameof(HasAnnouncements));
        OnPropertyChanged(nameof(ShowAnnouncementDetail));
        OnPropertyChanged(nameof(HasQuickLinks));
        OnPropertyChanged(nameof(HasOperationalItems));
        OnPropertyChanged(nameof(HasWorkAssignments));
        OnPropertyChanged(nameof(HasAppointments));
        OnPropertyChanged(nameof(ShowAnnouncementHint));
        OnPropertyChanged(nameof(ShowAnnouncementEmptyState));
        OnPropertyChanged(nameof(ShowWorkAssignmentsEmptyState));
        OnPropertyChanged(nameof(ShowAppointmentsEmptyState));
        OnPropertyChanged(nameof(IsAdminContext));
        OnPropertyChanged(nameof(ShowManagementSection));
        OnPropertyChanged(nameof(ManagementTitle));
        OnPropertyChanged(nameof(ManagementHintText));
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