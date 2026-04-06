using KGV.Core.Interfaces;
using KGV.Core.Security;
using KGV.Helpers;
using System;
using System.Threading.Tasks;

namespace KGV.ViewModels
{
    public sealed class AblesenOverviewViewModel : BaseViewModel, INavigationAware
    {
        private readonly MainWindowViewModel _mainVm;
        private readonly ISupabaseService _supabaseService;
        private bool _allowUserMeterReadingSubmissions;

        public string Title => "Ablesen";
        public string Description => EffectiveCanSubmitOwnMeterReadings && !CanReadMeters && !CanManageMeterChanges && !CanApproveMeterReadings
            ? "Bitte wähle eine Funktion. Eigene Zählerablesungen werden in diesem Kontext als Einreichung gespeichert und erst über den Prüfprozess wirksam."
            : CanSubmitOwnMeterReadings && !_allowUserMeterReadingSubmissions && !CanReadMeters && !CanManageMeterChanges && !CanApproveMeterReadings
                ? "Bitte wähle eine Funktion. Eigene Zählerablesungen sind aktuell zentral deaktiviert."
                : "Bitte wähle eine Funktion.";
        public bool CanReadMeters => PermissionChecks.CanReadMeters(_mainVm.UserContext);
        public bool CanSubmitOwnMeterReadings => PermissionChecks.CanSubmitOwnMeterReadings(_mainVm.UserContext);
        public bool EffectiveCanSubmitOwnMeterReadings => CanSubmitOwnMeterReadings && _allowUserMeterReadingSubmissions;
        public bool CanManageMeterChanges => PermissionChecks.CanManageMeterChanges(_mainVm.UserContext);
        public bool CanApproveMeterReadings => PermissionChecks.CanApproveMeterReadings(_mainVm.UserContext);
        public bool HasAnyMeterAccess => CanReadMeters || EffectiveCanSubmitOwnMeterReadings || CanManageMeterChanges || CanApproveMeterReadings;
        public bool CanOpenAblesungErfassen => CanReadMeters || EffectiveCanSubmitOwnMeterReadings;

        public RelayCommand<object?> OpenAblesungErfassenCommand { get; }
        public RelayCommand<object?> OpenZaehlerwechselCommand { get; }
        public RelayCommand<object?> OpenRfidEinrichtenCommand { get; }
        public RelayCommand<object?> OpenFaelligeZaehlerCommand { get; }
        public RelayCommand<object?> OpenAblesungenFreigabeCommand { get; }

        public AblesenOverviewViewModel(MainWindowViewModel mainVm, ISupabaseService supabaseService)
        {
            _mainVm = mainVm ?? throw new ArgumentNullException(nameof(mainVm));
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
            OpenAblesungErfassenCommand = new RelayCommand<object?>(_ => _ = NavigateAsync(_mainVm.NavigateToAblesungErfassenViewModel()), _ => CanOpenAblesungErfassen);
            OpenZaehlerwechselCommand = new RelayCommand<object?>(_ => _ = NavigateAsync(_mainVm.NavigateToZaehlerwechselScanViewModel()), _ => CanManageMeterChanges);
            OpenRfidEinrichtenCommand = new RelayCommand<object?>(_ => _ = NavigateAsync(_mainVm.NavigateToRfidEinrichtenViewModel()), _ => CanManageMeterChanges);
            OpenFaelligeZaehlerCommand = new RelayCommand<object?>(_ => _ = NavigateAsync(_mainVm.NavigateToFaelligeZaehlerViewModel()), _ => CanReadMeters);
            OpenAblesungenFreigabeCommand = new RelayCommand<object?>(_ => _ = NavigateAsync(_mainVm.NavigateToAblesungenFreigabeViewModel()), _ => CanApproveMeterReadings);
        }

        public async Task OnNavigatedToAsync()
        {
            _allowUserMeterReadingSubmissions = await _supabaseService.GetAllowUserMeterReadingSubmissionsAsync();
            OnPropertyChanged(nameof(Description));
            OnPropertyChanged(nameof(EffectiveCanSubmitOwnMeterReadings));
            OnPropertyChanged(nameof(HasAnyMeterAccess));
            OnPropertyChanged(nameof(CanOpenAblesungErfassen));
            OpenAblesungErfassenCommand.RaiseCanExecuteChanged();
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private async Task NavigateAsync(BaseViewModel? target)
        {
            if (target == null)
                return;

            await _mainVm.NavigateToAsync(target);
        }
    }
}
