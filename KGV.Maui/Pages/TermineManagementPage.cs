using KGV.Core.Interfaces;
using System.Globalization;

namespace KGV.Maui.Pages;

public sealed class TermineManagementPage : ManagementOverviewPageBase
{
    public TermineManagementPage(ISupabaseService supabaseService)
        : base(supabaseService)
    {
    }

    protected override string PageTitle => "Termine";
    protected override string PageDescription => "Ruhige mobile Übersicht des Verwaltungsbereichs `Termine`. Bereichsumschaltung innerhalb derselben Seite entfällt hier bewusst.";
    protected override string SectionQueryValue => "appointments";
    protected override string EmptyText => "Aktuell liegen keine Termine vor.";

    protected override async Task<IReadOnlyList<ManagementOverviewEntry>> LoadEntriesCoreAsync()
    {
        return (await SupabaseService.GetTermineVerwaltungAsync())
            .OrderByDescending(x => x.Datum)
            .ThenByDescending(x => x.Id)
            .Select(x => new ManagementOverviewEntry(
                x.Id,
                x.Titel ?? "(ohne Titel)",
                BuildSubtitle(x)))
            .ToList();
    }

    private static string BuildSubtitle(KGV.Core.Models.TerminRecord record)
    {
        var date = record.Datum == default ? "-" : record.Datum.ToString("dd.MM.yyyy", CultureInfo.CurrentCulture);
        var start = record.StartUhrzeit?.ToString("hh\\:mm", CultureInfo.CurrentCulture) ?? "-";
        var end = record.EndUhrzeit?.ToString("hh\\:mm", CultureInfo.CurrentCulture) ?? "-";
        return $"Datum: {date} · {start} - {end} · Aktiv: {(record.Aktiv ? "ja" : "nein")}";
    }
}
