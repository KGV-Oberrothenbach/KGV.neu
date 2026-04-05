using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Maui.Pages;
using KGV.Maui.Services.Diagnostics;
using KGV.Maui.Services.PendingPhotos;
using KGV.Maui.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace KGV.Maui;

public sealed class AdminShell : Shell
{
    private readonly IServiceProvider _services;
    private readonly UserContextState _userContextState;
    private readonly PendingPhotoMenuState _pendingPhotoMenuState;
    private bool _menuBuilt;

    private FlyoutItem? _pendingPhotoUploadsItem;
    private FlyoutItem? _workhoursReviewItem;
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
        _pendingPhotoMenuState = pendingPhotoMenuState;

        FlyoutBehavior = FlyoutBehavior.Flyout;
        BindingContext = memberContextState;
        Loaded += async (_, _) =>
        {
            BuildMenu();
            EnsureActiveRouteAfterLoad();
            await RefreshPendingPhotoUploadsMenu();
            await RefreshWorkhoursReviewMenuAsync();
        };
        memberContextState.Changed += (_, _) => RefreshMemberContextMenu(memberContextState);
        BuildMenu();
    }

    public void BuildMenu()
    {
        AppFileLog.Info("KGV.Navigation", $"AdminShell.BuildMenu aufgerufen. Items={Items.Count}, CurrentRoute={ShellNavigationHelper.GetActiveShellContentRoute(this) ?? "<none>"}.");

        if (_menuBuilt)
        {
            AppFileLog.Info("KGV.Navigation", "AdminShell.BuildMenu aktualisiert nur Sichtbarkeit/Status ohne Rebuild.");
            RefreshMemberContextMenu(BindingContext as MemberContextState);
            return;
        }

        Items.Add(CreateItem("Startseite", "home", () => _services.GetRequiredService<HomePage>()));
        Items.Add(CreateItem("Impressum", "impressum", () => _services.GetRequiredService<ImpressumPage>()));
        if (PermissionChecks.HasAnyMeterAccess(_userContextState.CurrentUserContext))
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

        if (PermissionChecks.CanManageWorkHours(_userContextState.CurrentUserContext))
        {
            _workhoursReviewItem = CreateItem("Arbeitsstunden freigeben", "workhours_review", () => _services.GetRequiredService<ArbeitsstundenReviewPage>());
            Items.Add(_workhoursReviewItem);
        }

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

        RefreshMemberContextMenu(BindingContext as MemberContextState);

        _menuBuilt = true;
        ShellNavigationHelper.EnsureActiveShellItem(this, "home");
    }

    private void RefreshMemberContextMenu(MemberContextState? state)
    {
        var hasMember = state?.SelectedMember != null;
        if (_memberDetailsItem != null) _memberDetailsItem.IsVisible = hasMember;
        if (_memberWartungsvertraegeItem != null) _memberWartungsvertraegeItem.IsVisible = hasMember;
        if (_memberNebenmitgliedItem != null) _memberNebenmitgliedItem.IsVisible = hasMember;
        if (_memberGardensItem != null) _memberGardensItem.IsVisible = hasMember;
        if (_memberAdminMenuItem != null) _memberAdminMenuItem.IsVisible = hasMember;
        if (_memberWorkhoursItem != null) _memberWorkhoursItem.IsVisible = hasMember;

        var currentRoute = ShellNavigationHelper.GetActiveShellContentRoute(this);
        if (currentRoute is "memberdetails" or "member_wartungsvertraege" or "member_nebenmitglied" or "member_gardens" or "member_adminmenu" or "member_workhours")
        {
            if (!hasMember)
                ShellNavigationHelper.EnsureActiveShellItem(this, "home");
        }
    }

    private void EnsureActiveRouteAfterLoad()
    {
        var currentRoute = ShellNavigationHelper.GetActiveShellContentRoute(this);
        if (currentRoute != null && ShellNavigationHelper.HasVisibleShellContentRoute(this, currentRoute))
        {
            AppFileLog.Info("KGV.Navigation", $"AdminShell belässt aktive Route: {currentRoute}.");
            return;
        }

        AppFileLog.Warning("KGV.Navigation", $"AdminShell verwendet Fallback auf home. Aktive Route war {(currentRoute ?? "<none>")}.");
        ShellNavigationHelper.EnsureActiveShellItem(this, "home");
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

    public Task RefreshPendingPhotoUploadsMenu()
    {
        if (_pendingPhotoUploadsItem == null)
            return Task.CompletedTask;

        _pendingPhotoMenuState.Refresh();
        _pendingPhotoUploadsItem.Title = _pendingPhotoMenuState.MenuTitle;
        _pendingPhotoUploadsItem.IsVisible = _pendingPhotoMenuState.HasOpenItems;
        return Task.CompletedTask;
    }
}
