using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Maui.Pages;
using KGV.Maui.Services.Diagnostics;
using KGV.Maui.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace KGV.Maui;

public sealed class UserShell : Shell, IAppShellInitializer
{
    private readonly IServiceProvider _services;
    private readonly UserContextState _state;
    private readonly MemberContextState _memberContextState;
    private bool _menuBuilt;

    public UserShell(IServiceProvider services, UserContextState state, MemberContextState memberContextState)
    {
        _services = services;
        _state = state;
        _memberContextState = memberContextState;

        FlyoutBehavior = FlyoutBehavior.Flyout;
        Loaded += (_, _) =>
        {
            EnsureOwnMemberContext();
            EnsureMenuBuilt();
            EnsureActiveRouteAfterLoad();
        };
        EnsureOwnMemberContext();
        EnsureMenuBuilt();
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

        Items.Add(CreateItem("Startseite", "home", () => _services.GetRequiredService<HomePage>()));
        Items.Add(CreateItem("Impressum", "impressum", () => _services.GetRequiredService<ImpressumPage>()));

        if (PermissionChecks.HasAnyMeterAccess(_state.CurrentUserContext))
            Items.Add(CreateItem("Ablesen", "ablesen", () => _services.GetRequiredService<AblesenOverviewPage>()));

        if (PermissionChecks.CanShowStammdaten(_state.CurrentUserContext))
            Items.Add(CreateItem("↳ Stammdaten", "mydetails", CreateOwnMemberDetailsPage));

        Items.Add(CreateItem("↳ Wartungsverträge", "my_wartungsvertraege", CreateOwnMemberWartungsvertraegePage));

        if (PermissionChecks.CanReadStammdaten(_state.CurrentUserContext))
            Items.Add(CreateItem("↳ Nebenmitglied", "nebenmitglied", () => _services.GetRequiredService<NebenmitgliedPage>()));

        if (PermissionChecks.CanShowParzellen(_state.CurrentUserContext))
            Items.Add(CreateItem("↳ Gärten des Mitglieds", "mygardens", CreateOwnMemberGardensPage));

        if (PermissionChecks.CanReadWorkHours(_state.CurrentUserContext))
            Items.Add(CreateItem("↳ Arbeitsstunden", "workhours", () => _services.GetRequiredService<MyArbeitsstundenPage>()));

        if (PermissionChecks.CanReadRoleManagement(_state.CurrentUserContext))
            Items.Add(CreateItem("↳ Admin-Menü", "my_adminmenu", CreateOwnAdminMenuPage));

        if (PermissionChecks.CanManageWorkHours(_state.CurrentUserContext))
            Items.Add(CreateItem("Arbeitsstunden freigeben", "workhours_review", () => _services.GetRequiredService<ArbeitsstundenReviewPage>()));

        _menuBuilt = true;
    }

    private void EnsureActiveRouteAfterLoad()
    {
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
