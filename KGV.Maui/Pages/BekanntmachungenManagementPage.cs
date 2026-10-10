using KGV.Core.Interfaces;
using KGV.Core.Security;
using KGV.Maui.State;
using Microsoft.Maui.Controls;
using System.Globalization;

namespace KGV.Maui.Pages;

public sealed class BekanntmachungenManagementPage : ManagementOverviewPageBase
{
    private readonly UserContextState _userContextState;
    public BekanntmachungenManagementPage(ISupabaseService supabaseService, UserContextState userContextState)
        : base(supabaseService)
    {
        _userContextState = userContextState;
    }

    protected override string PageTitle => "Bekanntmachungen";
    protected override string PageDescription => "Ruhige mobile Übersicht des Verwaltungsbereichs `Bekanntmachungen`. Neu und Bearbeiten öffnen jetzt einen eigenen mobilen Bekanntmachungen-Editor statt einer Mischseite.";
    protected override string SectionQueryValue => "announcements";
    protected override string EmptyText => "Aktuell liegen keine Bekanntmachungen vor.";

    protected override async Task<IReadOnlyList<ManagementOverviewEntry>> LoadEntriesCoreAsync()
    {
        if (!PermissionChecks.CanManageAnnouncements(_userContextState.CurrentUserContext))
            return Array.Empty<ManagementOverviewEntry>();
        return (await SupabaseService.GetBekanntmachungenVerwaltungAsync())
            .OrderBy(x => x.SortOrder ?? int.MaxValue)
            .ThenByDescending(x => x.SichtbarAb ?? DateTime.MinValue)
            .Select(x => new ManagementOverviewEntry(
                x.Id,
                x.Titel ?? "(ohne Titel)",
                BuildSubtitle(x)))
            .ToList();
    }

    protected override string HintText => "Antippen öffnet den eigenen Bekanntmachungen-Editor.";

    protected override Task OpenNewAsync()
    {
        if (!PermissionChecks.CanManageAnnouncements(_userContextState.CurrentUserContext)) return Shell.Current.GoToAsync("//home");
        return Shell.Current.GoToAsync(nameof(BekanntmachungEditorPage));
    }

    protected override Task OpenExistingAsync(long entryId)
    {
        if (!PermissionChecks.CanManageAnnouncements(_userContextState.CurrentUserContext)) return Shell.Current.GoToAsync("//home");
        return Shell.Current.GoToAsync($"{nameof(BekanntmachungEditorPage)}?entryId={entryId}");
    }

    private static string BuildSubtitle(KGV.Core.Models.BekanntmachungRecord record)
    {
        var visibleFrom = record.SichtbarAb?.ToString("dd.MM.yyyy HH:mm", CultureInfo.CurrentCulture) ?? "-";
        var visibleTo = record.SichtbarBis?.ToString("dd.MM.yyyy HH:mm", CultureInfo.CurrentCulture) ?? "-";
        return $"Sichtbar: {visibleFrom} bis {visibleTo} · Aktiv: {(record.Aktiv ? "ja" : "nein")}";
    }
}
