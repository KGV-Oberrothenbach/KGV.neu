using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Infrastructure.Authentication;
using KGV.Infrastructure.Services;
using KGV.Maui;
using KGV.Maui.Services.Diagnostics;
using KGV.Maui.State;
using KGV.Maui.Settings;
using KGV.Maui.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Linq;

namespace KGV.Maui.Pages;

public class LoginPage : ContentPage
{
    private const string NeutralLogoImageSource = "kgv_neutral_logo.png";
    private const string DemoLogoImageSource = "kgv_neutral_demo_logo.png";
    private const string OberrothenbachWappenImageSource = "kgv_logo.png";

    private readonly IAuthService _authService;
    private readonly ISupabaseService _supabaseService;
    private readonly UserContextState _userContextState;
    private readonly IUserContextService _userContextService;
    private readonly IVereinskontext _vereinskontext;
    private readonly BiometricSessionStore _biometricSessionStore;
    private readonly IBiometricAuthenticationService _biometricAuthenticationService;

    private readonly Entry _emailEntry;
    private readonly Entry _passwordEntry;
    private readonly Label _statusLabel;
    private readonly Label _otpDiagnosticLabel;
    private readonly Button _copyOtpDiagnosticButton;
    private string? _lastOtpDiagnosticCode;
    private bool _biometricPromptStarted;

    public LoginPage(
        IAuthService authService,
        ISupabaseService supabaseService,
        UserContextState userContextState,
        IUserContextService userContextService,
        IVereinskontext vereinskontext, BiometricSessionStore biometricSessionStore, IBiometricAuthenticationService biometricAuthenticationService)
    {
        _authService = authService;
        _supabaseService = supabaseService;
        _userContextState = userContextState;
        _userContextService = userContextService;
        _vereinskontext = vereinskontext;
        _biometricSessionStore = biometricSessionStore;
        _biometricAuthenticationService = biometricAuthenticationService;

        Title = "Login";

        _emailEntry = new Entry { Placeholder = "E-Mail", Keyboard = Keyboard.Email, Text = AppSettings.LastEmail ?? string.Empty };
        _passwordEntry = new Entry { Placeholder = "Passwort", IsPassword = true, BackgroundColor = Colors.Transparent };
        _statusLabel = new Label { TextColor = Colors.Red, LineBreakMode = LineBreakMode.WordWrap };
        _otpDiagnosticLabel = new Label { TextColor = Colors.Gray, FontSize = 12, LineBreakMode = LineBreakMode.WordWrap, IsVisible = false };
        _copyOtpDiagnosticButton = new Button { Text = "Code kopieren", IsVisible = false, FontSize = 12 };
        _copyOtpDiagnosticButton.Clicked += async (_, _) => await CopyOtpDiagnosticCodeAsync();
        var logoImage = new Image
        {
            Source = ResolveLogoImageSource(),
            HeightRequest = 120,
            Aspect = Aspect.AspectFit,
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 0, 0, 8)
        };
        var otpEntry = new Entry { Placeholder = "OTP-Code", IsVisible = false };
        var newPasswordEntry = new Entry { Placeholder = "Neues Passwort", IsPassword = true, BackgroundColor = Colors.Transparent };
        var confirmPasswordEntry = new Entry { Placeholder = "Passwort wiederholen", IsPassword = true, BackgroundColor = Colors.Transparent };
        var passwordRulesTitle = new Label { Text = "Passwortbedingungen", FontAttributes = FontAttributes.Bold, IsVisible = false };
        var minLengthRuleLabel = new Label { TextColor = Colors.Gray, IsVisible = false };
        var upperLowerRuleLabel = new Label { TextColor = Colors.Gray, IsVisible = false };
        var digitRuleLabel = new Label { TextColor = Colors.Gray, IsVisible = false };
        var specialRuleLabel = new Label { TextColor = Colors.Gray, IsVisible = false };
        var confirmationRuleLabel = new Label { TextColor = Colors.Gray, IsVisible = false };
        var passwordHintLayout = new VerticalStackLayout
        {
            Spacing = 4,
            IsVisible = false,
            Children =
            {
                passwordRulesTitle,
                minLengthRuleLabel,
                upperLowerRuleLabel,
                digitRuleLabel,
                specialRuleLabel,
                confirmationRuleLabel
            }
        };

