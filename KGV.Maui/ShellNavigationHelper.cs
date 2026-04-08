using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KGV.Maui.Services.Diagnostics;
using Microsoft.Maui.Controls;

namespace KGV.Maui;

internal static class ShellNavigationHelper
{
    private static readonly Dictionary<string, string> LogicalBackRoutes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["memberdetails"] = "membersearch",
        ["member_wartungsvertraege"] = "memberdetails",
        ["member_nebenmitglied"] = "memberdetails",
        ["member_gardens"] = "memberdetails",
        ["member_adminmenu"] = "memberdetails",
        ["member_workhours"] = "memberdetails",
        ["photo_uploads"] = "ablesen"
    };

    public static bool IsOnShellContentRoot(Shell shell, string route)
    {
        ArgumentNullException.ThrowIfNull(shell);

        if (string.IsNullOrWhiteSpace(route))
            return false;

        if (!string.Equals(GetActiveShellContentRoute(shell), route, StringComparison.OrdinalIgnoreCase))
            return false;

        return shell.Navigation.ModalStack.Count == 0
            && shell.Navigation.NavigationStack.Count <= 1;
    }

    public static bool HasValidActiveShellContentRoute(Shell shell)
    {
        ArgumentNullException.ThrowIfNull(shell);

        var route = GetActiveShellContentRoute(shell);
        return route != null && HasVisibleShellContentRoute(shell, route);
    }

    public static string? GetActiveShellContentRoute(Shell shell)
    {
        ArgumentNullException.ThrowIfNull(shell);

        var item = shell.CurrentItem;
        if (!IsValid(item))
            return null;

        var section = item.CurrentItem;
        if (!IsValid(section))
            return null;

        var content = section.CurrentItem;
        if (!IsValid(content))
            return null;

        return content.Route;
    }

    public static void EnsureActiveShellItem(Shell shell, string? preferredContentRoute = null)
    {
        ArgumentNullException.ThrowIfNull(shell);

        var preferredTarget = FindPreferredTarget(shell, preferredContentRoute);

        var activeItem = preferredTarget.Item
            ?? (IsValid(shell.CurrentItem)
                ? shell.CurrentItem
                : shell.Items.FirstOrDefault(IsValid));

        if (activeItem == null)
            return;

        if (!ReferenceEquals(shell.CurrentItem, activeItem))
            shell.CurrentItem = activeItem;

        var activeSection = preferredTarget.Section != null && ReferenceEquals(preferredTarget.Item, activeItem)
            ? preferredTarget.Section
            : (IsValid(activeItem.CurrentItem)
                ? activeItem.CurrentItem
                : activeItem.Items.FirstOrDefault(IsValid));

        if (activeSection == null)
            return;

        if (!ReferenceEquals(activeItem.CurrentItem, activeSection))
            activeItem.CurrentItem = activeSection;

        var activeContent = preferredTarget.Content != null && ReferenceEquals(preferredTarget.Section, activeSection)
            ? preferredTarget.Content
            : (IsValid(activeSection.CurrentItem)
                ? activeSection.CurrentItem
                : activeSection.Items.FirstOrDefault(IsValid));

        if (activeContent == null)
            return;

        if (!ReferenceEquals(activeSection.CurrentItem, activeContent))
            activeSection.CurrentItem = activeContent;
    }

    public static bool HasVisibleShellContentRoute(Shell shell, string route)
    {
        ArgumentNullException.ThrowIfNull(shell);

        if (string.IsNullOrWhiteSpace(route))
            return false;

        return FindPreferredTarget(shell, route).Content != null;
    }

    public static bool ShouldSuppressBackNavigation()
    {
        if (!NavigationCoordinator.IsActive(NavigationCoordinator.RootSwitchScope, NavigationCoordinator.MemberSwitchScope))
            return false;

        AppFileLog.Info("KGV.Navigation", "System-Zurück wird während aktivem Root-/Mitgliedswechsel unterdrückt.");
        return true;
    }

    public static async Task NavigateBackAsync(Shell shell, string homeRoute)
    {
        ArgumentNullException.ThrowIfNull(shell);

        if (shell.Navigation.ModalStack.Count > 0)
        {
            AppFileLog.Info("KGV.Navigation", $"System-Zurück schließt Modal. Aktive Route={GetActiveShellContentRoute(shell) ?? "<none>"}.");
            await shell.Navigation.PopModalAsync();
            return;
        }

        if (shell.Navigation.NavigationStack.Count > 1)
        {
            AppFileLog.Info("KGV.Navigation", $"System-Zurück navigiert eine Ebene zurück. Aktive Route={GetActiveShellContentRoute(shell) ?? "<none>"}, Stack={shell.Navigation.NavigationStack.Count}.");
            await shell.GoToAsync("..");
            return;
        }

        var currentRoute = GetActiveShellContentRoute(shell);
        if (string.IsNullOrWhiteSpace(currentRoute) || !HasVisibleShellContentRoute(shell, currentRoute))
        {
            AppFileLog.Info("KGV.Navigation", $"System-Zurück verwendet Fallback zur Startseite. Aktive Route={currentRoute ?? "<none>"}.");
            await shell.GoToAsync($"//{homeRoute}");
            return;
        }

        var targetRoute = ResolveLogicalBackTarget(shell, currentRoute, homeRoute);
        if (string.IsNullOrWhiteSpace(targetRoute) || string.Equals(targetRoute, currentRoute, StringComparison.OrdinalIgnoreCase))
        {
            AppFileLog.Info("KGV.Navigation", $"System-Zurück verwendet Fallback zur Startseite. Aktive Route={currentRoute}.");
            await shell.GoToAsync($"//{homeRoute}");
            return;
        }

        AppFileLog.Info("KGV.Navigation", $"System-Zurück navigiert logisch von {currentRoute} nach {targetRoute}.");
        await shell.GoToAsync($"//{targetRoute}");
    }

    private static string ResolveLogicalBackTarget(Shell shell, string currentRoute, string homeRoute)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var candidate = currentRoute;

        while (!string.IsNullOrWhiteSpace(candidate) && visited.Add(candidate))
        {
            if (!LogicalBackRoutes.TryGetValue(candidate, out var parentRoute) || string.IsNullOrWhiteSpace(parentRoute))
                break;

            if (HasVisibleShellContentRoute(shell, parentRoute))
                return parentRoute;

            candidate = parentRoute;
        }

        return homeRoute;
    }

    private static bool IsValid(ShellItem? item)
        => item?.IsVisible == true && item.Items.Any(IsValid);

    private static bool IsValid(ShellSection? section)
        => section?.IsVisible == true && section.Items.Any(IsValid);

    private static bool IsValid(ShellContent? content)
        => content?.IsVisible == true;

    private static (ShellItem? Item, ShellSection? Section, ShellContent? Content) FindPreferredTarget(Shell shell, string? preferredContentRoute)
    {
        if (string.IsNullOrWhiteSpace(preferredContentRoute))
            return default;

        foreach (var item in shell.Items)
        {
            if (!IsValid(item))
                continue;

            foreach (var section in item.Items)
            {
                if (!IsValid(section))
                    continue;

                var content = section.Items.FirstOrDefault(x => IsValid(x) && string.Equals(x.Route, preferredContentRoute, StringComparison.OrdinalIgnoreCase));
                if (content != null)
                    return (item, section, content);
            }
        }

        return default;
    }
}