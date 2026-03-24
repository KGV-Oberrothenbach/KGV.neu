using System.Linq;

namespace KGV.Maui;

internal static class ShellNavigationHelper
{
    public static void EnsureActiveShellItem(Shell shell)
    {
        ArgumentNullException.ThrowIfNull(shell);

        var activeItem = IsValid(shell.CurrentItem)
            ? shell.CurrentItem
            : shell.Items.FirstOrDefault(IsValid);

        if (activeItem == null)
            return;

        if (!ReferenceEquals(shell.CurrentItem, activeItem))
            shell.CurrentItem = activeItem;

        var activeSection = IsValid(activeItem.CurrentItem)
            ? activeItem.CurrentItem
            : activeItem.Items.FirstOrDefault(IsValid);

        if (activeSection == null)
            return;

        if (!ReferenceEquals(activeItem.CurrentItem, activeSection))
            activeItem.CurrentItem = activeSection;

        var activeContent = IsValid(activeSection.CurrentItem)
            ? activeSection.CurrentItem
            : activeSection.Items.FirstOrDefault(IsValid);

        if (activeContent == null)
            return;

        if (!ReferenceEquals(activeSection.CurrentItem, activeContent))
            activeSection.CurrentItem = activeContent;
    }

    private static bool IsValid(ShellItem? item)
        => item?.IsVisible == true && item.Items.Any(IsValid);

    private static bool IsValid(ShellSection? section)
        => section?.IsVisible == true && section.Items.Any(IsValid);

    private static bool IsValid(ShellContent? content)
        => content?.IsVisible == true;
}
