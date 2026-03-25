using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Maui.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KGV.Maui.ViewModels;

public sealed class RfidScanContextViewModel : INotifyPropertyChanged
{
    private readonly ISupabaseService _supabaseService;
    private readonly IAuthService _authService;
    private readonly INfcScanService _nfcScanService;
    private string _uidInput = string.Empty;
    private string _statusMessage = string.Empty;
    private bool _isBusy;
    private RfidScanContextResult? _resolution;
    private NfcAvailabilityInfo _nfcAvailability = new(NfcAvailabilityState.Unavailable, "NFC-Verfügbarkeit wird geprüft.");
    private ParzelleRecord? _selectedFallbackParzelle;
    private RfidMediumOption? _selectedFallbackMedium;
    private string _lastScannedUid = string.Empty;
    private DateTime _lastScannedAt = DateTime.MinValue;

    public RfidScanContextViewModel(ISupabaseService supabaseService, IAuthService authService, INfcScanService nfcScanService)
    {
        _supabaseService = supabaseService;
        _authService = authService;
        _nfcScanService = nfcScanService;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ParzelleRecord> FallbackParzellen { get; } = new();
    public ObservableCollection<RfidMediumOption> FallbackMediumOptions { get; } = new();

    public bool IsAuthorized => _authService.IsAdmin || _authService.IsVorstand;
    public string UidInput
    {
        get => _uidInput;
        set
        {
            if (_uidInput == value)
                return;

            _uidInput = value;
            Resolution = null;
            _lastScannedUid = string.Empty;
            _lastScannedAt = DateTime.MinValue;
            StatusMessage = string.Empty;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanResolve));
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
            OnPropertyChanged(nameof(CanResolve));
            OnPropertyChanged(nameof(CanStartNfcScan));
            OnPropertyChanged(nameof(CanApplyFallbackContext));
        }
    }

    public bool CanResolve => !IsBusy && !string.IsNullOrWhiteSpace(UidInput);
    public bool CanStartNfcScan => !IsBusy && IsAuthorized && NfcAvailability.State == NfcAvailabilityState.Available;
    public bool CanOpenNfcSettings => NfcAvailability.State == NfcAvailabilityState.Disabled;
    public bool CanApplyFallbackContext => !IsBusy && SelectedFallbackParzelle != null && SelectedFallbackMedium != null;

    public RfidScanContextResult? Resolution
    {
        get => _resolution;
        private set
        {
            if (_resolution == value)
                return;

            _resolution = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasResolution));
            OnPropertyChanged(nameof(StateDisplay));
            OnPropertyChanged(nameof(NormalizedUid));
            OnPropertyChanged(nameof(ParzelleDisplayName));
            OnPropertyChanged(nameof(MediumDisplay));
            OnPropertyChanged(nameof(RfidDisplay));
            OnPropertyChanged(nameof(ActiveMeterDisplay));
            OnPropertyChanged(nameof(ZaehlernummerDisplay));
            OnPropertyChanged(nameof(StatusDisplay));
            OnPropertyChanged(nameof(EichdatumDisplay));
            OnPropertyChanged(nameof(EichfaelligDisplay));
        }
    }

    public NfcAvailabilityInfo NfcAvailability
    {
        get => _nfcAvailability;
        private set
        {
            if (_nfcAvailability.State == value.State && _nfcAvailability.Message == value.Message)
                return;

            _nfcAvailability = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(NfcStatusTitle));
            OnPropertyChanged(nameof(NfcStatusMessage));
            OnPropertyChanged(nameof(CanStartNfcScan));
            OnPropertyChanged(nameof(CanOpenNfcSettings));
        }
    }

    public ParzelleRecord? SelectedFallbackParzelle
    {
        get => _selectedFallbackParzelle;
        set
        {
            if (_selectedFallbackParzelle == value)
                return;

            _selectedFallbackParzelle = value;
            OnPropertyChanged();
            RefreshFallbackMediumOptions();
            Resolution = null;
            StatusMessage = string.Empty;
            OnPropertyChanged(nameof(CanApplyFallbackContext));
        }
    }

    public RfidMediumOption? SelectedFallbackMedium
    {
        get => _selectedFallbackMedium;
        set
        {
            if (_selectedFallbackMedium == value)
                return;

            _selectedFallbackMedium = value;
            Resolution = null;
            StatusMessage = string.Empty;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanApplyFallbackContext));
        }
    }

    public bool HasResolution => Resolution != null;
    public string NfcStatusTitle => NfcAvailability.State switch
    {
        NfcAvailabilityState.Available => "RFID-Tag scannen",
        NfcAvailabilityState.Disabled => "NFC aktivieren",
        NfcAvailabilityState.NotSupported => "Kein NFC am Gerät",
        _ => "NFC-Status"
    };
    public string NfcStatusMessage => NfcAvailability.Message;
    public string StateDisplay => Resolution?.StateDisplay ?? "Noch kein RFID-Kontext geladen.";
    public string NormalizedUid => string.IsNullOrWhiteSpace(Resolution?.NormalizedUid) ? "—" : Resolution!.NormalizedUid;
    public string ParzelleDisplayName => Resolution?.Context?.ParzelleDisplayName ?? "—";
    public string MediumDisplay => Resolution?.Context?.MediumDisplay ?? "—";
    public string RfidDisplay => Resolution?.Context?.RfidDisplay ?? (string.IsNullOrWhiteSpace(Resolution?.NormalizedUid) ? "—" : Resolution!.NormalizedUid);
    public string ActiveMeterDisplay => Resolution?.Context?.ActiveMeterDisplay ?? "Nein";
    public string ZaehlernummerDisplay => Resolution?.Context?.ZaehlernummerDisplay ?? "—";
    public string StatusDisplay => Resolution?.Context?.StatusDisplay ?? "Kein Kontext";
    public string EichdatumDisplay => Resolution?.Context?.EichdatumDisplay ?? "—";
    public string EichfaelligDisplay => Resolution?.Context?.EichfaelligDisplay ?? "—";

    public async Task InitializeAsync()
    {
        if (!IsAuthorized)
        {
            StatusMessage = "Dieser Bereich ist nur für Admin oder Vorstand verfügbar.";
            return;
        }

        await LoadFallbackParzellenAsync();
        await RefreshNfcAvailabilityAsync();
    }

    public async Task RefreshNfcAvailabilityAsync()
    {
        NfcAvailability = await _nfcScanService.GetAvailabilityAsync();
    }

    public async Task StartNfcSessionAsync()
    {
        if (!IsAuthorized)
            return;

        _nfcScanService.TagScanned -= OnTagScanned;
        _nfcScanService.TagScanned += OnTagScanned;

        var availability = await _nfcScanService.StartScanningAsync();
        NfcAvailability = availability;

        if (availability.State == NfcAvailabilityState.Available)
            StatusMessage = "RFID-Scan aktiv. Halte den Tag an das Gerät.";
    }

    public async Task StopNfcSessionAsync()
    {
        _nfcScanService.TagScanned -= OnTagScanned;
        await _nfcScanService.StopScanningAsync();
    }

    public Task OpenNfcSettingsAsync()
    {
        return _nfcScanService.OpenSettingsAsync();
    }

    public async Task ResolveAsync()
    {
        if (string.IsNullOrWhiteSpace(UidInput))
        {
            StatusMessage = "Bitte eine RFID-UID eingeben.";
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _supabaseService.ResolveRfidScanContextAsync(UidInput);
            Resolution = result;
            StatusMessage = result.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ApplyFallbackContextAsync()
    {
        if (SelectedFallbackParzelle == null || SelectedFallbackMedium == null)
        {
            StatusMessage = "Bitte zuerst Parzelle und Medium wählen.";
            return;
        }

        IsBusy = true;
        try
        {
            _uidInput = string.Empty;
            OnPropertyChanged(nameof(UidInput));
            OnPropertyChanged(nameof(CanResolve));
            Resolution = await BuildFallbackResolutionAsync(SelectedFallbackParzelle, SelectedFallbackMedium.Key);
            StatusMessage = $"Fallback-Kontext für {SelectedFallbackMedium.DisplayName} bei {SelectedFallbackParzelle.DisplayName} geladen.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void Reset()
    {
        _uidInput = string.Empty;
        _resolution = null;
        _statusMessage = string.Empty;
        _lastScannedUid = string.Empty;
        _lastScannedAt = DateTime.MinValue;

        OnPropertyChanged(nameof(UidInput));
        OnPropertyChanged(nameof(CanResolve));
        OnPropertyChanged(nameof(CanStartNfcScan));
        OnPropertyChanged(nameof(CanApplyFallbackContext));
        OnPropertyChanged(nameof(Resolution));
        OnPropertyChanged(nameof(HasResolution));
        OnPropertyChanged(nameof(StateDisplay));
        OnPropertyChanged(nameof(NormalizedUid));
        OnPropertyChanged(nameof(ParzelleDisplayName));
        OnPropertyChanged(nameof(MediumDisplay));
        OnPropertyChanged(nameof(RfidDisplay));
        OnPropertyChanged(nameof(ActiveMeterDisplay));
        OnPropertyChanged(nameof(ZaehlernummerDisplay));
        OnPropertyChanged(nameof(StatusDisplay));
        OnPropertyChanged(nameof(EichdatumDisplay));
        OnPropertyChanged(nameof(EichfaelligDisplay));
        OnPropertyChanged(nameof(StatusMessage));
        OnPropertyChanged(nameof(HasStatusMessage));
    }

    private async void OnTagScanned(object? sender, string uid)
    {
        if (string.IsNullOrWhiteSpace(uid))
            return;

        var now = DateTime.UtcNow;
        if (string.Equals(_lastScannedUid, uid, StringComparison.OrdinalIgnoreCase)
            && (now - _lastScannedAt) < TimeSpan.FromSeconds(2))
        {
            return;
        }

        _lastScannedUid = uid;
        _lastScannedAt = now;
        UidInput = uid;
        StatusMessage = $"RFID-Tag {uid} gelesen. Kontext wird geladen.";
        await ResolveAsync();
        await _nfcScanService.StopScanningAsync();
    }

    private async Task LoadFallbackParzellenAsync()
    {
        var parzellen = await _supabaseService.GetAllParzellenAsync();
        var ordered = parzellen
            .Where(x => x.Aktiv)
            .OrderBy(x => x.GartenNrSortKey)
            .ThenBy(x => x.GartenNr, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        FallbackParzellen.Clear();
        foreach (var item in ordered)
            FallbackParzellen.Add(item);
    }

    private void RefreshFallbackMediumOptions()
    {
        FallbackMediumOptions.Clear();

        if (SelectedFallbackParzelle?.HatStrom == true)
            FallbackMediumOptions.Add(new RfidMediumOption("strom", "Strom"));

        if (SelectedFallbackParzelle?.HatWasser == true)
            FallbackMediumOptions.Add(new RfidMediumOption("wasser", "Wasser"));

        if (SelectedFallbackMedium != null && FallbackMediumOptions.All(x => x.Key != SelectedFallbackMedium.Key))
            SelectedFallbackMedium = null;
    }

    private async Task<RfidScanContextResult> BuildFallbackResolutionAsync(ParzelleRecord parzelle, string medium)
    {
        if (string.Equals(medium, "wasser", StringComparison.OrdinalIgnoreCase))
        {
            var meter = await _supabaseService.GetActiveWasserzaehlerAsync(parzelle.Id, DateTime.Today);
            return CreateFallbackResolution(parzelle, "wasser", meter?.Id, meter?.Zaehlernummer, meter?.Eichdatum, meter?.EingebautAm);
        }

        var stromMeter = await _supabaseService.GetActiveStromzaehlerAsync(parzelle.Id, DateTime.Today);
        return CreateFallbackResolution(parzelle, "strom", stromMeter?.Id, stromMeter?.Zaehlernummer, stromMeter?.Eichdatum, stromMeter?.EingebautAm);
    }

    private static RfidScanContextResult CreateFallbackResolution(
        ParzelleRecord parzelle,
        string medium,
        long? aktiverZaehlerId,
        string? zaehlernummer,
        DateTime? eichdatum,
        DateTime? eingebautAm)
    {
        var hasActiveMeter = aktiverZaehlerId.HasValue && aktiverZaehlerId.Value > 0;
        var context = new RfidScanContextRecord
        {
            ParzelleId = parzelle.Id,
            Anlage = parzelle.Anlage,
            GartenNr = parzelle.GartenNr,
            Medium = medium,
            AktiverZaehlerId = hasActiveMeter && aktiverZaehlerId.HasValue ? Convert.ToInt32(aktiverZaehlerId.Value) : null,
            Zaehlernummer = zaehlernummer,
            Eichdatum = eichdatum,
            EingebautAm = eingebautAm,
            Status = hasActiveMeter ? "Aktiv" : "Kein aktiver Zähler"
        };

        return new RfidScanContextResult
        {
            NormalizedUid = string.Empty,
            State = hasActiveMeter ? RfidScanContextState.KnownWithActiveMeter : RfidScanContextState.KnownWithoutActiveMeter,
            Context = context,
            Message = $"Fallback-Kontext für {context.MediumDisplay} bei {parzelle.DisplayName} geladen."
        };
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
