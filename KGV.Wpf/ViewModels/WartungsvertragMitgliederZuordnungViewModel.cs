using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Helpers;

namespace KGV.ViewModels
{
    public sealed class WartungsvertragMitgliederZuordnungViewModel : BaseViewModel, INavigationAware
    {
        private const string SortByName = "Name";
        private const string SortByGarden = "Gartennummer";

        private readonly ISupabaseService _supabaseService;
        private readonly MainWindowViewModel _mainVm;
        private readonly long _wartungsvertragId;
        private readonly BaseViewModel? _backTarget;
        private readonly List<AssignableWartungsvertragMemberItem> _allItems = new();
        private string _vertragTitel = "Wartungsvertrag";
        private string _statusMessage = string.Empty;
        private bool _isBusy;
        private string _selectedSortOption = SortByName;
        private int _freiePlaetze;
        private DateTime _gueltigAb = DateTime.Today;

        public WartungsvertragMitgliederZuordnungViewModel(ISupabaseService supabaseService, MainWindowViewModel mainVm, long wartungsvertragId, BaseViewModel? backTarget = null)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
            _mainVm = mainVm ?? throw new ArgumentNullException(nameof(mainVm));
            _wartungsvertragId = wartungsvertragId;
            _backTarget = backTarget;

            SortOptions.Add(SortByName);
            SortOptions.Add(SortByGarden);

            RefreshCommand = new RelayCommand<object?>(_ => _ = LoadAsync(), _ => !IsBusy);
            SaveCommand = new RelayCommand<object?>(_ => _ = SaveAsync(), _ => !IsBusy && CanSave);
            CancelCommand = new RelayCommand<object?>(_ => _ = CancelAsync(), _ => !IsBusy);
        }

        public ObservableCollection<AssignableWartungsvertragMemberItem> Items { get; } = new();
        public ObservableCollection<string> SortOptions { get; } = new();
        public string PageTitle => "Mitglieder zuweisen";
        public string Description => "Globale produktive Zuordnung aktiver Mitglieder mit Sortierung nach Name oder Gartennummer und sauberer Kontingentprüfung.";
        public string VertragTitel
        {
            get => _vertragTitel;
            private set => SetProperty(ref _vertragTitel, value);
        }

        public string SelectedSortOption
        {
            get => _selectedSortOption;
            set
            {
                if (SetProperty(ref _selectedSortOption, string.IsNullOrWhiteSpace(value) ? SortByName : value))
                    ApplySorting();
            }
        }

        public DateTime GueltigAb
        {
            get => _gueltigAb;
            set => SetProperty(ref _gueltigAb, value.Date);
        }

        public string FreiePlaetzeText => _freiePlaetze <= 0
            ? "Kein freier Platz mehr verfügbar"
            : _freiePlaetze == 1
                ? "Noch 1 Platz frei"
                : $"Noch {_freiePlaetze} Plätze frei";

        public bool CanSave => Items.Any(x => x.IsSelected && !x.IsAlreadyAssigned);

        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    RefreshCommand.RaiseCanExecuteChanged();
                    SaveCommand.RaiseCanExecuteChanged();
                    CancelCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public RelayCommand<object?> RefreshCommand { get; }
        public RelayCommand<object?> SaveCommand { get; }
        public RelayCommand<object?> CancelCommand { get; }

