using KGV.Maui.Pages;
using KGV.Core.Interfaces;
using KGV.Maui.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace KGV.Maui;

public partial class App : Application
{
    private static readonly TimeSpan ResumeTimeoutThreshold = TimeSpan.FromMinutes(15);

    private readonly IServiceProvider _services;
    private readonly UserContextState _userContextState;
    private Window? _mainWindow;
    private string? _pendingLoginMessage;
    private bool _resumeTimeoutResetInProgress;

    public App(IServiceProvider services, UserContextState userContextState)
    {
        _services = services;
        _userContextState = userContextState;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        _mainWindow = new Window(CreateRootPage());

        _mainWindow.Stopped += (_, _) =>
        {
            Settings.AppSettings.MarkBackgroundedNowUtc();
            Services.Diagnostics.AppFileLog.Marker("APP_STOPPED");
        };

        _mainWindow.Resumed += async (_, _) => await HandleWindowResumedAsync();

        return _mainWindow;
    }

    public async Task SwitchToCurrentRootAsync()
    {
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            var nextRootPage = CreateRootPage();
            var currentWindow = _mainWindow ?? Windows.FirstOrDefault();

            if (currentWindow == null)
            {
                _mainWindow = new Window(nextRootPage);
                OpenWindow(_mainWindow);
                return;
            }

            currentWindow.Page = nextRootPage;
            _mainWindow = currentWindow;
        });
    }

    private Page CreateRootPage()
    {
        if (_userContextState.CurrentUserId == null || _userContextState.CurrentUserContext == null)
        {
            var loginPage = _services.GetRequiredService<LoginPage>();
            if (!string.IsNullOrWhiteSpace(_pendingLoginMessage))
            {
                loginPage.ShowResumeTimeoutMessage(_pendingLoginMessage);
                _pendingLoginMessage = null;
            }

            return loginPage;
        }

        var shell = _userContextState.CurrentUserContext.Role is Core.Security.UserRole.Admin or Core.Security.UserRole.Vorstand
            ? (Shell)_services.GetRequiredService<AdminShell>()
            : _services.GetRequiredService<UserShell>();

        if (shell is IAppShellInitializer initializer)
            initializer.BuildMenu();

        ShellNavigationHelper.EnsureActiveShellItem(shell, "home");
        return shell;
    }

    private async Task HandleWindowResumedAsync()
    {
        try
        {
            var delta = Settings.AppSettings.TryGetTimeSinceLastBackgroundUtc(DateTime.UtcNow);

            Services.Diagnostics.AppFileLog.Marker("APP_RESUMED");
            Services.Diagnostics.AppFileLog.Info("KGV.Lifecycle", delta == null
                ? "App resumed (kein Background-Timestamp)."
                : $"App resumed nach {delta.Value.TotalSeconds:0} Sekunden im Hintergrund.");

            Settings.AppSettings.ClearBackgroundedTimestamp();

            if (delta == null)
            {
                Services.Diagnostics.AppFileLog.Marker("APP_RESUME_TIMEOUT_NO_TIMESTAMP");
                return;
            }

            if (delta <= ResumeTimeoutThreshold)
            {
                Services.Diagnostics.AppFileLog.Marker("APP_RESUME_TIMEOUT_WITHIN_THRESHOLD");
                return;
            }

            if (_userContextState.CurrentUserId == null || _userContextState.CurrentUserContext == null)
            {
                Services.Diagnostics.AppFileLog.Marker("APP_RESUME_TIMEOUT_SKIPPED_NO_SESSION");
                return;
            }

            await ResetToLoginAfterResumeTimeoutAsync(delta.Value);
        }
        catch (Exception ex)
        {
            Services.Diagnostics.AppFileLog.Error("KGV.Lifecycle", "Resume-Verarbeitung ist fehlgeschlagen.", ex);
        }
    }

    private async Task ResetToLoginAfterResumeTimeoutAsync(TimeSpan backgroundDuration)
    {
        if (_resumeTimeoutResetInProgress)
        {
            Services.Diagnostics.AppFileLog.Marker("APP_RESUME_TIMEOUT_ALREADY_RUNNING");
            return;
        }

        _resumeTimeoutResetInProgress = true;
        try
        {
            Services.Diagnostics.AppFileLog.Marker("APP_RESUME_TIMEOUT_TRIGGERED");
            Services.Diagnostics.AppFileLog.Warning(
                "KGV.Lifecycle",
                $"Resume-Timeout überschritten ({backgroundDuration.TotalMinutes:0.0} Minuten im Hintergrund). Sitzung wird auf Login zurückgesetzt.");

            await ClearActiveSessionAsync();
            ClearTransientState();

            Settings.AppSettings.AppMode = null;
            Settings.AppSettings.Save();

            _pendingLoginMessage = "Die App war zu lange im Hintergrund. Bitte erneut anmelden.";
            await SwitchToCurrentRootAsync();
            Services.Diagnostics.AppFileLog.Marker("APP_RESUME_TIMEOUT_COMPLETED");
        }
        finally
        {
            _resumeTimeoutResetInProgress = false;
        }
    }

    private async Task ClearActiveSessionAsync()
    {
        try
        {
            var authService = _services.GetService<IAuthService>();
            if (authService != null)
                await authService.LogoutAsync();
        }
        catch (Exception ex)
        {
            Services.Diagnostics.AppFileLog.Warning("KGV.Lifecycle", $"Logout beim Resume-Timeout fehlgeschlagen: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private void ClearTransientState()
    {
        _userContextState.CurrentUserId = null;
        _userContextState.CurrentMitgliedId = null;
        _userContextState.CurrentNebenMitgliedId = null;
        _userContextState.CurrentAppMode = null;
        _userContextState.CurrentUserContext = null;

        _services.GetService<MemberContextState>()?.Clear();
        _services.GetService<ParzellenContextState>()?.Clear();
        _services.GetService<HomeContextState>()?.Clear();
        _services.GetService<ArbeitsstundenReviewState>()?.Clear();
        _services.GetService<ArbeitseinsaetzeManagementState>()?.Clear();
        _services.GetService<ArbeitseinsaetzeUserState>()?.Clear();
        _services.GetService<TermineUserState>()?.Clear();
        _services.GetService<ZaehlerwechselWorkflowState>()?.Clear();
    }
}
