using KGV.Core.Interfaces;
using KGV.Core.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KGV.Maui.ViewModels;

public sealed class RfidEinrichtenViewModel : INotifyPropertyChanged
{
    private readonly ISupabaseService _supabaseService;
    private readonly IAuthService _authService;
    private ParzelleRecord? _selectedParzelle;
    private RfidMediumOption? _selectedMedium;
    private string _uidInput = string.Empty;
    private string _statusMessage = string.Empty;
    private bool _isBusy;
    private RfidAssignmentCheckResult? _lastCheck;

    public RfidEinrichtenViewModel(ISupabaseService supabaseService, IAuthService authService)
    {
        _supabaseService = supabaseService;
        _authService = authService;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ParzelleRecord> Parzellen { get; } = new();
    public ObservableCollection<RfidMediumOption> MediumOptions { get; } = new();

    public bool IsAuthorized => _authService.IsAdmin || _authService.IsVorstand;
    public bool HasSelectedParzelle => SelectedParzelle != null;
    public string CurrentStromRfid => SelectedParzelle?.StromRfidDisplay ?? "Nicht hinterlegt";
    public string CurrentWasserRfid => SelectedParzelle?.WasserRfidDisplay ?? "Nicht hinterlegt";
    public bool CanCheck => !IsBusy && SelectedParzelle != null && SelectedMedium != null && !string.IsNullOrWhiteSpace(UidInput);
    public bool CanSave => !IsBusy && _lastCheck?.IsValid == true;
    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    public ParzelleRecord? SelectedParzelle
    {
        get => _selectedParzelle;
        set
        {
            if (_selectedParzelle == value)
                return;

            _selectedParzelle = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelectedParzelle));
            OnPropertyChanged(nameof(CurrentStromRfid));
            OnPropertyChanged(nameof(CurrentWasserRfid));
            RefreshMediumOptions();
            ResetCheckState();
        }
    }

    public RfidMediumOption? SelectedMedium
    {
        get => _selectedMedium;
        set
        {
            if (_selectedMedium == value)
                return;

            _selectedMedium = value;
            OnPropertyChanged();
            ResetCheckState();
        }
    }

    public string UidInput
    {
        get => _uidInput;
        set
        {
            if (_uidInput == value)
                return;

            _uidInput = value;
            OnPropertyChanged();
            ResetCheckState();
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
            OnPropertyChanged(nameof(CanCheck));
            OnPropertyChanged(nameof(CanSave));
        }
    }

    public async Task InitializeAsync()
    {
        if (!IsAuthorized)
        {
            StatusMessage = "Dieser Bereich ist nur für Admin oder Vorstand verfügbar.";
            return;
        }

        await LoadParzellenAsync();
    }

    public async Task<RfidAssignmentCheckResult> CheckAsync()
    {
        if (SelectedParzelle == null || SelectedMedium == null)
            return CreateClientError("Bitte zuerst Parzelle und Medium wählen.");

        IsBusy = true;
        try
        {
            var result = await _supabaseService.CheckParzelleRfidAssignmentAsync(SelectedParzelle.Id, SelectedMedium.Key, UidInput);
            _lastCheck = result.IsValid ? result : null;
            StatusMessage = result.Message;
            OnPropertyChanged(nameof(CanSave));
            return result;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<RfidAssignmentResult> SaveAsync(bool overwriteExisting)
    {
        if (SelectedParzelle == null || SelectedMedium == null)
            return CreateClientErrorResult("Bitte zuerst Parzelle und Medium wählen.");

        IsBusy = true;
        try
        {
            var result = await _supabaseService.AssignParzelleRfidAsync(SelectedParzelle.Id, SelectedMedium.Key, UidInput, overwriteExisting);
            StatusMessage = result.Message;

            if (!result.Success)
            {
                _lastCheck = null;
                OnPropertyChanged(nameof(CanSave));
                return result;
            }

            var currentParzelleId = SelectedParzelle.Id;
            await LoadParzellenAsync(currentParzelleId);
            UidInput = string.Empty;
            ResetCheckState(clearStatus: false);
            StatusMessage = result.Message;
            return result;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadParzellenAsync(int? preferredParzelleId = null)
    {
        IsBusy = true;
        try
        {
            var parzellen = await _supabaseService.GetAllParzellenAsync();
            var ordered = parzellen
                .Where(x => x.Aktiv)
                .OrderBy(x => x.GartenNrSortKey)
                .ThenBy(x => x.GartenNr, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            Parzellen.Clear();
            foreach (var item in ordered)
                Parzellen.Add(item);

            SelectedParzelle = preferredParzelleId.HasValue
                ? Parzellen.FirstOrDefault(x => x.Id == preferredParzelleId.Value)
                : null;

            if (Parzellen.Count == 0)
                StatusMessage = "Keine aktiven Parzellen gefunden.";
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

    private void RefreshMediumOptions()
    {
        MediumOptions.Clear();

        if (SelectedParzelle?.HatStrom == true)
            MediumOptions.Add(new RfidMediumOption("strom", "Strom"));

        if (SelectedParzelle?.HatWasser == true)
            MediumOptions.Add(new RfidMediumOption("wasser", "Wasser"));

        if (SelectedMedium != null && MediumOptions.All(x => x.Key != SelectedMedium.Key))
            SelectedMedium = null;

        OnPropertyChanged(nameof(CanCheck));
        OnPropertyChanged(nameof(CanSave));
    }

    private void ResetCheckState(bool clearStatus = true)
    {
        _lastCheck = null;
        OnPropertyChanged(nameof(CanCheck));
        OnPropertyChanged(nameof(CanSave));
        if (clearStatus)
            StatusMessage = string.Empty;
    }

    private RfidAssignmentCheckResult CreateClientError(string message)
    {
        StatusMessage = message;
        return new RfidAssignmentCheckResult { IsValid = false, Message = message };
    }

    private RfidAssignmentResult CreateClientErrorResult(string message)
    {
        StatusMessage = message;
        return new RfidAssignmentResult { Success = false, Message = message };
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
