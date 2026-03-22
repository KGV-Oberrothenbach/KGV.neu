using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace KGV.Maui.ViewModels;

public sealed class UserManagementViewModel : INotifyPropertyChanged
{
    private readonly IAuthService _authService;
    private readonly ISupabaseService _supabaseService;
    private AppUserDTO? _selectedUser;
    private string _selectedRole = UserRoles.User;
    private string _statusMessage = string.Empty;
    private bool _isBusy;

    public UserManagementViewModel(IAuthService authService, ISupabaseService supabaseService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<AppUserDTO> Users { get; } = new();
    public ObservableCollection<string> Roles { get; } = new(UserRoles.AssignableRoles);

    public string Title => "Benutzerverwaltung";
    public string Description => "Lädt App-User-/Mitgliedszuordnungen und bietet die produktiven Auth-Admin-Aktionen für Einladung, Erstlogin und Passwort-Reset auch mobil an.";
    public string AdminHint => "Einladungen und Passwort-Reset laufen über denselben OTP-/Recovery-Hauptweg wie in WPF. Die E-Mail-Änderung bleibt weiterhin nur für das aktuell angemeldete Konto belastbar und wird mobil deshalb nur in diesem Fall angeboten.";
    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);
    public bool CanInvite => !IsBusy && SelectedUser != null && !string.IsNullOrWhiteSpace(SelectedUser.Email);
    public bool CanResetPassword => !IsBusy && SelectedUser != null && !string.IsNullOrWhiteSpace(SelectedUser.Email);
    public bool CanChangeSelectedEmail => !IsBusy && SelectedUser?.AuthUserId?.ToString().Equals(_authService.CurrentUserId, StringComparison.OrdinalIgnoreCase) == true;
    public bool IsRoleEditable => SelectedUser?.MitgliedId is > 0 and not 7;
    public bool CanSaveRole => !IsBusy && _authService.IsAdmin && IsRoleEditable && SelectedUser != null && !string.Equals(SelectedRole, NormalizeRole(SelectedUser.Role), StringComparison.OrdinalIgnoreCase);

    public string SelectedRole
    {
        get => _selectedRole;
        set
        {
            var normalized = NormalizeRole(value);
            if (string.Equals(_selectedRole, normalized, StringComparison.Ordinal))
                return;

            _selectedRole = normalized;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanSaveRole));
        }
    }

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
            OnPropertyChanged(nameof(IsRoleEditable));
            _ = LoadSelectedRoleAsync(value);
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

    public async Task<bool> SaveRoleAsync()
    {
        if (!CanSaveRole || SelectedUser?.MitgliedId is not > 0)
            return false;

        var userId = _authService.CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            StatusMessage = "Nicht angemeldet. Bitte erneut einloggen.";
            return false;
        }

        IsBusy = true;
        StatusMessage = string.Empty;
        var lockAcquired = false;

        try
        {
            var memberId = SelectedUser.MitgliedId.Value;
            lockAcquired = await _supabaseService.TryLockMitgliedAsync(memberId, userId);
            if (!lockAcquired)
            {
                StatusMessage = "Datensatz ist aktuell gesperrt. Bitte später erneut versuchen.";
                return false;
            }

            var rec = await _supabaseService.GetMitgliedByIdAsync(memberId);
            if (rec == null)
            {
                StatusMessage = "Mitglied konnte nicht geladen werden.";
                return false;
            }

            var dto = new MemberDTO
            {
                Id = rec.Id,
                Vorname = rec.Vorname ?? string.Empty,
                Nachname = rec.Name ?? string.Empty,
                Email = rec.Email ?? string.Empty,
                Role = SelectedRole,
                Geburtsdatum = rec.Geburtsdatum,
                Strasse = rec.Adresse ?? string.Empty,
                PLZ = rec.Plz ?? string.Empty,
                Ort = rec.Ort ?? string.Empty,
                Telefon = rec.Telefon ?? string.Empty,
                Mobilnummer = rec.Handy ?? string.Empty,
                Bemerkungen = rec.Bemerkung ?? string.Empty,
                WhatsappEinwilligung = rec.WhatsappEinwilligung,
                MitgliedSeit = rec.MitgliedSeit,
                MitgliedEnde = rec.MitgliedEnde
            };

            var ok = await _supabaseService.UpdateMitgliedAsync(dto, userId);
            if (!ok)
            {
                StatusMessage = "Speichern fehlgeschlagen (ggf. Lock verloren oder keine Berechtigung).";
                return false;
            }

            StatusMessage = "Rolle gespeichert.";
            await LoadAsync(reselectSelected: SelectedUser);
            return true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Fehler beim Speichern: {ex.Message}";
            return false;
        }
        finally
        {
            if (lockAcquired)
                await _supabaseService.ReleaseLockMitgliedAsync(SelectedUser!.MitgliedId!.Value, userId!, force: false);

            IsBusy = false;
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

            if (SelectedUser == null)
                SelectedRole = UserRoles.User;

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

    private async Task LoadSelectedRoleAsync(AppUserDTO? user)
    {
        if (user?.MitgliedId is not > 0)
        {
            SelectedRole = UserRoles.User;
            return;
        }

        try
        {
            var member = await _supabaseService.GetMitgliedByIdAsync(user.MitgliedId.Value);
            if (SelectedUser?.MitgliedId != user.MitgliedId)
                return;

            SelectedRole = NormalizeRole(member?.Role ?? user.Role);
        }
        catch
        {
            if (SelectedUser?.MitgliedId == user.MitgliedId)
                SelectedRole = NormalizeRole(user.Role);
        }
    }

    private static string NormalizeRole(string? role)
    {
        return UserRoles.Parse(role) switch
        {
            UserRole.Admin => UserRoles.Admin,
            UserRole.Vorstand => UserRoles.Vorstand,
            _ => UserRoles.User
        };
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
