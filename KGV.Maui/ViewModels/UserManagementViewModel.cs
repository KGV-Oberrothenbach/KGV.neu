using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Maui.State;
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
    private readonly MemberContextState _memberContextState;
    private readonly UserContextState _userContextState;
    private AppUserDTO? _selectedUser;
    private string _selectedRole = UserRoles.User;
    private string _statusMessage = string.Empty;
    private bool _isBusy;

    public UserManagementViewModel(IAuthService authService, ISupabaseService supabaseService, MemberContextState memberContextState, UserContextState userContextState)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        _memberContextState = memberContextState ?? throw new ArgumentNullException(nameof(memberContextState));
        _userContextState = userContextState ?? throw new ArgumentNullException(nameof(userContextState));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<AppUserDTO> Users { get; } = new();
    public ObservableCollection<string> Roles { get; } = new(UserRoles.AssignableRoles);

    private MemberDTO? BoundMember => _memberContextState.SelectedMember;
    private AppUserDTO? TargetUser => IsBoundToMember ? (Users.Count > 0 ? Users[0] : null) : SelectedUser;

    public bool IsBoundToMember => BoundMember?.Id > 0;
    public string BoundMemberDisplayName => BoundMember == null
        ? string.Empty
        : string.IsNullOrWhiteSpace(BoundMember.DisplayName)
            ? $"Mitglied #{BoundMember.Id}"
            : BoundMember.DisplayName;
    public string BoundMemberInfo => BoundMember == null
        ? "Es wurde kein Mitglied ausgewählt."
        : $"Ausgewähltes Mitglied: {BoundMemberDisplayName} (ID: {BoundMember.Id})";

    public string Title => "Benutzerverwaltung";
    public string Description => IsBoundToMember
        ? "Verwaltet den Appuser des aktuell ausgewählten Mitglieds. Rollenbearbeitung, Passwort-Reset und Nutzer entfernen bleiben damit im mobilen Mitgliedskontext gebunden."
        : "Lädt App-User-/Mitgliedszuordnungen und bietet die produktiven Auth-Admin-Aktionen für Einladung, Erstlogin und Passwort-Reset auch mobil an.";
    public string AdminHint => IsBoundToMember
        ? "Im gebundenen Mitgliedskontext bleibt 'Nutzer hinzufügen' sichtbar im Stammdatenpfad. Mailänderungen bei vorhandenem App-User/Auth-User erfolgen nur durch den Nutzer selbst über den vorgesehenen Self-Service-Mailänderungsweg."
        : "Einladungen und Passwort-Reset laufen über denselben OTP-/Recovery-Hauptweg wie in WPF. Die E-Mail-Änderung bleibt weiterhin nur für das aktuell angemeldete Konto belastbar und wird mobil deshalb nur in diesem Fall angeboten.";
    public string InviteActionText => "Nutzer hinzufügen";
    public string EmailChangeHint => CanChangeSelectedEmail
        ? "Für das aktuell angemeldete Konto steht hier der bestehende Self-Service-Mailänderungsweg zur Verfügung."
        : IsBoundToMember
            ? "Bei vorhandenem App-User/Auth-User kann die Mailadresse im normalen Mitgliedskontext nicht direkt geändert werden. Der Nutzer muss dafür selbst den vorgesehenen Self-Service-Mailänderungsweg verwenden."
            : "Mailänderungen sind nur für das aktuell angemeldete Konto verfügbar und laufen über den bestehenden Self-Service-Mailänderungsweg.";
    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);
    public bool HasSelectedUser => TargetUser != null;
    public bool HasLinkedUser => TargetUser?.AuthUserId != null;
    public bool ShowInviteAction => TargetUser != null && !string.IsNullOrWhiteSpace(TargetUser.Email) && !HasLinkedUser;
    public bool CanInvite => !IsBusy && ShowInviteAction && (!IsBoundToMember || BoundMember != null);
    public bool CanResetPassword => !IsBusy && TargetUser != null && !string.IsNullOrWhiteSpace(TargetUser.Email);
    public bool CanChangeSelectedEmail => !IsBusy && TargetUser?.AuthUserId?.ToString().Equals(_authService.CurrentUserId, StringComparison.OrdinalIgnoreCase) == true;
    public bool CanRemoveUser => !IsBusy && IsBoundToMember && TargetUser?.AuthUserId != null;
    public bool IsRoleEditable => TargetUser?.MitgliedId is > 0 and not 7;
    public bool CanManageRoleManagement => PermissionChecks.CanManageRoleManagement(_userContextState.CurrentUserContext);
    public bool CanEditRole => CanManageRoleManagement && IsRoleEditable;
    public bool CanSaveRole => !IsBusy && CanEditRole && HasLinkedUser && TargetUser != null && !string.Equals(SelectedRole, NormalizeRole(TargetUser.Role), StringComparison.OrdinalIgnoreCase);

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
            OnPropertyChanged(nameof(ShowInviteAction));
            OnPropertyChanged(nameof(HasLinkedUser));
            OnPropertyChanged(nameof(InviteActionText));
            OnPropertyChanged(nameof(CanResetPassword));
            OnPropertyChanged(nameof(CanChangeSelectedEmail));
            OnPropertyChanged(nameof(HasSelectedUser));
            OnPropertyChanged(nameof(IsRoleEditable));
            OnPropertyChanged(nameof(CanEditRole));
            OnPropertyChanged(nameof(CanRemoveUser));
            OnPropertyChanged(nameof(EmailChangeHint));
            _ = LoadSelectedRoleAsync(value);
        }
    }

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
        if (!CanSaveRole || TargetUser?.MitgliedId is not > 0)
            return false;

        if (!HasLinkedUser)
        {
            StatusMessage = "Für das ausgewählte Mitglied existiert aktuell kein verknüpfter App-User. Die Rolle kann deshalb noch nicht über app_user.role gespeichert werden.";
            return false;
        }

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            var memberId = TargetUser.MitgliedId.Value;
            var ok = await _supabaseService.SetAppUserRoleAsync(memberId, SelectedRole);
            if (!ok)
            {
                StatusMessage = "Die Rolle konnte aktuell nicht über app_user.role gespeichert werden.";
                return false;
            }

            TargetUser!.Role = SelectedRole;
            if (_memberContextState.SelectedMember?.Id == memberId)
            {
                var updatedMember = _memberContextState.SelectedMember.Clone();
                updatedMember.Role = SelectedRole;
                _memberContextState.SetSelectedMember(updatedMember);
            }

            StatusMessage = "Rolle gespeichert.";
            await LoadAsync(reselectSelected: TargetUser);
            return true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Fehler beim Speichern: {ex.Message}";
            return false;
        }
        finally
        {
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
            OnPropertyChanged(nameof(ShowInviteAction));
            OnPropertyChanged(nameof(HasLinkedUser));
            OnPropertyChanged(nameof(CanResetPassword));
            OnPropertyChanged(nameof(CanChangeSelectedEmail));
            OnPropertyChanged(nameof(CanRemoveUser));
            OnPropertyChanged(nameof(CanSaveRole));
            OnPropertyChanged(nameof(CanManageRoleManagement));
            OnPropertyChanged(nameof(CanEditRole));
            OnPropertyChanged(nameof(EmailChangeHint));
        }
    }

    public async Task InitializeAsync()
    {
        await LoadAsync(reselectSelected: TargetUser);
    }

    public async Task RefreshAsync()
    {
        await LoadAsync(reselectSelected: TargetUser);
    }

    public async Task<bool> InviteAsync()
    {
        var targetUser = TargetUser;
        if (targetUser == null)
            return false;

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            var result = await _authService.InviteUserAsync(targetUser);
            StatusMessage = result.Message ?? (result.Success ? "Einladung angestoßen." : "Einladung fehlgeschlagen.");
            await LoadAsync(reselectSelected: targetUser);
            return result.Success;
        }
        catch (Exception)
        {
            StatusMessage = "Nutzer hinzufügen fehlgeschlagen. Bitte später erneut versuchen.";
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<bool> RemoveUserAsync()
    {
        var targetUser = TargetUser;
        if (targetUser?.AuthUserId == null)
        {
            StatusMessage = "Für das ausgewählte Mitglied ist aktuell kein Appuser verknüpft.";
            return false;
        }

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            var removed = await _authService.RemoveUserAsync(targetUser);
            StatusMessage = removed
                ? "Der Appuser des ausgewählten Mitglieds wurde entfernt."
                : "Der Appuser konnte aktuell nicht entfernt werden.";
            await LoadAsync();
            return removed;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Nutzer entfernen fehlgeschlagen: {ex.Message}";
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<bool> SendPasswordResetAsync()
    {
        var targetUser = TargetUser;
        if (targetUser == null || string.IsNullOrWhiteSpace(targetUser.Email))
            return false;

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            var success = await _authService.SendPasswordResetEmailAsync(targetUser.Email.Trim());
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
                await LoadAsync(reselectSelected: TargetUser);

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
            var filteredUsers = users;
            if (IsBoundToMember)
                filteredUsers = users.FindAll(x => x.MitgliedId == BoundMember!.Id);

            if (IsBoundToMember && filteredUsers.Count == 0)
                filteredUsers.Add(CreatePlaceholderUser());

            Users.Clear();
            foreach (var user in filteredUsers)
                Users.Add(user);

            if (IsBoundToMember)
            {
                SelectedUser = Users.Count > 0 ? Users[0] : null;
            }
            else
            {
                SelectedUser = reselectSelected == null
                    ? null
                    : FindMatchingUser(reselectSelected.AuthUserId, reselectSelected.MitgliedId, reselectSelected.Email);
            }

            if (TargetUser == null)
                SelectedRole = UserRoles.User;

            StatusMessage = BuildStatusMessage();
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

    private AppUserDTO CreatePlaceholderUser()
    {
        return new AppUserDTO
        {
            MitgliedId = BoundMember?.Id,
            DisplayName = BoundMemberDisplayName,
            Email = BoundMember?.Email ?? string.Empty,
                Role = UserRoles.User,
            Aktiv = true
        };
    }

    private async Task LoadSelectedRoleAsync(AppUserDTO? user)
    {
        var target = IsBoundToMember ? TargetUser : user;
        if (target?.MitgliedId is not > 0)
        {
            SelectedRole = UserRoles.User;
            return;
        }

        try
        {
            var settings = await _supabaseService.GetUserPermissionSettingsAsync(target.MitgliedId.Value);
            if (TargetUser?.MitgliedId != target.MitgliedId)
                return;

            SelectedRole = NormalizeRole(settings?.Role ?? target.Role);
        }
        catch
        {
            if (TargetUser?.MitgliedId == target.MitgliedId)
                SelectedRole = NormalizeRole(target.Role);
        }
    }

    private string BuildStatusMessage()
    {
        if (!IsBoundToMember)
            return Users.Count == 0
                ? "Keine belastbar ableitbaren Benutzer-/Mitgliedszuordnungen gefunden."
                : $"{Users.Count} Benutzer-/Mitgliedseinträge geladen.";

        var targetUser = TargetUser;
        if (targetUser == null)
            return "Für das ausgewählte Mitglied konnte kein Benutzerkontext geladen werden.";

        return targetUser.AuthUserId.HasValue
            ? "Appuser-Zuordnung für das ausgewählte Mitglied geladen."
                : "Für das ausgewählte Mitglied besteht aktuell noch kein Appuser. Der produktive Flow 'Nutzer hinzufügen' bleibt sichtbar im Stammdatenpfad des ausgewählten Mitglieds.";
    }

    private static MemberDTO MapMember(MitgliedRecord rec)
    {
        return new MemberDTO
        {
            Id = rec.Id,
            Vorname = rec.Vorname ?? string.Empty,
            Nachname = rec.Name ?? string.Empty,
            Email = rec.Email ?? string.Empty,
            Telefon = rec.Telefon ?? string.Empty,
            Mobilnummer = rec.Handy ?? string.Empty,
            Strasse = rec.Adresse ?? string.Empty,
            PLZ = rec.Plz ?? string.Empty,
            Ort = rec.Ort ?? string.Empty,
            Geburtsdatum = rec.Geburtsdatum,
            MitgliedSeit = rec.MitgliedSeit,
            MitgliedEnde = rec.MitgliedEnde,
            Role = rec.Role ?? string.Empty
        };
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
