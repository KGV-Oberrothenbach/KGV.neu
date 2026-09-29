using KGV.Maui.Pages;
using KGV.Core.Interfaces;
using KGV.Maui.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace KGV.Maui;

public partial class App : Application
{
    private static readonly TimeSpan ResumeTimeoutThreshold = TimeSpan.FromMinutes(15);

    private readonly IServiceProvider _services;
    private readonly UserContextState _userContextState;
    private readonly IVereinskontext _vereinskontext;
    private Window? _mainWindow;
    private IDispatcherTimer? _inactivityTimer;
    private DateTime _lastUserActivityUtc = DateTime.UtcNow;
    private string? _pendingLoginMessage;
    private bool _resumeTimeoutResetInProgress;

    public App(IServiceProvider services, UserContextState userContextState, IVereinskontext vereinskontext)
    {
        _services = services;
        _userContextState = userContextState;
        _vereinskontext = vereinskontext;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        _mainWindow = new Window(CreateRootPage());

        _mainWindow.Stopped += (_, _) =>
        {
            _inactivityTimer?.Stop();
            Settings.AppSettings.MarkBackgroundedNowUtc();
            Services.Diagnostics.AppFileLog.Marker("APP_STOPPED");
        };

        _mainWindow.Resumed += async (_, _) =>
        {
            await HandleWindowResumedAsync();
            _lastUserActivityUtc = DateTime.UtcNow;
            _inactivityTimer?.Start();
        };

        StartInactivityTimer();

        return _mainWindow;
    }

    /// <summary>
    /// Wird von der Android-Aktivität für jede Benutzerinteraktion aufgerufen.
    /// Damit ist die Frist echte Inaktivität und nicht nur die Zeit im Hintergrund.
    /// </summary>
    public void RegisterUserActivity()
    {
        if (!_resumeTimeoutResetInProgress)
            _lastUserActivityUtc = DateTime.UtcNow;
    }

    private void StartInactivityTimer()
    {
        _inactivityTimer ??= Dispatcher.CreateTimer();
        _inactivityTimer.Interval = TimeSpan.FromSeconds(15);
        _inactivityTimer.Tick += async (_, _) => await HandleInactivityTimerTickAsync();
        _inactivityTimer.Start();
    }

    private async Task HandleInactivityTimerTickAsync()
    {
        if (_resumeTimeoutResetInProgress
            || _userContextState.CurrentUserId == null
            || _userContextState.CurrentUserContext == null)
            return;

        var inactiveFor = DateTime.UtcNow - _lastUserActivityUtc;
        if (inactiveFor <= ResumeTimeoutThreshold)
            return;

        await ResetToLoginAfterInactivityAsync(inactiveFor);
    }

    public Task SwitchToCurrentRootAsync()
        => SwitchToCurrentRootAsync(null);

    public async Task WechselVereinAsync()
    {
        try
        {
            var authService = _services.GetService<IAuthService>();
            if (authService != null)
                await authService.LogoutAsync();
        }
        catch (Exception ex)
        {
            Services.Diagnostics.AppFileLog.Warning("KGV.Vereinswechsel", $"Logout vor Vereinswechsel fehlgeschlagen: {ex.GetType().Name}: {ex.Message}");
        }

        ClearTransientState();
        _vereinskontext.Loeschen();
        Settings.AppSettings.Vereinskontext = null;
        Settings.AppSettings.AppMode = null;
        Settings.AppSettings.LastEmail = null;
        Settings.AppSettings.Save();
        Services.Diagnostics.AppFileLog.Info("KGV.Vereinswechsel", "Vereinskontext und lokale Anmeldedaten wurden gelöscht. App wird für einen sauberen Neustart beendet.");

        // Auth- und Supabase-Dienste halten Clients pro App-Lauf fest. Ein Prozessneustart
        // verhindert, dass eine bereits aufgebaute Verbindung in einen anderen Verein gelangt.
        Quit();
    }

