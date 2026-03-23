using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace KGV.ViewModels
{
    public sealed class ZaehlerwechselScanViewModel : BaseViewModel, INavigationAware
    {
        private readonly MainWindowViewModel _mainVm;

        public ZaehlerwechselScanViewModel(ISupabaseService supabaseService, MainWindowViewModel mainVm)
        {
            _mainVm = mainVm ?? throw new ArgumentNullException(nameof(mainVm));
            ScanContext = new RfidScanContextViewModel(supabaseService ?? throw new ArgumentNullException(nameof(supabaseService)));
            ScanContext.PropertyChanged += OnScanContextChanged;
        }

        public string Title => "Zählerwechsel";
        public string Description => "RFID-UID eingeben oder scannen, produktiv auflösen und daraus den Ausbau- oder Einbaupfad fachlich ableiten.";
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
                RfidScanContextState.KnownWithActiveMeter => "Aktiver Zähler gefunden. Für den Zählerwechsel ist damit als nächster Schritt der Ausbaupfad vorbereitet.",
                RfidScanContextState.KnownWithoutActiveMeter => "Bekannter Tag ohne aktiven Zähler. Für den Zählerwechsel ist damit als nächster Schritt der Einbaupfad vorbereitet.",
                _ => "Der Tag ist unbekannt. Für den Zählerwechsel kann kein produktiver Kontext vorbereitet werden."
            };
        }
    }
}
