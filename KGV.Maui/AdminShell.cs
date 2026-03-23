using KGV.Core.Interfaces;
using KGV.Maui.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace KGV.Maui;

public sealed class AdminShell : Shell, IAppShellInitializer
{
    private readonly IServiceProvider _services;
    private readonly IAuthService _authService;
    private FlyoutItem? _workhoursReviewItem;
    private static bool _readingRoutesRegistered;

    public AdminShell(IServiceProvider services, IAuthService authService)
    {
        _services = services;
        _authService = authService;
        FlyoutBehavior = FlyoutBehavior.Flyout;
        RegisterReadingRoutes();
    }

    private static void RegisterReadingRoutes()
    {
        if (_readingRoutesRegistered)
            return;

        Routing.RegisterRoute(nameof(AblesungErfassenPage), typeof(AblesungErfassenPage));
        Routing.RegisterRoute(nameof(ZaehlerwechselPage), typeof(ZaehlerwechselPage));
        Routing.RegisterRoute(nameof(RfidEinrichtenPage), typeof(RfidEinrichtenPage));
        Routing.RegisterRoute(nameof(FaelligeZaehlerPage), typeof(FaelligeZaehlerPage));

        _readingRoutesRegistered = true;
    }

    public void BuildMenu()
    {
        Items.Clear();

        Items.Add(new FlyoutItem
        {
            Title = "Startseite",
            Items =
            {
                new ShellContent
                {
                    Title = "Startseite",
                    Route = "home",
                    ContentTemplate = new DataTemplate(() => _services.GetRequiredService<HomePage>())
                }
            }
        });

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

        if (_authService.IsAdmin)
        {
            Items.Add(new FlyoutItem
            {
                Title = "Benutzerverwaltung",
                Items =
                {
                    new ShellContent
                    {
                        Title = "Benutzerverwaltung",
                        Route = "usermanagement",
                        ContentTemplate = new DataTemplate(() => _services.GetRequiredService<UserManagementPage>())
                    }
                }
            });
        }

        Items.Add(new FlyoutItem
        {
            Title = "Parzellen",
            Items =
            {
                new ShellContent
                {
                    Title = "Parzellen",
                    Route = "parzellen",
                    ContentTemplate = new DataTemplate(() => _services.GetRequiredService<ParzellenPage>())
                }
            }
        });

        if (_authService.IsAdmin || _authService.IsVorstand)
        {
            Items.Add(new FlyoutItem
            {
                Title = "Ablesen",
                Items =
                {
                    new ShellContent
                    {
                        Title = "Ablesen",
                        Route = "ablesen",
                        ContentTemplate = new DataTemplate(() => new AblesenOverviewPage())
                    }
                }
            });
        }

        _workhoursReviewItem = new FlyoutItem
        {
            Title = "Arbeitsstunden freigeben",
            IsVisible = false,
            Items =
            {
                new ShellContent
                {
                    Title = "Arbeitsstunden freigeben",
                    Route = "workhours_review",
                    ContentTemplate = new DataTemplate(() => _services.GetRequiredService<ArbeitsstundenReviewPage>())
                }
            }
        };

        Items.Add(_workhoursReviewItem);

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

        _ = RefreshWorkhoursReviewMenuAsync();
    }

    public async Task RefreshWorkhoursReviewMenuAsync()
    {
        if (_workhoursReviewItem == null)
            return;

        try
        {
            var supabaseService = _services.GetRequiredService<ISupabaseService>();
            var offene = await supabaseService.GetUnapprovedArbeitsstundenByMitgliedAsync();
            var count = offene.Sum(x => x.Count);

            _workhoursReviewItem.Title = count > 0
                ? $"Arbeitsstunden freigeben ({count})"
                : "Arbeitsstunden freigeben";
            _workhoursReviewItem.IsVisible = count > 0;

            if (!_workhoursReviewItem.IsVisible && CurrentItem == _workhoursReviewItem && Items.Count > 0)
                CurrentItem = Items[0];
        }
        catch
        {
            _workhoursReviewItem.Title = "Arbeitsstunden freigeben";
            _workhoursReviewItem.IsVisible = false;
        }
    }
}
