using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Helpers;
using System;
using System.Threading.Tasks;

namespace KGV.ViewModels
{
    public sealed class RfidScanContextViewModel : BaseViewModel
    {
        private readonly ISupabaseService _supabaseService;
        private string _uidInput = string.Empty;
        private string _statusMessage = string.Empty;
        private bool _isBusy;
        private RfidScanContextResult? _resolution;

        public RelayCommand<object?> ResolveCommand { get; }

        public RfidScanContextViewModel(ISupabaseService supabaseService)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
            ResolveCommand = new RelayCommand<object?>(_ => _ = ResolveAsync(), _ => CanResolve);
        }

        public string UidInput
        {
            get => _uidInput;
            set
            {
                if (SetProperty(ref _uidInput, value))
                {
                    Resolution = null;
                    StatusMessage = string.Empty;
                    OnPropertyChanged(nameof(CanResolve));
                    ResolveCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set
            {
                if (SetProperty(ref _statusMessage, value))
                    OnPropertyChanged(nameof(HasStatusMessage));
            }
        }

        public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    OnPropertyChanged(nameof(CanResolve));
                    ResolveCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public bool CanResolve => !IsBusy && !string.IsNullOrWhiteSpace(UidInput);

        public RfidScanContextResult? Resolution
        {
            get => _resolution;
            private set
            {
                if (SetProperty(ref _resolution, value))
                {
                    OnPropertyChanged(nameof(HasResolution));
                    OnPropertyChanged(nameof(HasKnownContext));
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
        }

        public bool HasResolution => Resolution != null;
        public bool HasKnownContext => Resolution?.IsKnown == true;
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
    }
}
