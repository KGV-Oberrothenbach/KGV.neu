using KGV.Core.Security;
using KGV.Maui;
using KGV.Maui.Settings;
using KGV.Maui.State;
using System.Linq;

namespace KGV.Maui.Pages;

public sealed class RoleChoicePage : ContentPage
{
    private readonly IPermissionService _permissionService;
    private readonly UserContextState _userContextState;
    private readonly AdminShell _adminShell;
    private readonly UserShell _userShell;

    private readonly Label _statusLabel;

    public RoleChoicePage(
        IPermissionService permissionService,
        UserContextState userContextState,
        AdminShell adminShell,
        UserShell userShell)
    {
        _permissionService = permissionService;
        _userContextState = userContextState;
        _adminShell = adminShell;
        _userShell = userShell;

        Title = "Modus wählen";

        _statusLabel = new Label { TextColor = Colors.Red };

        var userButton = new Button { Text = "Ich bin User" };
        userButton.Clicked += (_, _) => SelectMode(AppMode.User);

        var adminButton = new Button { Text = "Ich bin Vorstand/Admin" };
        adminButton.Clicked += (_, _) => SelectMode(AppMode.Admin);

        Content = new VerticalStackLayout
        {
            Padding = 24,
            Spacing = 12,
            Children =
            {
                new Label { Text = "Wer bist du?", FontSize = 24, FontAttributes = FontAttributes.Bold },
                userButton,
                adminButton,
                _statusLabel
            }
        };
    }

    private void SelectMode(AppMode mode)
    {
        if (_userContextState.CurrentUserId == null)
        {
            _statusLabel.Text = "UserId fehlt (bitte erneut anmelden).";
            return;
        }

        if (mode == AppMode.User && _userContextState.CurrentMitgliedId == null)
        {
            _statusLabel.Text = "Account ist keinem Mitglied zugeordnet.";
            return;
        }

        AppSettings.AppMode = AppModes.ToStorageValue(mode);
        AppSettings.Save();

        _userContextState.CurrentAppMode = mode;

        var role = mode == AppMode.Admin ? UserRoles.Vorstand : UserRoles.User;
        _userContextState.CurrentUserContext = _permissionService.CreateContext(
            _userContextState.CurrentUserId.Value,
            role,
            _userContextState.CurrentMitgliedId);

        var window = Application.Current?.Windows?.FirstOrDefault();
        if (window == null)
            return;

        window.Page = mode == AppMode.Admin ? _adminShell : _userShell;

        if (window.Page is IAppShellInitializer init)
            init.BuildMenu();
    }
}
