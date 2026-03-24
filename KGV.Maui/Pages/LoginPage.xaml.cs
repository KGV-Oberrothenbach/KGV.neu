using KGV.Core.Interfaces;
using KGV.Core.Security;
using KGV.Infrastructure.Services;
using KGV.Maui;
using KGV.Maui.State;
using KGV.Maui.Settings;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace KGV.Maui.Pages;

public class LoginPage : ContentPage
{
    private const string LogoImageSource = "kgv_logo.svg";

    private readonly IAuthService _authService;
    private readonly ISupabaseService _supabaseService;
    private readonly UserContextState _userContextState;
    private readonly IUserContextService _userContextService;
    private readonly IServiceProvider _services;

    private readonly Entry _emailEntry;
    private readonly Entry _passwordEntry;
    private readonly Label _statusLabel;

    public LoginPage(
        IAuthService authService,
        ISupabaseService supabaseService,
        UserContextState userContextState,
        IUserContextService userContextService,
        IServiceProvider services)
    {
        _authService = authService;
        _supabaseService = supabaseService;
        _userContextState = userContextState;
        _userContextService = userContextService;
        _services = services;

        Title = "Login";

        _emailEntry = new Entry { Placeholder = "E-Mail", Keyboard = Keyboard.Email, Text = AppSettings.LastEmail ?? string.Empty };
        _passwordEntry = new Entry { Placeholder = "Passwort", IsPassword = true };
        _statusLabel = new Label { TextColor = Colors.Red };
        var logoImage = new Image
        {
            Source = LogoImageSource,
            HeightRequest = 120,
            Aspect = Aspect.AspectFit,
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 0, 0, 8)
        };
        var otpEntry = new Entry { Placeholder = "OTP-Code", IsVisible = false };
        var newPasswordEntry = new Entry { Placeholder = "Neues Passwort", IsPassword = true, IsVisible = false };
        var confirmPasswordEntry = new Entry { Placeholder = "Passwort wiederholen", IsPassword = true, IsVisible = false };
        var passwordHintLabel = new Label { Text = "Passwortbedingungen: mindestens 8 Zeichen und identische Wiederholung.", TextColor = Colors.Gray, IsVisible = false };
        var togglePasswordButton = new Button { Text = "Passwort anzeigen" };
        var loginButton = new Button
        {
            Text = "Anmelden",
            Padding = new Thickness(16, 12),
            FontAttributes = FontAttributes.Bold
        };
        var setPasswordButton = new Button { Text = "Neues Passwort setzen", IsVisible = false };
        var verifyOtpButton = new Button { Text = "Code prüfen", IsVisible = false };
        var requestOtpButton = new Button { Text = "Einladung / Erstlogin-Code anfordern" };
        var forgotPasswordButton = new Button { Text = "Passwort vergessen" };

        void ShowNormalLogin()
        {
            _passwordEntry.IsVisible = true;
            togglePasswordButton.IsVisible = true;
            loginButton.IsVisible = true;
            requestOtpButton.IsVisible = true;
            forgotPasswordButton.IsVisible = true;

            otpEntry.IsVisible = false;
            verifyOtpButton.IsVisible = false;
            newPasswordEntry.IsVisible = false;
            confirmPasswordEntry.IsVisible = false;
            passwordHintLabel.IsVisible = false;
            setPasswordButton.IsVisible = false;
        }

        void ShowOtpVerification()
        {
            _passwordEntry.IsVisible = false;
            togglePasswordButton.IsVisible = false;
            loginButton.IsVisible = false;
            requestOtpButton.IsVisible = false;
            forgotPasswordButton.IsVisible = false;

            otpEntry.IsVisible = true;
            verifyOtpButton.IsVisible = true;
            newPasswordEntry.IsVisible = false;
            confirmPasswordEntry.IsVisible = false;
            passwordHintLabel.IsVisible = false;
            setPasswordButton.IsVisible = false;
        }

        void ShowSetPassword()
        {
            _passwordEntry.IsVisible = false;
            togglePasswordButton.IsVisible = false;
            loginButton.IsVisible = false;
            requestOtpButton.IsVisible = false;
            forgotPasswordButton.IsVisible = false;

            otpEntry.IsVisible = false;
            verifyOtpButton.IsVisible = false;
            newPasswordEntry.IsVisible = true;
            confirmPasswordEntry.IsVisible = true;
            passwordHintLabel.IsVisible = true;
            setPasswordButton.IsVisible = true;
        }

