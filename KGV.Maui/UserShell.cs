using KGV.Core.Models;
using KGV.Maui.Pages;
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

    public UserShell(IServiceProvider services, UserContextState state, MemberContextState memberContextState)
    {
        _services = services;
        _state = state;
        _memberContextState = memberContextState;

        FlyoutBehavior = FlyoutBehavior.Flyout;
        Loaded += (_, _) => ShellNavigationHelper.EnsureActiveShellItem(this, "home");
    }

    public void BuildMenu()
    {
        var preferredRoute = GetCurrentRoute() ?? "home";
        SetOwnMemberContext();

        Items.Clear();

        Items.Add(CreateItem("Startseite", "home", () => _services.GetRequiredService<HomePage>()));
        Items.Add(CreateItem("↳ Stammdaten", "mydetails", CreateOwnMemberDetailsPage));
        Items.Add(CreateItem("↳ Wartungsverträge", "my_wartungsvertraege", CreateOwnMemberWartungsvertraegePage));
        Items.Add(CreateItem("↳ Nebenmitglied", "nebenmitglied", () => _services.GetRequiredService<NebenmitgliedPage>()));
        Items.Add(CreateItem("↳ Gärten des Mitglieds", "mygardens", CreateOwnMemberGardensPage));
        Items.Add(CreateItem("↳ Arbeitsstunden", "workhours", () => _services.GetRequiredService<MyArbeitsstundenPage>()));

        ShellNavigationHelper.EnsureActiveShellItem(this, preferredRoute);
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

    private string? GetCurrentRoute()
        => CurrentItem?.CurrentItem?.CurrentItem?.Route;

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
