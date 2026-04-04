using CommunityToolkit.Mvvm.Input;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace KGV.ViewModels
{
    public sealed class ParzellenVerwaltungViewModel : BaseViewModel, INavigationAware
    {
        private readonly ISupabaseService _supabaseService;
        private readonly MainWindowViewModel _mainVm;
        private ParzelleVerwaltungItem? _selectedItem;
        private ParzelleDetailDTO? _selectedDetail;
        private string _statusMessage = string.Empty;
        private bool _isBusy;
        private bool _isEditMode;
        private string _editFlaeche = string.Empty;
        private bool _editHatWasser;
        private bool _editHatStrom;

        public string Title => "Parzellenverwaltung";
        public string Description => "Zeigt die belastbar ableitbaren Parzellen mit fokussierten Parzellen-Stammdaten in einer zentralen Detailansicht.";
        public string DetailHint => "Parzellen-Stammdaten bleiben hier fachlich getrennt von Mitgliedszuordnung, Ablesen und anderen Verwaltungsblöcken.";

        public ObservableCollection<ParzelleVerwaltungItem> Items { get; } = new();

        public ParzelleVerwaltungItem? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (!SetProperty(ref _selectedItem, value))
                    return;

                IsEditMode = false;
                OpenMemberCommand.NotifyCanExecuteChanged();
                OpenDokumenteCommand.NotifyCanExecuteChanged();
                OpenStromCommand.NotifyCanExecuteChanged();
                OpenWasserCommand.NotifyCanExecuteChanged();
                EditCommand.NotifyCanExecuteChanged();
                SaveStammdatenCommand.NotifyCanExecuteChanged();
                CancelEditCommand.NotifyCanExecuteChanged();

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
                OnPropertyChanged(nameof(ShowReadOnlyStammdaten));
                OnPropertyChanged(nameof(CanEditStammdaten));
                OnPropertyChanged(nameof(CanSaveStammdaten));
                OpenMemberCommand.NotifyCanExecuteChanged();
                OpenDokumenteCommand.NotifyCanExecuteChanged();
                OpenStromCommand.NotifyCanExecuteChanged();
                OpenWasserCommand.NotifyCanExecuteChanged();
                EditCommand.NotifyCanExecuteChanged();
                SaveStammdatenCommand.NotifyCanExecuteChanged();
                CancelEditCommand.NotifyCanExecuteChanged();

                if (!IsEditMode)
                    SyncEditFieldsFromDetail(value);
            }
        }

        public bool HasSelectedDetail => SelectedDetail != null;
        public bool ShowSelectionHint => !HasSelectedDetail;
        public bool ShowReadOnlyStammdaten => HasSelectedDetail && !IsEditMode;
        public bool CanEditStammdaten => HasSelectedDetail && !IsBusy && !IsEditMode;
        public bool CanSaveStammdaten => HasSelectedDetail && IsEditMode && !IsBusy;

        public bool IsEditMode
        {
            get => _isEditMode;
            private set
            {
                if (!SetProperty(ref _isEditMode, value))
                    return;

                OnPropertyChanged(nameof(ShowReadOnlyStammdaten));
                OnPropertyChanged(nameof(CanEditStammdaten));
                OnPropertyChanged(nameof(CanSaveStammdaten));
                EditCommand.NotifyCanExecuteChanged();
                SaveStammdatenCommand.NotifyCanExecuteChanged();
                CancelEditCommand.NotifyCanExecuteChanged();
            }
        }

        public string EditFlaeche
        {
            get => _editFlaeche;
            set => SetProperty(ref _editFlaeche, value);
        }

        public bool EditHatWasser
        {
            get => _editHatWasser;
            set => SetProperty(ref _editHatWasser, value);
        }

        public bool EditHatStrom
        {
            get => _editHatStrom;
            set => SetProperty(ref _editHatStrom, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (!SetProperty(ref _isBusy, value))
                    return;

                OnPropertyChanged(nameof(CanEditStammdaten));
                OnPropertyChanged(nameof(CanSaveStammdaten));
                RefreshCommand.NotifyCanExecuteChanged();
                OpenMemberCommand.NotifyCanExecuteChanged();
                OpenDokumenteCommand.NotifyCanExecuteChanged();
                OpenStromCommand.NotifyCanExecuteChanged();
                OpenWasserCommand.NotifyCanExecuteChanged();
                EditCommand.NotifyCanExecuteChanged();
                SaveStammdatenCommand.NotifyCanExecuteChanged();
                CancelEditCommand.NotifyCanExecuteChanged();
            }
        }

        public IAsyncRelayCommand RefreshCommand { get; }
        public IAsyncRelayCommand OpenMemberCommand { get; }
        public IAsyncRelayCommand OpenDokumenteCommand { get; }
        public IAsyncRelayCommand OpenStromCommand { get; }
        public IAsyncRelayCommand OpenWasserCommand { get; }
        public IRelayCommand EditCommand { get; }
        public IRelayCommand CancelEditCommand { get; }
        public IAsyncRelayCommand SaveStammdatenCommand { get; }

        public ParzellenVerwaltungViewModel(ISupabaseService supabaseService, MainWindowViewModel mainVm)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
            _mainVm = mainVm ?? throw new ArgumentNullException(nameof(mainVm));

            RefreshCommand = new AsyncRelayCommand(LoadAsync, () => !IsBusy);
            OpenMemberCommand = new AsyncRelayCommand(OpenMemberAsync, () => !IsBusy && SelectedItem?.MitgliedId is > 0);
            OpenDokumenteCommand = new AsyncRelayCommand(OpenDokumenteAsync, () => !IsBusy && SelectedItem != null);
            OpenStromCommand = new AsyncRelayCommand(OpenStromAsync, () => !IsBusy && SelectedItem != null);
            OpenWasserCommand = new AsyncRelayCommand(OpenWasserAsync, () => !IsBusy && SelectedItem != null);
            EditCommand = new RelayCommand(BeginEditMode, () => CanEditStammdaten);
            CancelEditCommand = new RelayCommand(CancelEditMode, () => IsEditMode);
            SaveStammdatenCommand = new AsyncRelayCommand(SaveStammdatenAsync, () => CanSaveStammdaten);
        }

        public async Task OnNavigatedToAsync()
        {
            await LoadAsync();
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private async Task LoadAsync()
        {
            IsBusy = true;
            StatusMessage = string.Empty;

            try
            {
                var parzellen = await _supabaseService.GetAllParzellenAsync();
                var belegungen = await _supabaseService.GetAllParzellenBelegungenAsync();
                var mitglieder = await _supabaseService.GetMitgliederAsync();
                var mitgliederById = mitglieder.ToDictionary(x => x.Id, x => x);

                var today = DateTime.Today;
                var currentByParzelle = belegungen
                    .GroupBy(x => x.ParzelleId)
                    .Select(g => g.Where(x => IsActiveOn(x, today))
                        .OrderByDescending(x => x.VonDatum ?? DateTime.MinValue)
                        .FirstOrDefault())
                    .Where(x => x != null)
                    .ToDictionary(x => x!.ParzelleId, x => x!);

                var selectedParzelleId = SelectedItem?.ParzelleId;

                Items.Clear();
                SelectedDetail = null;
                IsEditMode = false;

                foreach (var parzelle in parzellen
                             .OrderBy(x => GetGartenNrSortKey(x.GartenNr))
                             .ThenBy(x => x.GartenNr, StringComparer.CurrentCultureIgnoreCase))
                {
                    currentByParzelle.TryGetValue(parzelle.Id, out var belegung);
                    mitgliederById.TryGetValue(belegung?.MitgliedId ?? 0, out var mitglied);

                    Items.Add(new ParzelleVerwaltungItem
                    {
                        ParzelleId = parzelle.Id,
                        GartenNr = parzelle.GartenNr,
                        Anlage = parzelle.Anlage,
                        MitgliedId = belegung?.MitgliedId,
                        MitgliedName = FormatMemberName(mitglied),
                        IstVergeben = belegung != null,
                        StatusText = belegung != null ? "vergeben" : "frei"
                    });
                }

                if (selectedParzelleId.HasValue)
                    SelectedItem = Items.FirstOrDefault(x => x.ParzelleId == selectedParzelleId.Value);

                StatusMessage = Items.Count == 0
                    ? "Keine Parzellen geladen."
                    : $"{Items.Count} Parzellen geladen.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Parzellenverwaltung konnte nicht geladen werden: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task OpenMemberAsync()
        {
            if (SelectedItem?.MitgliedId is not > 0)
                return;

            var memberRecord = await _supabaseService.GetMitgliedByIdAsync(SelectedItem.MitgliedId.Value);
            if (memberRecord == null)
            {
                StatusMessage = "Zugehöriges Mitglied konnte nicht geladen werden.";
                return;
            }

            var member = ToMemberDto(memberRecord);
            _mainVm.SelectedMember = member;
            await _mainVm.NavigateToAsync(new MemberDetailViewModel(_supabaseService, _mainVm.AuthService, member));
        }

        private async Task OpenDokumenteAsync()
        {
            var context = CreateParzellenContext();
            if (context == null)
                return;

            await _mainVm.NavigateToAsync(new GartenDokumenteViewModel(_supabaseService, context, _mainVm.UserContext.Has(KGV.Core.Security.PermissionFlags.CanManageDocuments)));
        }

        private async Task OpenStromAsync()
        {
            var context = CreateParzellenContext();
            if (context == null)
                return;

            await _mainVm.NavigateToAsync(new GartenStromViewModel(_supabaseService, context, _mainVm));
        }

        private async Task OpenWasserAsync()
        {
            var context = CreateParzellenContext();
            if (context == null)
                return;

            await _mainVm.NavigateToAsync(new GartenWasserViewModel(_supabaseService, context, _mainVm));
        }

        private async Task LoadSelectedDetailAsync()
        {
            var selected = SelectedItem;
            if (selected == null)
            {
                SelectedDetail = null;
                return;
            }

            var detail = await _supabaseService.GetParzelleDetailAsync(selected.ParzelleId);
            if (SelectedItem?.ParzelleId != selected.ParzelleId)
                return;

            SelectedDetail = detail;
        }

        private void BeginEditMode()
        {
            if (!CanEditStammdaten || SelectedDetail == null)
                return;

            SyncEditFieldsFromDetail(SelectedDetail);
            IsEditMode = true;
        }

        private void CancelEditMode()
        {
            SyncEditFieldsFromDetail(SelectedDetail);
            IsEditMode = false;
            StatusMessage = "Bearbeiten abgebrochen.";
        }

        private bool HasFlaecheChanged()
        {
            return NormalizeFlaecheValue(SelectedDetail?.FlaecheQm) != NormalizeFlaecheValue(ParseEditableFlaeche());
        }

        private async Task SaveStammdatenAsync()
        {
            if (SelectedDetail == null)
                return;

            var flaeche = ParseEditableFlaeche();
            if (!string.IsNullOrWhiteSpace(EditFlaeche) && !flaeche.HasValue)
            {
                StatusMessage = "Die Fläche konnte nicht gelesen werden.";
                return;
            }

            if (HasFlaecheChanged())
            {
                var confirmation = MessageBox.Show(
                    "Bist du dir sicher, dass du die Fläche der Parzelle ändern möchtest?",
                    "Bestätigung",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirmation != MessageBoxResult.Yes)
                    return;
            }

            var record = new ParzelleRecord
            {
                Id = SelectedDetail.ParzelleId,
                GartenNr = SelectedDetail.GartenNr?.Trim() ?? string.Empty,
                Anlage = SelectedDetail.Anlage?.Trim() ?? string.Empty,
                FlaecheQm = flaeche,
                HatWasser = EditHatWasser,
                HatStrom = EditHatStrom,
                RfidWasser = SelectedDetail.RfidWasser,
                RfidStrom = SelectedDetail.RfidStrom
            };

            if (string.IsNullOrWhiteSpace(record.GartenNr))
            {
                StatusMessage = "Bitte eine Gartennummer angeben.";
                return;
            }

            IsBusy = true;
            try
            {
                var ok = await _supabaseService.UpdateParzelleStammdatenAsync(record);
                StatusMessage = ok ? "Parzellen-Stammdaten gespeichert." : "Parzellen-Stammdaten konnten nicht gespeichert werden.";
                if (!ok)
                    return;

                IsEditMode = false;
                await LoadAsync();
                SelectedItem = Items.FirstOrDefault(x => x.ParzelleId == record.Id);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void SyncEditFieldsFromDetail(ParzelleDetailDTO? detail)
        {
            EditFlaeche = detail?.FlaecheQm?.ToString("0.##", CultureInfo.CurrentCulture) ?? string.Empty;
            EditHatWasser = detail?.HatWasser == true;
            EditHatStrom = detail?.HatStrom == true;
        }

        private decimal? ParseEditableFlaeche()
        {
            if (string.IsNullOrWhiteSpace(EditFlaeche))
                return null;

            if (decimal.TryParse(EditFlaeche, NumberStyles.Number, CultureInfo.CurrentCulture, out var currentCultureValue))
                return currentCultureValue;

            if (decimal.TryParse(EditFlaeche, NumberStyles.Number, CultureInfo.InvariantCulture, out var invariantValue))
                return invariantValue;

            return null;
        }

        private static decimal? NormalizeFlaecheValue(decimal? value)
        {
            return value.HasValue ? decimal.Round(value.Value, 2) : null;
        }

        private ParzellenBelegungDTO? CreateParzellenContext()
        {
            if (SelectedItem == null)
                return null;

            return new ParzellenBelegungDTO
            {
                ParzelleId = SelectedItem.ParzelleId,
                MitgliedId = SelectedItem.MitgliedId ?? 0,
                GartenNr = SelectedItem.GartenNr,
                Anlage = SelectedItem.Anlage,
                VonDatum = SelectedDetail?.VonDatum,
                BisDatum = SelectedDetail?.BisDatum
            };
        }

        private static MemberDTO ToMemberDto(MitgliedRecord record)
        {
            return new MemberDTO
            {
                Id = record.Id,
                Vorname = record.Vorname ?? string.Empty,
                Nachname = record.Name ?? string.Empty,
                Email = record.Email ?? string.Empty,
                Role = record.Role ?? string.Empty
            };
        }

        private static string FormatMemberName(MitgliedRecord? member)
        {
            if (member == null)
                return string.Empty;

            var name = $"{member.Vorname} {member.Name}".Trim();
            return string.IsNullOrWhiteSpace(name) ? (member.Email ?? string.Empty) : name;
        }

        private static bool IsActiveOn(ParzellenBelegungRecord belegung, DateTime date)
        {
            var onDate = date.Date;
            var von = (belegung.VonDatum ?? DateTime.MinValue).Date;
            var bis = belegung.BisDatum?.Date;
            return von <= onDate && (bis == null || bis.Value >= onDate);
        }

        private static int GetGartenNrSortKey(string? gartenNr)
        {
            if (string.IsNullOrWhiteSpace(gartenNr))
                return int.MaxValue;

            var digits = new string(gartenNr.TakeWhile(char.IsDigit).ToArray());
            return int.TryParse(digits, out var value) ? value : int.MaxValue;
        }
    }
}
