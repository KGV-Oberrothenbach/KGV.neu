using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Core.Utilities;
using KGV.Helpers;

namespace KGV.ViewModels
{
    public sealed class SaisonverwaltungViewModel : BaseViewModel, INavigationAware
    {
        private readonly ISupabaseService _supabaseService;
        private readonly MainWindowViewModel _mainWindowViewModel;
        private SaisonRecord? _selectedSaison;
        private string _jahr = string.Empty;
        private string _pachtProQm = string.Empty;
        private string _mitgliedsbeitrag = string.Empty;
        private string _mitgliedsbeitragNebenmitglied = string.Empty;
        private string _aufnahmegebuehr = string.Empty;
        private string _gebuehrBauantrag = string.Empty;
        private string _pflichtstundenSoll = string.Empty;
        private string _euroProFehlstunde = string.Empty;
        private string _bemerkung = string.Empty;
        private string _statusMessage = string.Empty;
        private bool _isBusy;
        private bool _isProposal;

        public SaisonverwaltungViewModel(ISupabaseService supabaseService, MainWindowViewModel mainWindowViewModel)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
            _mainWindowViewModel = mainWindowViewModel ?? throw new ArgumentNullException(nameof(mainWindowViewModel));

            RefreshCommand = new RelayCommand<object?>(_ => _ = LoadAsync(), _ => !IsBusy);
            SuggestNextSaisonCommand = new RelayCommand<object?>(_ => SuggestNextSaison(), _ => !IsBusy && IsAdmin);
            SaveCommand = new RelayCommand<object?>(_ => _ = SaveAsync(), _ => CanSave);
        }

        public ObservableCollection<SaisonRecord> Saisons { get; } = new();

        public RelayCommand<object?> RefreshCommand { get; }
        public RelayCommand<object?> SuggestNextSaisonCommand { get; }
        public RelayCommand<object?> SaveCommand { get; }

        public string Titel => "Verwaltung";
        public string Untertitel => "Saisonverwaltung";
        public string Beschreibung => "Saison-ID und Saisonjahr entsprechen dem Kalenderjahr. Neue Saisons übernehmen automatisch die Werte des Vorjahres als Vorschlag.";
        public string Hinweistext => "Vergangene Jahre sind schreibgeschützt. Laufendes und zukünftige Jahre bleiben bearbeitbar.";
        public bool IsAdmin => _mainWindowViewModel.UserContext.Role == UserRole.Admin;
        public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

        public SaisonRecord? SelectedSaison
        {
            get => _selectedSaison;
            set
            {
                if (!SetProperty(ref _selectedSaison, value))
                    return;

                if (value != null)
                {
                    _isProposal = false;
                    ApplyEditor(value);
                }

                OnPropertyChanged(nameof(EditorTitel));
                OnPropertyChanged(nameof(IsEditable));
                OnPropertyChanged(nameof(CanSave));
                SaveCommand.RaiseCanExecuteChanged();
            }
        }

        public string MitgliedsbeitragNebenmitglied
        {
            get => _mitgliedsbeitragNebenmitglied;
            set
            {
                if (!SetProperty(ref _mitgliedsbeitragNebenmitglied, value ?? string.Empty))
                    return;

                SaveCommand.RaiseCanExecuteChanged();
            }
        }

        public string Aufnahmegebuehr
        {
            get => _aufnahmegebuehr;
            set
            {
                if (!SetProperty(ref _aufnahmegebuehr, value ?? string.Empty))
                    return;

                SaveCommand.RaiseCanExecuteChanged();
            }
        }

        public string GebuehrBauantrag
        {
            get => _gebuehrBauantrag;
            set
            {
                if (!SetProperty(ref _gebuehrBauantrag, value ?? string.Empty))
                    return;

                SaveCommand.RaiseCanExecuteChanged();
            }
        }

        public string EditorTitel => _isProposal || SelectedSaison == null ? "Neue Saison" : $"Saison {SaisonverwaltungHelper.GetSaisonJahr(SelectedSaison)}";

        public string Jahr
        {
            get => _jahr;
            set
            {
                if (!SetProperty(ref _jahr, value ?? string.Empty))
                    return;

                OnPropertyChanged(nameof(IsEditable));
                OnPropertyChanged(nameof(CanSave));
                SaveCommand.RaiseCanExecuteChanged();
            }
        }

