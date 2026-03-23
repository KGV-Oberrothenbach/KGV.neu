using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Helpers;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;

namespace KGV.ViewModels
{
    public sealed class HomeViewModel : BaseViewModel, KGV.Core.Interfaces.INavigationAware
    {
        private readonly MainWindowViewModel _mainVm;
        private HomeOverviewDTO _overview = HomeOverviewFactory.Build(UserRole.User);

        public string Title => "Startseite";
        public string Description => _overview.Description;
        public string WorkHoursHeader => $"Meine Arbeitsstunden {(_overview.WorkHoursSummary?.Year ?? DateTime.Today.Year)}";
        public string RequiredHoursValue => FormatHours(_overview.WorkHoursSummary?.RequiredHours);
        public string WorkedHoursValue => FormatHours(_overview.WorkHoursSummary?.WorkedHours);
        public string OpenHoursValue => FormatHours(_overview.WorkHoursSummary?.OpenHours);
        public string RequiredHoursHint => _overview.WorkHoursSummary == null
            ? "Für diesen Home-Kontext liegt aktuell kein belastbarer Pflichtstundenstand aus der Startseiten-View vor."
            : !string.IsNullOrWhiteSpace(_overview.WorkHoursSummary.RuleReason)
                ? _overview.WorkHoursSummary.RuleReason
                : "Sollstunden laut zentraler Pflichtstunden-Übersicht.";
        public string WorkedHoursHint => _overview.WorkHoursSummary == null
            ? "Noch keine Daten aus der Pflichtstunden-Übersicht geladen."
            : "Geleistete Stunden laut zentraler Pflichtstunden-Übersicht.";
        public string OpenHoursHint => _overview.WorkHoursSummary == null
            ? "Noch keine Daten aus der Pflichtstunden-Übersicht geladen."
            : "Offene Stunden laut zentraler Pflichtstunden-Übersicht.";
        public string WorkHoursInfoText => _overview.WorkHoursSummary == null
            ? "Für diesen Home-Kontext ist aktuell keine belastbare Pflichtstunden-Übersicht verfügbar."
            : BuildWorkHoursInfoText(_overview.WorkHoursSummary);

        public string WorkAssignmentsTitle => "Arbeitseinsätze";
        public string WorkAssignmentsEmptyText => _overview.WorkAssignmentsEmptyText;
        public string AppointmentsTitle => "Termine";
        public string AppointmentsEmptyText => _overview.AppointmentsEmptyText;
        public string AnnouncementTitle => _overview.AnnouncementTitle;
        public string AnnouncementEmptyText => _overview.AnnouncementEmptyText;
        public bool IsAdminContext => _mainVm.UserContext.Role is UserRole.Admin or UserRole.Vorstand;
        public bool CanOpenWorkHoursEntry => _mainVm.UserContext.MitgliedId is > 0 and <= int.MaxValue;
        public bool ShowAdminManagementSection => IsAdminContext;
        public bool HasWorkAssignments => WorkAssignments.Count > 0;
        public bool ShowWorkAssignmentsEmptyState => !HasWorkAssignments;
        public bool HasAppointments => Appointments.Count > 0;
        public bool ShowAppointmentEmptyState => !HasAppointments;
        public bool HasAnnouncements => Announcements.Count > 0;
        public bool ShowAnnouncementEmptyState => !HasAnnouncements;

        public ObservableCollection<HomeWorkAssignmentItem> WorkAssignments { get; } = new();
        public ObservableCollection<HomeAppointmentItem> Appointments { get; } = new();
        public ObservableCollection<HomeAnnouncementItem> Announcements { get; } = new();

        public RelayCommand<object?> OpenWorkedHoursCommand { get; }
        public RelayCommand<object?> OpenWorkHoursEntryCommand { get; }
        public RelayCommand<object?> OpenWorkAssignmentsManagementCommand { get; }
        public RelayCommand<object?> OpenAppointmentsManagementCommand { get; }
        public RelayCommand<object?> OpenAnnouncementsManagementCommand { get; }
        public RelayCommand<HomeWorkAssignmentItem> OpenWorkAssignmentDetailCommand { get; }
        public RelayCommand<HomeWorkAssignmentItem> RegisterForWorkAssignmentCommand { get; }
        public RelayCommand<HomeAppointmentItem> OpenAppointmentDetailCommand { get; }
        public RelayCommand<HomeAnnouncementItem> OpenAnnouncementDetailCommand { get; }

