using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Maui.Pages;
using KGV.Maui.Services.Diagnostics;
using KGV.Maui.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace KGV.Maui;

public sealed class UserShell : Shell, IAppShellInitializer
{
    private readonly IServiceProvider _services;
    private readonly UserContextState _state;
    private readonly MemberContextState _memberContextState;
    private bool _menuBuilt;
    private bool _backNavigationInProgress;
    private bool _exitConfirmationInProgress;
    private string? _preferredStartupRoute;
    private bool _loadedInitializationScheduled;

    public UserShell(IServiceProvider services, UserContextState state, MemberContextState memberContextState)
    {
        _services = services;
        _state = state;
        _memberContextState = memberContextState;

        FlyoutBehavior = FlyoutBehavior.Flyout;
        Loaded += (_, _) =>
        {
            if (_loadedInitializationScheduled)
                return;

            _loadedInitializationScheduled = true;
            Dispatcher.Dispatch(async () =>
            {
                try
                {
                    await Task.Yield();
                    EnsureOwnMemberContext();
                    EnsureMenuBuilt();
                    EnsureActiveRouteAfterLoad();
                }
                finally
                {
                    _loadedInitializationScheduled = false;
                }
            });
        };
    }

    public void SetPreferredStartupRoute(string? route)
    {
        _preferredStartupRoute = route;
    }

