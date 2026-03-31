using KGV.Maui.Pages;
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
    private readonly IServiceProvider _services;
    private readonly UserContextState _userContextState;
    private Window? _mainWindow;

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

        _mainWindow.Resumed += (_, _) =>
        {
            var delta = Settings.AppSettings.TryGetTimeSinceLastBackgroundUtc(DateTime.UtcNow);
            Services.Diagnostics.AppFileLog.Marker("APP_RESUMED");
            Services.Diagnostics.AppFileLog.Info("KGV.Lifecycle", delta == null
                ? "App resumed (kein Background-Timestamp)."
                : $"App resumed nach {delta.Value.TotalSeconds:0} Sekunden im Hintergrund.");
        };

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
            return _services.GetRequiredService<LoginPage>();

        var shell = _userContextState.CurrentUserContext.Role is Core.Security.UserRole.Admin or Core.Security.UserRole.Vorstand
            ? (Shell)_services.GetRequiredService<AdminShell>()
            : _services.GetRequiredService<UserShell>();

        if (shell is IAppShellInitializer initializer)
            initializer.BuildMenu();

        ShellNavigationHelper.EnsureActiveShellItem(shell, "home");
        return shell;
    }
}
