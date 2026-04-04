using KGV.Core.Interfaces;
using KGV.Core.Security;
using KGV.Maui.State;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
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
    private bool _allowUserMeterReadingSubmissions;

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
                    new Label { Text = "Zählerablesungen", FontAttributes = FontAttributes.Bold, Margin = new Thickness(0, 12, 0, 0) },
                    new HorizontalStackLayout
                    {
                        Spacing = 12,
                        Children =
                        {
                            new Label
                            {
                                Text = "Normale Nutzer dürfen eigene Zählerablesungen einreichen",
                                VerticalOptions = LayoutOptions.Center,
                                LineBreakMode = LineBreakMode.WordWrap,
                                HorizontalOptions = LayoutOptions.FillAndExpand
                            },
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

        if (canManageMeterReadingSubmissions)
            _allowUserMeterReadingSubmissions = await _supabaseService.GetAllowUserMeterReadingSubmissionsAsync();
        else
            _allowUserMeterReadingSubmissions = false;

        _meterReadingSubmissionsSwitch.IsToggled = _allowUserMeterReadingSubmissions;
        _meterReadingSubmissionsSwitch.IsEnabled = canManageMeterReadingSubmissions;
        UpdateMeterReadingSettingState();
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
}
