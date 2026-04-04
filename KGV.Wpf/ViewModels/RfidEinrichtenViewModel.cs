using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Helpers;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace KGV.ViewModels
{
    public sealed class RfidEinrichtenViewModel : BaseViewModel, INavigationAware
    {
        private readonly ISupabaseService _supabaseService;
        private readonly MainWindowViewModel _mainVm;
        private ParzelleRecord? _selectedParzelle;
        private RfidMediumOption? _selectedMedium;
        private string _uidInput = string.Empty;
        private string _statusMessage = string.Empty;
        private bool _isBusy;
        private RfidAssignmentCheckResult? _lastCheck;

        public ObservableCollection<ParzelleRecord> Parzellen { get; } = new();
        public ObservableCollection<RfidMediumOption> MediumOptions { get; } = new();

        public string Title => "RFID einrichten";
        public string Description => "Parzelle wählen, Medium festlegen, UID prüfen und anschließend produktiv speichern.";
        public bool IsAuthorized => PermissionChecks.CanManageMeterChanges(_mainVm.UserContext);
        public bool HasSelectedParzelle => SelectedParzelle != null;
        public string CurrentStromRfid => SelectedParzelle?.StromRfidDisplay ?? "Nicht hinterlegt";
        public string CurrentWasserRfid => SelectedParzelle?.WasserRfidDisplay ?? "Nicht hinterlegt";
        public bool CanCheck => !IsBusy && SelectedParzelle != null && SelectedMedium != null && !string.IsNullOrWhiteSpace(UidInput);
        public bool CanSave => !IsBusy && _lastCheck?.IsValid == true;
        public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

        public RelayCommand<object?> CheckCommand { get; }
        public RelayCommand<object?> SaveCommand { get; }

        public RfidEinrichtenViewModel(ISupabaseService supabaseService, MainWindowViewModel mainVm)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
            _mainVm = mainVm ?? throw new ArgumentNullException(nameof(mainVm));

            CheckCommand = new RelayCommand<object?>(_ => _ = CheckAsync(), _ => CanCheck);
            SaveCommand = new RelayCommand<object?>(_ => _ = SaveAsync(), _ => CanSave);
        }

        public ParzelleRecord? SelectedParzelle
        {
            get => _selectedParzelle;
            set
            {
                if (SetProperty(ref _selectedParzelle, value))
                {
                    OnPropertyChanged(nameof(HasSelectedParzelle));
                    OnPropertyChanged(nameof(CurrentStromRfid));
                    OnPropertyChanged(nameof(CurrentWasserRfid));
                    RefreshMediumOptions();
                    ResetCheckState();
                }
            }
        }

        public RfidMediumOption? SelectedMedium
        {
            get => _selectedMedium;
            set
            {
                if (SetProperty(ref _selectedMedium, value))
                    ResetCheckState();
            }
        }

        public string UidInput
        {
            get => _uidInput;
            set
            {
                if (SetProperty(ref _uidInput, value))
                    ResetCheckState();
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

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    OnPropertyChanged(nameof(CanCheck));
                    OnPropertyChanged(nameof(CanSave));
                    CheckCommand.RaiseCanExecuteChanged();
                    SaveCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public async Task OnNavigatedToAsync()
        {
            if (!IsAuthorized)
            {
                StatusMessage = "Dieser Bereich ist nur für Admin oder Vorstand verfügbar.";
                return;
            }

            await LoadParzellenAsync();
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

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
                foreach (var parzelle in ordered)
                    Parzellen.Add(parzelle);

                SelectedParzelle = preferredParzelleId.HasValue
                    ? Parzellen.FirstOrDefault(x => x.Id == preferredParzelleId.Value)
                    : SelectedParzelle != null
                        ? Parzellen.FirstOrDefault(x => x.Id == SelectedParzelle.Id)
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
            CheckCommand.RaiseCanExecuteChanged();
            SaveCommand.RaiseCanExecuteChanged();
        }

        private void ResetCheckState(bool clearStatus = true)
        {
            _lastCheck = null;
            OnPropertyChanged(nameof(CanCheck));
            OnPropertyChanged(nameof(CanSave));
            CheckCommand.RaiseCanExecuteChanged();
            SaveCommand.RaiseCanExecuteChanged();

            if (clearStatus)
                StatusMessage = string.Empty;
        }

        private async Task CheckAsync()
        {
            if (SelectedParzelle == null || SelectedMedium == null)
            {
                StatusMessage = "Bitte zuerst Parzelle und Medium wählen.";
                return;
            }

            IsBusy = true;
            try
            {
                var result = await _supabaseService.CheckParzelleRfidAssignmentAsync(SelectedParzelle.Id, SelectedMedium.Key, UidInput);
                _lastCheck = result.IsValid ? result : null;
                StatusMessage = result.Message;
                SaveCommand.RaiseCanExecuteChanged();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SaveAsync()
        {
            if (SelectedParzelle == null || SelectedMedium == null)
            {
                StatusMessage = "Bitte zuerst Parzelle und Medium wählen.";
                return;
            }

            IsBusy = true;
            try
            {
                var latestCheck = await _supabaseService.CheckParzelleRfidAssignmentAsync(SelectedParzelle.Id, SelectedMedium.Key, UidInput);
                if (!latestCheck.IsValid)
                {
                    _lastCheck = null;
                    StatusMessage = latestCheck.Message;
                    SaveCommand.RaiseCanExecuteChanged();
                    return;
                }

                var overwriteExisting = false;
                if (latestCheck.RequiresOverwriteConfirmation)
                {
                    var decision = MessageBox.Show(
                        latestCheck.Message + "\n\nSoll die bestehende RFID ersetzt werden?",
                        "RFID überschreiben",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (decision != MessageBoxResult.Yes)
                    {
                        StatusMessage = "Speichern abgebrochen.";
                        return;
                    }

                    overwriteExisting = true;
                }

                var result = await _supabaseService.AssignParzelleRfidAsync(SelectedParzelle.Id, SelectedMedium.Key, UidInput, overwriteExisting);
                StatusMessage = result.Message;
                if (!result.Success)
                {
                    _lastCheck = null;
                    SaveCommand.RaiseCanExecuteChanged();
                    return;
                }

                var selectedParzelleId = SelectedParzelle.Id;
                await LoadParzellenAsync(selectedParzelleId);
                UidInput = string.Empty;
                ResetCheckState(clearStatus: false);
                StatusMessage = result.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
