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
        private AppUserDTO? _selectedUser;
        private string _statusMessage = string.Empty;
        private bool _isBusy;

        public string Title => "Benutzerverwaltung";
        public string Description => "Lädt die aktuell belastbar ableitbaren App-User-/Mitgliedszuordnungen und bietet die ersten Auth-Admin-Aktionen an.";
        public string AdminHint => "Passwort-Reset kann für die ausgewählte E-Mail angestoßen werden. Eine E-Mail-Änderung ist im aktuellen Wiederaufbau nur für den aktuell angemeldeten Benutzer belastbar anschließbar.";

        public ObservableCollection<AppUserDTO> Users { get; } = new();

        public AppUserDTO? SelectedUser
        {
            get => _selectedUser;
            set
            {
                if (!SetProperty(ref _selectedUser, value))
                    return;

                OnPropertyChanged(nameof(CanChangeSelectedEmail));
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
                ChangeEmailCommand.NotifyCanExecuteChanged();
                ResetPasswordCommand.NotifyCanExecuteChanged();
            }
        }

        public bool CanChangeSelectedEmail =>
            SelectedUser?.AuthUserId?.ToString().Equals(_authService.CurrentUserId, StringComparison.OrdinalIgnoreCase) == true;

        public IAsyncRelayCommand RefreshCommand { get; }
        public IAsyncRelayCommand ChangeEmailCommand { get; }
        public IAsyncRelayCommand ResetPasswordCommand { get; }

        public UserManagementViewModel(IAuthService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            RefreshCommand = new AsyncRelayCommand(LoadAsync, () => !IsBusy);
            ChangeEmailCommand = new AsyncRelayCommand(OpenChangeEmailAsync, () => !IsBusy && SelectedUser != null && CanChangeSelectedEmail);
            ResetPasswordCommand = new AsyncRelayCommand(OpenResetPasswordAsync, () => !IsBusy && SelectedUser != null && !string.IsNullOrWhiteSpace(SelectedUser.Email));
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

                Users.Clear();
                foreach (var user in users)
                    Users.Add(user);

                if (SelectedUser != null)
                {
                    SelectedUser = Users.Count == 0
                        ? null
                        : FindMatchingUser(SelectedUser.AuthUserId, SelectedUser.MitgliedId, SelectedUser.Email);
                }

                StatusMessage = Users.Count == 0
                    ? "Keine belastbar ableitbaren Benutzer-/Mitgliedszuordnungen gefunden."
                    : $"{Users.Count} Benutzer-/Mitgliedseinträge geladen.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Benutzerverwaltung konnte nicht geladen werden: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task OpenChangeEmailAsync()
        {
            if (SelectedUser == null)
                return;

            var vm = new ChangeEmailViewModel(_authService, SelectedUser.Email, SelectedUser.AuthUserId?.ToString() == _authService.CurrentUserId);
            var window = new ChangeEmailWindow(vm)
            {
                Owner = Application.Current?.MainWindow
            };

            window.ShowDialog();
            await LoadAsync();
        }

        private async Task OpenResetPasswordAsync()
        {
            if (SelectedUser == null)
                return;

            var vm = new ResetPasswordViewModel(_authService, SelectedUser.Email);
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
    }
}
