using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Helpers;

namespace KGV.ViewModels
{
    public sealed class AdminRoleViewModel : BaseViewModel, INavigationAware
    {
        private readonly ISupabaseService _supabaseService;
        private readonly IAuthService _authService;
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly INavigationService _navigationService;

        private string? _lockUserId;
        private bool _allowUserMeterReadingSubmissions;
        private bool _initialAllowUserMeterReadingSubmissions;
        private UserPermissionSettings? _permissionSettings;
        private PermissionFlags _initialGrantedPermissions;
        private PermissionFlags _initialRevokedPermissions;

        public MemberDTO SelectedMember { get; }

        public ObservableCollection<string> Roles { get; } = new(UserRoles.AssignableRoles);
        public ObservableCollection<UserPermissionOverrideItemViewModel> PermissionOverrides { get; } = new();

        private string _selectedRole = "user";
        public string SelectedRole
        {
            get => _selectedRole;
            set
            {
                if (SetProperty(ref _selectedRole, value ?? "user"))
                {
                    IsDirty = true;
                    SaveCommand.RaiseCanExecuteChanged();
                    RebuildPermissionOverridesForCurrentRole();
                }
            }
        }

        private bool _isDirty;
        public bool IsDirty
        {
            get => _isDirty;
            private set => SetProperty(ref _isDirty, value);
        }

        public bool IsRoleEditable => SelectedMember.Id != 7;
        public bool CanReadRoleManagement => PermissionChecks.CanReadRoleManagement(_mainWindowViewModel.UserContext);
        public bool CanManageRoleManagement => PermissionChecks.CanManageRoleManagement(_mainWindowViewModel.UserContext);
        public bool CanEditRole => CanManageRoleManagement && IsRoleEditable;
        public bool IsRoleManagementReadOnly => CanReadRoleManagement && !CanManageRoleManagement;
        public bool CanOpenUserManagement => _authService.IsAdmin && SelectedMember.Id > 0;
        public bool CanManageUserMeterReadingSubmissions => _authService.IsAdmin || _authService.IsVorstand;

        public bool AllowUserMeterReadingSubmissions
        {
            get => _allowUserMeterReadingSubmissions;
            set
            {
                if (!SetProperty(ref _allowUserMeterReadingSubmissions, value))
                    return;

                OnPropertyChanged(nameof(IsUserMeterReadingSubmissionSettingDirty));
                SaveCommand.RaiseCanExecuteChanged();
            }
        }

        public bool IsUserMeterReadingSubmissionSettingDirty => AllowUserMeterReadingSubmissions != _initialAllowUserMeterReadingSubmissions;

        public bool HasLinkedAppUser => _permissionSettings?.HasLinkedUser == true;
        public bool CanEditPermissionOverrides => CanManageRoleManagement && HasLinkedAppUser;
        public PermissionFlags CurrentGrantedPermissions => PermissionOverrides.Aggregate(PermissionFlags.None, (current, item) => current | item.GrantedPermissions);
        public PermissionFlags CurrentRevokedPermissions => PermissionOverrides.Aggregate(PermissionFlags.None, (current, item) => current | item.RevokedPermissions);
        public bool ArePermissionOverridesDirty => CurrentGrantedPermissions != _initialGrantedPermissions || CurrentRevokedPermissions != _initialRevokedPermissions;
        public bool HasCustomPermissionOverrides => CurrentGrantedPermissions != PermissionFlags.None || CurrentRevokedPermissions != PermissionFlags.None;
        public string PermissionRoleBasis => _permissionSettings == null ? "wird geladen" : UserRoles.ToStorageValue(_permissionSettings.ParsedRole);
        public string CurrentOverrideState => $"Gewährt: {PermissionCatalog.FormatPermissions(CurrentGrantedPermissions, "keine")} | Entzogen: {PermissionCatalog.FormatPermissions(CurrentRevokedPermissions, "keine")}";
        public string EffectivePermissionState => PermissionCatalog.FormatPermissions(
            _permissionSettings?.EffectivePermissions ?? PermissionFlags.None,
            "Keine wirksamen Fachrechte aktiv.");
        public string PermissionSaveHint => HasLinkedAppUser
            ? IsDirty
                ? "Die Rollenbasis wurde geändert. Bitte zuerst die Rolle speichern und danach die benutzerspezifischen Fachrechte sichern."
                : IsRoleManagementReadOnly
                    ? "Rollen-/Rechteverwaltung ist in diesem Kontext nur lesend freigegeben. Die Rollenbasis und die wirksamen Fachrechte bleiben sichtbar, Speichern ist gesperrt."
                    : "Benutzerspezifische Fachrechte werden zentral als Grants/Revocations über der Rollenbasis gespeichert. Über den Standard-Button lassen sich alle Abweichungen der aktuellen Rolle gesammelt zurücksetzen."
            : "Für dieses Mitglied existiert aktuell kein verknüpfter App-User. Die Rechteübersicht bleibt sichtbar, Speichern ist gesperrt.";

