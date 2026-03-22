using KGV.Core.Interfaces;
using KGV.Core.Models;
using Microsoft.Maui.ApplicationModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace KGV.Maui.ViewModels;

public sealed class ParzellenViewModel : INotifyPropertyChanged
{
    private readonly ISupabaseService _supabaseService;
    private ParzelleVerwaltungItem? _selectedItem;
    private ParzelleDetailDTO? _selectedDetail;
    private MemberDTO? _selectedAssignMember;
    private DateTime _assignVonDatum = DateTime.Today;
    private string _statusMessage = string.Empty;
    private bool _isBusy;

    public ParzellenViewModel(ISupabaseService supabaseService)
    {
        _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ParzelleVerwaltungItem> Items { get; } = new();
    public ObservableCollection<MemberDTO> AssignableMembers { get; } = new();
    public ObservableCollection<ZaehlerAblesungDTO> StromAblesungen { get; } = new();
    public ObservableCollection<ZaehlerAblesungDTO> WasserAblesungen { get; } = new();
    public ObservableCollection<DocumentInfo> Dokumente { get; } = new();

    public string Title => "Parzellen";
    public string Description => "Zentrale Parzellenübersicht mit Stammdaten, Belegung, Wasser/Strom und Dokumentbezug.";
    public string DetailHint => "Separate Anschlussflags liegen aktuell nicht als eigenes Feld vor. Sichtbar ist deshalb der belastbare Status aus vorhandenen Zählern, Ablesungen und Dokumentpfaden.";

    public ParzelleVerwaltungItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (_selectedItem == value)
                return;

            _selectedItem = value;
            OnPropertyChanged();
            _ = LoadSelectedDetailAsync();
        }
    }

    public ParzelleDetailDTO? SelectedDetail
    {
        get => _selectedDetail;
        private set
        {
            if (_selectedDetail == value)
                return;

            _selectedDetail = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelectedDetail));
            OnPropertyChanged(nameof(ShowSelectionHint));
            OnPropertyChanged(nameof(CanManageAssignment));
            OnPropertyChanged(nameof(CanAssign));
            OnPropertyChanged(nameof(CanEndAssignment));
        }
    }

    public bool HasSelectedDetail => SelectedDetail != null;
    public bool ShowSelectionHint => !HasSelectedDetail;
    public bool CanManageAssignment => HasSelectedDetail && !IsBusy;
    public bool CanAssign => CanManageAssignment && SelectedAssignMember != null;
    public bool CanEndAssignment => CanManageAssignment && SelectedDetail?.BelegungId is > 0 && SelectedDetail.BisDatum == null;
    public bool HasStromAblesungen => StromAblesungen.Count > 0;
    public bool HasWasserAblesungen => WasserAblesungen.Count > 0;
    public bool HasDokumente => Dokumente.Count > 0;

    public MemberDTO? SelectedAssignMember
    {
        get => _selectedAssignMember;
        set
        {
            if (_selectedAssignMember == value)
                return;

            _selectedAssignMember = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanAssign));
        }
    }

    public DateTime AssignVonDatum
    {
        get => _assignVonDatum;
        set
        {
            if (_assignVonDatum == value.Date)
                return;

            _assignVonDatum = value.Date;
            OnPropertyChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (string.Equals(_statusMessage, value, StringComparison.Ordinal))
                return;

            _statusMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasStatusMessage));
        }
    }

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy == value)
                return;

            _isBusy = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanManageAssignment));
            OnPropertyChanged(nameof(CanAssign));
            OnPropertyChanged(nameof(CanEndAssignment));
        }
    }

    public async Task InitializeAsync()
    {
        if (Items.Count > 0)
            return;

        await LoadAsync(resetItems: true);
    }

    public async Task RefreshAsync()
    {
        Items.Clear();
        SelectedItem = null;
        SelectedDetail = null;
        await LoadAsync(resetItems: true);
    }

    public async Task OpenDocumentAsync(DocumentInfo? document)
    {
        if (document == null)
            return;

        var url = await _supabaseService.CreateDokumentSignedUrlAsync(document.StoragePath, 3600);
        if (string.IsNullOrWhiteSpace(url))
        {
            StatusMessage = "Dokument konnte nicht geöffnet werden.";
            return;
        }

        await Launcher.Default.OpenAsync(url);
    }

    public async Task<bool> SaveStromReadingAsync(DateTime ablesedatum, decimal stand, string? fotoPfad, ZaehlerAblesungDTO? existing = null)
    {
        if (SelectedItem == null)
            return false;

        var meterId = existing?.ZaehlerId;
        if (!meterId.HasValue)
        {
            var meter = await _supabaseService.GetActiveStromzaehlerAsync(SelectedItem.ParzelleId, ablesedatum);
            meterId = meter?.Id;
        }

        if (!meterId.HasValue)
        {
            StatusMessage = "Kein aktiver Stromzähler für dieses Datum gefunden.";
            return false;
        }

        var ok = existing == null
            ? await _supabaseService.AddAblesungAsync(1, meterId.Value, ablesedatum, stand, NormalizeOptionalText(fotoPfad))
            : await _supabaseService.UpdateAblesungAsync(existing.AblesungId, ablesedatum, stand, NormalizeOptionalText(fotoPfad));

        StatusMessage = ok ? "Strom-Ablesung gespeichert." : "Strom-Ablesung konnte nicht gespeichert werden.";
        if (!ok)
            return false;

        await ReloadSelectedDetailAsync();
        return true;
    }

    public async Task<bool> SaveWasserReadingAsync(DateTime ablesedatum, decimal stand, string? fotoPfad, ZaehlerAblesungDTO? existing = null)
    {
        if (SelectedItem == null)
            return false;

        var meterId = existing?.ZaehlerId;
        if (!meterId.HasValue)
        {
            var meter = await _supabaseService.GetActiveWasserzaehlerAsync(SelectedItem.ParzelleId, ablesedatum);
            meterId = meter?.Id;
        }

        if (!meterId.HasValue)
        {
            StatusMessage = "Kein aktiver Wasserzähler für dieses Datum gefunden.";
            return false;
        }

        var ok = existing == null
            ? await _supabaseService.AddAblesungAsync(2, meterId.Value, ablesedatum, stand, NormalizeOptionalText(fotoPfad))
            : await _supabaseService.UpdateAblesungAsync(existing.AblesungId, ablesedatum, stand, NormalizeOptionalText(fotoPfad));

        StatusMessage = ok ? "Wasser-Ablesung gespeichert." : "Wasser-Ablesung konnte nicht gespeichert werden.";
        if (!ok)
            return false;

        await ReloadSelectedDetailAsync();
        return true;
    }

    public async Task<bool> ReplaceStromMeterAsync(string zaehlernummer, DateTime eichdatum, DateTime eingebautAm)
    {
        if (SelectedItem == null)
            return false;

        var parzelleId = SelectedItem.ParzelleId;
        var current = await _supabaseService.GetActiveStromzaehlerAsync(parzelleId, eingebautAm);
        if (current != null)
        {
            var ended = await _supabaseService.SetStromzaehlerAusgebautAmAsync(current.Id, eingebautAm.Date);
            if (!ended)
            {
                StatusMessage = "Alter Stromzähler konnte nicht beendet werden.";
                return false;
            }
        }

        var ok = await _supabaseService.AddStromzaehlerAsync(parzelleId, zaehlernummer.Trim(), eichdatum, eingebautAm.Date);
        StatusMessage = ok ? "Stromzähler gespeichert." : "Stromzähler konnte nicht gespeichert werden.";
        if (!ok)
            return false;

        await ReloadSelectedDetailAsync();
        return true;
    }

    public async Task<bool> InstallWasserMeterAsync(string zaehlernummer, DateTime eichdatum, DateTime eingebautAm)
    {
        if (SelectedItem == null)
            return false;

        var ok = await _supabaseService.AddWasserzaehlerAsync(SelectedItem.ParzelleId, zaehlernummer.Trim(), eichdatum, eingebautAm.Date);
        StatusMessage = ok ? "Wasserzähler gespeichert." : "Wasserzähler konnte nicht gespeichert werden.";
        if (!ok)
            return false;

        await ReloadSelectedDetailAsync();
        return true;
    }

    public async Task<bool> RemoveWasserMeterAsync(DateTime ausgebautAm)
    {
        if (SelectedItem == null)
            return false;

        var meter = await _supabaseService.GetActiveWasserzaehlerAsync(SelectedItem.ParzelleId, ausgebautAm);
        if (meter == null)
        {
            StatusMessage = "Kein aktiver Wasserzähler für dieses Datum gefunden.";
            return false;
        }

        var ok = await _supabaseService.SetWasserzaehlerAusgebautAmAsync(meter.Id, ausgebautAm.Date);
        StatusMessage = ok ? "Wasserzähler ausgebaut." : "Wasserzähler konnte nicht ausgebaut werden.";
        if (!ok)
            return false;

        await ReloadSelectedDetailAsync();
        return true;
    }

    public async Task<bool> AssignAsync()
    {
        if (SelectedItem == null || SelectedAssignMember == null)
            return false;

        var parzelleId = SelectedItem.ParzelleId;
        var ok = await _supabaseService.AssignParzelleToMitgliedAsync(SelectedAssignMember.Id, parzelleId, AssignVonDatum);
        StatusMessage = ok
            ? "Parzelle erfolgreich zugeordnet."
            : "Parzelle konnte nicht zugeordnet werden. Möglicherweise ist sie zum gewählten Datum bereits belegt.";

        if (!ok)
            return false;

        await LoadAsync(resetItems: true);
        SelectedItem = Items.FirstOrDefault(x => x.ParzelleId == parzelleId);
        return true;
    }

    public async Task<bool> EndAssignmentAsync()
    {
        if (SelectedItem == null || SelectedDetail?.BelegungId is not > 0)
            return false;

        var parzelleId = SelectedItem.ParzelleId;
        var ok = await _supabaseService.EndParzellenBelegungAsync(SelectedDetail.BelegungId.Value, DateTime.Today);
        StatusMessage = ok ? "Aktive Belegung beendet." : "Aktive Belegung konnte nicht beendet werden.";

        if (!ok)
            return false;

        await LoadAsync(resetItems: true);
        SelectedItem = Items.FirstOrDefault(x => x.ParzelleId == parzelleId);
        return true;
    }

    private async Task LoadAsync(bool resetItems = false)
    {
        try
        {
            IsBusy = true;
            StatusMessage = string.Empty;

            if (resetItems)
            {
                Items.Clear();
                AssignableMembers.Clear();
                SelectedAssignMember = null;
                ClearDetailCollections();
            }

            var parzellen = await _supabaseService.GetAllParzellenAsync();
            var belegungen = await _supabaseService.GetAllParzellenBelegungenAsync();
            var mitglieder = await _supabaseService.GetMitgliederAsync();

            var mitgliederById = mitglieder.ToDictionary(x => x.Id, x => x);
            foreach (var member in mitglieder
                         .Where(x => x.Aktiv)
                         .OrderBy(x => FormatMemberName(x), StringComparer.CurrentCultureIgnoreCase))
            {
                AssignableMembers.Add(ToMemberDto(member));
            }

            var today = DateTime.Today;
            var currentByParzelle = belegungen
                .GroupBy(x => x.ParzelleId)
                .Select(g => g.Where(x => IsActiveOn(x, today))
                    .OrderByDescending(x => x.VonDatum ?? DateTime.MinValue)
                    .FirstOrDefault())
                .Where(x => x != null)
                .ToDictionary(x => x!.ParzelleId, x => x!);

            foreach (var parzelle in parzellen)
            {
                currentByParzelle.TryGetValue(parzelle.Id, out var belegung);
                mitgliederById.TryGetValue(belegung?.MitgliedId ?? 0, out var mitglied);

                Items.Add(new ParzelleVerwaltungItem
                {
                    ParzelleId = parzelle.Id,
                    GartenNr = parzelle.GartenNr,
                    Anlage = parzelle.Anlage,
                    MitgliedId = belegung?.MitgliedId,
                    MitgliedName = FormatMemberName(mitglied),
                    IstVergeben = belegung != null,
                    StatusText = belegung != null ? "vergeben" : "frei"
                });
            }

            StatusMessage = Items.Count == 0 ? "Keine Parzellen geladen." : string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Parzellen konnten nicht geladen werden: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadSelectedDetailAsync()
    {
        var selected = SelectedItem;
        if (selected == null)
        {
            SelectedDetail = null;
            ClearDetailCollections();
            return;
        }

        try
        {
            var detail = await _supabaseService.GetParzelleDetailAsync(selected.ParzelleId);
            if (SelectedItem?.ParzelleId != selected.ParzelleId)
                return;

            SelectedDetail = detail;
            await LoadDetailCollectionsAsync(selected.ParzelleId);
        }
        catch (Exception ex)
        {
            if (SelectedItem?.ParzelleId != selected.ParzelleId)
                return;

            SelectedDetail = null;
            ClearDetailCollections();
            StatusMessage = $"Parzellendetail konnte nicht geladen werden: {ex.Message}";
        }
    }

    private async Task ReloadSelectedDetailAsync()
    {
        if (SelectedItem == null)
            return;

        await LoadSelectedDetailAsync();
    }

    private async Task LoadDetailCollectionsAsync(int parzelleId)
    {
        var strom = await _supabaseService.GetStromAblesungenAsync(parzelleId);
        var wasser = await _supabaseService.GetWasserAblesungenAsync(parzelleId);
        var dokumente = await _supabaseService.GetParzelleDokumenteAsync(parzelleId);

        FillCollection(StromAblesungen, strom);
        FillCollection(WasserAblesungen, wasser);
        FillCollection(Dokumente, dokumente);

        OnPropertyChanged(nameof(HasStromAblesungen));
        OnPropertyChanged(nameof(HasWasserAblesungen));
        OnPropertyChanged(nameof(HasDokumente));
    }

    private void ClearDetailCollections()
    {
        StromAblesungen.Clear();
        WasserAblesungen.Clear();
        Dokumente.Clear();
        OnPropertyChanged(nameof(HasStromAblesungen));
        OnPropertyChanged(nameof(HasWasserAblesungen));
        OnPropertyChanged(nameof(HasDokumente));
    }

    private static void FillCollection<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source)
            target.Add(item);
    }

    private static bool IsActiveOn(ParzellenBelegungRecord belegung, DateTime date)
    {
        var onDate = date.Date;
        var von = (belegung.VonDatum ?? DateTime.MinValue).Date;
        var bis = belegung.BisDatum?.Date;
        return von <= onDate && (bis == null || bis.Value >= onDate);
    }

    private static string FormatMemberName(MitgliedRecord? member)
    {
        if (member == null)
            return string.Empty;

        var name = $"{member.Vorname} {member.Name}".Trim();
        return string.IsNullOrWhiteSpace(name) ? (member.Email ?? string.Empty) : name;
    }

    private static MemberDTO ToMemberDto(MitgliedRecord record)
    {
        return new MemberDTO
        {
            Id = record.Id,
            Vorname = record.Vorname ?? string.Empty,
            Nachname = record.Name ?? string.Empty,
            Email = record.Email ?? string.Empty,
            Role = record.Role ?? string.Empty,
            MitgliedEnde = record.MitgliedEnde
        };
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