    protected override bool OnBackButtonPressed()
    {
        if (_backNavigationInProgress)
            return true;

        if (ShellNavigationHelper.IsOnShellContentRoot(this, "home"))
        {
            if (_exitConfirmationInProgress)
                return true;

            _exitConfirmationInProgress = true;
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    var confirmExit = await DisplayAlert("App beenden", "Soll die App wirklich beendet werden?", "Beenden", "Abbrechen");
                    if (!confirmExit)
                        return;

                    AppFileLog.Info("KGV.Navigation", "UserShell.Zurück auf Startseite bestätigt App-Beenden.");
                    Application.Current?.Quit();
                }
                catch (Exception ex)
                {
                    AppFileLog.Warning("KGV.Navigation", $"UserShell.Beenden-Rückfrage fehlgeschlagen: {ex.GetType().Name}: {ex.Message}");
                }
                finally
                {
                    _exitConfirmationInProgress = false;
                }
            });

            return true;
        }

        _backNavigationInProgress = true;
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                AppFileLog.Info("KGV.Navigation", $"UserShell.Zurück navigiert zur Startseite. Aktive Route={ShellNavigationHelper.GetActiveShellContentRoute(this) ?? "<none>"}.");
                await GoToAsync("//home");
            }
            catch (Exception ex)
            {
                AppFileLog.Warning("KGV.Navigation", $"UserShell.Zurück zur Startseite fehlgeschlagen: {ex.GetType().Name}: {ex.Message}");
            }
            finally
            {
                _backNavigationInProgress = false;
            }
        });

        return true;
    }

    private void EnsureOwnMemberContext()
    {
        if (_state.CurrentMitgliedId.HasValue && _state.CurrentMitgliedId.Value > 0)
            _memberContextState.SetSelectedMember(new MemberDTO { Id = (int)_state.CurrentMitgliedId.Value });
    }

    public void BuildMenu()
    {
        AppFileLog.Info("KGV.Navigation", $"UserShell.BuildMenu angefordert. MenuBuilt={_menuBuilt}, ActiveRoute={ShellNavigationHelper.GetActiveShellContentRoute(this) ?? "<none>"}.");
        EnsureMenuBuilt();
        EnsureValidActiveRoute();
    }

    private Page CreateOwnMemberDetailsPage()
    {
        EnsureOwnMemberContext();
        return _services.GetRequiredService<MeineDatenPage>();
    }

    private Page CreateOwnMemberGardensPage()
    {
        EnsureOwnMemberContext();
        return _services.GetRequiredService<MemberGardensPage>();
    }

    private Page CreateOwnMemberWartungsvertraegePage()
    {
        EnsureOwnMemberContext();
        return _services.GetRequiredService<MemberWartungsvertraegePage>();
    }

    private Page CreateOwnAdminMenuPage()
    {
        EnsureOwnMemberContext();
        return _services.GetRequiredService<AdminMenuPage>();
    }

    private void EnsureMenuBuilt()
    {
        if (_menuBuilt)
            return;

        var ownMemberId = _state.CurrentMitgliedId is > 0 and <= int.MaxValue
            ? (int?)_state.CurrentMitgliedId.Value
            : null;
        var hasOwnMemberContext = PermissionChecks.HasOwnMemberContextAccess(_state.CurrentUserContext) && ownMemberId.HasValue;

        Items.Add(CreateItem("Startseite", "home", () => _services.GetRequiredService<HomePage>()));
        Items.Add(CreateItem("Impressum", "impressum", () => _services.GetRequiredService<ImpressumPage>()));

        if (PermissionChecks.HasAnyMeterAccess(_state.CurrentUserContext))
            Items.Add(CreateItem("Ablesen", "ablesen", () => _services.GetRequiredService<AblesenOverviewPage>()));

        if (hasOwnMemberContext)
            Items.Add(CreateItem("↳ Stammdaten", "mydetails", CreateOwnMemberDetailsPage));

        if (hasOwnMemberContext)
            Items.Add(CreateItem("↳ Wartungsverträge", "my_wartungsvertraege", CreateOwnMemberWartungsvertraegePage));

        if (PermissionChecks.CanReadStammdatenForMember(_state.CurrentUserContext, ownMemberId))
            Items.Add(CreateItem("↳ Nebenmitglied", "nebenmitglied", () => _services.GetRequiredService<NebenmitgliedPage>()));

        if (PermissionChecks.CanShowParzellenForMember(_state.CurrentUserContext, ownMemberId))
            Items.Add(CreateItem("↳ Gärten des Mitglieds", "mygardens", CreateOwnMemberGardensPage));

        if (PermissionChecks.CanReadWorkHoursForMember(_state.CurrentUserContext, ownMemberId))
            Items.Add(CreateItem("↳ Arbeitsstunden", "workhours", () => _services.GetRequiredService<MyArbeitsstundenPage>()));

        if (PermissionChecks.CanReadRoleManagement(_state.CurrentUserContext))
            Items.Add(CreateItem("↳ Admin-Menü", "my_adminmenu", CreateOwnAdminMenuPage));

        if (PermissionChecks.CanManageWorkHours(_state.CurrentUserContext))
            Items.Add(CreateItem("Arbeitsstunden freigeben", "workhours_review", () => _services.GetRequiredService<ArbeitsstundenReviewPage>()));

        _menuBuilt = true;
    }

    private void EnsureActiveRouteAfterLoad()
    {
        var preferredRoute = _preferredStartupRoute;
        _preferredStartupRoute = null;

        if (!string.IsNullOrWhiteSpace(preferredRoute)
            && ShellNavigationHelper.HasVisibleShellContentRoute(this, preferredRoute))
        {
            AppFileLog.Info("KGV.Navigation", $"UserShell aktiviert bevorzugte Start-Route: {preferredRoute}.");
            ShellNavigationHelper.EnsureActiveShellItem(this, preferredRoute);
            return;
        }

        if (ShellNavigationHelper.HasValidActiveShellContentRoute(this))
        {
            AppFileLog.Info("KGV.Navigation", $"UserShell.Loaded belässt aktive Route: {ShellNavigationHelper.GetActiveShellContentRoute(this)}.");
            return;
        }

        AppFileLog.Warning("KGV.Navigation", "UserShell.Loaded setzt Fallback auf home, weil kein gültiger aktiver Shell-Content vorhanden ist.");
        ShellNavigationHelper.EnsureActiveShellItem(this, "home");
    }

    private void EnsureValidActiveRoute()
    {
        var currentRoute = ShellNavigationHelper.GetActiveShellContentRoute(this);
        if (currentRoute != null && ShellNavigationHelper.HasVisibleShellContentRoute(this, currentRoute))
        {
            AppFileLog.Info("KGV.Navigation", $"UserShell belässt aktive Route: {currentRoute}.");
            return;
        }

        AppFileLog.Warning("KGV.Navigation", $"UserShell verwendet Fallback auf home. Aktive Route war {(currentRoute ?? "<none>")}.");
        ShellNavigationHelper.EnsureActiveShellItem(this, "home");
    }

    private static FlyoutItem CreateItem(string title, string route, Func<Page> pageFactory)
    {
        return new FlyoutItem
        {
            Title = title,
            Items =
            {
                new ShellContent
                {
                    Title = title,
                    Route = route,
                    ContentTemplate = new DataTemplate(pageFactory)
                }
            }
        };
    }
}
