using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace KGV.ViewModels
{
    public sealed class AblesungErfassenViewModel : BaseViewModel, INavigationAware
    {
        private readonly MainWindowViewModel _mainVm;

        public AblesungErfassenViewModel(ISupabaseService supabaseService, MainWindowViewModel mainVm)
        {
            _mainVm = mainVm ?? throw new ArgumentNullException(nameof(mainVm));
            ScanContext = new RfidScanContextViewModel(supabaseService ?? throw new ArgumentNullException(nameof(supabaseService)));
            ScanContext.PropertyChanged += OnScanContextChanged;
        }

        public string Title => "Ablesung erfassen";
        public string Description => "RFID-UID eingeben oder scannen, produktiv über `v_rfid_scan_context` auflösen und den Ablese-Kontext prüfen.";
        public bool IsAuthorized => _mainVm.UserContext.Role is UserRole.Admin or UserRole.Vorstand;
        public RfidScanContextViewModel ScanContext { get; }
        public string WorkflowDecisionText => GetDecisionText(ScanContext.Resolution);

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        public Task OnNavigatedToAsync()
        {
            if (!IsAuthorized)
                ScanContext.UidInput = string.Empty;

            return Task.CompletedTask;
        }

        private void OnScanContextChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(RfidScanContextViewModel.Resolution) || e.PropertyName == nameof(RfidScanContextViewModel.StateDisplay))
                OnPropertyChanged(nameof(WorkflowDecisionText));
        }

        private static string GetDecisionText(RfidScanContextResult? resolution)
        {
            if (resolution == null)
                return "Noch kein RFID-Kontext geladen.";

            return resolution.State switch
            {
                RfidScanContextState.KnownWithActiveMeter => "Aktiver Zähler gefunden. Der gemeinsame Ablese-Kontext ist damit produktiv vorbereitet.",
                RfidScanContextState.KnownWithoutActiveMeter => "Der Tag ist bekannt, aktuell aber ohne aktiven Zähler. Eine Ablesung ist damit noch nicht sinnvoll.",
                _ => "Der Tag ist unbekannt. Für die Ablesung kann kein produktiver Kontext vorbereitet werden."
            };
        }
    }
}
