using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace KGV.ViewModels
{
    public sealed class HomeViewModel : BaseViewModel, KGV.Core.Interfaces.INavigationAware
    {
        private readonly MainWindowViewModel _mainVm;
        private HomeOverviewDTO _overview = HomeOverviewFactory.Build(UserRole.User);
        private string _workHoursHeader = $"Meine Arbeitsstunden {DateTime.Today.Year}";
        private string _requiredHoursValue = "–";
        private string _requiredHoursHint = "Aktuell ist kein belastbarer Sollstundenpfad im aktiven Stand vorhanden.";
        private string _workedHoursValue = "0 h";
        private string _workedHoursHint = "Freigegebene Arbeitsstunden im aktuellen Kalenderjahr.";
        private string _openHoursValue = "0 h";
        private string _openHoursHint = "Noch offene Arbeitsstunden im aktuellen Kalenderjahr.";
        private string _workHoursInfoText = "Für diesen Home-Kontext liegen aktuell noch keine Arbeitsstunden im aktuellen Kalenderjahr vor.";

        public string Title => "Startseite";
        public string Description => _overview.Description;
        public string UserContextText => $"Kontext: {UserRoles.ToStorageValue(_mainVm.UserContext.Role)}";
        public bool HasQuickLinks => QuickLinks.Count > 0;
        public string StatusMessage => QuickLinks.Count == 0 ? _overview.QuickLinksEmptyText : string.Empty;
        public string QuickLinksTitle => "Schnellzugriffe";
        public string WorkHoursHeader
        {
            get => _workHoursHeader;
            private set => SetProperty(ref _workHoursHeader, value);
        }

        public string RequiredHoursValue
        {
            get => _requiredHoursValue;
            private set => SetProperty(ref _requiredHoursValue, value);
        }

        public string RequiredHoursHint
        {
            get => _requiredHoursHint;
            private set => SetProperty(ref _requiredHoursHint, value);
        }

        public string WorkedHoursValue
        {
            get => _workedHoursValue;
            private set => SetProperty(ref _workedHoursValue, value);
        }

        public string WorkedHoursHint
        {
            get => _workedHoursHint;
            private set => SetProperty(ref _workedHoursHint, value);
        }

        public string OpenHoursValue
        {
            get => _openHoursValue;
            private set => SetProperty(ref _openHoursValue, value);
        }

        public string OpenHoursHint
        {
            get => _openHoursHint;
            private set => SetProperty(ref _openHoursHint, value);
        }

        public string WorkHoursInfoText
        {
            get => _workHoursInfoText;
            private set => SetProperty(ref _workHoursInfoText, value);
        }

        public string WorkAssignmentsTitle => "Arbeitseinsätze";
        public string WorkAssignmentsEmptyText => "Für Home ist aktuell kein belastbarer Arbeitseinsatz-Pfad angebunden.";
        public string WorkAssignmentsAdminHint => "Für Arbeitseinsätze ist aktuell kein belastbarer Verwaltungsweg im aktiven WPF-Stand vorhanden.";
        public string AppointmentsTitle => "Termine";
        public string AppointmentsEmptyText => "Für Home ist aktuell kein belastbarer Termin-Pfad angebunden.";
        public string AppointmentsAdminHint => "Für Termine ist aktuell kein belastbarer Verwaltungsweg im aktiven WPF-Stand vorhanden.";
        public string AnnouncementTitle => _overview.AnnouncementTitle;
        public string AnnouncementHintText => _overview.AnnouncementHintText;
        public string AnnouncementEmptyText => _overview.AnnouncementEmptyText;
        public string AnnouncementsAdminHint => "Für Bekanntmachungen ist aktuell kein belastbarer Verwaltungsweg im aktiven WPF-Stand vorhanden.";
        public bool IsAdminContext => _mainVm.UserContext.Role is UserRole.Admin or UserRole.Vorstand;
        public bool HasAnnouncements => Announcements.Count > 0;
        public bool HasSelectedAnnouncement => SelectedAnnouncement != null;
        public bool ShowAnnouncementHint => HasAnnouncements && !HasSelectedAnnouncement;
        public bool ShowAnnouncementEmptyState => !HasAnnouncements;
        public bool ShowAnnouncementDetail => HasSelectedAnnouncement;

        public ObservableCollection<HomeQuickLinkItem> QuickLinks { get; } = new();
        public ObservableCollection<HomeAnnouncementItem> Announcements { get; } = new();

        public RelayCommand<HomeQuickLinkItem> OpenModuleCommand { get; }
        public RelayCommand<object?> CloseAnnouncementCommand { get; }

        private HomeAnnouncementItem? _selectedAnnouncement;
        public HomeAnnouncementItem? SelectedAnnouncement
        {
            get => _selectedAnnouncement;
            set
            {
                if (_selectedAnnouncement == value)
                    return;

                _selectedAnnouncement = value;
                OnPropertyChanged(nameof(SelectedAnnouncement));
                OnPropertyChanged(nameof(HasSelectedAnnouncement));
                OnPropertyChanged(nameof(ShowAnnouncementHint));
                OnPropertyChanged(nameof(ShowAnnouncementDetail));
                CloseAnnouncementCommand.RaiseCanExecuteChanged();
            }
        }

        public HomeViewModel(MainWindowViewModel mainVm)
        {
            _mainVm = mainVm ?? throw new ArgumentNullException(nameof(mainVm));
            OpenModuleCommand = new RelayCommand<HomeQuickLinkItem>(OpenModule, item => item != null);
            CloseAnnouncementCommand = new RelayCommand<object?>(_ => CloseAnnouncementDetail(), _ => HasSelectedAnnouncement);
        }

        public async Task OnNavigatedToAsync()
        {
            await LoadAsync();
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private async Task LoadAsync()
        {
            var mitgliedId = ToInt32(_mainVm.UserContext.MitgliedId);
            _overview = await _mainVm.SupabaseService.GetHomeOverviewAsync(_mainVm.UserContext.Role, ToInt32(_mainVm.UserContext.MitgliedId));

            FillCollection(QuickLinks, _overview.QuickLinks);
            FillCollection(Announcements, _overview.Announcements);
            SelectedAnnouncement = null;
            await LoadWorkHoursSummaryAsync(mitgliedId);

            OnPropertyChanged(nameof(Description));
            OnPropertyChanged(nameof(HasQuickLinks));
            OnPropertyChanged(nameof(StatusMessage));
            OnPropertyChanged(nameof(QuickLinksTitle));
            OnPropertyChanged(nameof(AnnouncementTitle));
            OnPropertyChanged(nameof(AnnouncementHintText));
            OnPropertyChanged(nameof(AnnouncementEmptyText));
            OnPropertyChanged(nameof(IsAdminContext));
            OnPropertyChanged(nameof(HasAnnouncements));
            OnPropertyChanged(nameof(ShowAnnouncementHint));
            OnPropertyChanged(nameof(ShowAnnouncementEmptyState));
            OnPropertyChanged(nameof(ShowAnnouncementDetail));
        }

        private void OpenModule(HomeQuickLinkItem? item)
        {
            if (item == null)
                return;

            var target = item.Key switch
            {
                HomeQuickLinkKey.MemberSearch => _mainVm.NavigationItems.FirstOrDefault(x => x.ViewModelType == typeof(MemberSearchViewModel) && x.IsVisible),
                HomeQuickLinkKey.PlotManagement => _mainVm.NavigationItems.FirstOrDefault(x => x.ViewModelType == typeof(ParzellenVerwaltungViewModel) && x.IsVisible),
                HomeQuickLinkKey.MyProfile => _mainVm.NavigationItems.FirstOrDefault(x => x.ViewModelType == typeof(MemberDetailViewModel) && x.IsVisible),
                HomeQuickLinkKey.MyWorkHours => CreateWorkHoursNavigationItem(),
                _ => null
            };

            if (target != null)
                _mainVm.NavigateCommand.Execute(target);
        }

        private void CloseAnnouncementDetail()
        {
            SelectedAnnouncement = null;
            CloseAnnouncementCommand.RaiseCanExecuteChanged();
            OnPropertyChanged(nameof(ShowAnnouncementDetail));
        }

        private async Task LoadWorkHoursSummaryAsync(int? mitgliedId)
        {
            var year = DateTime.Today.Year;
            WorkHoursHeader = $"Meine Arbeitsstunden {year}";
            RequiredHoursValue = "–";
            RequiredHoursHint = "Aktuell ist kein belastbarer Sollstundenpfad im aktiven Stand vorhanden.";
            WorkedHoursValue = "0 h";
            WorkedHoursHint = "Freigegebene Arbeitsstunden im aktuellen Kalenderjahr.";
            OpenHoursValue = "0 h";
            OpenHoursHint = "Noch offene Arbeitsstunden im aktuellen Kalenderjahr.";
            WorkHoursInfoText = "Für diesen Home-Kontext liegen aktuell noch keine Arbeitsstunden im aktuellen Kalenderjahr vor.";

            if (mitgliedId is not > 0)
            {
                WorkHoursInfoText = "Für diesen Benutzerkontext ist aktuell kein belastbarer Arbeitsstundenpfad auf Home verfügbar.";
                return;
            }

            var hauptmitglied = await _mainVm.SupabaseService.GetMitgliedByIdAsync(mitgliedId.Value);
            if (!OperationalDataFilter.IsOperationalMember(hauptmitglied))
            {
                WorkHoursInfoText = "Für Demo-/Testdaten werden auf Home keine Arbeitsstunden-Zusammenfassungen gebildet.";
                WorkedHoursValue = "–";
                OpenHoursValue = "–";
                return;
            }

            var ids = new List<int> { mitgliedId.Value };
            var includesNebenmitglied = false;
            var nebenmitglied = await _mainVm.SupabaseService.GetNebenmitgliedByHauptmitgliedIdAsync(mitgliedId.Value);
            if (OperationalDataFilter.IsOperationalMember(nebenmitglied) && nebenmitglied != null)
            {
                ids.Add(nebenmitglied.Id);
                includesNebenmitglied = true;
            }

            var arbeitsstunden = await _mainVm.SupabaseService.GetArbeitsstundenAsync(ids.ToArray());
            var relevant = arbeitsstunden.Where(x => x.Datum.Year == year).ToList();
            var geleistet = relevant.Where(x => x.Freigegeben).Sum(x => x.Stunden);
            var offen = relevant.Where(x => !x.Freigegeben && (string.IsNullOrWhiteSpace(x.Status) || x.Status.Equals("offen", StringComparison.OrdinalIgnoreCase))).Sum(x => x.Stunden);
            var abgelehnt = relevant.Where(x => string.Equals(x.Status, "abgelehnt", StringComparison.OrdinalIgnoreCase)).Sum(x => x.Stunden);

            WorkedHoursValue = FormatHours(geleistet);
            WorkedHoursHint = includesNebenmitglied
                ? "Freigegebene Arbeitsstunden im aktuellen Kalenderjahr inkl. Nebenmitglied."
                : "Freigegebene Arbeitsstunden im aktuellen Kalenderjahr.";

            OpenHoursValue = FormatHours(offen);
            OpenHoursHint = includesNebenmitglied
                ? "Noch offene Arbeitsstunden im aktuellen Kalenderjahr inkl. Nebenmitglied."
                : "Noch offene Arbeitsstunden im aktuellen Kalenderjahr.";

            WorkHoursInfoText = relevant.Count == 0
                ? "Für diesen Home-Kontext sind im aktuellen Kalenderjahr noch keine Arbeitsstunden erfasst."
                : offen > 0
                    ? $"{relevant.Count} Eintrag/Einträge im aktuellen Kalenderjahr; {FormatHours(offen)} davon warten noch auf Prüfung.{(abgelehnt > 0 ? $" Zusätzlich {FormatHours(abgelehnt)} abgelehnt." : string.Empty)}"
                    : $"{relevant.Count} Eintrag/Einträge im aktuellen Kalenderjahr, aktuell ohne offene Prüfung.{(abgelehnt > 0 ? $" {FormatHours(abgelehnt)} davon abgelehnt." : string.Empty)}";
        }

        private static string FormatHours(decimal value)
        {
            return $"{value.ToString("0.##", CultureInfo.CurrentCulture)} h";
        }

        private NavigationItem? CreateWorkHoursNavigationItem()
        {
            if (_mainVm.SelectedMember == null)
                return null;

            return new NavigationItem
            {
                Title = "Arbeitsstunden",
                ViewModelType = typeof(ArbeitsstundenViewModel),
                IsVisible = true
            };
        }

        private static void FillCollection<T>(ObservableCollection<T> target, IEnumerable<T> source)
        {
            target.Clear();
            foreach (var item in source)
                target.Add(item);
        }

        private static int? ToInt32(long? value)
        {
            return value is > 0 and <= int.MaxValue ? (int)value.Value : null;
        }
    }
}
