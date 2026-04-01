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
    private FlyoutItem? _memberDetailsItem;
    private FlyoutItem? _memberWartungsvertraegeItem;
    private FlyoutItem? _memberNebenmitgliedItem;
    private FlyoutItem? _memberGardensItem;
    private FlyoutItem? _memberAdminMenuItem;
    private FlyoutItem? _memberWorkhoursItem;

    public AdminShell(IServiceProvider services, UserContextState userContextState, MemberContextState memberContextState, PendingPhotoMenuState pendingPhotoMenuState)
    {
        _services = services;
        _userContextState = userContextState;
        _memberContextState = memberContextState;
        _pendingPhotoMenuState = pendingPhotoMenuState;
        FlyoutBehavior = FlyoutBehavior.Flyout;
        Loaded += (_, _) => ShellNavigationHelper.EnsureActiveShellItem(this, GetCurrentRoute() ?? "home");
        _memberContextState.Changed += (_, _) => UpdateSelectedMemberItemsVisibility();
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

        _memberDetailsItem = CreateItem("↳ Stammdaten", "memberdetails", () => _services.GetRequiredService<MeineDatenPage>());
        _memberWartungsvertraegeItem = CreateItem("↳ Wartungsverträge", "member_wartungsvertraege", () => _services.GetRequiredService<MemberWartungsvertraegePage>());
        _memberNebenmitgliedItem = CreateItem("↳ Nebenmitglied", "member_nebenmitglied", () => _services.GetRequiredService<NebenmitgliedPage>());
        _memberGardensItem = CreateItem("↳ Gärten des Mitglieds", "member_gardens", () => _services.GetRequiredService<MemberGardensPage>());
        _memberAdminMenuItem = CreateItem("↳ Admin-Menü", "member_adminmenu", () => _services.GetRequiredService<AdminMenuPage>());
        _memberWorkhoursItem = CreateItem("↳ Arbeitsstunden", "member_workhours", () => _services.GetRequiredService<MyArbeitsstundenPage>());

        Items.Add(_memberDetailsItem);
        Items.Add(_memberWartungsvertraegeItem);
        Items.Add(_memberNebenmitgliedItem);
        Items.Add(_memberGardensItem);
        Items.Add(_memberAdminMenuItem);
        Items.Add(_memberWorkhoursItem);

        UpdateSelectedMemberItemsVisibility();

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

    private void UpdateSelectedMemberItemsVisibility()
    {
        var hasSelectedMember = HasSelectedMember();
        var showAdminMenu = hasSelectedMember && _userContextState.CurrentUserContext?.Role == UserRole.Admin;

        if (_memberDetailsItem != null)
            _memberDetailsItem.IsVisible = hasSelectedMember;

        if (_memberWartungsvertraegeItem != null)
            _memberWartungsvertraegeItem.IsVisible = hasSelectedMember;

        if (_memberNebenmitgliedItem != null)
            _memberNebenmitgliedItem.IsVisible = hasSelectedMember;

        if (_memberGardensItem != null)
            _memberGardensItem.IsVisible = hasSelectedMember;

        if (_memberAdminMenuItem != null)
            _memberAdminMenuItem.IsVisible = showAdminMenu;

        if (_memberWorkhoursItem != null)
            _memberWorkhoursItem.IsVisible = hasSelectedMember;
    }

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
