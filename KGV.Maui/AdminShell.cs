using KGV.Core.Interfaces;
using KGV.Core.Security;
using KGV.Maui.Pages;
using KGV.Maui.State;
using KGV.Maui.Services.PendingPhotos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace KGV.Maui;

public sealed class AdminShell : Shell, IAppShellInitializer
{
    private readonly IServiceProvider _services;
    private readonly UserContextState _userContextState;
    private readonly MemberContextState _memberContextState;
    private readonly PendingPhotoMenuState _pendingPhotoMenuState;
    private FlyoutItem? _workhoursReviewItem;
    private FlyoutItem? _pendingPhotoUploadsItem;

    public AdminShell(IServiceProvider services, UserContextState userContextState, MemberContextState memberContextState, PendingPhotoMenuState pendingPhotoMenuState)
    {
        _services = services;
        _userContextState = userContextState;
        _memberContextState = memberContextState;
        _pendingPhotoMenuState = pendingPhotoMenuState;
        FlyoutBehavior = FlyoutBehavior.Flyout;
        Loaded += (_, _) => ShellNavigationHelper.EnsureActiveShellItem(this, GetCurrentRoute() ?? "home");
    }

    public void BuildMenu()
    {
        var preferredRoute = GetCurrentRoute() ?? "home";
        Items.Clear();

        Items.Add(CreateItem("Startseite", "home", () => _services.GetRequiredService<HomePage>()));
        Items.Add(CreateItem("Impressum", "impressum", () => _services.GetRequiredService<ImpressumPage>()));
        if (_userContextState.CurrentUserContext?.Role is UserRole.Admin or UserRole.Vorstand)
        {
            Items.Add(CreateItem("Ablesen", "ablesen", () => _services.GetRequiredService<AblesenOverviewPage>()));

            _pendingPhotoMenuState.Refresh();
            _pendingPhotoUploadsItem = CreateItem(
                _pendingPhotoMenuState.MenuTitle,
                "photo_uploads",
                () => _services.GetRequiredService<PendingPhotoUploadsPage>());
            _pendingPhotoUploadsItem.IsVisible = _pendingPhotoMenuState.HasOpenItems;
            Items.Add(_pendingPhotoUploadsItem);
        }

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
                Items.Add(CreateItem("↳ Admin-Menü", "member_adminmenu", () => _services.GetRequiredService<AdminMenuPage>()));
            Items.Add(CreateItem("↳ Arbeitsstunden", "member_workhours", () => _services.GetRequiredService<MyArbeitsstundenPage>()));
        }

        ShellNavigationHelper.EnsureActiveShellItem(this, preferredRoute);

        _ = RefreshWorkhoursReviewMenuAsync();
    }

    public void RefreshPendingPhotoUploadsMenu()
    {
        if (_pendingPhotoUploadsItem == null)
            return;

        _pendingPhotoMenuState.Refresh();
        _pendingPhotoUploadsItem.Title = _pendingPhotoMenuState.MenuTitle;
        _pendingPhotoUploadsItem.IsVisible = _pendingPhotoMenuState.HasOpenItems;
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
