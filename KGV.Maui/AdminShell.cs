using KGV.Core.Interfaces;
using KGV.Core.Security;
using KGV.Maui.Pages;
using KGV.Maui.State;
using Microsoft.Extensions.DependencyInjection;

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
        Loaded += (_, _) => ShellNavigationHelper.EnsureActiveShellItem(this, "home");
    }

    public void BuildMenu()
    {
        Items.Clear();

        Items.Add(CreateItem("Startseite", "home", () => _services.GetRequiredService<HomePage>()));

        Items.Add(CreateItem("Mitglieder · Mitgliedersuche", "membersearch", () => _services.GetRequiredService<MemberSearchPage>()));
        Items.Add(CreateItem("Mitglieder · Stammdaten", "memberdetails", () => _services.GetRequiredService<MeineDatenPage>()));
        Items.Add(CreateItem("Mitglieder · Nebenmitglied", "member_nebenmitglied", () => _services.GetRequiredService<NebenmitgliedPage>()));

        if (_userContextState.CurrentUserContext?.Role == UserRole.Admin)
            Items.Add(CreateItem("Mitglieder · Benutzerverwaltung", "member_usermanagement", () => _services.GetRequiredService<UserManagementPage>()));

        Items.Add(CreateItem("Mitglieder · Gärten des Mitglieds", "member_gardens", () => _services.GetRequiredService<MemberGardensPage>()));

        Items.Add(CreateItem("Parzellenverwaltung", "parzellen", () => _services.GetRequiredService<ParzellenPage>()));

        if (_userContextState.CurrentUserContext?.Role is UserRole.Admin or UserRole.Vorstand)
        {
            Items.Add(CreateItem("Ablesen · Ablesen", "ablesen", () => new AblesenOverviewPage()));
            Items.Add(CreateItem("Ablesen · RFID einrichten", "ablesen_rfid", () => _services.GetRequiredService<RfidEinrichtenPage>()));
            Items.Add(CreateItem("Ablesen · Fällige Zähler", "ablesen_faellig", () => _services.GetRequiredService<FaelligeZaehlerPage>()));
            Items.Add(CreateItem("Ablesen · Zählerwechsel", "ablesen_wechsel", () => _services.GetRequiredService<ZaehlerwechselPage>()));
        }

        Items.Add(CreateItem("Arbeitsstunden · Meine Arbeitsstunden", "workhours", () => _services.GetRequiredService<MyArbeitsstundenPage>()));

        _workhoursReviewItem = CreateItem("Arbeitsstunden · Arbeitsstunden freigeben", "workhours_review", () => _services.GetRequiredService<ArbeitsstundenReviewPage>());
        Items.Add(_workhoursReviewItem);

        if (_userContextState.CurrentUserContext?.Role is UserRole.Admin or UserRole.Vorstand)
        {
            Items.Add(CreateItem("Verwaltung · Bekanntmachungen", "management_announcements", () => _services.GetRequiredService<BekanntmachungenManagementPage>()));
            Items.Add(CreateItem("Verwaltung · Termine", "management_appointments", () => _services.GetRequiredService<TermineManagementPage>()));
            Items.Add(CreateItem("Verwaltung · Arbeitseinsätze", "management_workassignments", () => _services.GetRequiredService<ArbeitseinsaetzeManagementPage>()));
            Items.Add(CreateItem("Export", "export", () => _services.GetRequiredService<ExportPage>()));
        }

        Items.Add(CreateItem("Mein Profil", "myprofile", () => _services.GetRequiredService<MyProfilePage>()));

        ShellNavigationHelper.EnsureActiveShellItem(this, "home");

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
                ? $"Arbeitsstunden · Arbeitsstunden freigeben ({count})"
                : "Arbeitsstunden · Arbeitsstunden freigeben";
        }
        catch
        {
            _workhoursReviewItem.Title = "Arbeitsstunden · Arbeitsstunden freigeben";
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
