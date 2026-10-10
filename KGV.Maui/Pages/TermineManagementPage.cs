using KGV.Core.Interfaces;
using KGV.Core.Security;
using KGV.Maui.State;
using Microsoft.Maui.Controls;
using System.Globalization;

namespace KGV.Maui.Pages;

public sealed class TermineManagementPage : ManagementOverviewPageBase
{
    private readonly UserContextState _userContextState;
    public TermineManagementPage(ISupabaseService supabaseService, UserContextState userContextState)
        : base(supabaseService)
    {
        _userContextState = userContextState;
    }

    protected override string PageTitle => "Termine";
    protected override string PageDescription => "Ruhige mobile Übersicht des Verwaltungsbereichs `Termine`. Neu und Bearbeiten öffnen jetzt einen eigenen mobilen Termine-Editor statt einer Mischseite.";
    protected override string SectionQueryValue => "appointments";
    protected override string EmptyText => "Aktuell liegen keine Termine vor.";

    protected override async Task<IReadOnlyList<ManagementOverviewEntry>> LoadEntriesCoreAsync()
    {
        if (!PermissionChecks.CanManageAppointments(_userContextState.CurrentUserContext))
            return Array.Empty<ManagementOverviewEntry>();
        return (await SupabaseService.GetTermineVerwaltungAsync())
            .OrderBy(x => x.Datum)
            .ThenBy(x => x.StartUhrzeit ?? TimeSpan.MaxValue)
            .ThenBy(x => x.EndUhrzeit ?? TimeSpan.MaxValue)
            .ThenBy(x => x.Titel ?? string.Empty, StringComparer.CurrentCultureIgnoreCase)
            .Select(x => new ManagementOverviewEntry(
                x.Id,
                x.Titel ?? "(ohne Titel)",
                BuildSubtitle(x)))
            .ToList();
    }

    protected override string HintText => "Antippen öffnet den eigenen Termine-Editor.";

    protected override Task OpenNewAsync()
    {
        if (!PermissionChecks.CanManageAppointments(_userContextState.CurrentUserContext)) return Shell.Current.GoToAsync("//home");
        return Shell.Current.GoToAsync(nameof(TermineEditorPage));
    }

    protected override Task OpenExistingAsync(long entryId)
    {
        if (!PermissionChecks.CanManageAppointments(_userContextState.CurrentUserContext)) return Shell.Current.GoToAsync("//home");
        return Shell.Current.GoToAsync($"{nameof(TermineEditorPage)}?entryId={entryId}");
    }

    private static string BuildSubtitle(KGV.Core.Models.TerminRecord record)
    {
        var date = record.Datum == default ? "-" : record.Datum.ToString("dd.MM.yyyy", CultureInfo.CurrentCulture);
        var start = record.StartUhrzeit?.ToString("hh\\:mm", CultureInfo.CurrentCulture) ?? "-";
        var end = record.EndUhrzeit?.ToString("hh\\:mm", CultureInfo.CurrentCulture) ?? "-";
        return $"Datum: {date} · {start} - {end} · Aktiv: {(record.Aktiv ? "ja" : "nein")}";
    }
}
