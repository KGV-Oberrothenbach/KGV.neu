using CommunityToolkit.Mvvm.Input;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using KGV.Views;

namespace KGV.ViewModels
{
    public sealed class UserManagementViewModel : BaseViewModel, INavigationAware
    {
        private readonly IAuthService _authService;
        private readonly MemberDTO? _boundMember;
        private AppUserDTO? _selectedUser;
        private string _statusMessage = string.Empty;
        private bool _isBusy;

        public string Title => "Benutzerverwaltung";
        public string Description => IsBoundToMember
            ? "Verwaltet den Appuser des aktuell ausgewählten Mitglieds. Nutzer hinzufügen und Nutzer entfernen beziehen sich ausschließlich auf diesen Datensatz."
            : "Lädt App-User-/Mitgliedszuordnungen und bietet die produktiven Auth-Admin-Aktionen für Einladung, Erstlogin und Passwort-Reset an.";
        public string AdminHint => "Einladungen und Erstlogin laufen über E-Mail + OTP + Passwort-Neusetzen. Eine E-Mail-Änderung bleibt ein separater codebasierter Flow und ist weiterhin nur für das aktuell angemeldete Konto belastbar anschließbar.";
        public bool IsBoundToMember => _boundMember != null;
        public bool HasBoundMember => _boundMember?.Id > 0;
        public string BoundMemberDisplayName => _boundMember == null
            ? ""
            : string.IsNullOrWhiteSpace(_boundMember.DisplayName)
                ? $"Mitglied #{_boundMember.Id}"
                : _boundMember.DisplayName;
        public string BoundMemberInfo => _boundMember == null
            ? "Es wurde kein Mitglied ausgewählt."
            : $"Ausgewähltes Mitglied: {BoundMemberDisplayName} (ID: {_boundMember.Id})";

        public ObservableCollection<AppUserDTO> Users { get; } = new();

        public AppUserDTO? SelectedUser
        {
            get => _selectedUser;
            set
            {
                if (!SetProperty(ref _selectedUser, value))
                    return;

                OnPropertyChanged(nameof(ShowInviteAction));
                OnPropertyChanged(nameof(CanChangeSelectedEmail));
                OnPropertyChanged(nameof(CanRemoveUser));
                InviteCommand.NotifyCanExecuteChanged();
                RemoveUserCommand.NotifyCanExecuteChanged();
                ChangeEmailCommand.NotifyCanExecuteChanged();
                ResetPasswordCommand.NotifyCanExecuteChanged();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (!SetProperty(ref _isBusy, value))
                    return;

                RefreshCommand.NotifyCanExecuteChanged();
                InviteCommand.NotifyCanExecuteChanged();
                RemoveUserCommand.NotifyCanExecuteChanged();
                ChangeEmailCommand.NotifyCanExecuteChanged();
                ResetPasswordCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(ShowInviteAction));
                OnPropertyChanged(nameof(CanRemoveUser));
            }
        }

        public bool CanChangeSelectedEmail =>
            TargetUser?.AuthUserId?.ToString().Equals(_authService.CurrentUserId, StringComparison.OrdinalIgnoreCase) == true;
        public bool ShowInviteAction => !IsBusy && TargetUser != null && !string.IsNullOrWhiteSpace(TargetUser.Email) && TargetUser.AuthUserId == null && HasBoundMember;
        public bool CanRemoveUser => !IsBusy && TargetUser?.AuthUserId != null && HasBoundMember;

        public IAsyncRelayCommand RefreshCommand { get; }
        public IAsyncRelayCommand InviteCommand { get; }
        public IAsyncRelayCommand RemoveUserCommand { get; }
        public IAsyncRelayCommand ChangeEmailCommand { get; }
        public IAsyncRelayCommand ResetPasswordCommand { get; }

        private AppUserDTO? TargetUser => IsBoundToMember ? Users.Count > 0 ? Users[0] : null : SelectedUser;

        public UserManagementViewModel(IAuthService authService, MemberDTO? boundMember = null)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _boundMember = boundMember?.Clone();