        public string RoleManagementHint => !CanReadRoleManagement
            ? "Rollen-/Rechteverwaltung ist für den aktuellen Kontext nicht freigegeben."
            : IsRoleManagementReadOnly
                ? "Rollen-/Rechteverwaltung ist in diesem Kontext nur lesend freigegeben. Rolle, Rollenbasis und wirksame Rechte bleiben sichtbar."
                : IsRoleEditable
                    ? "Rolle bleibt das Basispaket. Benutzerspezifische Fachrechte werden als Grants/Revocations darüber gespeichert."
                    : "Rollenbearbeitung für dieses Mitglied ist gesperrt. Die Rollenbasis und die benutzerspezifischen Fachrechte bleiben sichtbar.";

        public RelayCommand<object?> SaveCommand { get; }
        public RelayCommand<object?> SavePermissionOverridesCommand { get; }
        public RelayCommand<object?> ResetPermissionOverridesCommand { get; }
        public RelayCommand<object?> OpenUserManagementCommand { get; }

        public AdminRoleViewModel(ISupabaseService supabaseService, IAuthService authService, MemberDTO member, MainWindowViewModel mainWindowViewModel, INavigationService navigationService)
        {
            _supabaseService = supabaseService;
            _authService = authService;
            SelectedMember = member;
            _mainWindowViewModel = mainWindowViewModel;
            _navigationService = navigationService;

            SaveCommand = new RelayCommand<object?>(_ => _ = SaveAsync(), _ => CanSave());
            SavePermissionOverridesCommand = new RelayCommand<object?>(_ => _ = SavePermissionOverridesAsync(), _ => CanSavePermissionOverrides());
            ResetPermissionOverridesCommand = new RelayCommand<object?>(_ => ResetPermissionOverrides(), _ => CanResetPermissionOverrides());
            OpenUserManagementCommand = new RelayCommand<object?>(_ => _ = OpenUserManagementAsync(), _ => CanOpenUserManagement);

            foreach (var definition in PermissionCatalog.GetUserSpecificEditablePermissions())
                PermissionOverrides.Add(new UserPermissionOverrideItemViewModel(this, definition));
        }

        public async Task OnNavigatedToAsync()
        {
            await LoadAsync();
            IsDirty = false;
            SaveCommand.RaiseCanExecuteChanged();
        }

        public async Task OnNavigatedFromAsync()
        {
            if (!string.IsNullOrEmpty(_lockUserId))
            {
                await _supabaseService.ReleaseLockMitgliedAsync(SelectedMember.Id, _lockUserId, force: false);
                _lockUserId = null;
            }
        }

        private async Task LoadAsync()
        {
            var rec = await _supabaseService.GetMitgliedByIdAsync(SelectedMember.Id);
            if (rec == null)
                return;

            SelectedMember.Vorname = rec.Vorname ?? string.Empty;
            SelectedMember.Nachname = rec.Name ?? string.Empty;
            SelectedMember.Mobilnummer = rec.Handy ?? string.Empty;
            SelectedMember.Role = rec.Role ?? "user";

            SelectedRole = SelectedMember.Role;
            AllowUserMeterReadingSubmissions = await _supabaseService.GetAllowUserMeterReadingSubmissionsAsync();
            _initialAllowUserMeterReadingSubmissions = AllowUserMeterReadingSubmissions;
            await LoadPermissionSettingsAsync();
            IsDirty = false;
            OnPropertyChanged(nameof(IsUserMeterReadingSubmissionSettingDirty));
            SaveCommand.RaiseCanExecuteChanged();
        }

