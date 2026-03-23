using KGV.Core.Interfaces;
using KGV.Core.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KGV.Maui.ViewModels;

public sealed class FaelligeZaehlerViewModel : INotifyPropertyChanged
{
    private readonly ISupabaseService _supabaseService;
    private readonly IAuthService _authService;
    private readonly List<ZaehlerEichstatusRecord> _allItems = new();
    private string _filterText = string.Empty;
    private string _selectedStatusFilter;
    private string _statusMessage = string.Empty;
    private bool _isBusy;

    public FaelligeZaehlerViewModel(ISupabaseService supabaseService, IAuthService authService)
    {
        _supabaseService = supabaseService;
        _authService = authService;
        StatusFilters.Add("Alle Status");
        StatusFilters.Add("Überfällig");
        StatusFilters.Add("Bald fällig");
        StatusFilters.Add("OK");
        _selectedStatusFilter = StatusFilters[0];
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ZaehlerEichstatusRecord> Items { get; } = new();
    public ObservableCollection<string> StatusFilters { get; } = new();

    public bool IsAuthorized => _authService.IsAdmin || _authService.IsVorstand;
    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);
    public bool HasItems => Items.Count > 0;
    public bool HasEmptyState => !IsBusy && Items.Count == 0;
    public string EmptyStateMessage => _allItems.Count == 0
        ? "Aktuell wurden keine Zählerdaten aus v_zaehler_eichstatus geladen."
        : "Keine Zähler passen auf den aktuellen Filter.";

    public string FilterText
    {
        get => _filterText;
        set
        {
            if (_filterText == value)
                return;

            _filterText = value;
            OnPropertyChanged();
            ApplyFilter();
        }
    }

    public string SelectedStatusFilter
    {
        get => _selectedStatusFilter;
        set
        {
            if (_selectedStatusFilter == value)
                return;

            _selectedStatusFilter = value;
            OnPropertyChanged();
            ApplyFilter();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (_statusMessage == value)
                return;

            _statusMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasStatusMessage));
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy == value)
                return;

            _isBusy = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasEmptyState));
        }
    }

    public async Task InitializeAsync()
    {
        if (!IsAuthorized)
        {
            StatusMessage = "Dieser Bereich ist nur für Admin oder Vorstand verfügbar.";
            return;
        }

        await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        IsBusy = true;
        try
        {
            StatusMessage = string.Empty;
            var items = await _supabaseService.GetZaehlerEichstatusAsync();
            _allItems.Clear();
            _allItems.AddRange(items);
            ApplyFilter();

            StatusMessage = _allItems.Count == 0
                ? "Keine Zählerdaten gefunden."
                : $"{_allItems.Count} Zähler geladen.";
        }
        catch (Exception ex)
        {
            _allItems.Clear();
            Items.Clear();
            OnPropertyChanged(nameof(HasItems));
            OnPropertyChanged(nameof(HasEmptyState));
            OnPropertyChanged(nameof(EmptyStateMessage));
            StatusMessage = $"Zählerdaten konnten nicht geladen werden: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyFilter()
    {
        var filtered = _allItems
            .Where(MatchesStatusFilter)
            .Where(MatchesTextFilter)
            .ToList();

        Items.Clear();
        foreach (var item in filtered)
            Items.Add(item);

        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(HasEmptyState));
        OnPropertyChanged(nameof(EmptyStateMessage));
    }

    private bool MatchesStatusFilter(ZaehlerEichstatusRecord item)
    {
        return SelectedStatusFilter switch
        {
            "Überfällig" => item.EichstatusDisplay == "Überfällig",
            "Bald fällig" => item.EichstatusDisplay == "Bald fällig",
            "OK" => item.EichstatusDisplay == "OK",
            _ => true
        };
    }

    private bool MatchesTextFilter(ZaehlerEichstatusRecord item)
    {
        if (string.IsNullOrWhiteSpace(FilterText))
            return true;

        return item.SearchText.Contains(FilterText.Trim(), StringComparison.CurrentCultureIgnoreCase);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
