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
        var otpEntry = new Entry { Placeholder = "OTP-Code", IsVisible = false };
        var newPasswordEntry = new Entry { Placeholder = "Neues Passwort", IsPassword = true, IsVisible = false };
        var confirmPasswordEntry = new Entry { Placeholder = "Passwort wiederholen", IsPassword = true, IsVisible = false };
        var passwordHintLabel = new Label { Text = "Passwortbedingungen: mindestens 8 Zeichen und identische Wiederholung.", TextColor = Colors.Gray, IsVisible = false };
        var togglePasswordButton = new Button { Text = "Passwort anzeigen" };
        togglePasswordButton.Clicked += (s, e) =>
        {
            _passwordEntry.IsPassword = !_passwordEntry.IsPassword;
            togglePasswordButton.Text = _passwordEntry.IsPassword ? "Passwort anzeigen" : "Passwort ausblenden";
        };

        var loginButton = new Button { Text = "Anmelden" };
        loginButton.Clicked += OnLoginClicked;

        var setPasswordButton = new Button { Text = "Neues Passwort setzen", IsVisible = false };

        var verifyOtpButton = new Button { Text = "Code prüfen", IsVisible = false };
        verifyOtpButton.Clicked += async (s, e) =>
        {
            _statusLabel.Text = string.Empty;
            var email = (_emailEntry.Text ?? string.Empty).Trim();
            var code = otpEntry.Text ?? string.Empty;
            if (await _authService.VerifyOtpAsync(email, code))
            {
                newPasswordEntry.IsVisible = true;
                confirmPasswordEntry.IsVisible = true;
                passwordHintLabel.IsVisible = true;
                setPasswordButton.IsVisible = true;
                verifyOtpButton.IsVisible = false;
                _statusLabel.Text = "Code bestätigt. Neues Passwort setzen.";
            }
            else
            {
                _statusLabel.Text = "Code ungültig.";
            }
        };

        var requestOtpButton = new Button { Text = "Erstlogin / OTP anfordern" };
        requestOtpButton.Clicked += async (s, e) =>
        {
            _statusLabel.Text = string.Empty;
            var email = (_emailEntry.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(email))
            {
                _statusLabel.Text = "Bitte E-Mail eingeben.";
                return;
            }

            var ok = await _authService.RequestOtpAsync(email);
            if (ok)
            {
                otpEntry.IsVisible = true;
                verifyOtpButton.IsVisible = true;
                newPasswordEntry.IsVisible = false;
                confirmPasswordEntry.IsVisible = false;
                passwordHintLabel.IsVisible = false;
                setPasswordButton.IsVisible = false;
                _statusLabel.Text = "Code wurde versendet. Bitte OTP eingeben.";
            }
            else
            {
                _statusLabel.Text = "OTP-Anforderung fehlgeschlagen.";
            }
        };

        setPasswordButton.Clicked += async (s, e) =>
        {
            _statusLabel.Text = string.Empty;
            var email = (_emailEntry.Text ?? string.Empty).Trim();
            var code = otpEntry.Text ?? string.Empty;
            var newPwd = newPasswordEntry.Text ?? string.Empty;
            var confirmPwd = confirmPasswordEntry.Text ?? string.Empty;
            if (newPwd.Length < 8)
            {
                _statusLabel.Text = "Passwort muss mindestens 8 Zeichen haben.";
                return;
            }

            if (!string.Equals(newPwd, confirmPwd, StringComparison.Ordinal))
            {
                _statusLabel.Text = "Passwort und Wiederholung stimmen nicht überein.";
                return;
            }

            var ok = await _authService.SetPasswordWithOtpAsync(email, code, newPwd);
            if (ok)
            {
                _statusLabel.Text = "Passwort gesetzt. Bitte normal anmelden.";
                otpEntry.IsVisible = false;
                newPasswordEntry.IsVisible = false;
                confirmPasswordEntry.IsVisible = false;
                passwordHintLabel.IsVisible = false;
                setPasswordButton.IsVisible = false;
                otpEntry.Text = string.Empty;
                newPasswordEntry.Text = string.Empty;
                confirmPasswordEntry.Text = string.Empty;
                _passwordEntry.Text = string.Empty;
            }
            else
            {
                _statusLabel.Text = "Neues Passwort konnte nicht gesetzt werden.";
            }
        };

        var forgotPasswordButton = new Button { Text = "Passwort vergessen" };
        forgotPasswordButton.Clicked += async (s, e) =>
        {
            _statusLabel.Text = string.Empty;
            var email = (_emailEntry.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(email))
            {
                _statusLabel.Text = "Bitte E-Mail eingeben.";
                return;
            }

            var ok = await _authService.SendPasswordResetEmailAsync(email);
            _statusLabel.Text = ok
                ? "Passwort-Reset wurde versendet."
                : "Passwort-Reset konnte nicht versendet werden.";
        };

        Content = new VerticalStackLayout
        {
            Padding = 24,
            Spacing = 12,
            Children =
            {
                new Label { Text = "Login", FontSize = 24, FontAttributes = FontAttributes.Bold },
                _emailEntry,
                _passwordEntry,
                togglePasswordButton,
                requestOtpButton,
                otpEntry,
                verifyOtpButton,
                newPasswordEntry,
                confirmPasswordEntry,
                passwordHintLabel,
                setPasswordButton,
                forgotPasswordButton,
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
