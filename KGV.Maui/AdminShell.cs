using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Maui.Pages;
using KGV.Maui.Services.Diagnostics;
using KGV.Maui.Services.PendingPhotos;
using KGV.Maui.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace KGV.Maui;

public sealed class AdminShell : Shell, IAppShellInitializer
{
    private readonly IServiceProvider _services;
    private readonly UserContextState _userContextState;
    private readonly MemberContextState _memberContextState;
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
    private bool _backNavigationInProgress;
    private bool _exitConfirmationInProgress;
    private string? _preferredStartupRoute;
    private bool _loadedInitializationScheduled;

    public AdminShell(IServiceProvider services, UserContextState userContextState, MemberContextState memberContextState, PendingPhotoMenuState pendingPhotoMenuState)
    {
        _services = services;
        _userContextState = userContextState;
        _memberContextState = memberContextState;
        _pendingPhotoMenuState = pendingPhotoMenuState;

        FlyoutBehavior = FlyoutBehavior.Flyout;
        BindingContext = memberContextState;
        Loaded += (_, _) =>
        {
            if (_loadedInitializationScheduled)
                return;

            _loadedInitializationScheduled = true;
            Dispatcher.Dispatch(async () =>
            {
                try
                {
                    await Task.Yield();
                    ClearImplicitOwnMemberContext();
                    BuildMenu();
                    EnsureActiveRouteAfterLoad();
                    await RefreshPendingPhotoUploadsMenu();
                    await RefreshWorkhoursReviewMenuAsync();
                }
                finally
                {
                    _loadedInitializationScheduled = false;
                }
            });
        };
        memberContextState.Changed += (_, _) => RefreshMemberContextMenu(memberContextState);
    }

    public void SetPreferredStartupRoute(string? route)
    {
        _preferredStartupRoute = route;
    }

    protected override bool OnBackButtonPressed()
    {
        AppFileLog.Info("KGV.Navigation", $"AdminShell.OnBackButtonPressed erreicht. Route={ShellNavigationHelper.GetActiveShellContentRoute(this) ?? "<none>"}");

        if (_backNavigationInProgress)
            return true;

        if (ShellNavigationHelper.ShouldSuppressBackNavigation())
            return true;

        if (ShellNavigationHelper.IsOnShellContentRoot(this, "home"))
        {
            if (_exitConfirmationInProgress)
                return true;

            _exitConfirmationInProgress = true;
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    var confirmExit = await DisplayAlert("App beenden", "Soll die App wirklich beendet werden?", "Beenden", "Abbrechen");
                    if (!confirmExit)
                        return;

                    AppFileLog.Info("KGV.Navigation", "AdminShell.Zurück auf Startseite bestätigt App-Beenden.");
                    Application.Current?.Quit();
                }
                catch (Exception ex)
                {
                    AppFileLog.Warning("KGV.Navigation", $"AdminShell.Beenden-Rückfrage fehlgeschlagen: {ex.GetType().Name}: {ex.Message}");
                }
                finally
                {
                    _exitConfirmationInProgress = false;
                }
            });

            return true;
        }

        _backNavigationInProgress = true;
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                await ShellNavigationHelper.NavigateBackAsync(this, "home");
            }
            catch (Exception ex)
            {
                AppFileLog.Warning("KGV.Navigation", $"AdminShell.System-Zurück fehlgeschlagen: {ex.GetType().Name}: {ex.Message}");
            }
            finally
            {
                _backNavigationInProgress = false;
            }
        });

        return true;
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

        if (PermissionChecks.CanReadParzellen(_userContextState.CurrentUserContext))
            Items.Add(CreateItem("Parzellenverwaltung", "parzellen", () => _services.GetRequiredService<ParzellenPage>()));

        if (PermissionChecks.CanEditAllMembers(_userContextState.CurrentUserContext))
            Items.Add(CreateItem("Wartungsverträge", "wartungsvertraege", () => _services.GetRequiredService<WartungsvertraegePage>()));

        if (PermissionChecks.CanManageWorkHours(_userContextState.CurrentUserContext))
        {
            _workhoursReviewItem = CreateItem("Arbeitsstunden freigeben", "workhours_review", () => _services.GetRequiredService<ArbeitsstundenReviewPage>());
            Items.Add(_workhoursReviewItem);
        }

        if (PermissionChecks.CanSearchMembers(_userContextState.CurrentUserContext))
            Items.Add(CreateItem("Export", "export", () => _services.GetRequiredService<ExportPage>()));

        if (PermissionChecks.CanSearchMembers(_userContextState.CurrentUserContext))
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
    }

    private void RefreshMemberContextMenu(MemberContextState? state)
    {
        var hasMember = state?.SelectedMember?.Id is > 0;
        if (_memberDetailsItem != null) _memberDetailsItem.IsVisible = hasMember && PermissionChecks.CanShowStammdaten(_userContextState.CurrentUserContext);
        if (_memberWartungsvertraegeItem != null) _memberWartungsvertraegeItem.IsVisible = hasMember;
        if (_memberNebenmitgliedItem != null) _memberNebenmitgliedItem.IsVisible = hasMember && PermissionChecks.CanReadStammdaten(_userContextState.CurrentUserContext);
        if (_memberGardensItem != null) _memberGardensItem.IsVisible = hasMember && PermissionChecks.CanShowParzellen(_userContextState.CurrentUserContext);
        if (_memberAdminMenuItem != null) _memberAdminMenuItem.IsVisible = hasMember && PermissionChecks.CanReadRoleManagement(_userContextState.CurrentUserContext);
        if (_memberWorkhoursItem != null) _memberWorkhoursItem.IsVisible = hasMember && PermissionChecks.CanReadWorkHours(_userContextState.CurrentUserContext);

        var currentRoute = ShellNavigationHelper.GetActiveShellContentRoute(this);
        if (currentRoute is "memberdetails" or "member_wartungsvertraege" or "member_nebenmitglied" or "member_gardens" or "member_adminmenu" or "member_workhours")
        {
            if (!hasMember)
                ShellNavigationHelper.EnsureActiveShellItem(this, "home");
        }
    }

    private void ClearImplicitOwnMemberContext()
    {
        var ownMemberId = _userContextState.CurrentMitgliedId is > 0 and <= int.MaxValue
            ? (int?)_userContextState.CurrentMitgliedId.Value
            : null;

        var selectedMember = _memberContextState.SelectedMember;
        if (!ownMemberId.HasValue || selectedMember?.Id != ownMemberId.Value)
            return;

        var hasExplicitSelectionContext = !string.IsNullOrWhiteSpace(selectedMember.DisplayName)
            || !string.IsNullOrWhiteSpace(selectedMember.Vorname)
            || !string.IsNullOrWhiteSpace(selectedMember.Nachname)
            || selectedMember.IstHauptmitglied;

        if (hasExplicitSelectionContext)
            return;

        _memberContextState.Clear();
    }

    private void EnsureActiveRouteAfterLoad()
    {
        var preferredRoute = _preferredStartupRoute;
        _preferredStartupRoute = null;

        if (!string.IsNullOrWhiteSpace(preferredRoute)
            && ShellNavigationHelper.HasVisibleShellContentRoute(this, preferredRoute))
        {
            AppFileLog.Info("KGV.Navigation", $"AdminShell aktiviert bevorzugte Start-Route: {preferredRoute}.");
            ShellNavigationHelper.EnsureActiveShellItem(this, preferredRoute);
            return;
        }

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