            RefreshCommand = new AsyncRelayCommand(LoadAsync, () => !IsBusy);
            InviteCommand = new AsyncRelayCommand(SendInviteAsync, () => ShowInviteAction);
            RemoveUserCommand = new AsyncRelayCommand(RemoveUserAsync, () => CanRemoveUser);
            ChangeEmailCommand = new AsyncRelayCommand(OpenChangeEmailAsync, () => !IsBusy && TargetUser != null && CanChangeSelectedEmail);
            ResetPasswordCommand = new AsyncRelayCommand(OpenResetPasswordAsync, () => !IsBusy && TargetUser != null && !string.IsNullOrWhiteSpace(TargetUser.Email));
        }

        public async Task OnNavigatedToAsync()
        {
            await LoadAsync();
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private async Task LoadAsync()
        {
            IsBusy = true;
            StatusMessage = string.Empty;

            try
            {
                var users = await _authService.GetAppUsersAsync();

                var filteredUsers = users;
                if (HasBoundMember)
                    filteredUsers = users.FindAll(x => x.MitgliedId == _boundMember!.Id);

                if (HasBoundMember && filteredUsers.Count == 0)
                    filteredUsers.Add(CreatePlaceholderUser());

                Users.Clear();
                foreach (var user in filteredUsers)
                    Users.Add(user);

                if (HasBoundMember)
                {
                    SelectedUser = Users.Count > 0 ? Users[0] : null;
                }
                else if (SelectedUser != null)
                {
                    SelectedUser = Users.Count == 0
                        ? null
                        : FindMatchingUser(SelectedUser.AuthUserId, SelectedUser.MitgliedId, SelectedUser.Email);
                }

                StatusMessage = BuildStatusMessage();
                OnPropertyChanged(nameof(ShowInviteAction));
                OnPropertyChanged(nameof(CanRemoveUser));
                InviteCommand.NotifyCanExecuteChanged();
                RemoveUserCommand.NotifyCanExecuteChanged();
            }
            catch
            {
                StatusMessage = "Benutzerverwaltung konnte aktuell nicht geladen werden.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task OpenChangeEmailAsync()
        {
            var targetUser = TargetUser;
            if (targetUser == null)
                return;

            var vm = new ChangeEmailViewModel(_authService, targetUser.Email, targetUser.AuthUserId?.ToString() == _authService.CurrentUserId);
            var window = new ChangeEmailWindow(vm)
            {
                Owner = Application.Current?.MainWindow
            };

            window.ShowDialog();
            await LoadAsync();
        }

        private async Task SendInviteAsync()
        {
            if (!HasBoundMember)
            {
                StatusMessage = "Bitte zuerst ein Mitglied auswählen, bevor ein Appuser hinzugefügt wird.";
                return;
            }

            var targetUser = TargetUser;
            if (targetUser == null)
                return;

            IsBusy = true;
            StatusMessage = string.Empty;

            try
            {
                var result = await _authService.InviteUserAsync(targetUser);
                StatusMessage = result.Message ?? (result.Success ? "Einladung angestoßen." : "Einladung fehlgeschlagen.");
                await LoadAsync();
            }
            catch
            {
                StatusMessage = "Nutzer hinzufügen fehlgeschlagen. Bitte später erneut versuchen.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RemoveUserAsync()
        {
            if (!HasBoundMember)
            {
                StatusMessage = "Bitte zuerst ein Mitglied auswählen, bevor ein Appuser entfernt wird.";
                return;
            }

            var targetUser = TargetUser;
            if (targetUser?.AuthUserId == null)
            {
                StatusMessage = "Für das ausgewählte Mitglied ist aktuell kein Appuser verknüpft.";
                return;
            }

            var confirmation = MessageBox.Show(
                $"Soll der Appuser für {BoundMemberDisplayName} entfernt werden?",
                "Nutzer entfernen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmation != MessageBoxResult.Yes)
                return;

            IsBusy = true;
            StatusMessage = string.Empty;

            try
            {
                var removed = await _authService.RemoveUserAsync(targetUser);
                StatusMessage = removed
                    ? "Der Appuser des ausgewählten Mitglieds wurde entfernt."
                    : "Der Appuser konnte aktuell nicht entfernt werden.";

                await LoadAsync();
            }
            catch
            {
                StatusMessage = "Nutzer entfernen fehlgeschlagen. Bitte später erneut versuchen.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task OpenResetPasswordAsync()
        {
            var targetUser = TargetUser;
            if (targetUser == null)
                return;

            var vm = new ResetPasswordViewModel(_authService, targetUser.Email);
            var window = new ResetPasswordWindow(vm)
            {
                Owner = Application.Current?.MainWindow
            };

            window.ShowDialog();
            await LoadAsync();
        }

        private AppUserDTO? FindMatchingUser(Guid? authUserId, int? mitgliedId, string? email)
        {
            foreach (var user in Users)
            {
                if (authUserId.HasValue && user.AuthUserId == authUserId)
                    return user;

                if (mitgliedId.HasValue && user.MitgliedId == mitgliedId)
                    return user;

                if (!string.IsNullOrWhiteSpace(email) && string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
                    return user;
            }

            return null;
        }

        private AppUserDTO CreatePlaceholderUser()
        {
            return new AppUserDTO
            {
                MitgliedId = _boundMember?.Id,
                DisplayName = BoundMemberDisplayName,
                Email = _boundMember?.Email ?? string.Empty,
                Role = _boundMember?.Role ?? string.Empty,
                Aktiv = true
            };
        }

        private string BuildStatusMessage()
        {
            if (!HasBoundMember)
                return Users.Count == 0
                    ? "Keine belastbar ableitbaren Benutzer-/Mitgliedszuordnungen gefunden."
                    : $"{Users.Count} Benutzer-/Mitgliedseinträge geladen.";

            var targetUser = TargetUser;
            if (targetUser == null)
                return "Für das ausgewählte Mitglied konnte kein Benutzerkontext geladen werden.";

            return targetUser.AuthUserId.HasValue
                ? "Appuser-Zuordnung für das ausgewählte Mitglied geladen."
                : "Für das ausgewählte Mitglied besteht aktuell noch kein Appuser. Über 'Nutzer hinzufügen' kann der produktive Einladungs-/Erstlogin-Pfad gestartet werden.";
        }
    }
}