        public HomeViewModel(MainWindowViewModel mainVm)
        {
            _mainVm = mainVm ?? throw new ArgumentNullException(nameof(mainVm));
            OpenWorkedHoursCommand = new RelayCommand<object?>(_ => _ = OpenWorkedHoursAsync(), _ => _mainVm.UserContext.MitgliedId is > 0);
            OpenWorkHoursEntryCommand = new RelayCommand<object?>(_ => _ = OpenWorkHoursEntryAsync(), _ => CanOpenWorkHoursEntry);
            OpenWorkAssignmentsManagementCommand = new RelayCommand<object?>(_ => _ = OpenWorkAssignmentsManagementAsync(), _ => IsAdminContext);
            OpenAppointmentsManagementCommand = new RelayCommand<object?>(_ => _ = OpenAppointmentsManagementAsync(), _ => IsAdminContext);
            OpenAnnouncementsManagementCommand = new RelayCommand<object?>(_ => _ = OpenAnnouncementsManagementAsync(), _ => IsAdminContext);
            OpenWorkAssignmentDetailCommand = new RelayCommand<HomeWorkAssignmentItem>(item => _ = OpenWorkAssignmentDetailAsync(item), item => item != null);
            RegisterForWorkAssignmentCommand = new RelayCommand<HomeWorkAssignmentItem>(item => _ = RegisterForWorkAssignmentAsync(item), item => item?.CanRegister == true);
            OpenAppointmentDetailCommand = new RelayCommand<HomeAppointmentItem>(item => _ = OpenAppointmentDetailAsync(item), item => item != null);
            OpenAnnouncementDetailCommand = new RelayCommand<HomeAnnouncementItem>(item => _ = OpenAnnouncementDetailAsync(item), item => item != null);
        }

