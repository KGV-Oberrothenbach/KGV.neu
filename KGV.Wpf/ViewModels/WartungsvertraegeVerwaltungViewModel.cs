using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Helpers;

namespace KGV.ViewModels
{
    public sealed class WartungsvertraegeVerwaltungViewModel : BaseViewModel, INavigationAware
    {
        private readonly ISupabaseService _supabaseService;
        private readonly MainWindowViewModel _mainVm;
        private WartungsvertragOverviewItem? _selectedItem;
        private string _statusMessage = string.Empty;
        private bool _isBusy;

        public WartungsvertraegeVerwaltungViewModel(ISupabaseService supabaseService, MainWindowViewModel mainVm)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
            _mainVm = mainVm ?? throw new ArgumentNullException(nameof(mainVm));

            RefreshCommand = new RelayCommand<object?>(_ => _ = LoadAsync(), _ => !IsBusy);
            OpenCommand = new RelayCommand<object?>(_ => _ = OpenSelectedAsync(), _ => SelectedItem != null && !IsBusy);
        }

        public ObservableCollection<WartungsvertragOverviewItem> Items { get; } = new();
        public string Title => "Wartungsverträge";
        public string Description => "Globale ReadOnly-Übersicht der Wartungsverträge mit Kontingent, Belegung und produktivem Wechsel in eine eigene Detailansicht.";
        public bool IsAuthorized => _mainVm.UserContext.Role is UserRole.Admin or UserRole.Vorstand;
        public bool HasItems => Items.Count > 0;
        public bool HasEmptyState => !IsBusy && Items.Count == 0;
        public string EmptyStateMessage => "Aktuell liegen keine belastbaren Wartungsverträge vor.";

        public WartungsvertragOverviewItem? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                    OpenCommand.RaiseCanExecuteChanged();
            }
        }

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
                    OnPropertyChanged(nameof(HasEmptyState));
                    RefreshCommand.RaiseCanExecuteChanged();
                    OpenCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public RelayCommand<object?> RefreshCommand { get; }
        public RelayCommand<object?> OpenCommand { get; }

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
            if (IsBusy)
                return;

            IsBusy = true;
            try
            {
                StatusMessage = string.Empty;
                var items = await _supabaseService.GetWartungsvertraegeOverviewAsync();

                Items.Clear();
                foreach (var item in items)
                    Items.Add(item);

                SelectedItem = null;
                OnPropertyChanged(nameof(HasItems));
                OnPropertyChanged(nameof(HasEmptyState));
                StatusMessage = items.Count == 0
                    ? "Keine Wartungsverträge gefunden."
                    : $"{items.Count} Wartungsvertrag/-verträge geladen.";
            }
            catch (Exception ex)
            {
                Items.Clear();
                SelectedItem = null;
                OnPropertyChanged(nameof(HasItems));
                OnPropertyChanged(nameof(HasEmptyState));
                StatusMessage = $"Wartungsverträge konnten nicht geladen werden: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task OpenSelectedAsync()
        {
            if (SelectedItem == null)
                return;

            await _mainVm.NavigateToAsync(new WartungsvertragDetailViewModel(_supabaseService, _mainVm, SelectedItem.Id, this));
        }
    }
}
