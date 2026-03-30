using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace KGV.ViewModels
{
    public sealed class ZaehlerwechselScanViewModel : BaseViewModel, INavigationAware
    {
        private readonly ISupabaseService _supabaseService;
        private readonly MainWindowViewModel _mainVm;
        private readonly List<ParzelleRecord> _allParzellen = new();
        private string _searchText = string.Empty;
        private string _statusMessage = string.Empty;
        private bool _isBusy;
        private ParzelleRecord? _selectedParzelle;
        private ParzelleDetailDTO? _selectedDetail;

        public ZaehlerwechselScanViewModel(ISupabaseService supabaseService, MainWindowViewModel mainVm)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
            _mainVm = mainVm ?? throw new ArgumentNullException(nameof(mainVm));

            RefreshCommand = new RelayCommand<object?>(_ => _ = LoadAsync(), _ => IsAuthorized && !IsBusy);
            OpenStromPathCommand = new RelayCommand<object?>(_ => _ = OpenMediumPathAsync("strom"), _ => CanOpenStromPath);
            OpenWasserPathCommand = new RelayCommand<object?>(_ => _ = OpenMediumPathAsync("wasser"), _ => CanOpenWasserPath);
        }

        public ObservableCollection<ParzelleRecord> FilteredParzellen { get; } = new();

        public RelayCommand<object?> RefreshCommand { get; }
        public RelayCommand<object?> OpenStromPathCommand { get; }
        public RelayCommand<object?> OpenWasserPathCommand { get; }

        public string Title => "Zählerwechsel";
        public string Description => "Korrektur- und Verwaltungsweg über Gartennummer oder Parzelle. RFID-Scan ist in WPF kein Pflichtschritt.";
        public bool IsAuthorized => _mainVm.UserContext.Role is UserRole.Admin or UserRole.Vorstand;
        public bool HasFilteredParzellen => FilteredParzellen.Count > 0;
        public bool HasSelectedDetail => SelectedDetail != null;
        public bool ShowSelectionHint => !HasSelectedDetail;
        public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (!SetProperty(ref _searchText, value))
                    return;

                ApplyFilter();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set
            {
                if (!SetProperty(ref _statusMessage, value))
                    return;

                OnPropertyChanged(nameof(HasStatusMessage));
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (!SetProperty(ref _isBusy, value))
                    return;

                RefreshCommand.RaiseCanExecuteChanged();
                OpenStromPathCommand.RaiseCanExecuteChanged();
                OpenWasserPathCommand.RaiseCanExecuteChanged();
            }
        }

        public ParzelleRecord? SelectedParzelle
        {
            get => _selectedParzelle;
            set
            {
                if (!SetProperty(ref _selectedParzelle, value))
                    return;

                OnPropertyChanged(nameof(SelectedParzelleDisplayName));
                _ = LoadSelectedDetailAsync();
            }
        }

        public ParzelleDetailDTO? SelectedDetail
        {
            get => _selectedDetail;
            private set
            {
                if (!SetProperty(ref _selectedDetail, value))
                    return;

                OnPropertyChanged(nameof(HasSelectedDetail));
                OnPropertyChanged(nameof(ShowSelectionHint));
                OnPropertyChanged(nameof(SelectedParzelleDisplayName));
                OnPropertyChanged(nameof(StromStatusText));
                OnPropertyChanged(nameof(WasserStatusText));
                OnPropertyChanged(nameof(StromActionText));
                OnPropertyChanged(nameof(WasserActionText));
                OnPropertyChanged(nameof(CanOpenStromPath));
                OnPropertyChanged(nameof(CanOpenWasserPath));
                OpenStromPathCommand.RaiseCanExecuteChanged();
                OpenWasserPathCommand.RaiseCanExecuteChanged();
            }
        }

        public string SelectedParzelleDisplayName => SelectedDetail?.DisplayName ?? SelectedParzelle?.DisplayName ?? "Keine Parzelle ausgewählt.";

        public string StromStatusText => BuildMediumStatusText("strom");
        public string WasserStatusText => BuildMediumStatusText("wasser");

        public string StromActionText => HasActiveMeter("strom")
            ? "Ausbau-/Korrekturpfad öffnen"
            : "Einbaupfad öffnen";

        public string WasserActionText => HasActiveMeter("wasser")
            ? "Ausbau-/Korrekturpfad öffnen"
            : "Einbaupfad öffnen";

        public bool CanOpenStromPath => !IsBusy && HasMediumContext("strom");
        public bool CanOpenWasserPath => !IsBusy && HasMediumContext("wasser");

        public async Task OnNavigatedToAsync()
        {
            await LoadAsync();
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private async Task LoadAsync()
        {
            if (!IsAuthorized)
            {
                FilteredParzellen.Clear();
                _allParzellen.Clear();
                SelectedParzelle = null;
                SelectedDetail = null;
                StatusMessage = "Dieser Bereich ist nur für Admin oder Vorstand verfügbar.";
                return;
            }

            var selectedParzelleId = SelectedParzelle?.Id;

            IsBusy = true;
            try
            {
                var parzellen = await _supabaseService.GetAllParzellenAsync();
                _allParzellen.Clear();
                _allParzellen.AddRange(parzellen.Where(x => x.Aktiv));

                ApplyFilter();

                if (selectedParzelleId.HasValue)
                    SelectedParzelle = FilteredParzellen.FirstOrDefault(x => x.Id == selectedParzelleId.Value)
                        ?? _allParzellen.FirstOrDefault(x => x.Id == selectedParzelleId.Value);

                if (!selectedParzelleId.HasValue)
                    SelectedDetail = null;

                StatusMessage = _allParzellen.Count == 0
                    ? "Keine aktiven Parzellen gefunden."
                    : "Parzelle auswählen, um den Korrekturpfad für Strom oder Wasser zu öffnen.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Zählerwechsel konnte nicht geladen werden: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ApplyFilter()
        {
            var search = (SearchText ?? string.Empty).Trim();
            var filtered = string.IsNullOrWhiteSpace(search)
                ? _allParzellen
                : _allParzellen.Where(x =>
                    (!string.IsNullOrWhiteSpace(x.GartenNr) && x.GartenNr.Contains(search, StringComparison.CurrentCultureIgnoreCase))
                    || (!string.IsNullOrWhiteSpace(x.Anlage) && x.Anlage.Contains(search, StringComparison.CurrentCultureIgnoreCase))
                    || (!string.IsNullOrWhiteSpace(x.DisplayName) && x.DisplayName.Contains(search, StringComparison.CurrentCultureIgnoreCase)))
                    .ToList();

            FilteredParzellen.Clear();
            foreach (var item in filtered)
                FilteredParzellen.Add(item);

            OnPropertyChanged(nameof(HasFilteredParzellen));

            if (SelectedParzelle != null && !FilteredParzellen.Contains(SelectedParzelle))
            {
                SelectedParzelle = null;
                SelectedDetail = null;
            }
        }

        private async Task LoadSelectedDetailAsync()
        {
            if (SelectedParzelle == null)
            {
                SelectedDetail = null;
                return;
            }

            IsBusy = true;
            try
            {
                var detail = await _supabaseService.GetParzelleDetailAsync(SelectedParzelle.Id);
                if (SelectedParzelle?.Id != detail?.ParzelleId && detail != null)
                    return;

                SelectedDetail = detail;
                StatusMessage = detail == null
                    ? "Die ausgewählte Parzelle konnte nicht geladen werden."
                    : $"Korrekturzustand für {detail.DisplayName} geladen.";
            }
            catch (Exception ex)
            {
                SelectedDetail = null;
                StatusMessage = $"Parzellenzustand konnte nicht geladen werden: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task OpenMediumPathAsync(string medium)
        {
            if (SelectedDetail == null)
                return;

            BaseViewModel target = HasActiveMeter(medium)
                ? new ZaehlerwechselAusbauViewModel(_supabaseService, _mainVm, SelectedDetail, medium)
                : new ZaehlerwechselEinbauViewModel(_supabaseService, _mainVm, SelectedDetail, medium);

            await _mainVm.NavigateToAsync(target);
        }

        private bool HasMediumContext(string medium)
        {
            if (SelectedDetail == null)
                return false;

            return string.Equals(medium, "strom", StringComparison.OrdinalIgnoreCase)
                ? SelectedDetail.HatStrom || SelectedDetail.AktiverStromzaehler != null
                : SelectedDetail.HatWasser || SelectedDetail.AktiverWasserzaehler != null;
        }

        private bool HasActiveMeter(string medium)
        {
            if (SelectedDetail == null)
                return false;

            return string.Equals(medium, "strom", StringComparison.OrdinalIgnoreCase)
                ? SelectedDetail.AktiverStromzaehler?.Id > 0
                : SelectedDetail.AktiverWasserzaehler?.Id > 0;
        }

        private string BuildMediumStatusText(string medium)
        {
            if (SelectedDetail == null)
                return "Bitte zuerst eine Parzelle auswählen.";

            if (string.Equals(medium, "strom", StringComparison.OrdinalIgnoreCase))
            {
                if (!SelectedDetail.HatStrom && SelectedDetail.AktiverStromzaehler == null)
                    return "An der Parzelle ist aktuell kein Stromanschluss hinterlegt.";

                if (SelectedDetail.AktiverStromzaehler != null)
                    return $"Aktiver Stromzähler {SelectedDetail.AktiverStromzaehler.Zaehlernummer} seit {SelectedDetail.AktiverStromzaehler.EingebautAm:dd.MM.yyyy}.";

                return "Kein aktiver Stromzähler vorhanden. Der Einbaupfad ist vorbereitet.";
            }

            if (!SelectedDetail.HatWasser && SelectedDetail.AktiverWasserzaehler == null)
                return "An der Parzelle ist aktuell kein Wasseranschluss hinterlegt.";

            if (SelectedDetail.AktiverWasserzaehler != null)
                return $"Aktiver Wasserzähler {SelectedDetail.AktiverWasserzaehler.Zaehlernummer} seit {SelectedDetail.AktiverWasserzaehler.EingebautAm:dd.MM.yyyy}.";

            return "Kein aktiver Wasserzähler vorhanden. Der Einbaupfad ist vorbereitet.";
        }
    }
}