        togglePasswordButton.Clicked += (s, e) =>
        {
            _passwordEntry.IsPassword = !_passwordEntry.IsPassword;
            togglePasswordButton.Text = _passwordEntry.IsPassword ? "Passwort anzeigen" : "Passwort ausblenden";
        };
        loginButton.Clicked += OnLoginClicked;
        verifyOtpButton.Clicked += async (s, e) =>
        {
            _statusLabel.Text = string.Empty;
            var email = (_emailEntry.Text ?? string.Empty).Trim();
            var code = otpEntry.Text ?? string.Empty;
            if (await _authService.VerifyOtpAsync(email, code))
            {
                ShowSetPassword();
                _statusLabel.Text = "Code bestätigt. Neues Passwort setzen.";
            }
            else
            {
                _statusLabel.Text = "Code ungültig.";
            }
        };

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
                otpEntry.Text = string.Empty;
                ShowOtpVerification();
                _statusLabel.Text = "Einladungs-/Erstlogin-Code wurde versendet. Bitte OTP eingeben.";
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
                otpEntry.Text = string.Empty;
                newPasswordEntry.Text = string.Empty;
                confirmPasswordEntry.Text = string.Empty;
                _passwordEntry.Text = string.Empty;
                ShowNormalLogin();
            }
            else
            {
                _statusLabel.Text = "Neues Passwort konnte nicht gesetzt werden.";
            }
        };

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
                ? "OTP-Code für Passwort-vergessen wurde versendet. Bitte danach im Login den Code prüfen und ein neues Passwort setzen."
                : "Passwort-Reset konnte nicht versendet werden.";
        };

        Content = new VerticalStackLayout
        {
            Padding = 24,
            Spacing = 12,
            Children =
            {
                logoImage,
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

        ShowNormalLogin();
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

            var userContext = await _userContextService.GetUserContextAsync(userId);
            _userContextState.CurrentUserContext = userContext;
            _userContextState.CurrentMitgliedId = userContext.MitgliedId;
            _userContextState.CurrentAppMode = userContext.Role is UserRole.Admin or UserRole.Vorstand
                ? AppMode.Admin
                : AppMode.User;

            if (userContext.Role == UserRole.User && userContext.MitgliedId == null)
            {
                _statusLabel.Text = "Account ist keinem Mitglied zugeordnet.";
                return;
            }

            if (userContext.MitgliedId is > 0 and <= int.MaxValue)
            {
                var neben = await _supabaseService.GetNebenmitgliedByHauptmitgliedIdAsync((int)userContext.MitgliedId.Value);
                _userContextState.CurrentNebenMitgliedId = neben?.Id;
            }
            else
            {
                _userContextState.CurrentNebenMitgliedId = null;
            }

            AppSettings.AppMode = null;
            AppSettings.Save();

            await SwitchToUserContextAsync(userContext);
        }
        catch (Exception ex)
        {
            _statusLabel.Text = ex.Message;
        }
    }

    private async Task SwitchToUserContextAsync(UserContext userContext)
    {
        if (_userContextState.CurrentUserId == null)
            return;

        var mode = userContext.Role is UserRole.Admin or UserRole.Vorstand
            ? AppMode.Admin
            : AppMode.User;

        if (mode == AppMode.User && _userContextState.CurrentMitgliedId == null)
        {
            _statusLabel.Text = "Account ist keinem Mitglied zugeordnet.";
            return;
        }

        _userContextState.CurrentAppMode = mode;
        _userContextState.CurrentUserContext = userContext;

        if (Application.Current is App app)
        {
            await app.SwitchToCurrentRootAsync();
            return;
        }

        var window = Application.Current?.Windows?.FirstOrDefault();
        if (window == null)
            return;

        var shell = mode == AppMode.Admin
            ? (Shell)_services.GetRequiredService<AdminShell>()
            : _services.GetRequiredService<UserShell>();

        if (shell is IAppShellInitializer init)
            init.BuildMenu();

        ShellNavigationHelper.EnsureActiveShellItem(shell);
        window.Page = shell;
    }
}
