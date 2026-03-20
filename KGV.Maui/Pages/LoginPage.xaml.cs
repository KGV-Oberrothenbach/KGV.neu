using KGV.Core.Interfaces;
using KGV.Core.Security;
using KGV.Maui;
using KGV.Maui.State;
using KGV.Maui.Settings;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace KGV.Maui.Pages;

public class LoginPage : ContentPage
{
    private readonly IAuthService _authService;
    private readonly ISupabaseService _supabaseService;
    private readonly UserContextState _userContextState;
    private readonly IPermissionService _permissionService;
    private readonly IServiceProvider _services;

    private readonly Entry _emailEntry;
    private readonly Entry _passwordEntry;
    private readonly Label _statusLabel;

    public LoginPage(
        IAuthService authService,
        ISupabaseService supabaseService,
        UserContextState userContextState,
        IPermissionService permissionService,
        IServiceProvider services)
    {
        _authService = authService;
        _supabaseService = supabaseService;
        _userContextState = userContextState;
        _permissionService = permissionService;
        _services = services;

        Title = "Login";

        _emailEntry = new Entry { Placeholder = "E-Mail", Keyboard = Keyboard.Email, Text = AppSettings.LastEmail ?? string.Empty };
        _passwordEntry = new Entry { Placeholder = "Passwort", IsPassword = true };
        _statusLabel = new Label { TextColor = Colors.Red };

        var loginButton = new Button { Text = "Anmelden" };
        loginButton.Clicked += OnLoginClicked;

        Content = new VerticalStackLayout
        {
            Padding = 24,
            Spacing = 12,
            Children =
            {
                new Label { Text = "Login", FontSize = 24, FontAttributes = FontAttributes.Bold },
                _emailEntry,
                _passwordEntry,
                loginButton,
                _statusLabel
            }
        };
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        _statusLabel.Text = string.Empty;

        var email = (_emailEntry.Text ?? string.Empty).Trim();
        var password = _passwordEntry.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            _statusLabel.Text = "Bitte E-Mail und Passwort eingeben.";
            return;
        }

        try
        {
            var ok = await _authService.LoginAsync(email, password);
            if (!ok)
            {
                _statusLabel.Text = "Login fehlgeschlagen.";
                return;
            }

            AppSettings.LastEmail = email;
            AppSettings.Save();

            if (string.IsNullOrWhiteSpace(_authService.CurrentUserId) || !Guid.TryParse(_authService.CurrentUserId, out var userId))
            {
                _statusLabel.Text = "Login ok, aber UserId ist ungültig.";
                return;
            }

            _userContextState.CurrentUserId = userId;

            var mitglied = await _supabaseService.GetMitgliedByAuthUserIdAsync(userId);
            _userContextState.CurrentMitgliedId = mitglied?.Id;

            if (mitglied != null)
            {
                var neben = await _supabaseService.GetNebenmitgliedByHauptmitgliedIdAsync(mitglied.Id);
                _userContextState.CurrentNebenMitgliedId = neben?.Id;
            }
            else
            {
                _userContextState.CurrentNebenMitgliedId = null;
            }

            var parsedMode = AppModes.Parse(AppSettings.AppMode);
            if (parsedMode != null)
            {
                SwitchToMode(parsedMode.Value);
                return;
            }

            var roleChoice = _services.GetRequiredService<RoleChoicePage>();
            await Navigation.PushAsync(roleChoice);
        }
        catch (Exception ex)
        {
            _statusLabel.Text = ex.Message;
        }
    }

    private void SwitchToMode(AppMode mode)
    {
        if (_userContextState.CurrentUserId == null)
            return;

        if (mode == AppMode.User && _userContextState.CurrentMitgliedId == null)
        {
            _statusLabel.Text = "Account ist keinem Mitglied zugeordnet.";
            return;
        }

        _userContextState.CurrentAppMode = mode;

        var role = mode == AppMode.Admin ? UserRoles.Vorstand : UserRoles.User;
        _userContextState.CurrentUserContext = _permissionService.CreateContext(
            _userContextState.CurrentUserId.Value,
            role,
            _userContextState.CurrentMitgliedId);

        var window = Application.Current?.Windows?.FirstOrDefault();
        if (window == null)
            return;

        window.Page = mode == AppMode.Admin
            ? BuildAndGetShell(_services.GetRequiredService<AdminShell>())
            : BuildAndGetShell(_services.GetRequiredService<UserShell>());
    }

    private static Shell BuildAndGetShell(Shell shell)
    {
        if (shell is IAppShellInitializer init)
            init.BuildMenu();

        return shell;
    }
}
