using KGV.Core.Models;
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

        SetOwnMemberContext();

        FlyoutBehavior = FlyoutBehavior.Flyout;
        Loaded += (_, _) => EnsureActiveRouteAfterLoad();
    }

    public void BuildMenu()
    {
        AppFileLog.Info("KGV.Navigation", $"UserShell.BuildMenu angefordert. MenuBuilt={_menuBuilt}, ActiveRoute={ShellNavigationHelper.GetActiveShellContentRoute(this) ?? "<none>"}.");
        EnsureMenuBuilt();
        EnsureValidActiveRoute();
    }

    private Page CreateOwnMemberDetailsPage()
    {
        SetOwnMemberContext();
        return _services.GetRequiredService<MeineDatenPage>();
    }

    private Page CreateOwnMemberGardensPage()
    {
        SetOwnMemberContext();
        return _services.GetRequiredService<MemberGardensPage>();
    }

    private Page CreateOwnMemberWartungsvertraegePage()
    {
        SetOwnMemberContext();
        return _services.GetRequiredService<MemberWartungsvertraegePage>();
    }

    private void SetOwnMemberContext()
    {
        if (_state.CurrentMitgliedId is > 0 and <= int.MaxValue)
        {
            _memberContextState.SetSelectedMember(new MemberDTO { Id = (int)_state.CurrentMitgliedId.Value });
        }
    }

    private void EnsureMenuBuilt()
    {
        if (_menuBuilt)
            return;

        Items.Add(CreateItem("Startseite", "home", () => _services.GetRequiredService<HomePage>()));
        Items.Add(CreateItem("Impressum", "impressum", () => _services.GetRequiredService<ImpressumPage>()));
        Items.Add(CreateItem("↳ Stammdaten", "mydetails", CreateOwnMemberDetailsPage));
        Items.Add(CreateItem("↳ Wartungsverträge", "my_wartungsvertraege", CreateOwnMemberWartungsvertraegePage));
        Items.Add(CreateItem("↳ Nebenmitglied", "nebenmitglied", () => _services.GetRequiredService<NebenmitgliedPage>()));
        Items.Add(CreateItem("↳ Gärten des Mitglieds", "mygardens", CreateOwnMemberGardensPage));
        Items.Add(CreateItem("↳ Arbeitsstunden", "workhours", () => _services.GetRequiredService<MyArbeitsstundenPage>()));

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
