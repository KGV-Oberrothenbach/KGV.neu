using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Maui.State;
using Microsoft.Maui.ApplicationModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace KGV.Maui.ViewModels;

public sealed class ParzellenViewModel : INotifyPropertyChanged
{
    private readonly ISupabaseService _supabaseService;
    private readonly ParzellenContextState _parzellenContextState;
    private readonly List<ParzelleVerwaltungItem> _allItems = new();
    private ParzelleVerwaltungItem? _selectedItem;
    private ParzelleDetailDTO? _selectedDetail;
    private MemberDTO? _selectedAssignMember;
    private DateTime _assignVonDatum = DateTime.Today;
    private string _searchText = string.Empty;
    private string _statusMessage = string.Empty;
    private bool _isBusy;
    private bool _isAssignMode;
    private bool _isEditMode;
    private string _editGartenNr = string.Empty;
    private string _editAnlage = string.Empty;
    private string _editFlaeche = string.Empty;
    private string _editRfidStrom = string.Empty;
    private string _editRfidWasser = string.Empty;
    private bool _editHatStrom;
    private bool _editHatWasser;

    public ParzellenViewModel(ISupabaseService supabaseService, ParzellenContextState parzellenContextState)
    {
        _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        _parzellenContextState = parzellenContextState ?? throw new ArgumentNullException(nameof(parzellenContextState));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ParzelleVerwaltungItem> Items { get; } = new();
    public ObservableCollection<ParzelleVerwaltungItem> FilteredItems { get; } = new();
    public ObservableCollection<MemberDTO> AssignableMembers { get; } = new();
    public ObservableCollection<ZaehlerAblesungDTO> StromAblesungen { get; } = new();
    public ObservableCollection<ZaehlerAblesungDTO> WasserAblesungen { get; } = new();
    public ObservableCollection<DocumentInfo> Dokumente { get; } = new();

    public string Title => _parzellenContextState.IsFromMemberContext
        ? (_parzellenContextState.ContextTitle ?? "Gartenkontext")
        : "Parzellen";
    public string Description => _parzellenContextState.IsFromMemberContext
        ? "Mitgliedsbezogener Garten-/Parzellenkontext mit fokussierten Parzellenstammdaten."
        : "Zentrale Parzellenübersicht mit fokussierten Parzellenstammdaten.";
    public string DetailHint => _parzellenContextState.IsFromMemberContext
        ? "Parzellenstammdaten bleiben hier fachlich getrennt von Ablesen, Wartungsverträgen und sonstigen Verwaltungsblöcken."
        : "Parzellenstammdaten bleiben hier fachlich getrennt von Ablesen und anderen Verwaltungsblöcken.";
    public bool IsContextBound => _parzellenContextState.IsFromMemberContext;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (string.Equals(_searchText, value, StringComparison.Ordinal))
                return;

            _searchText = value ?? string.Empty;
            OnPropertyChanged();
            ApplyFilter();
        }
    }
    public bool HasFilteredItems => FilteredItems.Count > 0;
    public bool ShowFilteredEmptyState => !IsBusy && Items.Count > 0 && FilteredItems.Count == 0;

    public ParzelleVerwaltungItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (_selectedItem == value)
                return;

            _selectedItem = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanSelectPrevious));
            OnPropertyChanged(nameof(CanSelectNext));
            OnPropertyChanged(nameof(NavigationText));
            OnPropertyChanged(nameof(SelectedParzelleDisplayName));
            OnPropertyChanged(nameof(ShowSelectionHint));
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
            OnPropertyChanged(nameof(CanStartAssign));
            OnPropertyChanged(nameof(CanEndAssignment));
            OnPropertyChanged(nameof(HasAssignedMember));
            OnPropertyChanged(nameof(CanOpenAssignedMember));
            OnPropertyChanged(nameof(SelectedParzelleDisplayName));

            if (!IsEditMode)
                SyncEditFieldsFromDetail(value);
        }
    }

    public bool HasSelectedDetail => SelectedDetail != null;
    public bool ShowReadOnlyStammdaten => HasSelectedDetail && !IsEditMode;
    public bool ShowSelectionHint => !HasSelectedDetail && HasFilteredItems;
    public bool CanEditStammdaten => HasSelectedDetail && !IsBusy && !IsEditMode;
    public bool CanManageAssignment => HasSelectedDetail && !IsBusy;
    public bool CanAssign => CanManageAssignment && IsAssignMode && SelectedAssignMember != null;
    public bool CanStartAssign => CanManageAssignment && SelectedDetail?.IstVergeben == false && !IsAssignMode;
    public bool CanEndAssignment => CanManageAssignment && SelectedDetail?.BelegungId is > 0 && SelectedDetail.BisDatum == null;
    public bool HasAssignedMember => SelectedDetail?.MitgliedId is > 0 && SelectedDetail.IstVergeben;
    public bool CanOpenAssignedMember => HasAssignedMember && !IsBusy;
    public bool IsAssignMode
    {
        get => _isAssignMode;
        private set
        {
            if (_isAssignMode == value)
                return;

            _isAssignMode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanAssign));
            OnPropertyChanged(nameof(CanStartAssign));
        }
    }

    public bool IsEditMode
    {
        get => _isEditMode;
        private set
        {
            if (_isEditMode == value)
                return;

            _isEditMode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowReadOnlyStammdaten));
            OnPropertyChanged(nameof(CanEditStammdaten));
            OnPropertyChanged(nameof(CanSaveStammdaten));
        }
    }

    public bool CanSaveStammdaten => HasSelectedDetail && IsEditMode && !IsBusy;
    public bool CanSelectPrevious => SelectedItem != null && Items.IndexOf(SelectedItem) > 0;
    public bool CanSelectNext => SelectedItem != null && Items.IndexOf(SelectedItem) >= 0 && Items.IndexOf(SelectedItem) < Items.Count - 1;
    public string NavigationText => SelectedItem == null || Items.Count == 0
        ? "Keine Parzelle ausgewählt"
        : $"{Items.IndexOf(SelectedItem) + 1} / {Items.Count}";
    public string SelectedParzelleDisplayName => SelectedDetail?.DisplayName ?? SelectedItem?.DisplayText ?? "Parzelle";
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

    public string EditGartenNr
    {
        get => _editGartenNr;
        set
        {
            if (_editGartenNr == value)
                return;

            _editGartenNr = value;
            OnPropertyChanged();
        }
    }

    public string EditAnlage
    {
        get => _editAnlage;
        set
        {
            if (_editAnlage == value)
                return;

            _editAnlage = value;
            OnPropertyChanged();
        }
    }

    public string EditFlaeche
    {
        get => _editFlaeche;
        set
        {
            if (_editFlaeche == value)
                return;

            _editFlaeche = value;
            OnPropertyChanged();
        }
    }

    public string EditRfidStrom
    {
        get => _editRfidStrom;
        set
        {
            if (_editRfidStrom == value)
                return;

            _editRfidStrom = value;
            OnPropertyChanged();
        }
    }

    public string EditRfidWasser
    {
        get => _editRfidWasser;
        set
        {
            if (_editRfidWasser == value)
                return;

            _editRfidWasser = value;
            OnPropertyChanged();
        }
    }

    public bool EditHatStrom
    {
        get => _editHatStrom;
        set
        {
            if (_editHatStrom == value)
                return;

            _editHatStrom = value;
            OnPropertyChanged();
        }
    }

    public bool EditHatWasser
    {
        get => _editHatWasser;
        set
        {
            if (_editHatWasser == value)
                return;

            _editHatWasser = value;
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
            OnPropertyChanged(nameof(CanEditStammdaten));
            OnPropertyChanged(nameof(CanManageAssignment));
            OnPropertyChanged(nameof(CanAssign));
            OnPropertyChanged(nameof(CanStartAssign));
            OnPropertyChanged(nameof(CanEndAssignment));
            OnPropertyChanged(nameof(CanOpenAssignedMember));
            OnPropertyChanged(nameof(CanSaveStammdaten));
        }
    }

    public async Task InitializeAsync()
    {
        if (Items.Count > 0)
        {
            await ApplyRequestedContextAsync();
            return;
        }

        await LoadAsync(resetItems: true);
        await ApplyRequestedContextAsync();
    }

    public async Task RefreshAsync()
    {
        Items.Clear();
        FilteredItems.Clear();
        _allItems.Clear();
        SelectedItem = null;
        SelectedDetail = null;
        await LoadAsync(resetItems: true);
        await ApplyRequestedContextAsync();
    }

    public async Task ApplyRequestedContextAsync()
    {
        if (_parzellenContextState.SelectedParzelleId is not > 0)
            return;

        if (Items.Count == 0)
            await LoadAsync(resetItems: true);

        var requestedId = _parzellenContextState.SelectedParzelleId.Value;
        var target = Items.FirstOrDefault(x => x.ParzelleId == requestedId);
        if (target != null)
            SelectedItem = target;

        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(DetailHint));
        OnPropertyChanged(nameof(IsContextBound));
    }

    public async Task ClearRequestedContextAsync()
    {
        _parzellenContextState.Clear();
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(DetailHint));
        OnPropertyChanged(nameof(IsContextBound));
        await RefreshAsync();
    }

    public async Task RefreshSelectedDetailAsync()
    {
        if (SelectedItem == null)
            return;

        await LoadSelectedDetailAsync();
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
            ? await _supabaseService.AddAblesungAsync(new AblesungInsertRecord
            {
                ZaehlerTyp = 1,
                ZaehlerId = meterId.Value,
                Ablesedatum = ablesedatum,
                Stand = stand,
                FotoPfad = NormalizeOptionalText(fotoPfad)
            })
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
            ? await _supabaseService.AddAblesungAsync(new AblesungInsertRecord
            {
                ZaehlerTyp = 2,
                ZaehlerId = meterId.Value,
                Ablesedatum = ablesedatum,
                Stand = stand,
                FotoPfad = NormalizeOptionalText(fotoPfad)
            })
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

        var ok = await _supabaseService.AddStromzaehlerAsync(new StromzaehlerInsertRecord
        {
            ParzelleId = parzelleId,
            Zaehlernummer = zaehlernummer.Trim(),
            Eichdatum = eichdatum,
            EingebautAm = eingebautAm.Date
        });
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

        var ok = await _supabaseService.AddWasserzaehlerAsync(new WasserzaehlerInsertRecord
        {
            ParzelleId = SelectedItem.ParzelleId,
            Zaehlernummer = zaehlernummer.Trim(),
            Eichdatum = eichdatum,
            EingebautAm = eingebautAm.Date
        });
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

        IsAssignMode = false;
        await LoadAsync(resetItems: true);
        SelectedItem = Items.FirstOrDefault(x => x.ParzelleId == parzelleId);
        return true;
    }

    public void BeginAssignMode()
    {
        if (!CanStartAssign)
            return;

        IsEditMode = false;
        SelectedAssignMember = null;
        AssignVonDatum = DateTime.Today;
        IsAssignMode = true;
    }

    public void CancelAssignMode()
    {
        SelectedAssignMember = null;
        AssignVonDatum = DateTime.Today;
        IsAssignMode = false;
    }

    public void BeginEditMode()
    {
        if (SelectedDetail == null)
            return;

        IsAssignMode = false;
        SyncEditFieldsFromDetail(SelectedDetail);
        IsEditMode = true;
    }

    public void CancelEditMode()
    {
        SyncEditFieldsFromDetail(SelectedDetail);
        IsEditMode = false;
    }

    public bool HasFlaecheChanged()
    {
        return NormalizeFlaecheValue(SelectedDetail?.FlaecheQm) != NormalizeFlaecheValue(ParseEditableFlaeche());
    }

    public async Task<bool> SaveStammdatenAsync()
    {
        if (SelectedDetail == null || SelectedItem == null)
            return false;

        var parzelleId = SelectedDetail.ParzelleId;
        var flaeche = ParseEditableFlaeche();
        if (!string.IsNullOrWhiteSpace(EditFlaeche) && !flaeche.HasValue)
        {
            StatusMessage = "Die Fläche konnte nicht gelesen werden.";
            return false;
        }

        var record = new ParzelleRecord
        {
            Id = parzelleId,
            GartenNr = SelectedDetail.GartenNr?.Trim() ?? string.Empty,
            Anlage = SelectedDetail.Anlage?.Trim() ?? string.Empty,
            FlaecheQm = flaeche,
            HatStrom = EditHatStrom,
            HatWasser = EditHatWasser,
            RfidStrom = NormalizeOptionalText(SelectedDetail.RfidStrom),
            RfidWasser = NormalizeOptionalText(SelectedDetail.RfidWasser)
        };

        if (string.IsNullOrWhiteSpace(record.GartenNr))
        {
            StatusMessage = "Bitte eine Gartennummer angeben.";
            return false;
        }

        var ok = await _supabaseService.UpdateParzelleStammdatenAsync(record);
        StatusMessage = ok ? "Parzellen-Stammdaten gespeichert." : "Parzellen-Stammdaten konnten nicht gespeichert werden.";
        if (!ok)
            return false;

        IsEditMode = false;
        await LoadAsync(resetItems: true);
        SelectedItem = Items.FirstOrDefault(x => x.ParzelleId == parzelleId);
        return true;
    }

    public async Task SelectPreviousAsync()
    {
        if (!CanSelectPrevious || SelectedItem == null)
            return;

        SelectedItem = Items[Items.IndexOf(SelectedItem) - 1];
        await Task.CompletedTask;
    }

    public async Task SelectNextAsync()
    {
        if (!CanSelectNext || SelectedItem == null)
            return;

        SelectedItem = Items[Items.IndexOf(SelectedItem) + 1];
        await Task.CompletedTask;
    }

    public async Task<MemberDTO?> LoadAssignedMemberAsync()
    {
        if (SelectedDetail?.MitgliedId is not > 0)
            return null;

        var member = await _supabaseService.GetMitgliedByIdAsync(SelectedDetail.MitgliedId.Value);
        return member == null ? null : ToMemberDto(member);
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

        IsAssignMode = false;
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
                FilteredItems.Clear();
                _allItems.Clear();
                AssignableMembers.Clear();
                SelectedAssignMember = null;
                ClearDetailCollections();
                IsAssignMode = false;
                IsEditMode = false;
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

            foreach (var parzelle in parzellen.OrderBy(x => x.GartenNrSortKey, StringComparer.CurrentCultureIgnoreCase))
            {
                currentByParzelle.TryGetValue(parzelle.Id, out var belegung);
                mitgliederById.TryGetValue(belegung?.MitgliedId ?? 0, out var mitglied);

                var item = new ParzelleVerwaltungItem
                {
                    ParzelleId = parzelle.Id,
                    GartenNr = parzelle.GartenNr,
                    GartenNrSortKey = parzelle.GartenNrSortKey,
                    Anlage = parzelle.Anlage,
                    MitgliedId = belegung?.MitgliedId,
                    MitgliedName = FormatMemberName(mitglied),
                    PaechterDisplayText = FormatPaechterListName(mitglied),
                    IstVergeben = belegung != null,
                    StatusText = belegung != null ? "vergeben" : "frei"
                };

                _allItems.Add(item);
                Items.Add(item);
            }

            ApplyFilter();

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

    private static string FormatPaechterListName(MitgliedRecord? member)
    {
        if (member == null)
            return "Nicht verpachtet";

        var nachname = member.Name?.Trim() ?? string.Empty;
        var vorname = member.Vorname?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(nachname) && !string.IsNullOrWhiteSpace(vorname))
            return $"{nachname}, {vorname}";

        var combined = string.Join(", ", new[] { nachname, vorname }.Where(x => !string.IsNullOrWhiteSpace(x)));
        return string.IsNullOrWhiteSpace(combined) ? (member.Email ?? "Nicht verpachtet") : combined;
    }

    private void ApplyFilter()
    {
        var search = SearchText.Trim();
        var filtered = string.IsNullOrWhiteSpace(search)
            ? _allItems
            : _allItems
                .Where(x => x.SearchText.Contains(search, StringComparison.CurrentCultureIgnoreCase))
                .ToList();

        FillCollection(FilteredItems, filtered);

        if (SelectedItem != null && !FilteredItems.Contains(SelectedItem))
        {
            SelectedItem = null;
        }

        OnPropertyChanged(nameof(HasFilteredItems));
        OnPropertyChanged(nameof(ShowFilteredEmptyState));
        OnPropertyChanged(nameof(ShowSelectionHint));
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

    private void SyncEditFieldsFromDetail(ParzelleDetailDTO? detail)
    {
        EditGartenNr = detail?.GartenNr ?? string.Empty;
        EditAnlage = detail?.Anlage ?? string.Empty;
        EditFlaeche = detail?.FlaecheQm?.ToString("0.##", CultureInfo.CurrentCulture) ?? string.Empty;
        EditHatStrom = detail?.HatStrom == true;
        EditHatWasser = detail?.HatWasser == true;
        EditRfidStrom = detail?.RfidStrom ?? string.Empty;
        EditRfidWasser = detail?.RfidWasser ?? string.Empty;
    }

    private decimal? ParseEditableFlaeche()
    {
        if (string.IsNullOrWhiteSpace(EditFlaeche))
            return null;

        if (decimal.TryParse(EditFlaeche, NumberStyles.Number, CultureInfo.CurrentCulture, out var currentCultureValue))
            return currentCultureValue;

        if (decimal.TryParse(EditFlaeche, NumberStyles.Number, CultureInfo.InvariantCulture, out var invariantValue))
            return invariantValue;

        return null;
    }

    private static decimal? NormalizeFlaecheValue(decimal? value)
    {
        return value.HasValue ? decimal.Round(value.Value, 2) : null;
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