        private async Task LoadPermissionSettingsAsync()
        {
            var settings = await _supabaseService.GetUserPermissionSettingsAsync(SelectedMember.Id)
                ?? new UserPermissionSettings
                {
                    MitgliedId = SelectedMember.Id,
                    Role = string.IsNullOrWhiteSpace(SelectedRole) ? UserRoles.User : SelectedRole,
                    GrantedPermissions = PermissionFlags.None,
                    RevokedPermissions = PermissionFlags.None
                };

            _permissionSettings = settings;
            _selectedRole = UserRoles.ToStorageValue(settings.ParsedRole);
            OnPropertyChanged(nameof(SelectedRole));
            _initialGrantedPermissions = settings.GrantedPermissions;
            _initialRevokedPermissions = settings.RevokedPermissions;

            foreach (var item in PermissionOverrides)
                item.Apply(settings, CanEditPermissionOverrides);

            RefreshPermissionState();
        }

        private bool CanSave()
        {
            var canSaveRole = CanEditRole && IsDirty;
            var canSaveSetting = CanManageUserMeterReadingSubmissions && IsUserMeterReadingSubmissionSettingDirty;
            return canSaveRole || canSaveSetting;
        }

        private bool CanSavePermissionOverrides()
            => CanEditPermissionOverrides && ArePermissionOverridesDirty && !IsDirty;

        private bool CanResetPermissionOverrides()
            => CanEditPermissionOverrides && HasCustomPermissionOverrides && !IsDirty;

