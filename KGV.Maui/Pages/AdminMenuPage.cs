using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Maui.State;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using System.Collections.Generic;
using System.Linq;
using System;

namespace KGV.Maui.Pages;

public sealed class AdminMenuPage : ContentPage
{
    private readonly UserContextState _userContextState;
    private readonly MemberContextState _memberContextState;
    private readonly ISupabaseService _supabaseService;
    private readonly Label _memberInfoLabel;
    private readonly Label _hintLabel;
    private readonly Button _userManagementButton;
    private readonly Switch _meterReadingSubmissionsSwitch;
    private readonly Button _saveMeterReadingSubmissionsButton;
    private readonly Label _meterReadingSubmissionsHintLabel;
    private readonly Picker _rolePicker;
    private readonly Label _roleManagementHintLabel;
    private readonly Button _saveRoleButton;
    private readonly Label _permissionRoleBasisLabel;
    private readonly Label _permissionOverrideStateLabel;
    private readonly Label _effectivePermissionStateLabel;
    private readonly Label _permissionEditorHintLabel;
    private readonly Button _resetPermissionOverridesButton;
    private readonly Button _savePermissionOverridesButton;
    private readonly VerticalStackLayout _permissionOverridesLayout;
    private readonly List<PermissionAreaEditorRow> _permissionOverrideRows = new();
    private bool _allowUserMeterReadingSubmissions;
    private UserPermissionSettings? _permissionSettings;
    private bool _isPermissionSettingsLoadReliable;
    private bool _suppressPermissionOverrideRefresh;
    private bool _suppressRoleRefresh;
    private string _selectedRole = UserRoles.User;
    private string _initialRole = UserRoles.User;