        public Task OnNavigatedToAsync() => LoadAsync();
        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private async Task LoadAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;
            try
            {
                StatusMessage = "Wartungsvertrag wird geladen.";
                Items.Clear();
                _allItems.Clear();

                var contract = await _supabaseService.GetWartungsvertragByIdAsync(_wartungsvertragId);
                var detail = await _supabaseService.GetWartungsvertragDetailAsync(_wartungsvertragId);
                if (contract == null || detail == null)
                {
                    StatusMessage = "Der ausgewählte Wartungsvertrag konnte nicht geladen werden.";
                    return;
                }

                VertragTitel = string.IsNullOrWhiteSpace(detail.Titel) ? "Wartungsvertrag" : detail.Titel;
                _freiePlaetze = Math.Max(0, detail.Frei);
                OnPropertyChanged(nameof(FreiePlaetzeText));

                var members = await _supabaseService.GetMitgliederAsync();
                var parzellen = await _supabaseService.GetAllParzellenAsync();
                var belegungen = await _supabaseService.GetAllParzellenBelegungenAsync();
                var gardenLookup = BuildGardenLookup(parzellen, belegungen);
                var assignedIds = detail.ZugeordneteMitglieder
                    .Where(x => x.MitgliedId > 0)
                    .Select(x => x.MitgliedId)
                    .ToHashSet();

                foreach (var member in members
                    .Where(OperationalDataFilter.IsOperationalMember)
                    .Where(x => x.Aktiv)
                    )
                {
                    var item = new AssignableWartungsvertragMemberItem(
                        member.Id,
                        BuildDisplayName(member),
                        member.Name,
                        member.Vorname,
                        gardenLookup.TryGetValue(member.Id, out var gardens) ? gardens : string.Empty,
                        assignedIds.Contains(member.Id),
                        UpdateSelectionState);
                    _allItems.Add(item);
                }

                ApplySorting();
                UpdateSelectionState();
                StatusMessage = string.Empty;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Mitglieder konnten nicht geladen werden: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ApplySorting()
        {
            var ordered = SelectedSortOption == SortByGarden
                ? _allItems
                    .OrderBy(x => GetGartenNrSortKey(x.GartenNummern))
                    .ThenBy(x => x.GartenNummern, StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(x => x.SortNachname, StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(x => x.SortVorname, StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(x => x.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                : _allItems
                    .OrderBy(x => x.SortNachname, StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(x => x.SortVorname, StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(x => x.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(x => x.GartenNummern, StringComparer.CurrentCultureIgnoreCase);

            Items.Clear();
            foreach (var item in ordered)
                Items.Add(item);
        }

        private void UpdateSelectionState()
        {
            var selectedCount = _allItems.Count(x => x.IsSelected && !x.IsAlreadyAssigned);
            var remaining = Math.Max(0, _freiePlaetze - selectedCount);
            foreach (var item in _allItems)
                item.CanSelect = !item.IsAlreadyAssigned && (item.IsSelected || remaining > 0);

            OnPropertyChanged(nameof(CanSave));
            SaveCommand.RaiseCanExecuteChanged();
        }

        private async Task SaveAsync()
        {
            if (IsBusy)
                return;

            var selectedIds = _allItems
                .Where(x => x.IsSelected && !x.IsAlreadyAssigned)
                .Select(x => x.MitgliedId)
                .ToList();
            if (selectedIds.Count == 0)
            {
                StatusMessage = "Bitte mindestens ein neues Mitglied auswählen.";
                return;
            }

            IsBusy = true;
            try
            {
                StatusMessage = "Zuordnungen werden gespeichert.";
                var result = await _supabaseService.AssignMitgliederToWartungsvertragAsync(_wartungsvertragId, GueltigAb, selectedIds);
                if (!result.Success)
                {
                    StatusMessage = result.Message;
                    return;
                }

                await NavigateToDetailAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Zuordnungen konnten nicht gespeichert werden: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CancelAsync()
        {
            await NavigateToDetailAsync();
        }

        private async Task NavigateToDetailAsync()
        {
            await _mainVm.NavigateToAsync(new WartungsvertragDetailViewModel(_supabaseService, _mainVm, _wartungsvertragId, ResolveOverviewTarget(), true));
        }

        private BaseViewModel ResolveOverviewTarget()
            => _backTarget ?? new WartungsvertraegeVerwaltungViewModel(_supabaseService, _mainVm);

        private static Dictionary<int, string> BuildGardenLookup(IReadOnlyCollection<ParzelleRecord> parzellen, IReadOnlyCollection<ParzellenBelegungRecord> belegungen)
        {
            var parzellenById = parzellen
                .Where(x => x.Id > 0)
                .ToDictionary(x => x.Id);
            var today = DateTime.Today;

            return belegungen
                .Where(x => x.MitgliedId > 0)
                .Where(x => x.ParzelleId > 0 && parzellenById.ContainsKey(x.ParzelleId))
                .Where(x => IsActiveBelegungOn(x, today))
                .GroupBy(x => x.MitgliedId)
                .ToDictionary(
                    x => x.Key,
                    x => string.Join(", ", x
                        .Select(b => parzellenById[b.ParzelleId].GartenNr)
                        .Where(g => !string.IsNullOrWhiteSpace(g))
                        .Select(g => g!.Trim())
                        .Distinct(StringComparer.CurrentCultureIgnoreCase)
                        .OrderBy(GetGartenNrSortKey)
                        .ThenBy(g => g, StringComparer.CurrentCultureIgnoreCase)));
        }

        private static bool IsActiveBelegungOn(ParzellenBelegungRecord belegung, DateTime date)
        {
            var target = date.Date;
            var start = belegung.VonDatum?.Date;
            var end = belegung.BisDatum?.Date;
            return (!start.HasValue || start.Value <= target)
                && (!end.HasValue || end.Value >= target);
        }

        private static string BuildDisplayName(MitgliedRecord member)
        {
            var displayName = $"{member.Vorname} {member.Name}".Trim();
            displayName = string.IsNullOrWhiteSpace(displayName) ? member.Email ?? $"Mitglied #{member.Id}" : displayName;
            return member.HauptmitgliedId is > 0
                ? $"{displayName} (Nebenmitglied)"
                : $"{displayName} (Hauptmitglied)";
        }

        private static int GetGartenNrSortKey(string? gartenNummern)
        {
            if (string.IsNullOrWhiteSpace(gartenNummern))
                return int.MaxValue;

            var firstGarden = gartenNummern
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .FirstOrDefault();
            if (string.IsNullOrWhiteSpace(firstGarden))
                return int.MaxValue;

            var digits = new string(firstGarden.TakeWhile(char.IsDigit).ToArray());
            return int.TryParse(digits, out var number) ? number : int.MaxValue;
        }
    }

    public sealed class AssignableWartungsvertragMemberItem : BaseViewModel
    {
        private readonly Action _selectionChanged;
        private bool _isSelected;
        private bool _canSelect;

        public AssignableWartungsvertragMemberItem(int mitgliedId, string displayName, string? nachname, string? vorname, string gartenNummern, bool isAlreadyAssigned, Action selectionChanged)
        {
            MitgliedId = mitgliedId;
            DisplayName = displayName;
            SortNachname = string.IsNullOrWhiteSpace(nachname) ? displayName : nachname.Trim();
            SortVorname = string.IsNullOrWhiteSpace(vorname) ? string.Empty : vorname.Trim();
            GartenNummern = gartenNummern;
            IsAlreadyAssigned = isAlreadyAssigned;
            _selectionChanged = selectionChanged;
            _canSelect = !isAlreadyAssigned;
        }

        public int MitgliedId { get; }
        public string DisplayName { get; }
        public string SortNachname { get; }
        public string SortVorname { get; }
        public string GartenNummern { get; }
        public bool IsAlreadyAssigned { get; }
        public string StatusText => IsAlreadyAssigned ? "Bereits aktiv zugeordnet" : string.IsNullOrWhiteSpace(GartenNummern) ? "Kein aktiver Garten" : $"Gärten: {GartenNummern}";

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (IsAlreadyAssigned)
                    value = false;

                if (SetProperty(ref _isSelected, value))
                    _selectionChanged();
            }
        }

        public bool CanSelect
        {
            get => _canSelect;
            set => SetProperty(ref _canSelect, value);
        }
    }
}