        private async Task SaveAsync()
        {
            try
            {
                if (CanManageRoleManagement && IsDirty && !IsRoleEditable)
                {
                    MessageBox.Show("Für dieses Mitglied ist die Rollenbearbeitung gesperrt.", "Gesperrt", MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                if (!CanSave())
                    return;

                var savedParts = new List<string>();

                if (CanEditRole && IsDirty)
                {
                    var userId = _authService.CurrentUserId;
                    if (string.IsNullOrWhiteSpace(userId))
                    {
                        MessageBox.Show("Nicht angemeldet. Bitte erneut einloggen.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var locked = await _supabaseService.TryLockMitgliedAsync(SelectedMember.Id, userId);
                    if (!locked)
                    {
                        MessageBox.Show("Datensatz ist aktuell gesperrt. Bitte später erneut versuchen.", "Gesperrt",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    _lockUserId = userId;

                    var rec = await _supabaseService.GetMitgliedByIdAsync(SelectedMember.Id);
                    if (rec == null)
                    {
                        MessageBox.Show("Mitglied konnte nicht geladen werden.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
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
                        MessageBox.Show("Speichern fehlgeschlagen (ggf. Lock verloren oder keine Berechtigung).", "Fehler",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    SelectedMember.Role = SelectedRole;
                    IsDirty = false;
                    savedParts.Add("Rolle");

                    await _supabaseService.ReleaseLockMitgliedAsync(SelectedMember.Id, userId, force: false);
                    _lockUserId = null;
                }

                if (CanManageUserMeterReadingSubmissions && IsUserMeterReadingSubmissionSettingDirty)
                {
                    var ok = await _supabaseService.SetAllowUserMeterReadingSubmissionsAsync(AllowUserMeterReadingSubmissions);
                    if (!ok)
                    {
                        MessageBox.Show("Die globale Ablesungs-Einstellung konnte nicht gespeichert werden.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    _initialAllowUserMeterReadingSubmissions = AllowUserMeterReadingSubmissions;
                    OnPropertyChanged(nameof(IsUserMeterReadingSubmissionSettingDirty));
                    savedParts.Add("Ablesungs-Einstellung");
                }

                SaveCommand.RaiseCanExecuteChanged();
                MessageBox.Show(savedParts.Count == 0 ? "Keine Änderungen gespeichert." : $"Gespeichert: {string.Join(", ", savedParts)}.", "OK", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Speichern: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task SavePermissionOverridesAsync()
        {
            try
            {
                if (!HasLinkedAppUser)
                {
                    MessageBox.Show("Für dieses Mitglied existiert aktuell kein verknüpfter App-User. Fachrechte können deshalb noch nicht gespeichert werden.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (IsDirty)
                {
                    MessageBox.Show("Bitte zuerst die geänderte Rollenbasis speichern und danach die benutzerspezifischen Fachrechte sichern.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (!CanSavePermissionOverrides())
                    return;

                var ok = await _supabaseService.SetUserPermissionSettingsAsync(
                    SelectedMember.Id,
                    SelectedRole,
                    (long)CurrentGrantedPermissions,
                    (long)CurrentRevokedPermissions);

                if (!ok)
                {
                    MessageBox.Show("Die benutzerspezifischen Fachrechte konnten nicht gespeichert werden.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _initialGrantedPermissions = CurrentGrantedPermissions;
                _initialRevokedPermissions = CurrentRevokedPermissions;
                if (_permissionSettings != null)
                {
                    _permissionSettings.GrantedPermissions = _initialGrantedPermissions;
                    _permissionSettings.RevokedPermissions = _initialRevokedPermissions;
                }

                RefreshPermissionState();
                MessageBox.Show("Benutzerspezifische Fachrechte wurden gespeichert.", "OK", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Speichern der Fachrechte: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        internal void OnPermissionOverrideChanged()
            => RefreshPermissionState();

        private void ResetPermissionOverrides()
        {
            if (!CanResetPermissionOverrides())
                return;

            foreach (var item in PermissionOverrides)
                item.ResetToDefault();

            RefreshPermissionState();
        }

        private void RefreshPermissionState()
        {
            if (_permissionSettings != null)
            {
                _permissionSettings.Role = string.IsNullOrWhiteSpace(SelectedRole) ? _permissionSettings.Role : SelectedRole;
                _permissionSettings.GrantedPermissions = CurrentGrantedPermissions;
                _permissionSettings.RevokedPermissions = CurrentRevokedPermissions;
            }

            OnPropertyChanged(nameof(HasLinkedAppUser));
            OnPropertyChanged(nameof(CanManageRoleManagement));
            OnPropertyChanged(nameof(CanEditRole));
            OnPropertyChanged(nameof(IsRoleManagementReadOnly));
            OnPropertyChanged(nameof(CanEditPermissionOverrides));
            OnPropertyChanged(nameof(ArePermissionOverridesDirty));
            OnPropertyChanged(nameof(HasCustomPermissionOverrides));
            OnPropertyChanged(nameof(RoleManagementHint));
            OnPropertyChanged(nameof(PermissionRoleBasis));
            OnPropertyChanged(nameof(CurrentOverrideState));
            OnPropertyChanged(nameof(EffectivePermissionState));
            OnPropertyChanged(nameof(PermissionSaveHint));
            SavePermissionOverridesCommand.RaiseCanExecuteChanged();
            ResetPermissionOverridesCommand.RaiseCanExecuteChanged();

            foreach (var item in PermissionOverrides)
                item.RefreshEditState(CanEditPermissionOverrides);
        }

        private void RebuildPermissionOverridesForCurrentRole()
        {
            if (_permissionSettings == null)
                return;

            _permissionSettings.Role = SelectedRole;
            _permissionSettings.GrantedPermissions = CurrentGrantedPermissions;
            _permissionSettings.RevokedPermissions = CurrentRevokedPermissions;

            foreach (var item in PermissionOverrides)
                item.Apply(_permissionSettings, CanEditPermissionOverrides);

            RefreshPermissionState();
        }

        private async Task OpenUserManagementAsync()
        {
            if (!CanOpenUserManagement)
                return;

            var created = _navigationService.CreateViewModel(typeof(UserManagementViewModel), _mainWindowViewModel, SelectedMember);
            if (created is not BaseViewModel vm)
            {
                MessageBox.Show("Benutzerverwaltung konnte nicht geöffnet werden.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            await _mainWindowViewModel.NavigateToAsync(vm);
        }

        public sealed class UserPermissionOverrideItemViewModel : BaseViewModel
        {
            private const string DefaultOverrideMode = "Standard";
            private const string GrantOverrideMode = "Zusätzlich gewähren";
            private const string RevokeOverrideMode = "Entziehen";
            private static readonly PermissionFlags UserRolePermissions = PermissionService.GetRolePermissions(UserRole.User);
            private static readonly PermissionFlags VorstandRolePermissions = PermissionService.GetRolePermissions(UserRole.Vorstand);
            private static readonly PermissionFlags AdminRolePermissions = PermissionService.GetRolePermissions(UserRole.Admin);

            private readonly AdminRoleViewModel _owner;
            private bool _isBaseGranted;
            private string _selectedOverrideMode = DefaultOverrideMode;
            private bool _isEditable;

            public UserPermissionOverrideItemViewModel(AdminRoleViewModel owner, PermissionDefinition definition)
            {
                _owner = owner;
                Definition = definition;
            }

            public PermissionDefinition Definition { get; }
            public string DisplayName => Definition.DisplayName;
            public IReadOnlyList<string> OverrideModes { get; } = new[] { DefaultOverrideMode, GrantOverrideMode, RevokeOverrideMode };
            public bool IsGrantedForUser => UserRolePermissions.HasFlag(Definition.Flag);
            public bool IsGrantedForVorstand => VorstandRolePermissions.HasFlag(Definition.Flag);
            public bool IsGrantedForAdmin => AdminRolePermissions.HasFlag(Definition.Flag);

            public bool IsBaseGranted
            {
                get => _isBaseGranted;
                private set
                {
                    if (!SetProperty(ref _isBaseGranted, value))
                        return;

                    OnPropertyChanged(nameof(IsEffectivelyGranted));
                }
            }

            public string SelectedOverrideMode
            {
                get => _selectedOverrideMode;
                set
                {
                    var normalized = OverrideModes.Contains(value) ? value : DefaultOverrideMode;
                    if (!SetProperty(ref _selectedOverrideMode, normalized))
                        return;

                    OnPropertyChanged(nameof(IsEffectivelyGranted));
                    _owner.OnPermissionOverrideChanged();
                }
            }

            public bool IsEditable
            {
                get => _isEditable;
                private set => SetProperty(ref _isEditable, value);
            }

            public bool IsGrantOverrideSelected
            {
                get => SelectedOverrideMode == GrantOverrideMode;
                set
                {
                    if (value)
                    {
                        SelectedOverrideMode = GrantOverrideMode;
                        return;
                    }

                    if (SelectedOverrideMode == GrantOverrideMode)
                        SelectedOverrideMode = DefaultOverrideMode;
                }
            }

            public bool IsRevokeOverrideSelected
            {
                get => SelectedOverrideMode == RevokeOverrideMode;
                set
                {
                    if (value)
                    {
                        SelectedOverrideMode = RevokeOverrideMode;
                        return;
                    }

                    if (SelectedOverrideMode == RevokeOverrideMode)
                        SelectedOverrideMode = DefaultOverrideMode;
                }
            }

            public bool IsEffectivelyGranted => SelectedOverrideMode switch
            {
                GrantOverrideMode => true,
                RevokeOverrideMode => false,
                _ => IsBaseGranted
            };

            public PermissionFlags GrantedPermissions => SelectedOverrideMode == GrantOverrideMode ? Definition.Flag : PermissionFlags.None;
            public PermissionFlags RevokedPermissions => SelectedOverrideMode == RevokeOverrideMode ? Definition.Flag : PermissionFlags.None;

            public void ResetToDefault()
                => SelectedOverrideMode = DefaultOverrideMode;

            public void Apply(UserPermissionSettings settings, bool isEditable)
            {
                IsBaseGranted = settings.BasePermissions.HasFlag(Definition.Flag);
                IsEditable = isEditable;

                if (settings.GrantedPermissions.HasFlag(Definition.Flag))
                    _selectedOverrideMode = GrantOverrideMode;
                else if (settings.RevokedPermissions.HasFlag(Definition.Flag))
                    _selectedOverrideMode = RevokeOverrideMode;
                else
                    _selectedOverrideMode = DefaultOverrideMode;

                OnPropertyChanged(nameof(SelectedOverrideMode));
                OnPropertyChanged(nameof(IsGrantOverrideSelected));
                OnPropertyChanged(nameof(IsRevokeOverrideSelected));
                OnPropertyChanged(nameof(IsEffectivelyGranted));
            }

            public void RefreshEditState(bool isEditable)
                => IsEditable = isEditable;
        }
    }
}
