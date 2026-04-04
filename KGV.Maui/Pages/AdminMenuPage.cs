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
    private readonly Label _permissionRoleBasisLabel;
    private readonly Label _permissionOverrideStateLabel;
    private readonly Label _effectivePermissionStateLabel;
    private readonly Label _permissionEditorHintLabel;
    private readonly Button _savePermissionOverridesButton;
    private readonly VerticalStackLayout _permissionOverridesLayout;
    private readonly List<PermissionOverrideEditorRow> _permissionOverrideRows = new();
    private bool _allowUserMeterReadingSubmissions;
    private UserPermissionSettings? _permissionSettings;
    private bool _suppressPermissionOverrideRefresh;

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

        _permissionRoleBasisLabel = new Label { LineBreakMode = LineBreakMode.WordWrap };
        _permissionOverrideStateLabel = new Label { LineBreakMode = LineBreakMode.WordWrap };
        _effectivePermissionStateLabel = new Label { LineBreakMode = LineBreakMode.WordWrap };
        _permissionEditorHintLabel = new Label { TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
        _permissionOverridesLayout = new VerticalStackLayout { Spacing = 12 };

        foreach (var definition in PermissionCatalog.GetUserSpecificEditablePermissions())
        {
            var row = new PermissionOverrideEditorRow(definition, OnPermissionOverrideChanged);
            _permissionOverrideRows.Add(row);
            _permissionOverridesLayout.Children.Add(row.Container);
        }

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
                    new Label { Text = "Benutzerspezifische Fachrechte", FontAttributes = FontAttributes.Bold, Margin = new Thickness(0, 12, 0, 0) },
                    _permissionRoleBasisLabel,
                    _permissionEditorHintLabel,
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
        var isAdmin = _userContextState.CurrentUserContext?.Role == UserRole.Admin;
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
        _permissionSettings = hasSelectedMember
            ? await _supabaseService.GetUserPermissionSettingsAsync(selectedMember!.Id)
            : null;

        if (_permissionSettings == null && hasSelectedMember)
        {
            _permissionSettings = new UserPermissionSettings
            {
                MitgliedId = selectedMember!.Id,
                Role = NormalizeRole(selectedMember.Role),
                GrantedPermissions = PermissionFlags.None,
                RevokedPermissions = PermissionFlags.None
            };
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
           && _userContextState.CurrentUserContext?.Role is UserRole.Admin or UserRole.Vorstand
           && _permissionSettings?.HasLinkedUser == true;

    private PermissionFlags CurrentGrantedPermissions
        => _permissionOverrideRows.Aggregate(PermissionFlags.None, (current, row) => current | row.GrantedPermissions);

    private PermissionFlags CurrentRevokedPermissions
        => _permissionOverrideRows.Aggregate(PermissionFlags.None, (current, row) => current | row.RevokedPermissions);

    private void OnPermissionOverrideChanged()
    {
        if (_suppressPermissionOverrideRefresh)
            return;

        RefreshPermissionOverrideState();
    }

    private void RefreshPermissionOverrideState()
    {
        var canEditPermissionOverrides = CanEditPermissionOverrides();
        foreach (var row in _permissionOverrideRows)
            row.SetEditable(canEditPermissionOverrides);

        var roleBasis = _permissionSettings == null
            ? "Rollenbasis: –"
            : $"Rollenbasis: {UserRoles.ToStorageValue(_permissionSettings.ParsedRole)}";

        var linkedUser = _permissionSettings?.HasLinkedUser == true ? "App-User verknüpft" : "Kein App-User verknüpft";
        _permissionRoleBasisLabel.Text = $"{roleBasis} · {linkedUser}";
        _permissionEditorHintLabel.Text = _permissionSettings?.HasLinkedUser == true
            ? "Benutzerspezifische Fachrechte werden zentral als Grants/Revocations über der Rollenbasis gespeichert."
            : "Für dieses Mitglied existiert aktuell kein verknüpfter App-User. Die Rechteübersicht bleibt sichtbar, Speichern ist gesperrt.";

        var effectivePermissions = _permissionSettings == null
            ? PermissionFlags.None
            : PermissionService.ApplyOverrides(
                _permissionSettings.BasePermissions,
                CurrentGrantedPermissions,
                CurrentRevokedPermissions);

        _permissionOverrideStateLabel.Text = $"Aktueller Override-Zustand: Gewährt: {PermissionCatalog.FormatPermissions(CurrentGrantedPermissions, "keine")} · Entzogen: {PermissionCatalog.FormatPermissions(CurrentRevokedPermissions, "keine")}";
        _effectivePermissionStateLabel.Text = $"Wirksame Rechte: {PermissionCatalog.FormatPermissions(effectivePermissions, "Keine wirksamen Fachrechte aktiv.")}";
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

        if (!CanEditPermissionOverrides())
        {
            await DisplayAlert("Hinweis", "Für dieses Mitglied existiert aktuell kein verknüpfter App-User. Fachrechte können deshalb noch nicht gespeichert werden.", "OK");
            return;
        }

        var ok = await _supabaseService.SetUserPermissionSettingsAsync(
            selectedMember.Id,
            _permissionSettings?.Role ?? NormalizeRole(selectedMember.Role),
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

    private static string NormalizeRole(string? role)
        => UserRoles.ToStorageValue(UserRoles.Parse(role));

    private sealed class PermissionOverrideEditorRow
    {
        private const string DefaultMode = "Standard";
        private const string GrantMode = "Zusätzlich gewähren";
        private const string RevokeMode = "Entziehen";
        private readonly Action _changed;
        private readonly Label _roleBaseValueLabel;
        private readonly Label _effectiveValueLabel;

        public PermissionOverrideEditorRow(PermissionDefinition definition, Action changed)
        {
            Definition = definition;
            _changed = changed;
            OverridePicker = new Picker
            {
                Title = "Override wählen",
                ItemsSource = new List<string> { DefaultMode, GrantMode, RevokeMode }
            };
            OverridePicker.SelectedIndexChanged += (_, _) =>
            {
                UpdateLabels();
                _changed();
            };

            _roleBaseValueLabel = new Label { TextColor = Colors.Gray, FontSize = 12 };
            _effectiveValueLabel = new Label { FontSize = 12 };

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
                        OverridePicker,
                        _effectiveValueLabel
                    }
                }
            };
        }

        public PermissionDefinition Definition { get; }
        public Border Container { get; }
        public Picker OverridePicker { get; }
        public bool IsBaseGranted { get; private set; }

        public PermissionFlags GrantedPermissions => SelectedMode == GrantMode ? Definition.Flag : PermissionFlags.None;
        public PermissionFlags RevokedPermissions => SelectedMode == RevokeMode ? Definition.Flag : PermissionFlags.None;

        private string SelectedMode => OverridePicker.SelectedItem as string ?? DefaultMode;

        public void Apply(UserPermissionSettings? settings, bool canEdit)
        {
            IsBaseGranted = settings?.BasePermissions.HasFlag(Definition.Flag) == true;
            OverridePicker.SelectedItem = settings?.GrantedPermissions.HasFlag(Definition.Flag) == true
                ? GrantMode
                : settings?.RevokedPermissions.HasFlag(Definition.Flag) == true
                    ? RevokeMode
                    : DefaultMode;
            OverridePicker.IsEnabled = canEdit;
            UpdateLabels();
        }

        public void SetEditable(bool canEdit)
            => OverridePicker.IsEnabled = canEdit;

        private void UpdateLabels()
        {
            _roleBaseValueLabel.Text = $"Rollenbasis: {(IsBaseGranted ? "Ja" : "Nein")} · Override: {SelectedMode}";
            var isEffectiveGranted = SelectedMode switch
            {
                GrantMode => true,
                RevokeMode => false,
                _ => IsBaseGranted
            };
            _effectiveValueLabel.Text = $"Wirksames Recht: {(isEffectiveGranted ? "Ja" : "Nein")}";
        }
    }
}
