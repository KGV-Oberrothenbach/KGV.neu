using KGV.Core.Interfaces;
using KGV.Core.Security;
using KGV.Maui.State;
using Microsoft.Maui.Controls;
using System.Globalization;

namespace KGV.Maui.Pages;

public sealed class ArbeitseinsaetzeManagementPage : ManagementOverviewPageBase
{
    private readonly ArbeitseinsaetzeManagementState _managementState;
    private readonly UserContextState _userContextState;

    public ArbeitseinsaetzeManagementPage(ISupabaseService supabaseService, ArbeitseinsaetzeManagementState managementState, UserContextState userContextState)
        : base(supabaseService)
    {
        _managementState = managementState;
        _userContextState = userContextState;
    }

    protected override string PageTitle => "Arbeitseinsätze";
    protected override string PageDescription => "Ruhige mobile Übersicht des Verwaltungsbereichs `Arbeitseinsätze` mit chronologischer Reihenfolge und getrenntem Editor-/Datensatzfluss.";
    protected override string SectionQueryValue => "workassignments";
    protected override string EmptyText => "Aktuell liegen keine Arbeitseinsätze vor.";
    protected override string HintText => "Antippen öffnet den ruhigen mobilen Datensatz-/Editorpfad für diesen Arbeitseinsatz.";

    protected override async Task<IReadOnlyList<ManagementOverviewEntry>> LoadEntriesCoreAsync()
    {
        EnsureCanManageWorkAssignments();
        return (await LoadRecordsAsync())
            .Select(x => new ManagementOverviewEntry(
                x.Id,
                x.Titel ?? "(ohne Titel)",
                BuildSubtitle(x)))
            .ToList();
    }

    protected override async Task OpenNewAsync()
    {
        EnsureCanManageWorkAssignments();
        _managementState.SetEntries(await LoadRecordsAsync());
        await Shell.Current.GoToAsync(nameof(ArbeitseinsaetzeEditorPage));
    }

    protected override async Task OpenExistingAsync(long entryId)
    {
        EnsureCanManageWorkAssignments();
        _managementState.SetEntries(await LoadRecordsAsync(), entryId);
        await Shell.Current.GoToAsync($"{nameof(ArbeitseinsaetzeEditorPage)}?entryId={entryId}");
    }

    private void EnsureCanManageWorkAssignments()
    {
        if (!PermissionChecks.CanManageWorkAssignments(_userContextState.CurrentUserContext))
            throw new UnauthorizedAccessException("Für die Verwaltung von Arbeitseinsätzen fehlt die Berechtigung.");
    }

    private async Task<IReadOnlyList<KGV.Core.Models.ArbeitseinsatzRecord>> LoadRecordsAsync()
    {
        return (await SupabaseService.GetArbeitseinsaetzeVerwaltungAsync())
            .OrderBy(x => x.Datum)
            .ThenBy(x => x.StartUhrzeit ?? TimeSpan.MaxValue)
            .ThenBy(x => x.EndUhrzeit ?? TimeSpan.MaxValue)
            .ThenBy(x => x.Titel ?? string.Empty, StringComparer.CurrentCultureIgnoreCase)
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
