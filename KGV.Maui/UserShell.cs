using KGV.Core.Models;
using KGV.Maui.Pages;
using KGV.Maui.State;
using Microsoft.Extensions.DependencyInjection;

namespace KGV.Maui;

public sealed class UserShell : Shell, IAppShellInitializer
{
    private readonly IServiceProvider _services;
    private readonly UserContextState _state;

    public UserShell(IServiceProvider services, UserContextState state)
    {
        _services = services;
        _state = state;

        FlyoutBehavior = FlyoutBehavior.Flyout;
        Loaded += (_, _) => ShellNavigationHelper.EnsureActiveShellItem(this, "home");
    }

    public void BuildMenu()
    {
        Items.Clear();

        Items.Add(CreateItem("Startseite", "home", () => _services.GetRequiredService<HomePage>()));
        Items.Add(CreateItem("Mein Bereich · Meine Stammdaten", "mydetails", CreateOwnMemberDetailsPage));
        Items.Add(CreateItem("Mein Bereich · Nebenmitglied", "nebenmitglied", () => _services.GetRequiredService<NebenmitgliedPage>()));
        Items.Add(CreateItem("Mein Bereich · Meine Arbeitsstunden", "workhours", () => _services.GetRequiredService<MyArbeitsstundenPage>()));
        Items.Add(CreateItem("Mein Bereich · Mein Profil", "myprofile", () => _services.GetRequiredService<MyProfilePage>()));

        ShellNavigationHelper.EnsureActiveShellItem(this, "home");
    }

    private Page CreateOwnMemberDetailsPage()
    {
        if (_state.CurrentMitgliedId is > 0 and <= int.MaxValue)
        {
            var memberContextState = _services.GetRequiredService<MemberContextState>();
            memberContextState.SetSelectedMember(new MemberDTO { Id = (int)_state.CurrentMitgliedId.Value });
        }

        return _services.GetRequiredService<MeineDatenPage>();
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