        var togglePasswordButton = CreateVisibilityButton(_passwordEntry);
        var toggleNewPasswordButton = CreateVisibilityButton(newPasswordEntry);
        var toggleConfirmPasswordButton = CreateVisibilityButton(confirmPasswordEntry);
        var passwordField = CreatePasswordField(_passwordEntry, togglePasswordButton);
        var newPasswordField = CreatePasswordField(newPasswordEntry, toggleNewPasswordButton);
        newPasswordField.IsVisible = false;
        var confirmPasswordField = CreatePasswordField(confirmPasswordEntry, toggleConfirmPasswordButton);
        confirmPasswordField.IsVisible = false;
        var loginButton = new Button
        {
            Text = "Anmelden",
            Padding = new Thickness(16, 12),
            FontAttributes = FontAttributes.Bold
        };
        var versionLabel = new Label
        {
            Text = BuildVersionText(),
            FontSize = 12,
            TextColor = Colors.Gray,
            HorizontalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 12, 0, 0)
        };
        var setPasswordButton = new Button { Text = "Neues Passwort setzen", IsVisible = false };
        var verifyOtpButton = new Button { Text = "Code prüfen", IsVisible = false };
        var backToLoginButton = new Button { Text = "Zurück zum Login", IsVisible = false };
        var requestOtpButton = new Button { Text = "Einladung / Erstlogin-Code anfordern" };
        var forgotPasswordButton = new Button { Text = "Passwort vergessen" };
        var changeClubButton = new Button { Text = "Anderen Verein auswählen" };
        changeClubButton.Clicked += async (_, _) => await ChangeClubAsync();
        Loaded += async (_, _) => await StartBiometricLoginIfAvailableAsync();

        void UpdatePasswordHintState()
        {
            var password = newPasswordEntry.Text ?? string.Empty;
            var confirmPassword = confirmPasswordEntry.Text ?? string.Empty;

            SetRequirementLabel(minLengthRuleLabel, HasMinimumPasswordLength(password), "Mindestens 8 Zeichen");
            SetRequirementLabel(upperLowerRuleLabel, HasUpperAndLowercase(password), "Groß- und Kleinbuchstaben");
            SetRequirementLabel(digitRuleLabel, HasDigit(password), "Mindestens eine Zahl");
            SetRequirementLabel(specialRuleLabel, HasSpecialCharacter(password), "Mindestens ein Sonderzeichen");
            SetRequirementLabel(confirmationRuleLabel, !string.IsNullOrWhiteSpace(password) && string.Equals(password, confirmPassword, StringComparison.Ordinal), "Passwort und Wiederholung stimmen überein");
        }

        bool TryValidateNewPassword(out string message)
        {
            var password = newPasswordEntry.Text ?? string.Empty;
            var confirmPassword = confirmPasswordEntry.Text ?? string.Empty;

            if (!HasMinimumPasswordLength(password))
            {
                message = "Passwort muss mindestens 8 Zeichen haben.";
                return false;
            }

            if (!HasUpperAndLowercase(password))
            {
                message = "Passwort braucht Groß- und Kleinbuchstaben.";
                return false;
            }

            if (!HasDigit(password))
            {
                message = "Passwort braucht mindestens eine Zahl.";
                return false;
            }

            if (!HasSpecialCharacter(password))
            {
                message = "Passwort braucht mindestens ein Sonderzeichen.";
                return false;
            }

            if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
            {
                message = "Passwort und Wiederholung stimmen nicht überein.";
                return false;
            }

            message = string.Empty;
            return true;
        }

        void ShowNormalLogin()
        {
            ResetOtpDiagnosticUi();
            _passwordEntry.IsPassword = true;
            togglePasswordButton.Text = "👁";
            newPasswordEntry.IsPassword = true;
            confirmPasswordEntry.IsPassword = true;
            toggleNewPasswordButton.Text = "👁";
            toggleConfirmPasswordButton.Text = "👁";
            passwordField.IsVisible = true;
            loginButton.IsVisible = true;
            requestOtpButton.IsVisible = true;
            forgotPasswordButton.IsVisible = true;
            backToLoginButton.IsVisible = false;

            otpEntry.IsVisible = false;
            verifyOtpButton.IsVisible = false;
            newPasswordField.IsVisible = false;
            confirmPasswordField.IsVisible = false;
            passwordHintLayout.IsVisible = false;
            setPasswordButton.IsVisible = false;
        }

        void ShowOtpVerification()
        {
            passwordField.IsVisible = false;
            loginButton.IsVisible = false;
            requestOtpButton.IsVisible = false;
            forgotPasswordButton.IsVisible = false;
            backToLoginButton.IsVisible = true;

            otpEntry.IsVisible = true;
            verifyOtpButton.IsVisible = true;
            newPasswordField.IsVisible = false;
            confirmPasswordField.IsVisible = false;
            passwordHintLayout.IsVisible = false;
            setPasswordButton.IsVisible = false;
        }

        void ShowSetPassword()
        {
            passwordField.IsVisible = false;
            loginButton.IsVisible = false;
            requestOtpButton.IsVisible = false;
            forgotPasswordButton.IsVisible = false;
            backToLoginButton.IsVisible = true;

            otpEntry.IsVisible = false;
            verifyOtpButton.IsVisible = false;
            newPasswordField.IsVisible = true;
            confirmPasswordField.IsVisible = true;
            passwordHintLayout.IsVisible = true;
            setPasswordButton.IsVisible = true;
            UpdatePasswordHintState();
            Dispatcher.Dispatch(() => newPasswordEntry.Focus());
        }

        newPasswordEntry.TextChanged += (_, _) => UpdatePasswordHintState();
        confirmPasswordEntry.TextChanged += (_, _) => UpdatePasswordHintState();
        loginButton.Clicked += OnLoginClicked;
        backToLoginButton.Clicked += (_, _) =>
        {
            otpEntry.Text = string.Empty;
            newPasswordEntry.Text = string.Empty;
            confirmPasswordEntry.Text = string.Empty;
            ShowNormalLogin();
            _statusLabel.Text = string.Empty;
        };
        verifyOtpButton.Clicked += async (s, e) =>
        {
            _statusLabel.Text = string.Empty;
            ResetOtpDiagnosticUi();
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
            ResetOtpDiagnosticUi();
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
                var diagnostic = GetLastOtpFailureInfo();
                AppFileLog.Warning("KGV.Login", $"OTP-Anforderung fehlgeschlagen für {MaskEmail(email)}. Diagnosecode: {diagnostic?.Code ?? "unknown"}. Details siehe Diagnose-Log.");
                ShowOtpFailureForSupport("OTP-Anforderung fehlgeschlagen.");
            }
        };

        setPasswordButton.Clicked += async (s, e) =>
        {
            _statusLabel.Text = string.Empty;
            ResetOtpDiagnosticUi();
            var email = (_emailEntry.Text ?? string.Empty).Trim();
            var code = otpEntry.Text ?? string.Empty;
            var newPwd = newPasswordEntry.Text ?? string.Empty;
            if (!TryValidateNewPassword(out var validationMessage))
            {
                _statusLabel.Text = validationMessage;
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
            ResetOtpDiagnosticUi();
            var email = (_emailEntry.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(email))
            {
                _statusLabel.Text = "Bitte E-Mail eingeben.";
                return;
            }

            var ok = await _authService.SendPasswordResetEmailAsync(email);
            if (ok)
            {
                otpEntry.Text = string.Empty;
                ShowOtpVerification();
                _statusLabel.Text = "OTP-Code für Passwort-vergessen wurde versendet. Bitte Code prüfen und direkt ein neues Passwort setzen.";
            }
            else
            {
                _statusLabel.Text = "Passwort-Reset konnte nicht versendet werden.";
            }
        };

        Content = new VerticalStackLayout
        {
            Padding = 24,
            Spacing = 12,
            Children =
            {
                logoImage,
                new Label { Text = "Login", FontSize = 24, FontAttributes = FontAttributes.Bold },
                new Label { Text = $"Verein: {_vereinskontext.Aktuell?.Vereinsname}", TextColor = Colors.Gray },
                _emailEntry,
                passwordField,
                loginButton,
                requestOtpButton,
                forgotPasswordButton,
                changeClubButton,
                otpEntry,
                verifyOtpButton,
                newPasswordField,
                confirmPasswordField,
                passwordHintLayout,
                setPasswordButton,
                backToLoginButton,
                _statusLabel,
                _otpDiagnosticLabel,
                _copyOtpDiagnosticButton,
                versionLabel
            }
        };

        ShowNormalLogin();
    }

    private static string BuildVersionText()
    {
        var version = AppInfo.Current.VersionString;
        var build = AppInfo.Current.BuildString;

        return string.IsNullOrWhiteSpace(build)
            ? $"Version {version}"
            : $"Version {version} (Build {build})";
    }

    private async Task ChangeClubAsync()
    {
        var confirmed = await DisplayAlert(
            "Verein wechseln",
            "Du wirst abgemeldet. Die Vereinszuordnung und lokale Anmeldedaten werden auf diesem Gerät gelöscht. Danach die App erneut öffnen und die neue Vereins-ID eingeben.",
            "Abmelden und wechseln",
            "Abbrechen");
        if (confirmed && Application.Current is App app)
            await app.WechselVereinAsync();
    }

    private string ResolveLogoImageSource()
    {
        var code = _vereinskontext.Aktuell?.VereinsCode;
        if (string.Equals(code, "KGV-DEMO", StringComparison.OrdinalIgnoreCase))
            return DemoLogoImageSource;

        // Das vorhandene Wappen bleibt nur für den derzeit bekannten Produktionsverein.
        // Weitere Vereinswappen werden im nächsten Block über eine Registry-URL eingebunden.
        if (string.Equals(code, "KGV-OBERROTHENBACH", StringComparison.OrdinalIgnoreCase))
            return OberrothenbachWappenImageSource;

        return NeutralLogoImageSource;
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
            AppFileLog.Marker("LOGIN_STARTED");
            AppFileLog.Info(nameof(LoginPage), $"Login gestartet für {MaskEmail(email)}.");
            var ok = await _authService.LoginAsync(email, password);
            if (!ok)
            {
                AppFileLog.Warning(nameof(LoginPage), $"Login fehlgeschlagen für {MaskEmail(email)}.");
                AppFileLog.Marker("LOGIN_RESULT_FAIL");
                _statusLabel.Text = "Login fehlgeschlagen. Details im Diagnose-Log.";
                return;
            }

            AppSettings.LastEmail = email;
            AppSettings.Save();
            var biometricTokens = await _authService.GetSessionTokensAsync();
            if (biometricTokens != null && _vereinskontext.Aktuell?.VereinId is { } clubId)
            {
                var clubIdText = clubId.ToString();
                var alreadyEnabled = await _biometricSessionStore.GetAsync(clubIdText) != null;
                if (!alreadyEnabled && await _biometricAuthenticationService.IsAvailableAsync())
                {
                    var enableBiometrics = await DisplayAlert(
                        "Fingerabdruck-Anmeldung aktivieren?",
                        "Beim nächsten Start kannst du dich auf diesem Gerät mit deinem Fingerabdruck anmelden. Die Anmeldedaten werden geschützt auf dem Gerät gespeichert.",
                        "Aktivieren",
                        "Nicht jetzt");

                    if (enableBiometrics)
                        await _biometricSessionStore.SaveAsync(clubIdText, biometricTokens);
                    else
                        _biometricSessionStore.Clear();
                }
            }

            if (string.IsNullOrWhiteSpace(_authService.CurrentUserId) || !Guid.TryParse(_authService.CurrentUserId, out var userId))
            {
                AppFileLog.Error(nameof(LoginPage), "LOGIN_EXCEPTION:INVALID_USER_ID");
                AppFileLog.Marker("LOGIN_RESULT_FAIL");
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
                AppFileLog.Error(nameof(LoginPage), "LOGIN_EXCEPTION:USER_WITHOUT_MITGLIED");
                AppFileLog.Marker("LOGIN_RESULT_FAIL");
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

            var rootSwitched = await SwitchToUserContextAsync(userContext);
            if (!rootSwitched)
            {
                AppFileLog.Error(nameof(LoginPage), "LOGIN_EXCEPTION:APP_ROOT_SWITCH_FAILED");
                AppFileLog.Marker("LOGIN_RESULT_FAIL");
                return;
            }

            AppFileLog.Marker("LOGIN_RESULT_SUCCESS");
        }
        catch (Exception ex)
        {
            AppFileLog.Error(nameof(LoginPage), $"LOGIN_EXCEPTION:{SanitizeDiagnosticMessage(ex.Message)}", ex);
            AppFileLog.Marker("LOGIN_RESULT_FAIL");
            _statusLabel.Text = "Login fehlgeschlagen. Details im Diagnose-Log.";
        }
    }

    private async Task<bool> CanUseBiometricLoginAsync()
        => _vereinskontext.Aktuell?.VereinId is { } clubId
           && await _biometricSessionStore.GetAsync(clubId.ToString()) != null
           && await _biometricAuthenticationService.IsAvailableAsync();

    private async Task StartBiometricLoginIfAvailableAsync()
    {
        if (_biometricPromptStarted || !await CanUseBiometricLoginAsync())
            return;

        _biometricPromptStarted = true;
        await LoginWithBiometricsAsync();
    }

    private async Task LoginWithBiometricsAsync()
    {
        _statusLabel.Text = string.Empty;
        var clubId = _vereinskontext.Aktuell?.VereinId.ToString();
        var tokens = await _biometricSessionStore.GetAsync(clubId);
        if (tokens == null || !await _biometricAuthenticationService.AuthenticateAsync("Mit Fingerabdruck anmelden")) return;
        if (!await _authService.RestoreSessionAsync(tokens))
        {
            _biometricSessionStore.Clear();
            _statusLabel.Text = "Die gespeicherte Anmeldung ist nicht mehr gültig. Bitte mit Passwort anmelden.";
            return;
        }
        if (!Guid.TryParse(_authService.CurrentUserId, out var userId))
        {
            _biometricSessionStore.Clear();
            _statusLabel.Text = "Die gespeicherte Anmeldung ist nicht mehr gültig. Bitte mit Passwort anmelden.";
            return;
        }
        var userContext = await _userContextService.GetUserContextAsync(userId);
        if (userContext.Role == UserRole.User && userContext.MitgliedId == null) { _statusLabel.Text = "Account ist keinem Mitglied zugeordnet."; return; }
        _userContextState.CurrentUserId = userId;
        _userContextState.CurrentUserContext = userContext;
        _userContextState.CurrentMitgliedId = userContext.MitgliedId;
        _userContextState.CurrentAppMode = userContext.Role is UserRole.Admin or UserRole.Vorstand ? AppMode.Admin : AppMode.User;
        if (userContext.MitgliedId is > 0 and <= int.MaxValue)
            _userContextState.CurrentNebenMitgliedId = (await _supabaseService.GetNebenmitgliedByHauptmitgliedIdAsync((int)userContext.MitgliedId.Value))?.Id;
        await SwitchToUserContextAsync(userContext);
    }

    private static string MaskEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return "<leer>";
        }

        var parts = email.Split('@');
        if (parts.Length != 2)
        {
            return "***";
        }

        var localPart = parts[0];
        var domain = parts[1];
        var maskedLocalPart = localPart.Length <= 2
            ? "**"
            : $"{localPart[0]}***{localPart[^1]}";

        return $"{maskedLocalPart}@{domain}";
    }

    private OtpFailureDiagnosticInfo? GetLastOtpFailureInfo()
        => _authService is AuthService authService
            ? authService.LastOtpFailureInfo
            : null;

    private void ShowOtpFailureForSupport(string fallbackUserMessage)
    {
        var diagnostic = GetLastOtpFailureInfo();
        _statusLabel.Text = !string.IsNullOrWhiteSpace(diagnostic?.UserMessage)
            ? diagnostic!.UserMessage
            : fallbackUserMessage;

        if (string.IsNullOrWhiteSpace(diagnostic?.Code))
        {
            ResetOtpDiagnosticUi();
            return;
        }

        _lastOtpDiagnosticCode = diagnostic.Code.Trim();
        _otpDiagnosticLabel.Text = $"Support-Hinweis: Bitte diesen Diagnosecode an den Vorstand weitergeben.\nDiagnosecode: {_lastOtpDiagnosticCode}";
        _otpDiagnosticLabel.IsVisible = true;
        _copyOtpDiagnosticButton.IsVisible = true;
    }

    private void ResetOtpDiagnosticUi()
    {
        _lastOtpDiagnosticCode = null;
        _otpDiagnosticLabel.Text = string.Empty;
        _otpDiagnosticLabel.IsVisible = false;
        _copyOtpDiagnosticButton.IsVisible = false;
    }

    private async Task CopyOtpDiagnosticCodeAsync()
    {
        if (string.IsNullOrWhiteSpace(_lastOtpDiagnosticCode))
            return;

        await Clipboard.Default.SetTextAsync(_lastOtpDiagnosticCode);
        await DisplayAlert("Diagnosecode", "Diagnosecode wurde kopiert.", "OK");
    }

    private async Task<bool> SwitchToUserContextAsync(UserContext userContext)
    {
        if (_userContextState.CurrentUserId == null)
            return false;

        var mode = userContext.Role is UserRole.Admin or UserRole.Vorstand
            ? AppMode.Admin
            : AppMode.User;

        if (mode == AppMode.User && _userContextState.CurrentMitgliedId == null)
        {
            _statusLabel.Text = "Account ist keinem Mitglied zugeordnet.";
            return false;
        }

        _userContextState.CurrentAppMode = mode;
        _userContextState.CurrentUserContext = userContext;

        if (Application.Current is not App app)
        {
            _statusLabel.Text = "App-Root konnte nicht gewechselt werden.";
            return false;
        }

        await app.SwitchToCurrentRootAsync();
        return true;
    }

    public void ShowResumeTimeoutMessage(string message)
    {
        ResetOtpDiagnosticUi();
        _passwordEntry.Text = string.Empty;
        _statusLabel.Text = message;
    }

    private static string SanitizeDiagnosticMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "unknown";
        }

        var sanitized = message.Replace("\r", " ").Replace("\n", " ").Trim();
        if (sanitized.Length > 160)
        {
            sanitized = sanitized[..160];
        }

        return sanitized;
    }

    private static Button CreateVisibilityButton(Entry entry)
    {
        var button = new Button
        {
            Text = "👁",
            BackgroundColor = Colors.Transparent,
            Padding = new Thickness(8, 0),
            WidthRequest = 44,
            HeightRequest = 44,
            FontSize = 18
        };

        button.Clicked += (_, _) =>
        {
            entry.IsPassword = !entry.IsPassword;
            button.Text = entry.IsPassword ? "👁" : "🙈";
        };

        return button;
    }

    private static Border CreatePasswordField(Entry entry, Button toggleButton)
    {
        var content = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 0,
            Children =
            {
                entry,
                toggleButton
            }
        };
        Grid.SetColumn(entry, 0);
        Grid.SetColumn(toggleButton, 1);

        return new Border
        {
            Stroke = Color.FromArgb("#C8CDD3"),
            StrokeThickness = 1,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(8) },
            Padding = new Thickness(12, 0, 8, 0),
            Content = content
        };
    }

    private static bool HasMinimumPasswordLength(string password) => !string.IsNullOrWhiteSpace(password) && password.Length >= 8;

    private static bool HasUpperAndLowercase(string password) => !string.IsNullOrWhiteSpace(password) && password.Any(char.IsUpper) && password.Any(char.IsLower);

    private static bool HasDigit(string password) => !string.IsNullOrWhiteSpace(password) && password.Any(char.IsDigit);

    private static bool HasSpecialCharacter(string password) => !string.IsNullOrWhiteSpace(password) && password.Any(ch => !char.IsLetterOrDigit(ch));

    private static void SetRequirementLabel(Label label, bool met, string text)
    {
        label.Text = $"{(met ? "✓" : "•")} {text}";
        label.TextColor = met ? Colors.ForestGreen : Colors.Gray;
    }
}
