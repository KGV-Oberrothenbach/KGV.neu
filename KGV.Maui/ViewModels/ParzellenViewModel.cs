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
    private string _statusMessage = string.Empty;
    private bool _isBusy;

    public ParzellenViewModel(ISupabaseService supabaseService)
    {
        _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ParzelleVerwaltungItem> Items { get; } = new();

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
        }
    }

    public bool HasSelectedDetail => SelectedDetail != null;
    public bool ShowSelectionHint => !HasSelectedDetail;

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
        }
    }

    public async Task InitializeAsync()
    {
        if (Items.Count > 0)
            return;

        await LoadAsync();
    }

    public async Task RefreshAsync()
    {
        Items.Clear();
        SelectedItem = null;
        SelectedDetail = null;
        await LoadAsync();
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

    private async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = string.Empty;

            var parzellen = await _supabaseService.GetAllParzellenAsync();
            var belegungen = await _supabaseService.GetAllParzellenBelegungenAsync();
            var mitglieder = await _supabaseService.GetMitgliederAsync();

            var mitgliederById = mitglieder.ToDictionary(x => x.Id, x => x);
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
            return;
        }

        try
        {
            var detail = await _supabaseService.GetParzelleDetailAsync(selected.ParzelleId);
            if (SelectedItem?.ParzelleId != selected.ParzelleId)
                return;

            SelectedDetail = detail;
        }
        catch (Exception ex)
        {
            if (SelectedItem?.ParzelleId != selected.ParzelleId)
                return;

            SelectedDetail = null;
            StatusMessage = $"Parzellendetail konnte nicht geladen werden: {ex.Message}";
        }
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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
