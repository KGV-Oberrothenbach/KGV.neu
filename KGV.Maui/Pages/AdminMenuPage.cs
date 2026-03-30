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
    private readonly Label _memberInfoLabel;
    private readonly Label _hintLabel;
    private readonly Button _userManagementButton;

    public AdminMenuPage(UserContextState userContextState, MemberContextState memberContextState)
    {
        _userContextState = userContextState ?? throw new ArgumentNullException(nameof(userContextState));
        _memberContextState = memberContextState ?? throw new ArgumentNullException(nameof(memberContextState));

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
                    _userManagementButton
                }
            }
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshState();
    }

    private void RefreshState()
    {
        var selectedMember = _memberContextState.SelectedMember;
        var isAdmin = _userContextState.CurrentUserContext?.Role == UserRole.Admin;
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
    }
}
