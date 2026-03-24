using KGV.Maui.Pages;
using KGV.Maui.State;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace KGV.Maui;

public partial class App : Application
{
    private readonly IServiceProvider _services;
    private readonly UserContextState _userContextState;
    private Window? _mainWindow;

    public App(IServiceProvider services, UserContextState userContextState)
    {
        InitializeComponent();
        _services = services;
        _userContextState = userContextState;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        _mainWindow = new Window(CreateRootPage());
        return _mainWindow;
    }

    public async Task SwitchToCurrentRootAsync()
    {
        var nextRootPage = CreateRootPage();
        var currentWindow = _mainWindow ?? Windows.FirstOrDefault();

        if (currentWindow == null)
        {
            _mainWindow = new Window(nextRootPage);
            OpenWindow(_mainWindow);
            return;
        }

#if ANDROID
        var nextWindow = new Window(nextRootPage);
        var loadedSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        void OnLoaded(object? sender, EventArgs e)
        {
            nextRootPage.Loaded -= OnLoaded;
            loadedSource.TrySetResult();
        }

        nextRootPage.Loaded += OnLoaded;
        OpenWindow(nextWindow);
        await Task.WhenAny(loadedSource.Task, Task.Delay(1500));
        nextRootPage.Loaded -= OnLoaded;

        _mainWindow = nextWindow;
        if (Windows.Contains(currentWindow))
            CloseWindow(currentWindow);
#else
        currentWindow.Page = nextRootPage;
        _mainWindow = currentWindow;
#endif
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
