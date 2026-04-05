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

        private bool _allowUserMeterReadingSubmissions;
        private bool _initialAllowUserMeterReadingSubmissions;
        private UserPermissionSettings? _permissionSettings;
        private bool _isPermissionSettingsLoadReliable;
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
        public bool IsLinkedAppUserStatusKnown => _isPermissionSettingsLoadReliable;
        public string LinkedAppUserStatusText => !_isPermissionSettingsLoadReliable
            ? "Status derzeit unbekannt"
            : HasLinkedAppUser
                ? "verknüpft"
                : "nicht verknüpft";
        public bool CanEditPermissionOverrides => CanManageRoleManagement && IsLinkedAppUserStatusKnown && HasLinkedAppUser;
        public PermissionFlags CurrentGrantedPermissions => BuildPermissionOverrideState().GrantedPermissions;
        public PermissionFlags CurrentRevokedPermissions => BuildPermissionOverrideState().RevokedPermissions;
        public bool ArePermissionOverridesDirty => CurrentGrantedPermissions != _initialGrantedPermissions || CurrentRevokedPermissions != _initialRevokedPermissions;
        public bool HasCustomPermissionOverrides => CurrentGrantedPermissions != PermissionFlags.None || CurrentRevokedPermissions != PermissionFlags.None;
        public string PermissionRoleBasis => _permissionSettings == null ? "wird geladen" : UserRoles.ToStorageValue(_permissionSettings.ParsedRole);
        public string CurrentOverrideState => $"Gewährt: {PermissionCatalog.FormatPermissions(CurrentGrantedPermissions, "keine")} | Entzogen: {PermissionCatalog.FormatPermissions(CurrentRevokedPermissions, "keine")}";
        public string EffectivePermissionState => PermissionCatalog.FormatPermissions(
            _permissionSettings?.EffectivePermissions ?? PermissionFlags.None,
            "Keine wirksamen Fachrechte aktiv.");
        public string PermissionSaveHint => !IsLinkedAppUserStatusKnown
            ? "Der Verknüpfungsstatus des App-Users konnte aktuell nicht belastbar geladen werden. Die Rechteübersicht bleibt sichtbar, Speichern ist vorsorglich gesperrt."
            : HasLinkedAppUser
            ? IsDirty
                ? "Die Rollenbasis wurde geändert. Bitte zuerst die Rolle speichern und danach die benutzerspezifischen Fachrechte sichern."
                : IsRoleManagementReadOnly
                    ? "Rollen-/Rechteverwaltung ist in diesem Kontext nur lesend freigegeben. Die Rollenbasis und die wirksamen Fachrechte bleiben sichtbar, Speichern ist gesperrt."
                    : "Globale Fachrechte werden kompakt pro Fachbereich als Aus / Lesen / Bearbeiten über der Rollenbasis gespeichert. Eigenkontext-Rechte bleiben davon getrennt; der Sonderfall Nutzerablesung bleibt separat."
            : "Für dieses Mitglied existiert aktuell kein verknüpfter App-User. Die Rechteübersicht bleibt sichtbar, Speichern ist gesperrt.";

        public string RoleManagementHint => !CanReadRoleManagement
            ? "Rollen-/Rechteverwaltung ist für den aktuellen Kontext nicht freigegeben."
            : IsRoleManagementReadOnly
                ? "Rollen-/Rechteverwaltung ist in diesem Kontext nur lesend freigegeben. Rolle, Rollenbasis und wirksame Rechte bleiben sichtbar."
                : IsRoleEditable
                    ? "Rolle bleibt das Basispaket. Globale Fachrechte werden kompakt pro Fachbereich darüber angepasst; Eigenkontext-Rechte bleiben getrennt."
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

            foreach (var definition in PermissionCatalog.GetGlobalEditablePermissionAreas())
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
            await Task.CompletedTask;
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
            SelectedMember.Role = SelectedRole;
            IsDirty = false;
            OnPropertyChanged(nameof(IsUserMeterReadingSubmissionSettingDirty));
            SaveCommand.RaiseCanExecuteChanged();
        }

        private async Task LoadPermissionSettingsAsync()
        {
            _isPermissionSettingsLoadReliable = false;

            var settings = await _supabaseService.GetUserPermissionSettingsAsync(SelectedMember.Id);
            if (settings != null)
            {
                _isPermissionSettingsLoadReliable = true;
            }
            else
            {
                var memberRecord = await _supabaseService.GetMitgliedByIdAsync(SelectedMember.Id);
                if (memberRecord != null)
                {
                    settings = new UserPermissionSettings
                    {
                        AuthUserId = memberRecord.AuthUserId,
                        MitgliedId = SelectedMember.Id,
                        Role = UserRoles.ToStorageValue(UserRoles.Parse(string.IsNullOrWhiteSpace(memberRecord.Role) ? SelectedRole : memberRecord.Role)),
                        GrantedPermissions = PermissionFlags.None,
                        RevokedPermissions = PermissionFlags.None
                    };
                    _isPermissionSettingsLoadReliable = true;
                }
            }

            settings ??= new UserPermissionSettings
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
                    if (!HasLinkedAppUser)
                    {
                        MessageBox.Show(IsLinkedAppUserStatusKnown
                                ? "Für dieses Mitglied existiert aktuell kein verknüpfter App-User. Die Rolle kann deshalb noch nicht über app_user.role gespeichert werden."
                                : "Der Verknüpfungsstatus des App-Users konnte aktuell nicht belastbar geladen werden. Die Rolle bleibt deshalb vorsorglich gesperrt.",
                            "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    var ok = await _supabaseService.SetAppUserRoleAsync(SelectedMember.Id, SelectedRole);
                    if (!ok)
                    {
                        MessageBox.Show("Die Rolle konnte nicht über app_user.role gespeichert werden.", "Fehler",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    SelectedMember.Role = SelectedRole;
                    if (_permissionSettings != null)
                        _permissionSettings.Role = SelectedRole;
                    IsDirty = false;
                    savedParts.Add("Rolle");
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
                    MessageBox.Show(IsLinkedAppUserStatusKnown
                            ? "Für dieses Mitglied existiert aktuell kein verknüpfter App-User. Fachrechte können deshalb noch nicht gespeichert werden."
                            : "Der Verknüpfungsstatus des App-Users konnte aktuell nicht belastbar geladen werden. Fachrechte bleiben deshalb vorsorglich gesperrt.",
                        "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
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

        private PermissionFlags GetBasePermissionsForSelectedRole()
            => PermissionService.GetRolePermissions(UserRoles.Parse(SelectedRole));

        private (PermissionFlags GrantedPermissions, PermissionFlags RevokedPermissions) BuildPermissionOverrideState()
        {
            var basePermissions = GetBasePermissionsForSelectedRole();
            var requiredPermissions = PermissionOverrides.Aggregate(
                PermissionFlags.None,
                (current, item) => current | PermissionCatalog.GetRequiredPermissions(item.Definition, item.SelectedLevel));

            var controllableMask = PermissionCatalog.GetGlobalEditablePermissionMask();
            var grantedPermissions = requiredPermissions & ~basePermissions;
            var revokedPermissions = (basePermissions & controllableMask) & ~requiredPermissions;
            return (grantedPermissions, revokedPermissions);
        }

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
            OnPropertyChanged(nameof(IsLinkedAppUserStatusKnown));
            OnPropertyChanged(nameof(LinkedAppUserStatusText));
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
            private readonly AdminRoleViewModel _owner;
            private PermissionAreaAccessLevel _baseLevel;
            private PermissionAreaAccessLevel _selectedLevel;
            private bool _isEditable;

            public UserPermissionOverrideItemViewModel(AdminRoleViewModel owner, PermissionAreaDefinition definition)
            {
                _owner = owner;
                Definition = definition;
            }

            public PermissionAreaDefinition Definition { get; }
            public string DisplayName => Definition.DisplayName;
            public string BaseLevelDisplay => PermissionCatalog.FormatAccessLevel(BaseLevel);
            public PermissionAreaAccessLevel BaseLevel
            {
                get => _baseLevel;
                private set
                {
                    if (!SetProperty(ref _baseLevel, value))
                        return;

                    OnPropertyChanged(nameof(BaseLevelDisplay));
                }
            }

            public PermissionAreaAccessLevel SelectedLevel
            {
                get => _selectedLevel;
                private set
                {
                    if (!SetProperty(ref _selectedLevel, value))
                        return;

                    OnPropertyChanged(nameof(IsNoneSelected));
                    OnPropertyChanged(nameof(IsReadSelected));
                    OnPropertyChanged(nameof(IsWriteSelected));
                    OnPropertyChanged(nameof(EffectiveLevelDisplay));
                    _owner.OnPermissionOverrideChanged();
                }
            }

            public bool IsEditable
            {
                get => _isEditable;
                private set => SetProperty(ref _isEditable, value);
            }

            public bool IsNoneSelected
            {
                get => SelectedLevel == PermissionAreaAccessLevel.None;
                set
                {
                    if (value)
                        SelectedLevel = PermissionAreaAccessLevel.None;
                }
            }

            public bool IsReadSelected
            {
                get => SelectedLevel == PermissionAreaAccessLevel.Read;
                set
                {
                    if (value)
                        SelectedLevel = PermissionAreaAccessLevel.Read;
                }
            }

            public bool IsWriteSelected
            {
                get => SelectedLevel == PermissionAreaAccessLevel.Write;
                set
                {
                    if (value)
                        SelectedLevel = PermissionAreaAccessLevel.Write;
                }
            }

            public string EffectiveLevelDisplay => PermissionCatalog.FormatAccessLevel(SelectedLevel);

            public void ResetToDefault()
                => SelectedLevel = BaseLevel;

            public void Apply(UserPermissionSettings settings, bool isEditable)
            {
                BaseLevel = PermissionCatalog.GetAccessLevel(settings.BasePermissions, Definition);
                IsEditable = isEditable;
                _selectedLevel = PermissionCatalog.GetAccessLevel(settings.EffectivePermissions, Definition);
                OnPropertyChanged(nameof(SelectedLevel));
                OnPropertyChanged(nameof(IsNoneSelected));
                OnPropertyChanged(nameof(IsReadSelected));
                OnPropertyChanged(nameof(IsWriteSelected));
                OnPropertyChanged(nameof(EffectiveLevelDisplay));
            }

            public void RefreshEditState(bool isEditable)
                => IsEditable = isEditable;
        }
    }
}
