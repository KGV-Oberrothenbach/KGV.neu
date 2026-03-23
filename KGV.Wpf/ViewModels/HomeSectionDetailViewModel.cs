using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Helpers;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using KGV.Views;

namespace KGV.ViewModels
{
    public sealed class HomeSectionDetailViewModel : BaseViewModel, INavigationAware
    {
        private readonly MainWindowViewModel _mainVm;
        private readonly HomeSectionDetailContext _context;
        private string _registrationInfo;
        private bool _showRegisterButton;
        private bool _isBusy;

        public string SectionTitle => _context.SectionTitle;
        public string Title => _context.Title;
        public string Subtitle => _context.Subtitle;
        public string StartTimeText => _context.StartTimeText;
        public string EndTimeText => _context.EndTimeText;
        public string Content => _context.Content;
        public string AdditionalInfo => _context.AdditionalInfo;
        public string RegistrationInfo
        {
            get => _registrationInfo;
            private set => SetProperty(ref _registrationInfo, value);
        }
        public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);
        public bool HasStartTimeText => !string.IsNullOrWhiteSpace(StartTimeText);
        public bool HasEndTimeText => !string.IsNullOrWhiteSpace(EndTimeText);
        public bool HasContent => !string.IsNullOrWhiteSpace(Content);
        public bool HasAdditionalInfo => !string.IsNullOrWhiteSpace(AdditionalInfo);
        public bool HasRegistrationInfo => !string.IsNullOrWhiteSpace(RegistrationInfo);
        public bool IsAdminContext => _mainVm.UserContext.Role is UserRole.Admin or UserRole.Vorstand;
        public bool ShowParticipantsSection => _context.IsWorkAssignment && IsAdminContext;
        public bool HasParticipants => Participants.Count > 0;
        public bool ShowParticipantsEmptyState => ShowParticipantsSection && !HasParticipants;
        public string ParticipantsEmptyText => "Aktuell keine angemeldeten Teilnehmer.";
        public bool ShowRegisterButton
        {
            get => _showRegisterButton;
            private set => SetProperty(ref _showRegisterButton, value);
        }
        public ObservableCollection<WorkAssignmentParticipantItem> Participants { get; } = new();

        public RelayCommand<object?> ZurueckCommand { get; }
        public RelayCommand<object?> AnmeldenCommand { get; }
        public RelayCommand<object?> AddParticipantCommand { get; }
        public RelayCommand<WorkAssignmentParticipantItem> RemoveParticipantCommand { get; }

        public HomeSectionDetailViewModel(MainWindowViewModel mainVm, HomeSectionDetailContext context)
        {
            _mainVm = mainVm ?? throw new ArgumentNullException(nameof(mainVm));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _registrationInfo = context.RegistrationInfo;
            _showRegisterButton = context.ShowRegisterButton;
            ZurueckCommand = new RelayCommand<object?>(_ => _ = ZurueckAsync());
            AnmeldenCommand = new RelayCommand<object?>(_ => _ = RegisterAsync(), _ => ShowRegisterButton);
            AddParticipantCommand = new RelayCommand<object?>(_ => _ = AddParticipantAsync(), _ => ShowParticipantsSection && !_isBusy && _context.WorkAssignmentId > 0);
            RemoveParticipantCommand = new RelayCommand<WorkAssignmentParticipantItem>(participant => _ = RemoveParticipantAsync(participant), participant => ShowParticipantsSection && !_isBusy && participant?.MitgliedId > 0);
        }

