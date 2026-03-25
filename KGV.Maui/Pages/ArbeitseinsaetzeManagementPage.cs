using KGV.Core.Interfaces;
using System.Globalization;

namespace KGV.Maui.Pages;

public sealed class ArbeitseinsaetzeManagementPage : ManagementOverviewPageBase
{
    public ArbeitseinsaetzeManagementPage(ISupabaseService supabaseService)
        : base(supabaseService)
    {
    }

    protected override string PageTitle => "Arbeitseinsätze";
    protected override string PageDescription => "Ruhige mobile Übersicht des Verwaltungsbereichs `Arbeitseinsätze`. Der Editor-/Detailfluss bleibt in diesem Block bewusst außerhalb der neuen Hauptübersicht.";
    protected override string SectionQueryValue => "workassignments";
    protected override string EmptyText => "Aktuell liegen keine Arbeitseinsätze vor.";

    protected override async Task<IReadOnlyList<ManagementOverviewEntry>> LoadEntriesCoreAsync()
    {
        return (await SupabaseService.GetArbeitseinsaetzeVerwaltungAsync())
            .OrderByDescending(x => x.Datum)
            .ThenByDescending(x => x.Id)
            .Select(x => new ManagementOverviewEntry(
                x.Id,
                x.Titel ?? "(ohne Titel)",
                BuildSubtitle(x)))
            .ToList();
    }

    private static string BuildSubtitle(KGV.Core.Models.ArbeitseinsatzRecord record)
    {
        var date = record.Datum == default ? "-" : record.Datum.ToString("dd.MM.yyyy", CultureInfo.CurrentCulture);
        var location = string.IsNullOrWhiteSpace(record.Treffpunkt) ? "ohne Treffpunkt" : record.Treffpunkt.Trim();
        var maxParticipants = record.MaxTeilnehmer?.ToString(CultureInfo.CurrentCulture) ?? "unbegrenzt";
        return $"Datum: {date} · Treffpunkt: {location} · Teilnehmer: {maxParticipants}";
    }
}
