using KGV.Core.Interfaces;
using KGV.Core.Security;
using KGV.Maui.Pages;
using KGV.Maui.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace KGV.Maui;

public sealed class AdminShell : Shell, IAppShellInitializer
{
    private readonly IServiceProvider _services;
    private readonly UserContextState _userContextState;
    private readonly MemberContextState _memberContextState;
    private FlyoutItem? _workhoursReviewItem;

    public AdminShell(IServiceProvider services, UserContextState userContextState, MemberContextState memberContextState)
    {
        _services = services;
        _userContextState = userContextState;
        _memberContextState = memberContextState;
        FlyoutBehavior = FlyoutBehavior.Flyout;
        Loaded += (_, _) => ShellNavigationHelper.EnsureActiveShellItem(this, "home");
    }

    public void BuildMenu()
    {
        var preferredRoute = GetCurrentRoute() ?? "home";
        Items.Clear();

        Items.Add(CreateItem("Startseite", "home", () => _services.GetRequiredService<HomePage>()));
        Items.Add(CreateItem("Impressum", "impressum", () => _services.GetRequiredService<ImpressumPage>()));
        if (_userContextState.CurrentUserContext?.Role is UserRole.Admin or UserRole.Vorstand)
            Items.Add(CreateItem("Ablesen", "ablesen", () => new AblesenOverviewPage()));

        Items.Add(CreateItem("Parzellenverwaltung", "parzellen", () => _services.GetRequiredService<ParzellenPage>()));

        if (_userContextState.CurrentUserContext?.Role is UserRole.Admin or UserRole.Vorstand)
            Items.Add(CreateItem("Wartungsverträge", "wartungsvertraege", () => _services.GetRequiredService<WartungsvertraegePage>()));

        _workhoursReviewItem = CreateItem("Arbeitsstunden freigeben", "workhours_review", () => _services.GetRequiredService<ArbeitsstundenReviewPage>());
        Items.Add(_workhoursReviewItem);

        if (_userContextState.CurrentUserContext?.Role is UserRole.Admin or UserRole.Vorstand)
            Items.Add(CreateItem("Export", "export", () => _services.GetRequiredService<ExportPage>()));

        Items.Add(CreateItem("Mitgliedersuche", "membersearch", () => _services.GetRequiredService<MemberSearchPage>()));

        if (HasSelectedMember())
        {
            Items.Add(CreateItem("↳ Stammdaten", "memberdetails", () => _services.GetRequiredService<MeineDatenPage>()));
            Items.Add(CreateItem("↳ Wartungsverträge", "member_wartungsvertraege", () => _services.GetRequiredService<MemberWartungsvertraegePage>()));
            Items.Add(CreateItem("↳ Nebenmitglied", "member_nebenmitglied", () => _services.GetRequiredService<NebenmitgliedPage>()));
            Items.Add(CreateItem("↳ Gärten des Mitglieds", "member_gardens", () => _services.GetRequiredService<MemberGardensPage>()));
            if (_userContextState.CurrentUserContext?.Role == UserRole.Admin)
                Items.Add(CreateItem("↳ Benutzerverwaltung", "member_usermanagement", () => _services.GetRequiredService<UserManagementPage>()));
            Items.Add(CreateItem("↳ Arbeitsstunden", "member_workhours", () => _services.GetRequiredService<MyArbeitsstundenPage>()));
        }

        ShellNavigationHelper.EnsureActiveShellItem(this, preferredRoute);

        _ = RefreshWorkhoursReviewMenuAsync();
    }

    private bool HasSelectedMember()
        => _memberContextState.SelectedMember?.Id is > 0;

    private string? GetCurrentRoute()
        => CurrentItem?.CurrentItem?.CurrentItem?.Route;

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
        }
        catch
        {
            _workhoursReviewItem.Title = "Arbeitsstunden freigeben";
        }
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
