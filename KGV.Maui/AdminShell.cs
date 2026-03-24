using KGV.Core.Interfaces;
using KGV.Core.Security;
using KGV.Maui.Pages;
using KGV.Maui.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Dispatching;

namespace KGV.Maui;

public sealed class AdminShell : Shell, IAppShellInitializer
{
    private readonly IServiceProvider _services;
    private readonly UserContextState _userContextState;
    private FlyoutItem? _workhoursReviewItem;

    public AdminShell(IServiceProvider services, UserContextState userContextState)
    {
        _services = services;
        _userContextState = userContextState;
        FlyoutBehavior = FlyoutBehavior.Flyout;
        ShellRouteRegistrar.RegisterCommonRoutes();
        Loaded += (_, _) => ShellNavigationHelper.EnsureActiveShellItem(this);
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

        if (_userContextState.CurrentUserContext?.Role is UserRole.Admin or UserRole.Vorstand)
        {
            Items.Add(new FlyoutItem
            {
                Title = "Export",
                Items =
                {
                    new ShellContent
                    {
                        Title = "Export",
                        Route = "export",
                        ContentTemplate = new DataTemplate(() => _services.GetRequiredService<ExportPage>())
                    }
                }
            });
        }

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

        if (_userContextState.CurrentUserContext?.Role is UserRole.Admin or UserRole.Vorstand)
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

        ShellNavigationHelper.EnsureActiveShellItem(this);

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

            DispatchEnsureActiveShellItem();
        }
        catch
        {
            _workhoursReviewItem.Title = "Arbeitsstunden freigeben";
            _workhoursReviewItem.IsVisible = false;
            DispatchEnsureActiveShellItem();
        }
    }

    private void DispatchEnsureActiveShellItem()
    {
        if (Dispatcher.IsDispatchRequired)
        {
            Dispatcher.Dispatch(() => ShellNavigationHelper.EnsureActiveShellItem(this));
            return;
        }

        ShellNavigationHelper.EnsureActiveShellItem(this);
    }
}