    public async Task SwitchToCurrentRootAsync(string? preferredContentRoute)
    {
        using var navigationScope = NavigationCoordinator.TryBegin(
            NavigationCoordinator.RootSwitchScope,
            $"root -> {(string.IsNullOrWhiteSpace(preferredContentRoute) ? "login/default" : preferredContentRoute)}",
            NavigationCoordinator.MemberSwitchScope);

        if (navigationScope == null)
            return;

        await SwitchToCurrentRootCoreAsync(preferredContentRoute);
    }

    private async Task SwitchToCurrentRootCoreAsync(string? preferredContentRoute)
    {
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            var nextRootPage = CreateRootPage(preferredContentRoute);
            var currentWindow = _mainWindow ?? Windows.FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(preferredContentRoute))
            {
                Services.Diagnostics.AppFileLog.Info(
                    "KGV.Navigation",
                    $"Shell-Root wird neu aufgebaut. Zielroute: {preferredContentRoute}.");
            }

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

    private Page CreateRootPage(string? preferredContentRoute = null)
    {
        if (!_vereinskontext.IstAusgewaehlt)
            return _services.GetRequiredService<VereinsauswahlPage>();

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

        var currentContext = _userContextState.CurrentUserContext;
        var useAdminShell = currentContext.Role is Core.Security.UserRole.Admin or Core.Security.UserRole.Vorstand
            || Core.Security.PermissionChecks.CanSearchMembers(currentContext)
            || Core.Security.PermissionChecks.CanManageWorkHours(currentContext)
            || Core.Security.PermissionChecks.HasAnyRoleManagementAccess(currentContext);

        var shell = useAdminShell
            ? (Shell)_services.GetRequiredService<AdminShell>()
            : _services.GetRequiredService<UserShell>();

        if (shell is IAppShellInitializer initializer)
            initializer.BuildMenu();

        var initialRoute = string.IsNullOrWhiteSpace(preferredContentRoute)
            ? "home"
            : preferredContentRoute;

        ShellNavigationHelper.EnsureActiveShellItem(shell, initialRoute);
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

            await ResetToLoginAsync(
                "APP_RESUME_TIMEOUT_TRIGGERED",
                $"Resume-Timeout überschritten ({delta.Value.TotalMinutes:0.0} Minuten im Hintergrund). Sitzung wird auf Login zurückgesetzt.",
                "Die App war zu lange im Hintergrund. Bitte erneut anmelden.");
        }
        catch (Exception ex)
        {
            Services.Diagnostics.AppFileLog.Error("KGV.Lifecycle", "Resume-Verarbeitung ist fehlgeschlagen.", ex);
        }
    }

    private Task ResetToLoginAfterInactivityAsync(TimeSpan inactiveFor)
        => ResetToLoginAsync(
            "APP_IDLE_TIMEOUT_TRIGGERED",
            $"Inaktivitäts-Timeout überschritten ({inactiveFor.TotalMinutes:0.0} Minuten ohne Eingabe). Sitzung wird auf Login zurückgesetzt.",
            "Die App war 15 Minuten nicht aktiv. Bitte erneut anmelden.");

    private async Task ResetToLoginAsync(string marker, string logMessage, string loginMessage)
    {
        if (_resumeTimeoutResetInProgress)
        {
            Services.Diagnostics.AppFileLog.Marker("APP_RESUME_TIMEOUT_ALREADY_RUNNING");
            return;
        }

        _resumeTimeoutResetInProgress = true;
        try
        {
            Services.Diagnostics.AppFileLog.Marker(marker);
            Services.Diagnostics.AppFileLog.Warning("KGV.Lifecycle", logMessage);

            await ClearActiveSessionAsync();
            ClearTransientState();

            Settings.AppSettings.AppMode = null;
            Settings.AppSettings.Save();

            _pendingLoginMessage = loginMessage;
            await SwitchToCurrentRootCoreAsync(null);
            Services.Diagnostics.AppFileLog.Marker("APP_TIMEOUT_RESET_COMPLETED");
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