        public string PachtProQm
        {
            get => _pachtProQm;
            set
            {
                if (!SetProperty(ref _pachtProQm, value ?? string.Empty))
                    return;

                SaveCommand.RaiseCanExecuteChanged();
            }
        }

        public string Mitgliedsbeitrag
        {
            get => _mitgliedsbeitrag;
            set
            {
                if (!SetProperty(ref _mitgliedsbeitrag, value ?? string.Empty))
                    return;

                SaveCommand.RaiseCanExecuteChanged();
            }
        }

        public string PflichtstundenSoll
        {
            get => _pflichtstundenSoll;
            set
            {
                if (!SetProperty(ref _pflichtstundenSoll, value ?? string.Empty))
                    return;

                SaveCommand.RaiseCanExecuteChanged();
            }
        }

        public string EuroProFehlstunde
        {
            get => _euroProFehlstunde;
            set
            {
                if (!SetProperty(ref _euroProFehlstunde, value ?? string.Empty))
                    return;

                SaveCommand.RaiseCanExecuteChanged();
            }
        }

        public string Bemerkung
        {
            get => _bemerkung;
            set
            {
                if (!SetProperty(ref _bemerkung, value ?? string.Empty))
                    return;

                SaveCommand.RaiseCanExecuteChanged();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set
            {
                if (!SetProperty(ref _statusMessage, value ?? string.Empty))
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

                OnPropertyChanged(nameof(IsEditable));
                OnPropertyChanged(nameof(CanSave));
                RefreshCommand.RaiseCanExecuteChanged();
                SuggestNextSaisonCommand.RaiseCanExecuteChanged();
                SaveCommand.RaiseCanExecuteChanged();
            }
        }

        public bool IsEditable => IsAdmin && !IsBusy && int.TryParse(Jahr, out var jahr) && jahr >= DateTime.Today.Year;
        public bool CanSave => IsEditable;

        public Task OnNavigatedToAsync() => LoadAsync();
        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private async Task LoadAsync(int? preferredYear = null)
        {
            try
            {
                IsBusy = true;
                StatusMessage = string.Empty;

                var ordered = (await _supabaseService.GetSaisonRecordsAsync())
                    .OrderByDescending(SaisonverwaltungHelper.GetSaisonJahr)
                    .ToList();

                Saisons.Clear();
                foreach (var saison in ordered)
                    Saisons.Add(saison);

                var selected = preferredYear.HasValue
                    ? ordered.FirstOrDefault(x => SaisonverwaltungHelper.GetSaisonJahr(x) == preferredYear.Value)
                    : ordered.FirstOrDefault();

                if (selected != null)
                {
                    SelectedSaison = selected;
                    return;
                }

                SuggestNextSaison();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Saisons konnten nicht geladen werden: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void SuggestNextSaison()
        {
            var proposal = SaisonverwaltungHelper.CreateNextSaisonProposal(Saisons);
            _isProposal = true;
            SelectedSaison = null;
            ApplyEditor(proposal);
            StatusMessage = "Neue Saison wurde auf Basis des Vorjahres vorgeschlagen.";
            OnPropertyChanged(nameof(EditorTitel));
            OnPropertyChanged(nameof(IsEditable));
            OnPropertyChanged(nameof(CanSave));
            SaveCommand.RaiseCanExecuteChanged();
        }

        private void ApplyEditor(SaisonRecord saison)
        {
            Jahr = SaisonverwaltungHelper.GetSaisonJahr(saison).ToString(CultureInfo.InvariantCulture);
            PachtProQm = FormatDecimal(saison.PachtProQm);
            Mitgliedsbeitrag = FormatDecimal(saison.Mitgliedsbeitrag);
            MitgliedsbeitragNebenmitglied = FormatDecimal(saison.MitgliedsbeitragNebenmitglied);
            Aufnahmegebuehr = FormatDecimal(saison.Aufnahmegebuehr);
            GebuehrBauantrag = FormatDecimal(saison.GebuehrBauantrag);
            PflichtstundenSoll = saison.PflichtstundenSoll.ToString("0.##", CultureInfo.CurrentCulture);
            EuroProFehlstunde = saison.EuroProFehlstunde.ToString("0.##", CultureInfo.CurrentCulture);
            Bemerkung = saison.Bemerkung ?? string.Empty;
        }

        private async Task SaveAsync()
        {
            if (!IsEditable)
            {
                StatusMessage = "Vergangene Jahre können nicht bearbeitet werden.";
                return;
            }

            if (!TryBuildSaisonRecord(out var saison, out var validationMessage))
            {
                StatusMessage = validationMessage;
                return;
            }

            try
            {
                IsBusy = true;
                var saved = await _supabaseService.SaveSaisonAsync(saison);
                if (saved == null)
                {
                    StatusMessage = "Saison konnte nicht gespeichert werden.";
                    return;
                }

                await LoadAsync(SaisonverwaltungHelper.GetSaisonJahr(saved));
                await _mainWindowViewModel.RefreshSeasonsAsync(SaisonverwaltungHelper.GetSaisonJahr(saved));
                StatusMessage = $"Saison {SaisonverwaltungHelper.GetSaisonJahr(saved)} gespeichert.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Saison konnte nicht gespeichert werden: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool TryBuildSaisonRecord(out SaisonRecord saison, out string validationMessage)
        {
            saison = new SaisonRecord();
            validationMessage = string.Empty;

            if (!int.TryParse(Jahr, out var jahr) || jahr < 1900 || jahr > 3000)
            {
                validationMessage = "Bitte ein gültiges Kalenderjahr angeben.";
                return false;
            }

            if (!TryParseRequiredDecimal(PflichtstundenSoll, out var pflichtstundenSoll))
            {
                validationMessage = "Bitte einen gültigen Wert für Pflichtstunden Soll eingeben.";
                return false;
            }

            if (!TryParseRequiredDecimal(EuroProFehlstunde, out var euroProFehlstunde))
            {
                validationMessage = "Bitte einen gültigen Wert für Euro pro Fehlstunde eingeben.";
                return false;
            }

            if (!TryParseOptionalDecimal(PachtProQm, out var pachtProQm))
            {
                validationMessage = "Bitte einen gültigen Wert für Pacht pro qm eingeben.";
                return false;
            }

            if (!TryParseOptionalDecimal(Mitgliedsbeitrag, out var mitgliedsbeitrag))
            {
                validationMessage = "Bitte einen gültigen Wert für Mitgliedsbeitrag eingeben.";
                return false;
            }

            if (!TryParseOptionalDecimal(MitgliedsbeitragNebenmitglied, out var mitgliedsbeitragNebenmitglied))
            {
                validationMessage = "Bitte einen gültigen Wert für Nebenmitgliedsbeitrag eingeben.";
                return false;
            }

            if (!TryParseOptionalDecimal(Aufnahmegebuehr, out var aufnahmegebuehr))
            {
                validationMessage = "Bitte einen gültigen Wert für Aufnahmegebühr eingeben.";
                return false;
            }

            if (!TryParseOptionalDecimal(GebuehrBauantrag, out var gebuehrBauantrag))
            {
                validationMessage = "Bitte einen gültigen Wert für Gebühr Bauantrag eingeben.";
                return false;
            }

            saison = new SaisonRecord
            {
                Id = jahr,
                Jahr = jahr,
                PflichtstundenSoll = pflichtstundenSoll,
                EuroProFehlstunde = euroProFehlstunde,
                PachtProQm = pachtProQm,
                Mitgliedsbeitrag = mitgliedsbeitrag,
                MitgliedsbeitragNebenmitglied = mitgliedsbeitragNebenmitglied,
                Aufnahmegebuehr = aufnahmegebuehr,
                GebuehrBauantrag = gebuehrBauantrag,
                Bemerkung = string.IsNullOrWhiteSpace(Bemerkung) ? null : Bemerkung.Trim()
            };

            return true;
        }

        private static string FormatDecimal(decimal? value)
            => value.HasValue ? value.Value.ToString("0.##", CultureInfo.CurrentCulture) : string.Empty;

        private static bool TryParseOptionalDecimal(string raw, out decimal? value)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                value = null;
                return true;
            }

            if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed)
                || decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed))
            {
                value = parsed;
                return true;
            }

            value = null;
            return false;
        }

        private static bool TryParseRequiredDecimal(string raw, out decimal value)
        {
            if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.CurrentCulture, out value)
                || decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out value))
                return true;

            value = 0m;
            return false;
        }
    }
}