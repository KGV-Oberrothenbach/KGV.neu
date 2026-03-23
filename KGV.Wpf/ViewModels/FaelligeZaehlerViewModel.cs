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
    public sealed class FaelligeZaehlerViewModel : BaseViewModel, INavigationAware
    {
        private readonly ISupabaseService _supabaseService;
        private readonly MainWindowViewModel _mainVm;
        private readonly List<ZaehlerEichstatusRecord> _allItems = new();
        private string _filterText = string.Empty;
        private string _selectedStatusFilter;
        private string _statusMessage = string.Empty;
        private bool _isBusy;

        public ObservableCollection<ZaehlerEichstatusRecord> Items { get; } = new();
        public ObservableCollection<string> StatusFilters { get; } = new();

        public string Title => "Fällige Zähler";
        public string Description => "Übersicht zu Eichfälligkeit und Status der aktiven Zähler auf Basis von `v_zaehler_eichstatus`.";
        public bool IsAuthorized => _mainVm.UserContext.Role is UserRole.Admin or UserRole.Vorstand;
        public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);
        public bool HasItems => Items.Count > 0;
        public bool HasEmptyState => !IsBusy && Items.Count == 0;
        public string EmptyStateMessage => _allItems.Count == 0
            ? "Aktuell wurden keine Zählerdaten aus `v_zaehler_eichstatus` geladen."
            : "Keine Zähler passen auf den aktuellen Filter.";

        public RelayCommand<object?> RefreshCommand { get; }

        public FaelligeZaehlerViewModel(ISupabaseService supabaseService, MainWindowViewModel mainVm)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
            _mainVm = mainVm ?? throw new ArgumentNullException(nameof(mainVm));

            StatusFilters.Add("Alle Status");
            StatusFilters.Add("Überfällig");
            StatusFilters.Add("Bald fällig");
            StatusFilters.Add("OK");
            _selectedStatusFilter = StatusFilters[0];

            RefreshCommand = new RelayCommand<object?>(_ => _ = LoadAsync(), _ => !IsBusy);
        }

        public string FilterText
        {
            get => _filterText;
            set
            {
                if (SetProperty(ref _filterText, value))
                    ApplyFilter();
            }
        }

        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                if (SetProperty(ref _selectedStatusFilter, value))
                    ApplyFilter();
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
                    OnPropertyChanged(nameof(HasEmptyState));
                    RefreshCommand.RaiseCanExecuteChanged();
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

            await LoadAsync();
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private async Task LoadAsync()
        {
            IsBusy = true;
            try
            {
                StatusMessage = string.Empty;
                var items = await _supabaseService.GetZaehlerEichstatusAsync();

                _allItems.Clear();
                _allItems.AddRange(items);
                ApplyFilter();

                StatusMessage = _allItems.Count == 0
                    ? "Keine Zählerdaten gefunden."
                    : $"{_allItems.Count} Zähler geladen.";
            }
            catch (Exception ex)
            {
                _allItems.Clear();
                Items.Clear();
                OnPropertyChanged(nameof(HasItems));
                OnPropertyChanged(nameof(HasEmptyState));
                StatusMessage = $"Zählerdaten konnten nicht geladen werden: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ApplyFilter()
        {
            var filtered = _allItems
                .Where(MatchesStatusFilter)
                .Where(MatchesTextFilter)
                .ToList();

            Items.Clear();
            foreach (var item in filtered)
                Items.Add(item);

            OnPropertyChanged(nameof(HasItems));
            OnPropertyChanged(nameof(HasEmptyState));
            OnPropertyChanged(nameof(EmptyStateMessage));
        }

        private bool MatchesStatusFilter(ZaehlerEichstatusRecord item)
        {
            return SelectedStatusFilter switch
            {
                "Überfällig" => item.EichstatusDisplay == "Überfällig",
                "Bald fällig" => item.EichstatusDisplay == "Bald fällig",
                "OK" => item.EichstatusDisplay == "OK",
                _ => true
            };
        }

        private bool MatchesTextFilter(ZaehlerEichstatusRecord item)
        {
            if (string.IsNullOrWhiteSpace(FilterText))
                return true;

            return item.SearchText.Contains(FilterText.Trim(), StringComparison.CurrentCultureIgnoreCase);
        }
    }
}
