using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Helpers;
using System;
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
        public string WorkAssignmentsAdminHint => "Eine Anmeldung oder Pflege ist auf Home nur dort verdrahtet, wo bereits ein belastbarer Produktpfad vorhanden ist.";
        public string AppointmentsTitle => "Termine";
        public string AppointmentsEmptyText => _overview.AppointmentsEmptyText;
        public string AppointmentsAdminHint => "Die Detailansicht zeigt nur die Felder, die über die Startseiten-View belastbar vorliegen.";
        public string AppointmentHintText => "Bitte einen Termin aus der Liste auswählen.";
        public string AnnouncementTitle => _overview.AnnouncementTitle;
        public string AnnouncementHintText => _overview.AnnouncementHintText;
        public string AnnouncementEmptyText => _overview.AnnouncementEmptyText;
        public string AnnouncementsAdminHint => "Die Home-Seite verwendet nur die vorhandene Startseiten-View für Bekanntmachungen.";
        public bool IsAdminContext => _mainVm.UserContext.Role is UserRole.Admin or UserRole.Vorstand;
        public bool HasWorkAssignments => WorkAssignments.Count > 0;
        public bool ShowWorkAssignmentsEmptyState => !HasWorkAssignments;
        public bool HasAppointments => Appointments.Count > 0;
        public bool HasSelectedAppointment => SelectedAppointment != null;
        public bool ShowAppointmentHint => HasAppointments && !HasSelectedAppointment;
        public bool ShowAppointmentEmptyState => !HasAppointments;
        public bool ShowAppointmentDetail => HasSelectedAppointment;
        public bool HasAnnouncements => Announcements.Count > 0;
        public bool HasSelectedAnnouncement => SelectedAnnouncement != null;
        public bool ShowAnnouncementHint => HasAnnouncements && !HasSelectedAnnouncement;
        public bool ShowAnnouncementEmptyState => !HasAnnouncements;
        public bool ShowAnnouncementDetail => HasSelectedAnnouncement;

        public ObservableCollection<HomeWorkAssignmentItem> WorkAssignments { get; } = new();
        public ObservableCollection<HomeAppointmentItem> Appointments { get; } = new();
        public ObservableCollection<HomeAnnouncementItem> Announcements { get; } = new();

        public RelayCommand<object?> OpenWorkedHoursCommand { get; }
        public RelayCommand<object?> CloseAppointmentCommand { get; }
        public RelayCommand<object?> CloseAnnouncementCommand { get; }

        private HomeAppointmentItem? _selectedAppointment;
        public HomeAppointmentItem? SelectedAppointment
        {
            get => _selectedAppointment;
            set
            {
                if (_selectedAppointment == value)
                    return;

                _selectedAppointment = value;
                OnPropertyChanged(nameof(SelectedAppointment));
                OnPropertyChanged(nameof(HasSelectedAppointment));
                OnPropertyChanged(nameof(ShowAppointmentHint));
                OnPropertyChanged(nameof(ShowAppointmentDetail));
                CloseAppointmentCommand.RaiseCanExecuteChanged();
            }
        }

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
            OpenWorkedHoursCommand = new RelayCommand<object?>(_ => _ = OpenWorkedHoursAsync(), _ => _mainVm.UserContext.MitgliedId is > 0);
            CloseAppointmentCommand = new RelayCommand<object?>(_ => CloseAppointmentDetail(), _ => HasSelectedAppointment);
            CloseAnnouncementCommand = new RelayCommand<object?>(_ => CloseAnnouncementDetail(), _ => HasSelectedAnnouncement);
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
            SelectedAppointment = null;
            SelectedAnnouncement = null;

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
            OnPropertyChanged(nameof(HasWorkAssignments));
            OnPropertyChanged(nameof(ShowWorkAssignmentsEmptyState));
            OnPropertyChanged(nameof(HasAppointments));
            OnPropertyChanged(nameof(ShowAppointmentHint));
            OnPropertyChanged(nameof(ShowAppointmentEmptyState));
            OnPropertyChanged(nameof(ShowAppointmentDetail));
            OnPropertyChanged(nameof(HasAnnouncements));
            OnPropertyChanged(nameof(ShowAnnouncementHint));
            OnPropertyChanged(nameof(ShowAnnouncementEmptyState));
            OnPropertyChanged(nameof(ShowAnnouncementDetail));
        }

        private void CloseAppointmentDetail()
        {
            SelectedAppointment = null;
            CloseAppointmentCommand.RaiseCanExecuteChanged();
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

        private void CloseAnnouncementDetail()
        {
            SelectedAnnouncement = null;
            CloseAnnouncementCommand.RaiseCanExecuteChanged();
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
