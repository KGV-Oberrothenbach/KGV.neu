using KGV.Core.Interfaces;
using KGV.Core.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KGV.Maui.ViewModels;

public sealed class RfidScanContextViewModel : INotifyPropertyChanged
{
    private readonly ISupabaseService _supabaseService;
    private readonly IAuthService _authService;
    private string _uidInput = string.Empty;
    private string _statusMessage = string.Empty;
    private bool _isBusy;
    private RfidScanContextResult? _resolution;

    public RfidScanContextViewModel(ISupabaseService supabaseService, IAuthService authService)
    {
        _supabaseService = supabaseService;
        _authService = authService;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

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
        }
    }

    public bool CanResolve => !IsBusy && !string.IsNullOrWhiteSpace(UidInput);

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

    public bool HasResolution => Resolution != null;
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

    public Task InitializeAsync()
    {
        if (!IsAuthorized)
            StatusMessage = "Dieser Bereich ist nur für Admin oder Vorstand verfügbar.";

        return Task.CompletedTask;
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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
