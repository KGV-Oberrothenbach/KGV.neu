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
        public bool CanReadMeters => PermissionChecks.CanReadMeters(_mainVm.UserContext);
        public bool CanManageMeterChanges => PermissionChecks.CanManageMeterChanges(_mainVm.UserContext);
        public bool CanApproveMeterReadings => PermissionChecks.CanApproveMeterReadings(_mainVm.UserContext);
        public bool HasAnyMeterAccess => PermissionChecks.HasAnyMeterAccess(_mainVm.UserContext);

        public RelayCommand<object?> OpenAblesungErfassenCommand { get; }
        public RelayCommand<object?> OpenZaehlerwechselCommand { get; }
        public RelayCommand<object?> OpenRfidEinrichtenCommand { get; }
        public RelayCommand<object?> OpenFaelligeZaehlerCommand { get; }
        public RelayCommand<object?> OpenAblesungenFreigabeCommand { get; }
        public RelayCommand<object?> OpenFotoUploadTestCommand { get; }

        public AblesenOverviewViewModel(MainWindowViewModel mainVm)
        {
            _mainVm = mainVm ?? throw new ArgumentNullException(nameof(mainVm));
            OpenAblesungErfassenCommand = new RelayCommand<object?>(_ => _ = NavigateAsync(_mainVm.NavigateToAblesungErfassenViewModel()), _ => CanReadMeters);
            OpenZaehlerwechselCommand = new RelayCommand<object?>(_ => _ = NavigateAsync(_mainVm.NavigateToZaehlerwechselScanViewModel()), _ => CanManageMeterChanges);
            OpenRfidEinrichtenCommand = new RelayCommand<object?>(_ => _ = NavigateAsync(_mainVm.NavigateToRfidEinrichtenViewModel()), _ => CanManageMeterChanges);
            OpenFaelligeZaehlerCommand = new RelayCommand<object?>(_ => _ = NavigateAsync(_mainVm.NavigateToFaelligeZaehlerViewModel()), _ => CanReadMeters);
            OpenAblesungenFreigabeCommand = new RelayCommand<object?>(_ => _ = NavigateAsync(_mainVm.NavigateToAblesungenFreigabeViewModel()), _ => CanApproveMeterReadings);
            OpenFotoUploadTestCommand = new RelayCommand<object?>(_ => _ = NavigateAsync(_mainVm.NavigateToFotoUploadTestViewModel()), _ => HasAnyMeterAccess);
        }

        private async Task NavigateAsync(BaseViewModel? target)
        {
            if (target == null)
                return;

            await _mainVm.NavigateToAsync(target);
        }
    }
}
