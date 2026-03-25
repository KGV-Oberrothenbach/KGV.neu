using KGV.Core.Security;
using KGV.Helpers;
using System;
using System.Threading.Tasks;

namespace KGV.ViewModels
{
    public sealed class AblesenOverviewViewModel : BaseViewModel
    {
        private readonly MainWindowViewModel _mainVm;

        public string Title => "Ablesen";
        public string Description => "Bitte wähle eine Funktion.";
        public bool IsAdminOrVorstand => _mainVm.UserContext.Role is UserRole.Admin or UserRole.Vorstand;

        public RelayCommand<object?> OpenAblesungErfassenCommand { get; }
        public RelayCommand<object?> OpenZaehlerwechselCommand { get; }
        public RelayCommand<object?> OpenRfidEinrichtenCommand { get; }
        public RelayCommand<object?> OpenFaelligeZaehlerCommand { get; }
        public RelayCommand<object?> OpenFotoUploadTestCommand { get; }

        public AblesenOverviewViewModel(MainWindowViewModel mainVm)
        {
            _mainVm = mainVm ?? throw new ArgumentNullException(nameof(mainVm));
            OpenAblesungErfassenCommand = new RelayCommand<object?>(_ => _ = NavigateAsync(_mainVm.NavigateToAblesungErfassenViewModel()), _ => IsAdminOrVorstand);
            OpenZaehlerwechselCommand = new RelayCommand<object?>(_ => _ = NavigateAsync(_mainVm.NavigateToZaehlerwechselScanViewModel()), _ => IsAdminOrVorstand);
            OpenRfidEinrichtenCommand = new RelayCommand<object?>(_ => _ = NavigateAsync(_mainVm.NavigateToRfidEinrichtenViewModel()), _ => IsAdminOrVorstand);
            OpenFaelligeZaehlerCommand = new RelayCommand<object?>(_ => _ = NavigateAsync(_mainVm.NavigateToFaelligeZaehlerViewModel()), _ => IsAdminOrVorstand);
            OpenFotoUploadTestCommand = new RelayCommand<object?>(_ => _ = NavigateAsync(_mainVm.NavigateToFotoUploadTestViewModel()), _ => IsAdminOrVorstand);
        }

        private async Task NavigateAsync(BaseViewModel? target)
        {
            if (target == null)
                return;

            await _mainVm.NavigateToAsync(target);
        }
    }
}