        public async Task OnNavigatedToAsync()
        {
            await RefreshDetailStateAsync();
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private async Task ZurueckAsync()
        {
            var created = _mainVm.NavigateToHomeViewModel();
            if (created != null)
                await _mainVm.NavigateToAsync(created);
        }

        private async Task RegisterAsync()
        {
            if (_context.WorkAssignmentId <= 0)
            {
                MessageBox.Show(
                    "Die Anmeldung konnte nicht gestartet werden, weil dem ausgewählten Arbeitseinsatz keine gültige ID zugeordnet ist.",
                    "Anmeldung",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

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

            var result = await _mainVm.SupabaseService.SignUpForArbeitseinsatzAsync(_context.WorkAssignmentId, mitgliedId.Value);
            await RefreshDetailStateAsync(result.UpdatedItem);

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

        private async Task AddParticipantAsync()
        {
            if (_context.WorkAssignmentId <= 0)
            {
                MessageBox.Show(
                    "Der ausgewählte Arbeitseinsatz konnte nicht eindeutig geladen werden.",
                    "Teilnehmer hinzufügen",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var member = await PickMemberAsync();
            if (member == null)
                return;

            _isBusy = true;
            AddParticipantCommand.RaiseCanExecuteChanged();
            try
            {
                var result = await _mainVm.SupabaseService.SignUpForArbeitseinsatzAsync(_context.WorkAssignmentId, member.Id);
                await RefreshDetailStateAsync(result.UpdatedItem);

                MessageBox.Show(
                    result.Message,
                    "Teilnehmer hinzufügen",
                    MessageBoxButton.OK,
                    result.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);
            }
            finally
            {
                _isBusy = false;
                AddParticipantCommand.RaiseCanExecuteChanged();
            }
        }

        private async Task RefreshDetailStateAsync(HomeWorkAssignmentItem? updatedItem = null)
        {
            if (_context.IsWorkAssignment && _context.WorkAssignmentId > 0)
            {
                var item = updatedItem ?? await _mainVm.SupabaseService.GetStartseiteArbeitseinsatzByIdAsync(_context.WorkAssignmentId);
                if (item != null)
                    ApplyRegistrationUpdate(item);
            }

            if (ShowParticipantsSection && _context.WorkAssignmentId > 0)
            {
                var participants = await _mainVm.SupabaseService.GetArbeitseinsatzParticipantsAsync(_context.WorkAssignmentId);
                Participants.Clear();
                foreach (var participant in participants)
                    Participants.Add(participant);

                OnPropertyChanged(nameof(HasParticipants));
                OnPropertyChanged(nameof(ShowParticipantsEmptyState));
            }

            AnmeldenCommand.RaiseCanExecuteChanged();
            AddParticipantCommand.RaiseCanExecuteChanged();
            RemoveParticipantCommand.RaiseCanExecuteChanged();
        }

        private void ApplyRegistrationUpdate(HomeWorkAssignmentItem item)
        {
            RegistrationInfo = item.RegistrationInfo;
            ShowRegisterButton = item.CanRegister;
        }

        private Task<MemberDTO?> PickMemberAsync()
        {
            var searchVm = new MemberSearchViewModel(_mainVm.SupabaseService, _mainVm, isSelectionMode: true);
            var searchView = new MemberSearchView { DataContext = searchVm };
            var window = new Window
            {
                Title = "Mitglied suchen",
                Content = searchView,
                Owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive) ?? Application.Current?.MainWindow,
                Width = 720,
                Height = 640,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            searchVm.CloseRequested += (_, _) =>
            {
                window.DialogResult = true;
                window.Close();
            };

            var dialogResult = window.ShowDialog();
            return Task.FromResult(dialogResult == true ? searchVm.SelectionResult : null);
        }

        private async Task RemoveParticipantAsync(WorkAssignmentParticipantItem? participant)
        {
            if (participant == null || participant.MitgliedId <= 0 || _context.WorkAssignmentId <= 0)
            {
                MessageBox.Show(
                    "Der ausgewählte Teilnehmer konnte nicht eindeutig abgemeldet werden.",
                    "Teilnehmer abmelden",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var participantDisplayName = string.IsNullOrWhiteSpace(participant.DisplayName)
                ? $"Mitglied #{participant.MitgliedId}"
                : participant.DisplayName;

            var confirmation = MessageBox.Show(
                $"Soll {participantDisplayName} von diesem Arbeitseinsatz abgemeldet werden?",
                "Teilnehmer abmelden",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmation != MessageBoxResult.Yes)
                return;

            _isBusy = true;
            AddParticipantCommand.RaiseCanExecuteChanged();
            RemoveParticipantCommand.RaiseCanExecuteChanged();
            try
            {
                var result = await _mainVm.SupabaseService.SignOffFromArbeitseinsatzAsync(_context.WorkAssignmentId, participant.MitgliedId);
                await RefreshDetailStateAsync(result.UpdatedItem);

                MessageBox.Show(
                    result.Message,
                    "Teilnehmer abmelden",
                    MessageBoxButton.OK,
                    result.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);
            }
            finally
            {
                _isBusy = false;
                AddParticipantCommand.RaiseCanExecuteChanged();
                RemoveParticipantCommand.RaiseCanExecuteChanged();
            }
        }
    }
}