        public async Task OnNavigatedToAsync()
        {
            await LoadAsync();
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private async Task LoadAsync()
        {
            _overview = await _mainVm.SupabaseService.GetHomeOverviewAsync(_mainVm.UserContext.Role, ToInt32(_mainVm.UserContext.MitgliedId));

            FillCollection(WorkAssignments, _overview.WorkAssignments);
            FillCollection(Appointments, _overview.Appointments);
            FillCollection(Announcements, _overview.Announcements);

            OnPropertyChanged(nameof(Description));
            OnPropertyChanged(nameof(WorkHoursHeader));
            OnPropertyChanged(nameof(RequiredHoursValue));
            OnPropertyChanged(nameof(WorkedHoursValue));
            OnPropertyChanged(nameof(OpenHoursValue));
            OnPropertyChanged(nameof(RequiredHoursHint));
            OnPropertyChanged(nameof(WorkedHoursHint));
            OnPropertyChanged(nameof(OpenHoursHint));
            OnPropertyChanged(nameof(WorkHoursInfoText));
            OnPropertyChanged(nameof(WorkAssignmentsEmptyText));
            OnPropertyChanged(nameof(AppointmentsEmptyText));
            OnPropertyChanged(nameof(AnnouncementEmptyText));
            OnPropertyChanged(nameof(IsAdminContext));
            OnPropertyChanged(nameof(CanOpenWorkHoursEntry));
            OnPropertyChanged(nameof(ShowAdminManagementSection));
            OnPropertyChanged(nameof(HasWorkAssignments));
            OnPropertyChanged(nameof(ShowWorkAssignmentsEmptyState));
            OnPropertyChanged(nameof(HasAppointments));
            OnPropertyChanged(nameof(ShowAppointmentEmptyState));
            OnPropertyChanged(nameof(HasAnnouncements));
            OnPropertyChanged(nameof(ShowAnnouncementEmptyState));
        }

        private async Task OpenWorkedHoursAsync()
        {
            var member = await _mainVm.EnsureCurrentMemberSelectedAsync();
            if (member == null)
                return;

            var created = _mainVm.NavigateToArbeitsstundenViewModel(member);
            if (created != null)
                await _mainVm.NavigateToAsync(created);
        }

        private async Task OpenWorkHoursEntryAsync()
        {
            var created = _mainVm.NavigateToArbeitsstundenErfassungViewModel();
            if (created != null)
                await _mainVm.NavigateToAsync(created);
        }

        private async Task OpenWorkAssignmentsManagementAsync()
        {
            var created = _mainVm.NavigateToArbeitseinsaetzeVerwaltungViewModel();
            if (created != null)
                await _mainVm.NavigateToAsync(created);
        }

        private async Task OpenAppointmentsManagementAsync()
        {
            var created = _mainVm.NavigateToTermineVerwaltungViewModel();
            if (created != null)
                await _mainVm.NavigateToAsync(created);
        }

        private async Task OpenAnnouncementsManagementAsync()
        {
            var created = _mainVm.NavigateToBekanntmachungenVerwaltungViewModel();
            if (created != null)
                await _mainVm.NavigateToAsync(created);
        }

        private async Task OpenWorkAssignmentDetailAsync(HomeWorkAssignmentItem? item)
        {
            if (item == null)
                return;

            var created = _mainVm.NavigateToHomeSectionDetailViewModel(new HomeSectionDetailContext
            {
                WorkAssignmentId = item.Id,
                SectionTitle = "Arbeitseinsatz",
                Title = item.Title,
                Subtitle = item.Subtitle,
                StartTimeText = item.StartTimeText,
                EndTimeText = item.EndTimeText,
                Content = item.Details,
                AdditionalInfo = item.DetailInfo,
                RegistrationInfo = item.RegistrationInfo,
                ShowRegisterButton = item.CanRegister
            });

            if (created != null)
                await _mainVm.NavigateToAsync(created);
        }

        private async Task OpenAppointmentDetailAsync(HomeAppointmentItem? item)
        {
            if (item == null)
                return;

            var created = _mainVm.NavigateToHomeSectionDetailViewModel(new HomeSectionDetailContext
            {
                SectionTitle = "Termin",
                Title = item.Title,
                Subtitle = item.Subtitle,
                StartTimeText = item.StartTimeText,
                EndTimeText = item.EndTimeText,
                Content = item.Details,
                AdditionalInfo = item.DetailInfo,
                RegistrationInfo = string.Empty,
                ShowRegisterButton = false
            });

            if (created != null)
                await _mainVm.NavigateToAsync(created);
        }

        private async Task OpenAnnouncementDetailAsync(HomeAnnouncementItem? item)
        {
            if (item == null)
                return;

            var created = _mainVm.NavigateToHomeSectionDetailViewModel(new HomeSectionDetailContext
            {
                SectionTitle = "Bekanntmachung",
                Title = item.Title,
                Subtitle = item.Subtitle,
                StartTimeText = string.Empty,
                EndTimeText = string.Empty,
                Content = item.Content,
                AdditionalInfo = item.DetailInfo,
                RegistrationInfo = string.Empty,
                ShowRegisterButton = false
            });

            if (created != null)
                await _mainVm.NavigateToAsync(created);
        }

        private async Task RegisterForWorkAssignmentAsync(HomeWorkAssignmentItem? item)
        {
            if (item == null)
                return;

            var mitgliedId = await ResolveCurrentMemberIdAsync();
            if (!mitgliedId.HasValue)
            {
                MessageBox.Show(
                    "Der aktuelle Benutzer ist keinem Mitglied zugeordnet.",
                    "Anmeldung",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var result = await _mainVm.SupabaseService.SignUpForArbeitseinsatzAsync(item.Id, mitgliedId.Value);
            if (result.UpdatedItem != null)
                ReplaceWorkAssignmentItem(result.UpdatedItem, forceDisableRegistration: !result.Success || result.UpdatedItem.CanRegister == false);

            MessageBox.Show(
                result.Message,
                "Anmeldung",
                MessageBoxButton.OK,
                result.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }

        private async Task<int?> ResolveCurrentMemberIdAsync()
        {
            if (_mainVm.UserContext.MitgliedId is > 0 and <= int.MaxValue)
                return (int)_mainVm.UserContext.MitgliedId.Value;

            var member = await _mainVm.EnsureCurrentMemberSelectedAsync();
            return member?.Id > 0 ? member.Id : null;
        }

        private void ReplaceWorkAssignmentItem(HomeWorkAssignmentItem item, bool forceDisableRegistration)
        {
            var index = WorkAssignments
                .Select((current, currentIndex) => new { current, currentIndex })
                .FirstOrDefault(x => x.current.Id == item.Id)
                ?.currentIndex;

            if (!index.HasValue)
                return;

            WorkAssignments[index.Value] = CreateUpdatedWorkAssignmentItem(item, forceDisableRegistration);
        }

        private static HomeWorkAssignmentItem CreateUpdatedWorkAssignmentItem(HomeWorkAssignmentItem item, bool forceDisableRegistration)
        {
            return new HomeWorkAssignmentItem
            {
                Id = item.Id,
                Title = item.Title,
                Subtitle = item.Subtitle,
                StartTimeText = item.StartTimeText,
                EndTimeText = item.EndTimeText,
                Details = item.Details,
                DetailInfo = item.DetailInfo,
                RegistrationInfo = item.RegistrationInfo,
                CanRegister = forceDisableRegistration ? false : item.CanRegister
            };
        }

        private static string BuildWorkHoursInfoText(HomeWorkHoursSummary summary)
        {
            var parts = new ObservableCollection<string>();
            if (summary.IsExempt)
                parts.Add("Der aktuelle Mitgliedskontext ist laut Pflichtstunden-Übersicht befreit.");
            if (summary.HasMaintenanceContract)
                parts.Add("Ein Wartungsvertrag ist in der zentralen Regelbewertung berücksichtigt.");
            if (!string.IsNullOrWhiteSpace(summary.RuleReason))
                parts.Add(summary.RuleReason);

            if (parts.Count == 0)
                return "Die Werte stammen direkt aus der zentralen Pflichtstunden-Übersicht für Startseite/Home.";

            return string.Join(" ", parts);
        }

        private static string FormatHours(decimal? value)
        {
            return value.HasValue
                ? $"{value.Value.ToString("0.##", CultureInfo.CurrentCulture)} h"
                : "–";
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
    }
}