    public AdminMenuPage(UserContextState userContextState, MemberContextState memberContextState, ISupabaseService supabaseService)
    {
        _userContextState = userContextState ?? throw new ArgumentNullException(nameof(userContextState));
        _memberContextState = memberContextState ?? throw new ArgumentNullException(nameof(memberContextState));
        _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));

        Title = "Admin-Menü";

        var titleLabel = new Label
        {
            Text = "Admin-Menü",
            FontSize = 24,
            FontAttributes = FontAttributes.Bold
        };

        _memberInfoLabel = new Label
        {
            LineBreakMode = LineBreakMode.WordWrap
        };

        _hintLabel = new Label
        {
            TextColor = Colors.Gray,
            LineBreakMode = LineBreakMode.WordWrap
        };

        _userManagementButton = new Button
        {
            Text = "Benutzerverwaltung öffnen"
        };
        _userManagementButton.Clicked += async (_, _) => await Shell.Current.GoToAsync(nameof(UserManagementPage));

        _meterReadingSubmissionsSwitch = new Switch();
        _meterReadingSubmissionsSwitch.Toggled += (_, e) =>
        {
            _allowUserMeterReadingSubmissions = e.Value;
            UpdateMeterReadingSettingState();
        };

        _saveMeterReadingSubmissionsButton = new Button
        {
            Text = "Ablesungs-Einstellung speichern"
        };
        _saveMeterReadingSubmissionsButton.Clicked += async (_, _) => await SaveMeterReadingSubmissionSettingAsync();

        _meterReadingSubmissionsHintLabel = new Label
        {
            TextColor = Colors.Gray,
            LineBreakMode = LineBreakMode.WordWrap
        };

        _rolePicker = new Picker
        {
            Title = "Rolle wählen",
            ItemsSource = UserRoles.AssignableRoles.ToList()
        };
        _rolePicker.SelectedIndexChanged += (_, _) => OnRoleChanged();

        _roleManagementHintLabel = new Label
        {
            TextColor = Colors.Gray,
            LineBreakMode = LineBreakMode.WordWrap
        };

        _saveRoleButton = new Button
        {
            Text = "Rolle speichern"
        };
        _saveRoleButton.Clicked += async (_, _) => await SaveRoleAsync();

        _permissionRoleBasisLabel = new Label { LineBreakMode = LineBreakMode.WordWrap };
        _permissionOverrideStateLabel = new Label { LineBreakMode = LineBreakMode.WordWrap };
        _effectivePermissionStateLabel = new Label { LineBreakMode = LineBreakMode.WordWrap };
        _permissionEditorHintLabel = new Label { TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
        _permissionOverridesLayout = new VerticalStackLayout { Spacing = 12 };

        foreach (var definition in PermissionCatalog.GetGlobalEditablePermissionAreas())
        {
            var row = new PermissionAreaEditorRow(definition, OnPermissionOverrideChanged);
            _permissionOverrideRows.Add(row);
            _permissionOverridesLayout.Children.Add(row.Container);
        }

        _resetPermissionOverridesButton = new Button
        {
            Text = "Alles auf Rollen-Standard zurücksetzen"
        };
        _resetPermissionOverridesButton.Clicked += (_, _) => ResetPermissionOverrides();

        _savePermissionOverridesButton = new Button
        {
            Text = "Fachrechte speichern"
        };
        _savePermissionOverridesButton.Clicked += async (_, _) => await SavePermissionOverridesAsync();

        var meterReadingSubmissionsLabel = new Label
        {
            Text = "Normale Nutzer dürfen eigene Zählerablesungen einreichen",
            VerticalOptions = LayoutOptions.Center,
            LineBreakMode = LineBreakMode.WordWrap
        };
        Grid.SetColumn(meterReadingSubmissionsLabel, 0);
        Grid.SetColumn(_meterReadingSubmissionsSwitch, 1);

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 12,
                Children =
                {
                    titleLabel,
                    _memberInfoLabel,
                    _hintLabel,
                    _userManagementButton,
                    new Label { Text = "Rolle", FontAttributes = FontAttributes.Bold, Margin = new Thickness(0, 12, 0, 0) },
                    _rolePicker,
                    _roleManagementHintLabel,
                    _saveRoleButton,
                    new Label { Text = "Globale Fachrechte", FontAttributes = FontAttributes.Bold, Margin = new Thickness(0, 12, 0, 0) },
                    _permissionRoleBasisLabel,
                    _permissionEditorHintLabel,
                    _resetPermissionOverridesButton,
                    _permissionOverridesLayout,
                    _permissionOverrideStateLabel,
                    _effectivePermissionStateLabel,
                    _savePermissionOverridesButton,
                    new Label { Text = "Zählerablesungen", FontAttributes = FontAttributes.Bold, Margin = new Thickness(0, 12, 0, 0) },
                    new Grid
                    {
                        ColumnDefinitions =
                        {
                            new ColumnDefinition(GridLength.Star),
                            new ColumnDefinition(GridLength.Auto)
                        },
                        ColumnSpacing = 12,
                        Children =
                        {
                            meterReadingSubmissionsLabel,
                            _meterReadingSubmissionsSwitch
                        }
                    },
                    _meterReadingSubmissionsHintLabel,
                    _saveMeterReadingSubmissionsButton
                }
            }
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = RefreshStateAsync();
    }

    private async Task RefreshStateAsync()
    {
        var selectedMember = _memberContextState.SelectedMember;
        var currentContext = _userContextState.CurrentUserContext;
        var isAdmin = currentContext?.Role == UserRole.Admin;
        var canReadRoleManagement = PermissionChecks.CanReadRoleManagement(currentContext);
        var canManageMeterReadingSubmissions = _userContextState.CurrentUserContext?.Role is UserRole.Admin or UserRole.Vorstand;
        var hasSelectedMember = selectedMember?.Id > 0;

        _memberInfoLabel.Text = hasSelectedMember
            ? $"Ausgewähltes Mitglied: {selectedMember!.DisplayName} (ID: {selectedMember.Id})"
            : "Es wurde aktuell kein Mitglied ausgewählt.";

        _hintLabel.Text = !isAdmin
            ? "Die Benutzerverwaltung ist nur für Admin sichtbar."
            : hasSelectedMember
                ? "App-User-bezogene Aktionen bleiben an das aktuell ausgewählte Mitglied gebunden."
                : "Bitte zuerst ein Mitglied auswählen.";

        _userManagementButton.IsVisible = isAdmin;
        _userManagementButton.IsEnabled = isAdmin && hasSelectedMember;

        await LoadPermissionSettingsAsync(selectedMember, hasSelectedMember);
        UpdateRoleManagementState(selectedMember, hasSelectedMember, canReadRoleManagement);

        if (canManageMeterReadingSubmissions)
            _allowUserMeterReadingSubmissions = await _supabaseService.GetAllowUserMeterReadingSubmissionsAsync();
        else
            _allowUserMeterReadingSubmissions = false;

        _meterReadingSubmissionsSwitch.IsToggled = _allowUserMeterReadingSubmissions;
        _meterReadingSubmissionsSwitch.IsEnabled = canManageMeterReadingSubmissions;
        UpdateMeterReadingSettingState();
    }

    private async Task LoadPermissionSettingsAsync(MemberDTO? selectedMember, bool hasSelectedMember)
    {
        _isPermissionSettingsLoadReliable = !hasSelectedMember;
        _permissionSettings = null;

        if (hasSelectedMember)
        {
            _permissionSettings = await _supabaseService.GetUserPermissionSettingsAsync(selectedMember!.Id);
            if (_permissionSettings != null)
            {
                _isPermissionSettingsLoadReliable = true;
            }
            else
            {
                var memberRecord = await _supabaseService.GetMitgliedByIdAsync(selectedMember.Id);
                if (memberRecord != null)
                {
                    _permissionSettings = new UserPermissionSettings
                    {
                        AuthUserId = memberRecord.AuthUserId,
                        MitgliedId = selectedMember.Id,
                        Role = NormalizeRole(string.IsNullOrWhiteSpace(memberRecord.Role) ? selectedMember.Role : memberRecord.Role),
                        GrantedPermissions = PermissionFlags.None,
                        RevokedPermissions = PermissionFlags.None
                    };
                    _isPermissionSettingsLoadReliable = true;
                }
            }
        }

        _selectedRole = NormalizeRole(_permissionSettings?.Role ?? selectedMember?.Role);
        _initialRole = _selectedRole;

        _suppressRoleRefresh = true;
        try
        {
            _rolePicker.SelectedItem = _selectedRole;
        }
        finally
        {
            _suppressRoleRefresh = false;
        }

        var canEditPermissionOverrides = CanEditPermissionOverrides();

        _suppressPermissionOverrideRefresh = true;
        try
        {
            foreach (var row in _permissionOverrideRows)
                row.Apply(_permissionSettings, canEditPermissionOverrides);
        }
        finally
        {
            _suppressPermissionOverrideRefresh = false;
        }

        RefreshPermissionOverrideState();
    }

    private void UpdateMeterReadingSettingState()
    {
        var canManageMeterReadingSubmissions = _userContextState.CurrentUserContext?.Role is UserRole.Admin or UserRole.Vorstand;
        _saveMeterReadingSubmissionsButton.IsVisible = canManageMeterReadingSubmissions;
        _saveMeterReadingSubmissionsButton.IsEnabled = canManageMeterReadingSubmissions;
        _meterReadingSubmissionsHintLabel.Text = canManageMeterReadingSubmissions
            ? "Der Schalter gilt zentral für WPF und MAUI und bereitet den späteren Nutzer-Einreichungspfad auf derselben Shared-Persistenz vor."
            : "Die globale Ablesungs-Einstellung ist nur für Admin oder Vorstand sichtbar.";
    }

    private bool CanEditPermissionOverrides()
        => _memberContextState.SelectedMember?.Id is > 0
           && CanManageRoleManagement()
           && _permissionSettings?.HasLinkedUser == true
           && !IsRoleDirty();

    private PermissionFlags CurrentGrantedPermissions
        => BuildPermissionOverrideState().GrantedPermissions;

    private PermissionFlags CurrentRevokedPermissions
        => BuildPermissionOverrideState().RevokedPermissions;

    private bool HasCustomPermissionOverrides()
        => CurrentGrantedPermissions != PermissionFlags.None || CurrentRevokedPermissions != PermissionFlags.None;

    private void OnPermissionOverrideChanged()
    {
        if (_suppressPermissionOverrideRefresh)
            return;

        RefreshPermissionOverrideState();
    }

    private (PermissionFlags GrantedPermissions, PermissionFlags RevokedPermissions) BuildPermissionOverrideState()
    {
        var basePermissions = PermissionService.GetRolePermissions(UserRoles.Parse(_selectedRole));
        var requiredPermissions = _permissionOverrideRows.Aggregate(
            PermissionFlags.None,
            (current, row) => current | PermissionCatalog.GetRequiredPermissions(row.Definition, row.SelectedLevel));

        var controllableMask = PermissionCatalog.GetGlobalEditablePermissionMask();
        var grantedPermissions = requiredPermissions & ~basePermissions;
        var revokedPermissions = (basePermissions & controllableMask) & ~requiredPermissions;
        return (grantedPermissions, revokedPermissions);
    }

    private void ResetPermissionOverrides()
    {
        if (!CanEditPermissionOverrides() || !HasCustomPermissionOverrides())
            return;

        foreach (var row in _permissionOverrideRows)
            row.ResetToDefault();

        RefreshPermissionOverrideState();
    }

    private void RefreshPermissionOverrideState()
    {
        var canEditPermissionOverrides = CanEditPermissionOverrides();
        foreach (var row in _permissionOverrideRows)
            row.SetEditable(canEditPermissionOverrides);

        var canReadRoleManagement = CanReadRoleManagement();
        var roleBasis = _permissionSettings == null
            ? "Rollenbasis: –"
            : $"Rollenbasis: {_selectedRole}";

        var linkedUser = !_isPermissionSettingsLoadReliable
            ? "App-User-Status unbekannt"
            : _permissionSettings?.HasLinkedUser == true
                ? "App-User verknüpft"
                : "Kein App-User verknüpft";
        _permissionRoleBasisLabel.Text = $"{roleBasis} · {linkedUser}";
        _permissionEditorHintLabel.Text = !canReadRoleManagement
            ? "Rollen-/Rechteverwaltung ist für den aktuellen Kontext nicht freigegeben."
            : !_isPermissionSettingsLoadReliable
                ? "Der Verknüpfungsstatus des App-Users konnte aktuell nicht belastbar geladen werden. Rechte bleiben vorsorglich gesperrt, bis die Daten erneut erfolgreich geladen wurden."
            : _permissionSettings?.HasLinkedUser != true
                ? "Für dieses Mitglied existiert aktuell kein verknüpfter App-User. Die Rechteübersicht bleibt sichtbar, Speichern ist gesperrt."
                : IsRoleDirty()
                    ? "Die Rollenbasis wurde geändert. Bitte zuerst die Rolle speichern und danach die benutzerspezifischen Fachrechte sichern."
                    : CanManageRoleManagement()
                        ? "Globale Fachrechte werden kompakt pro Fachbereich als Aus / Lesen / Bearbeiten über der Rollenbasis gespeichert. Eigenkontext-Rechte bleiben davon getrennt; der Sonderfall Nutzerablesung bleibt separat."
                        : "Rollen-/Rechteverwaltung ist in diesem Kontext nur lesend freigegeben. Die Rollenbasis und die wirksamen Fachrechte bleiben sichtbar.";

        var effectivePermissions = _permissionSettings == null
            ? PermissionFlags.None
            : PermissionService.ApplyOverrides(
                PermissionService.GetRolePermissions(UserRoles.Parse(_selectedRole)),
                CurrentGrantedPermissions,
                CurrentRevokedPermissions);

        _permissionOverrideStateLabel.Text = $"Aktueller Override-Zustand: Gewährt: {PermissionCatalog.FormatPermissions(CurrentGrantedPermissions, "keine")} · Entzogen: {PermissionCatalog.FormatPermissions(CurrentRevokedPermissions, "keine")}";
        _effectivePermissionStateLabel.Text = $"Wirksame Rechte: {PermissionCatalog.FormatPermissions(effectivePermissions, "Keine wirksamen Fachrechte aktiv.")}";
        _resetPermissionOverridesButton.IsEnabled = canEditPermissionOverrides && HasCustomPermissionOverrides();
        _savePermissionOverridesButton.IsEnabled = canEditPermissionOverrides && _permissionSettings != null && (CurrentGrantedPermissions != _permissionSettings.GrantedPermissions || CurrentRevokedPermissions != _permissionSettings.RevokedPermissions);
    }

    private async Task SaveMeterReadingSubmissionSettingAsync()
    {
        var canManageMeterReadingSubmissions = _userContextState.CurrentUserContext?.Role is UserRole.Admin or UserRole.Vorstand;
        if (!canManageMeterReadingSubmissions)
            return;

        var ok = await _supabaseService.SetAllowUserMeterReadingSubmissionsAsync(_allowUserMeterReadingSubmissions);
        await DisplayAlert(ok ? "Gespeichert" : "Fehler",
            ok
                ? "Die globale Ablesungs-Einstellung wurde gespeichert."
                : "Die globale Ablesungs-Einstellung konnte nicht gespeichert werden.",
            "OK");
    }

    private async Task SavePermissionOverridesAsync()
    {
        var selectedMember = _memberContextState.SelectedMember;
        if (selectedMember?.Id is not > 0)
            return;

        if (!CanReadRoleManagement())
        {
            await DisplayAlert("Hinweis", "Rollen-/Rechteverwaltung ist für den aktuellen Kontext nicht freigegeben.", "OK");
            return;
        }

        if (IsRoleDirty())
        {
            await DisplayAlert("Hinweis", "Bitte zuerst die geänderte Rollenbasis speichern und danach die benutzerspezifischen Fachrechte sichern.", "OK");
            return;
        }

        if (!CanManageRoleManagement())
        {
            await DisplayAlert("Hinweis", "Rollen-/Rechteverwaltung ist in diesem Kontext nur lesend freigegeben.", "OK");
            return;
        }

        if (!CanEditPermissionOverrides())
        {
            await DisplayAlert("Hinweis", "Für dieses Mitglied existiert aktuell kein verknüpfter App-User. Fachrechte können deshalb noch nicht gespeichert werden.", "OK");
            return;
        }

        var ok = await _supabaseService.SetUserPermissionSettingsAsync(
            selectedMember.Id,
            _selectedRole,
            (long)CurrentGrantedPermissions,
            (long)CurrentRevokedPermissions);

        if (!ok)
        {
            await DisplayAlert("Fehler", "Die benutzerspezifischen Fachrechte konnten nicht gespeichert werden.", "OK");
            return;
        }

        if (_permissionSettings != null)
        {
            _permissionSettings.GrantedPermissions = CurrentGrantedPermissions;
            _permissionSettings.RevokedPermissions = CurrentRevokedPermissions;
        }

        RefreshPermissionOverrideState();
        await DisplayAlert("Gespeichert", "Die benutzerspezifischen Fachrechte wurden gespeichert.", "OK");
    }

    private void OnRoleChanged()
    {
        if (_suppressRoleRefresh)
            return;

        _selectedRole = NormalizeRole(_rolePicker.SelectedItem as string);
        RebuildPermissionRowsForCurrentRole();
        UpdateRoleManagementState(_memberContextState.SelectedMember, _memberContextState.SelectedMember?.Id > 0, CanReadRoleManagement());
    }

    private void RebuildPermissionRowsForCurrentRole()
    {
        if (_permissionSettings == null)
            return;

        var canEditPermissionOverrides = CanEditPermissionOverrides();
        var previewSettings = new UserPermissionSettings
        {
            AuthUserId = _permissionSettings.AuthUserId,
            MitgliedId = _permissionSettings.MitgliedId,
            Role = _selectedRole,
            GrantedPermissions = CurrentGrantedPermissions,
            RevokedPermissions = CurrentRevokedPermissions
        };

        _suppressPermissionOverrideRefresh = true;
        try
        {
            foreach (var row in _permissionOverrideRows)
                row.Apply(previewSettings, canEditPermissionOverrides);
        }
        finally
        {
            _suppressPermissionOverrideRefresh = false;
        }

        RefreshPermissionOverrideState();
    }

    private void UpdateRoleManagementState(MemberDTO? selectedMember, bool hasSelectedMember, bool canReadRoleManagement)
    {
        var canManageRoleManagement = CanManageRoleManagement();
        var isRoleEditable = selectedMember?.Id is > 0 and not 7;

        _rolePicker.IsVisible = canReadRoleManagement && hasSelectedMember;
        _saveRoleButton.IsVisible = canReadRoleManagement && hasSelectedMember;
        _rolePicker.IsEnabled = canManageRoleManagement && isRoleEditable;
        _saveRoleButton.IsEnabled = canManageRoleManagement && isRoleEditable && hasSelectedMember && IsRoleDirty();

        _roleManagementHintLabel.Text = !canReadRoleManagement
            ? "Rollen-/Rechteverwaltung ist für den aktuellen Kontext nicht freigegeben."
            : !hasSelectedMember
                ? "Bitte zuerst ein Mitglied auswählen."
                : !isRoleEditable
                    ? "Rollenbearbeitung für dieses Mitglied ist gesperrt. Die Rollenbasis und die wirksamen Rechte bleiben sichtbar."
                    : canManageRoleManagement
                        ? "Rolle bleibt das Basispaket. Benutzerspezifische Fachrechte werden als Grants/Revocations darüber gespeichert."
                        : "Rollen-/Rechteverwaltung ist in diesem Kontext nur lesend freigegeben. Rolle, Rollenbasis und wirksame Rechte bleiben sichtbar.";
    }

    private async Task SaveRoleAsync()
    {
        var selectedMember = _memberContextState.SelectedMember;
        if (selectedMember?.Id is not > 0)
            return;

        if (!CanReadRoleManagement())
        {
            await DisplayAlert("Hinweis", "Rollen-/Rechteverwaltung ist für den aktuellen Kontext nicht freigegeben.", "OK");
            return;
        }

        if (!CanManageRoleManagement())
        {
            await DisplayAlert("Hinweis", "Rollen-/Rechteverwaltung ist in diesem Kontext nur lesend freigegeben.", "OK");
            return;
        }

        if (selectedMember.Id == 7)
        {
            await DisplayAlert("Gesperrt", "Für dieses Mitglied ist die Rollenbearbeitung gesperrt.", "OK");
            return;
        }

        if (!IsRoleDirty())
        {
            await DisplayAlert("Hinweis", "Es gibt keine Rollenänderung zu speichern.", "OK");
            return;
        }

        var userId = _userContextState.CurrentUserId?.ToString();
        if (string.IsNullOrWhiteSpace(userId))
        {
            await DisplayAlert("Fehler", "Nicht angemeldet. Bitte erneut einloggen.", "OK");
            return;
        }

        var lockAcquired = false;
        try
        {
            lockAcquired = await _supabaseService.TryLockMitgliedAsync(selectedMember.Id, userId);
            if (!lockAcquired)
            {
                await DisplayAlert("Gesperrt", "Datensatz ist aktuell gesperrt. Bitte später erneut versuchen.", "OK");
                return;
            }

            var rec = await _supabaseService.GetMitgliedByIdAsync(selectedMember.Id);
            if (rec == null)
            {
                await DisplayAlert("Fehler", "Mitglied konnte nicht geladen werden.", "OK");
                return;
            }

            var dto = new MemberDTO
            {
                Id = rec.Id,
                Vorname = rec.Vorname ?? string.Empty,
                Nachname = rec.Name ?? string.Empty,
                Email = rec.Email ?? string.Empty,
                Role = _selectedRole,
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
                await DisplayAlert("Fehler", "Rolle konnte nicht gespeichert werden.", "OK");
                return;
            }

            _memberContextState.SetSelectedMember(dto.Clone());
            _initialRole = _selectedRole;
            if (_permissionSettings != null)
                _permissionSettings.Role = _selectedRole;

            RefreshPermissionOverrideState();
            UpdateRoleManagementState(dto, true, CanReadRoleManagement());
            await DisplayAlert("Gespeichert", "Rolle wurde gespeichert.", "OK");
        }
        finally
        {
            if (lockAcquired)
                await _supabaseService.ReleaseLockMitgliedAsync(selectedMember.Id, userId, force: false);
        }
    }

    private static string NormalizeRole(string? role)
        => UserRoles.ToStorageValue(UserRoles.Parse(role));

    private bool CanReadRoleManagement()
        => PermissionChecks.CanReadRoleManagement(_userContextState.CurrentUserContext);

    private bool CanManageRoleManagement()
        => PermissionChecks.CanManageRoleManagement(_userContextState.CurrentUserContext);

    private bool IsRoleDirty()
        => !string.Equals(_selectedRole, _initialRole, StringComparison.OrdinalIgnoreCase);

    private sealed class PermissionAreaEditorRow
    {
        private readonly Action _changed;
        private readonly Label _roleBaseValueLabel;
        private readonly Button _noneButton;
        private readonly Button _readButton;
        private readonly Button _writeButton;
        private PermissionAreaAccessLevel _baseLevel;
        private PermissionAreaAccessLevel _selectedLevel;

        public PermissionAreaEditorRow(PermissionAreaDefinition definition, Action changed)
        {
            Definition = definition;
            _changed = changed;
            _roleBaseValueLabel = new Label { TextColor = Colors.Gray, FontSize = 12 };
            _noneButton = CreateLevelButton("Aus", PermissionAreaAccessLevel.None);
            _readButton = CreateLevelButton("Lesen", PermissionAreaAccessLevel.Read);
            _writeButton = CreateLevelButton("Bearbeiten", PermissionAreaAccessLevel.Write);

            var levelGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Star)
                },
                ColumnSpacing = 8
            };
            levelGrid.Add(_noneButton, 0, 0);
            levelGrid.Add(_readButton, 1, 0);
            levelGrid.Add(_writeButton, 2, 0);

            Container = new Border
            {
                Padding = 12,
                Stroke = Colors.LightGray,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(6) },
                Content = new VerticalStackLayout
                {
                    Spacing = 6,
                    Children =
                    {
                        new Label { Text = Definition.DisplayName, FontAttributes = FontAttributes.Bold },
                        _roleBaseValueLabel,
                        levelGrid
                    }
                }
            };
        }

        public PermissionAreaDefinition Definition { get; }
        public Border Container { get; }
        public PermissionAreaAccessLevel SelectedLevel => _selectedLevel;

        public void Apply(UserPermissionSettings? settings, bool canEdit)
        {
            _baseLevel = settings == null
                ? PermissionAreaAccessLevel.None
                : PermissionCatalog.GetAccessLevel(settings.BasePermissions, Definition);
            _selectedLevel = settings == null
                ? PermissionAreaAccessLevel.None
                : PermissionCatalog.GetAccessLevel(settings.EffectivePermissions, Definition);
            SetEditable(canEdit);
            UpdateVisualState();
        }

        public void SetEditable(bool canEdit)
        {
            _noneButton.IsEnabled = canEdit;
            _readButton.IsEnabled = canEdit;
            _writeButton.IsEnabled = canEdit;
        }

        public void ResetToDefault()
        {
            SetLevel(_baseLevel);
        }

        private Button CreateLevelButton(string text, PermissionAreaAccessLevel level)
        {
            var button = new Button
            {
                Text = text,
                Padding = new Thickness(10, 6),
                CornerRadius = 8,
                FontSize = 13
            };
            button.Clicked += (_, _) => SetLevel(level);
            return button;
        }

        private void SetLevel(PermissionAreaAccessLevel level)
        {
            if (_selectedLevel == level)
                return;

            _selectedLevel = level;
            UpdateVisualState();
            _changed();
        }

        private void UpdateVisualState()
        {
            _roleBaseValueLabel.Text = $"Rollenbasis: {PermissionCatalog.FormatAccessLevel(_baseLevel)} · Wirksam: {PermissionCatalog.FormatAccessLevel(_selectedLevel)}";
            ApplyButtonStyle(_noneButton, _selectedLevel == PermissionAreaAccessLevel.None);
            ApplyButtonStyle(_readButton, _selectedLevel == PermissionAreaAccessLevel.Read);
            ApplyButtonStyle(_writeButton, _selectedLevel == PermissionAreaAccessLevel.Write);
        }

        private static void ApplyButtonStyle(Button button, bool isSelected)
        {
            button.BackgroundColor = isSelected ? Color.FromArgb("#0F6CBD") : Color.FromArgb("#F3F4F6");
            button.TextColor = isSelected ? Colors.White : Colors.Black;
        }
    }
}
