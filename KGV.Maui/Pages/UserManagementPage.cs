using KGV.Core.Models;
using KGV.Maui.ViewModels;

namespace KGV.Maui.Pages;

public sealed class UserManagementPage : ContentPage
{
    private readonly UserManagementViewModel _viewModel;
    private bool _initialized;

    public UserManagementPage(UserManagementViewModel viewModel)
    {
        _viewModel = viewModel;
        BindingContext = _viewModel;
        Title = "Benutzerverwaltung";

        var titleLabel = new Label { FontSize = 24, FontAttributes = FontAttributes.Bold };
        titleLabel.SetBinding(Label.TextProperty, nameof(UserManagementViewModel.Title));

        var descriptionLabel = new Label { LineBreakMode = LineBreakMode.WordWrap };
        descriptionLabel.SetBinding(Label.TextProperty, nameof(UserManagementViewModel.Description));

        var hintLabel = new Label { TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
        hintLabel.SetBinding(Label.TextProperty, nameof(UserManagementViewModel.AdminHint));

        var refreshButton = new Button { Text = "Aktualisieren" };
        refreshButton.Clicked += async (_, _) => await _viewModel.RefreshAsync();

        var usersView = new CollectionView
        {
            SelectionMode = SelectionMode.Single,
            HeightRequest = 320,
            ItemTemplate = new DataTemplate(() =>
            {
                var name = new Label { FontAttributes = FontAttributes.Bold };
                name.SetBinding(Label.TextProperty, nameof(AppUserDTO.DisplayName));

                var email = new Label { FontSize = 12, TextColor = Colors.Gray };
                email.SetBinding(Label.TextProperty, nameof(AppUserDTO.Email));

                var role = new Label { FontSize = 12, TextColor = Colors.Gray };
                role.SetBinding(Label.TextProperty, new Binding(nameof(AppUserDTO.Role), stringFormat: "Rolle: {0}"));

                return new Border
                {
                    Padding = 12,
                    Margin = new Thickness(0, 0, 0, 8),
                    Stroke = Colors.LightGray,
                    Content = new VerticalStackLayout
                    {
                        Spacing = 4,
                        Children = { name, email, role }
                    }
                };
            })
        };
        usersView.SetBinding(ItemsView.ItemsSourceProperty, nameof(UserManagementViewModel.Users));
        usersView.SetBinding(SelectableItemsView.SelectedItemProperty, nameof(UserManagementViewModel.SelectedUser), BindingMode.TwoWay);

        var selectedSection = new VerticalStackLayout { Spacing = 6 };
        selectedSection.SetBinding(IsVisibleProperty, nameof(UserManagementViewModel.HasSelectedUser));
        selectedSection.Children.Add(CreateValueLabel("Name", "SelectedUser.DisplayName"));
        selectedSection.Children.Add(CreateValueLabel("E-Mail", "SelectedUser.Email"));
        selectedSection.Children.Add(CreateValueLabel("Rolle", "SelectedUser.Role"));
        selectedSection.Children.Add(CreateValueLabel("Mitglied", "SelectedUser.MitgliedId"));

        var rolePicker = new Picker { Title = "Rolle wählen" };
        rolePicker.SetBinding(Picker.ItemsSourceProperty, nameof(UserManagementViewModel.Roles));
        rolePicker.SetBinding(Picker.SelectedItemProperty, nameof(UserManagementViewModel.SelectedRole), BindingMode.TwoWay);
        rolePicker.SetBinding(IsEnabledProperty, nameof(UserManagementViewModel.IsRoleEditable));

        var roleHintLabel = new Label
        {
            Text = "Rollenbearbeitung für dieses Mitglied ist gesperrt.",
            TextColor = Colors.DarkRed,
            LineBreakMode = LineBreakMode.WordWrap
        };
        roleHintLabel.SetBinding(IsVisibleProperty, nameof(UserManagementViewModel.IsRoleEditable), converter: new InverseBooleanConverter());

        var saveRoleButton = new Button { Text = "Rolle speichern" };
        saveRoleButton.SetBinding(IsEnabledProperty, nameof(UserManagementViewModel.CanSaveRole));
        saveRoleButton.Clicked += async (_, _) =>
        {
            var ok = await _viewModel.SaveRoleAsync();
            if (ok)
                await DisplayAlert("OK", "Rolle gespeichert.", "OK");
        };

        var roleSection = new VerticalStackLayout { Spacing = 8 };
        roleSection.SetBinding(IsVisibleProperty, nameof(UserManagementViewModel.HasSelectedUser));
        roleSection.Children.Add(new Label { Text = "Rolle", FontAttributes = FontAttributes.Bold });
        roleSection.Children.Add(rolePicker);
        roleSection.Children.Add(roleHintLabel);
        roleSection.Children.Add(saveRoleButton);

        var inviteButton = new Button { Text = "Einladung / Erstlogin-Code senden" };
        inviteButton.SetBinding(IsEnabledProperty, nameof(UserManagementViewModel.CanInvite));
        inviteButton.Clicked += async (_, _) =>
        {
            var ok = await _viewModel.InviteAsync();
            if (ok)
                await DisplayAlert("OK", "Einladung bzw. Erstlogin-Code wurde angestoßen.", "OK");
        };

        var resetButton = new Button { Text = "Passwort-Reset senden" };
        resetButton.SetBinding(IsEnabledProperty, nameof(UserManagementViewModel.CanResetPassword));
        resetButton.Clicked += async (_, _) =>
        {
            var ok = await _viewModel.SendPasswordResetAsync();
            if (ok)
                await DisplayAlert("OK", "Passwort-Reset wurde angestoßen.", "OK");
        };

        var changeEmailButton = new Button { Text = "Eigene E-Mail ändern" };
        changeEmailButton.SetBinding(IsEnabledProperty, nameof(UserManagementViewModel.CanChangeSelectedEmail));
        changeEmailButton.SetBinding(IsVisibleProperty, nameof(UserManagementViewModel.CanChangeSelectedEmail));
        changeEmailButton.Clicked += async (_, _) =>
        {
            var currentEmail = _viewModel.SelectedUser?.Email ?? string.Empty;
            var newEmail = await DisplayPromptAsync("E-Mail ändern", "Neue E-Mail-Adresse eingeben", initialValue: currentEmail, keyboard: Keyboard.Email);
            if (string.IsNullOrWhiteSpace(newEmail))
                return;

            var requested = await _viewModel.RequestEmailChangeAsync(newEmail);
            if (!requested)
                return;

            var otpCode = await DisplayPromptAsync("OTP-Code", "Code aus der neuen E-Mail-Adresse eingeben", keyboard: Keyboard.Numeric);
            if (string.IsNullOrWhiteSpace(otpCode))
                return;

            var verified = await _viewModel.VerifyEmailChangeAsync(newEmail, otpCode);
            if (verified)
                await DisplayAlert("OK", "Mailadresse erfolgreich geändert.", "OK");
        };

        var emailHintLabel = new Label
        {
            Text = "Die E-Mail-Änderung bleibt mobil wie in WPF auf das aktuell angemeldete Konto begrenzt.",
            TextColor = Colors.Gray,
            LineBreakMode = LineBreakMode.WordWrap
        };

        var statusLabel = new Label { TextColor = Colors.DarkSlateBlue, LineBreakMode = LineBreakMode.WordWrap };
        statusLabel.SetBinding(Label.TextProperty, nameof(UserManagementViewModel.StatusMessage));
        statusLabel.SetBinding(IsVisibleProperty, nameof(UserManagementViewModel.HasStatusMessage));

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 12,
                Children =
                {
                    titleLabel,
                    descriptionLabel,
                    hintLabel,
                    refreshButton,
                    usersView,
                    selectedSection,
                    roleSection,
                    emailHintLabel,
                    inviteButton,
                    resetButton,
                    changeEmailButton,
                    statusLabel
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_initialized)
            return;

        await _viewModel.InitializeAsync();
        _initialized = true;
    }

    private static View CreateValueLabel(string title, string path)
    {
        var valueLabel = new Label { LineBreakMode = LineBreakMode.WordWrap };
        valueLabel.SetBinding(Label.TextProperty, path);

        return new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                new Label { Text = title, FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Colors.Gray },
                valueLabel
            }
        };
    }

    private sealed class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
            => value is bool b ? !b : true;

        public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
            => throw new NotSupportedException();
    }
}
