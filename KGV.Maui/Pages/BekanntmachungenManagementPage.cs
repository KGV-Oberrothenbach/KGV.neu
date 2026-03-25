using KGV.Core.Interfaces;
using System.Globalization;

namespace KGV.Maui.Pages;

public sealed class BekanntmachungenManagementPage : ManagementOverviewPageBase
{
    public BekanntmachungenManagementPage(ISupabaseService supabaseService)
        : base(supabaseService)
    {
    }

    protected override string PageTitle => "Bekanntmachungen";
    protected override string PageDescription => "Ruhige mobile Übersicht des Verwaltungsbereichs `Bekanntmachungen`. Der eigentliche Editor bleibt in diesem Block noch bewusst im technischen Fortsetzungspfad.";
    protected override string SectionQueryValue => "announcements";
    protected override string EmptyText => "Aktuell liegen keine Bekanntmachungen vor.";

    protected override async Task<IReadOnlyList<ManagementOverviewEntry>> LoadEntriesCoreAsync()
    {
        return (await SupabaseService.GetBekanntmachungenVerwaltungAsync())
            .OrderBy(x => x.SortOrder ?? int.MaxValue)
            .ThenByDescending(x => x.SichtbarAb ?? DateTime.MinValue)
            .Select(x => new ManagementOverviewEntry(
                x.Id,
                x.Titel ?? "(ohne Titel)",
                BuildSubtitle(x)))
            .ToList();
    }

    private static string BuildSubtitle(KGV.Core.Models.BekanntmachungRecord record)
    {
        var visibleFrom = record.SichtbarAb?.ToString("dd.MM.yyyy HH:mm", CultureInfo.CurrentCulture) ?? "-";
        var visibleTo = record.SichtbarBis?.ToString("dd.MM.yyyy HH:mm", CultureInfo.CurrentCulture) ?? "-";
        return $"Sichtbar: {visibleFrom} bis {visibleTo} · Aktiv: {(record.Aktiv ? "ja" : "nein")}";
    }
}
