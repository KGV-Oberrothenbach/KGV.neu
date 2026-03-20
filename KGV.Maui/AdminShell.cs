using KGV.Maui.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace KGV.Maui;

public sealed class AdminShell : Shell, IAppShellInitializer
{
    private readonly IServiceProvider _services;

    public AdminShell(IServiceProvider services)
    {
        _services = services;
        FlyoutBehavior = FlyoutBehavior.Flyout;
    }

    public void BuildMenu()
    {
        Items.Clear();

        Items.Add(new FlyoutItem
        {
            Title = "Mitgliedersuche",
            Items =
            {
                new ShellContent
                {
                    Title = "Mitgliedersuche",
                    Route = "membersearch",
                    ContentTemplate = new DataTemplate(() => _services.GetRequiredService<MemberSearchPage>())
                }
            }
        });

        Items.Add(new FlyoutItem
        {
            Title = "Arbeitsstunden prüfen",
            Items =
            {
                new ShellContent
                {
                    Title = "Arbeitsstunden prüfen",
                    Route = "workhours_review",
                    ContentTemplate = new DataTemplate(() => _services.GetRequiredService<ArbeitsstundenReviewPage>())
                }
            }
        });

        Items.Add(new FlyoutItem
        {
            Title = "Beenden",
            Items =
            {
                new ShellContent
                {
                    Title = "Beenden",
                    Route = "exit",
                    ContentTemplate = new DataTemplate(() => _services.GetRequiredService<ExitPage>())
                }
            }
        });

        if (Items.Count > 0)
            CurrentItem = Items[0];
    }
}
