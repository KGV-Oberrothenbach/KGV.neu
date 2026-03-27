using KGV.Core.Interfaces;
using KGV.Core.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

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
    private RfidScanContextResult? _scanResolution;

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
    public bool HasResolvedScan => ScanResolution != null;
    public bool ShowAssignmentStep => ScanResolution?.State == RfidScanContextState.Unknown;
    public bool ShowKnownTagResult => ScanResolution?.IsKnown == true;
    public string ScannedUidDisplay => string.IsNullOrWhiteSpace(ScanResolution?.NormalizedUid) ? "—" : ScanResolution!.NormalizedUid;
    public string ExistingTagSummary => ScanResolution?.Context == null
        ? "Der RFID-Tag ist bereits im System vorhanden. Ein normaler Speichern-Flow ist hier nicht möglich."
        : $"Der RFID-Tag ist bereits bei {ScanResolution.Context.ParzelleDisplayName} für {ScanResolution.Context.MediumDisplay} vorhanden. Ein normaler Speichern-Flow ist hier nicht möglich.";
    public bool CanCheck => ShowAssignmentStep && !IsBusy && SelectedParzelle != null && SelectedMedium != null && !string.IsNullOrWhiteSpace(UidInput);
    public bool CanSave => ShowAssignmentStep && !IsBusy && _lastCheck?.IsValid == true;
    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    public RfidScanContextResult? ScanResolution
    {
        get => _scanResolution;
        private set
        {
            if (_scanResolution == value)
                return;

            _scanResolution = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasResolvedScan));
            OnPropertyChanged(nameof(ShowAssignmentStep));
            OnPropertyChanged(nameof(ShowKnownTagResult));
            OnPropertyChanged(nameof(ScannedUidDisplay));
            OnPropertyChanged(nameof(ExistingTagSummary));
            OnPropertyChanged(nameof(CanCheck));
            OnPropertyChanged(nameof(CanSave));
        }
    }

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
            _ = RefreshMediumOptionsAsync();
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
            ScanResolution = null;
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
        if (!ShowAssignmentStep)
            return CreateClientError("Bitte zuerst einen neuen RFID-Tag scannen.");

        if (SelectedParzelle == null || SelectedMedium == null)
            return CreateClientError("Bitte zuerst Parzelle und Medium wählen.");

        IsBusy = true;
        try
        {
            StatusMessage = "RFID wird geprüft.";
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
        if (!ShowAssignmentStep)
            return CreateClientErrorResult("Bitte zuerst einen neuen RFID-Tag scannen.");

        if (SelectedParzelle == null || SelectedMedium == null)
            return CreateClientErrorResult("Bitte zuerst Parzelle und Medium wählen.");

        IsBusy = true;
        try
        {
            var latestCheck = await _supabaseService.CheckParzelleRfidAssignmentAsync(SelectedParzelle.Id, SelectedMedium.Key, UidInput);
            if (!latestCheck.IsValid)
            {
                _lastCheck = null;
                StatusMessage = latestCheck.Message;
                OnPropertyChanged(nameof(CanSave));
                return new RfidAssignmentResult { Success = false, Message = latestCheck.Message, NormalizedUid = latestCheck.NormalizedUid };
            }

            StatusMessage = "RFID wird gespeichert.";
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
            ResetForNewScan(clearStatus: false);
            ResetCheckState(clearStatus: false);
            StatusMessage = result.Message;
            return result;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<RfidScanContextResult> ResolveUidAsync()
    {
        if (string.IsNullOrWhiteSpace(UidInput))
            return CreateClientScanError("Bitte zuerst einen RFID-Tag scannen.");

        IsBusy = true;
        try
        {
            StatusMessage = "RFID wird geprüft.";
            var result = await _supabaseService.ResolveRfidScanContextAsync(UidInput);
            ScanResolution = result;
            _lastCheck = null;
            OnPropertyChanged(nameof(CanSave));

            StatusMessage = result.IsKnown
                ? ExistingTagSummary
                : "RFID ist noch nicht zugeordnet. Bitte jetzt Parzelle und Medium wählen.";

            return result;
        }
        catch (Exception ex)
        {
            ScanResolution = null;
            _lastCheck = null;
            var message = $"RFID konnte nicht geprüft werden: {ex.Message}";
            StatusMessage = message;
            return new RfidScanContextResult
            {
                NormalizedUid = (UidInput ?? string.Empty).Trim().ToUpperInvariant(),
                State = RfidScanContextState.Unknown,
                Message = message
            };
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void ResetForNewScan(bool clearStatus = true)
    {
        _uidInput = string.Empty;
        _lastCheck = null;
        ScanResolution = null;
        OnPropertyChanged(nameof(UidInput));
        OnPropertyChanged(nameof(CanCheck));
        OnPropertyChanged(nameof(CanSave));

        if (clearStatus)
            StatusMessage = string.Empty;
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

    private async Task RefreshMediumOptionsAsync()
    {
        MediumOptions.Clear();

        var selectedParzelle = SelectedParzelle;
        if (selectedParzelle == null)
        {
            SelectedMedium = null;
            OnPropertyChanged(nameof(CanCheck));
            OnPropertyChanged(nameof(CanSave));
            return;
        }

        var previousMediumKey = SelectedMedium?.Key;
        var options = await _supabaseService.GetAvailableRfidMediumOptionsForParzelleAsync(selectedParzelle.Id);
        if (SelectedParzelle?.Id != selectedParzelle.Id)
            return;

        foreach (var option in options)
            MediumOptions.Add(option);

        SelectedMedium = previousMediumKey == null
            ? MediumOptions.Count == 1 ? MediumOptions[0] : null
            : MediumOptions.FirstOrDefault(x => x.Key == previousMediumKey);

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

    private RfidScanContextResult CreateClientScanError(string message)
    {
        StatusMessage = message;
        ScanResolution = null;
        return new RfidScanContextResult { State = RfidScanContextState.Unknown, Message = message };
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
