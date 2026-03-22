using KGV.Core.Interfaces;
using KGV.Core.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace KGV.Maui.ViewModels;

public sealed class UserManagementViewModel : INotifyPropertyChanged
{
    private readonly IAuthService _authService;
    private AppUserDTO? _selectedUser;
    private string _statusMessage = string.Empty;
    private bool _isBusy;

    public UserManagementViewModel(IAuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<AppUserDTO> Users { get; } = new();

    public string Title => "Benutzerverwaltung";
    public string Description => "Lädt App-User-/Mitgliedszuordnungen und bietet die produktiven Auth-Admin-Aktionen für Einladung, Erstlogin und Passwort-Reset auch mobil an.";
    public string AdminHint => "Einladungen und Passwort-Reset laufen über denselben OTP-/Recovery-Hauptweg wie in WPF. Die E-Mail-Änderung bleibt weiterhin nur für das aktuell angemeldete Konto belastbar und wird mobil deshalb nur in diesem Fall angeboten.";
    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);
    public bool CanInvite => !IsBusy && SelectedUser != null && !string.IsNullOrWhiteSpace(SelectedUser.Email);
    public bool CanResetPassword => !IsBusy && SelectedUser != null && !string.IsNullOrWhiteSpace(SelectedUser.Email);
    public bool CanChangeSelectedEmail => !IsBusy && SelectedUser?.AuthUserId?.ToString().Equals(_authService.CurrentUserId, StringComparison.OrdinalIgnoreCase) == true;

    public AppUserDTO? SelectedUser
    {
        get => _selectedUser;
        set
        {
            if (_selectedUser == value)
                return;

            _selectedUser = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanInvite));
            OnPropertyChanged(nameof(CanResetPassword));
            OnPropertyChanged(nameof(CanChangeSelectedEmail));
            OnPropertyChanged(nameof(HasSelectedUser));
        }
    }

    public bool HasSelectedUser => SelectedUser != null;

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (string.Equals(_statusMessage, value, StringComparison.Ordinal))
                return;

            _statusMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasStatusMessage));
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy == value)
                return;

            _isBusy = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanInvite));
            OnPropertyChanged(nameof(CanResetPassword));
            OnPropertyChanged(nameof(CanChangeSelectedEmail));
        }
    }

    public async Task InitializeAsync()
    {
        if (Users.Count > 0)
            return;

        await LoadAsync();
    }

    public async Task RefreshAsync()
    {
        await LoadAsync();
    }

    public async Task<bool> InviteAsync()
    {
        if (SelectedUser == null)
            return false;

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            var result = await _authService.InviteUserAsync(SelectedUser);
            StatusMessage = result.Message ?? (result.Success ? "Einladung angestoßen." : "Einladung fehlgeschlagen.");
            await LoadAsync(reselectSelected: SelectedUser);
            return result.Success;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Einladung fehlgeschlagen: {ex.Message}";
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<bool> SendPasswordResetAsync()
    {
        if (SelectedUser == null || string.IsNullOrWhiteSpace(SelectedUser.Email))
            return false;

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            var success = await _authService.SendPasswordResetEmailAsync(SelectedUser.Email.Trim());
            StatusMessage = success
                ? "OTP-Code für Passwort-vergessen wurde versendet. Die Codeeingabe erfolgt weiterhin im Login."
                : "Passwort-Reset konnte nicht angestoßen werden.";
            return success;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Passwort-Reset fehlgeschlagen: {ex.Message}";
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<bool> RequestEmailChangeAsync(string newEmail)
    {
        if (!CanChangeSelectedEmail)
            return false;

        var trimmed = (newEmail ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            StatusMessage = "Bitte eine neue E-Mail-Adresse eingeben.";
            return false;
        }

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            var success = await _authService.RequestEmailChangeAsync(trimmed);
            StatusMessage = success
                ? "OTP-Code wurde an die neue E-Mail-Adresse gesendet."
                : "E-Mail-Änderung konnte nicht angestoßen werden.";
            return success;
        }
        catch (Exception ex)
        {
            StatusMessage = $"E-Mail-Änderung fehlgeschlagen: {ex.Message}";
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<bool> VerifyEmailChangeAsync(string newEmail, string otpCode)
    {
        if (!CanChangeSelectedEmail)
            return false;

        var trimmedEmail = (newEmail ?? string.Empty).Trim();
        var trimmedCode = (otpCode ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmedEmail) || string.IsNullOrWhiteSpace(trimmedCode))
        {
            StatusMessage = "Neue E-Mail-Adresse und OTP-Code sind erforderlich.";
            return false;
        }

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            var success = await _authService.VerifyEmailChangeOtpAsync(trimmedEmail, trimmedCode);
            StatusMessage = success
                ? "Mailadresse erfolgreich geändert."
                : "OTP-Code konnte nicht bestätigt werden.";

            if (success)
                await LoadAsync(reselectSelected: SelectedUser);

            return success;
        }
        catch (Exception ex)
        {
            StatusMessage = $"OTP-Prüfung fehlgeschlagen: {ex.Message}";
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadAsync(AppUserDTO? reselectSelected = null)
    {
        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            var users = await _authService.GetAppUsersAsync();
            Users.Clear();
            foreach (var user in users)
                Users.Add(user);

            SelectedUser = reselectSelected == null
                ? null
                : FindMatchingUser(reselectSelected.AuthUserId, reselectSelected.MitgliedId, reselectSelected.Email);

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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
