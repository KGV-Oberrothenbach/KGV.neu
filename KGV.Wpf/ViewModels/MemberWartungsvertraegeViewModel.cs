using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Helpers;

namespace KGV.ViewModels
{
    public sealed class MemberWartungsvertraegeViewModel : BaseViewModel, INavigationAware
    {
        private readonly ISupabaseService _supabaseService;
        private readonly MainWindowViewModel _mainVm;
        private MemberDTO _member;
        private MemberWartungsvertragItem? _selectedItem;
        private string _statusMessage = string.Empty;
        private bool _isBusy;

        public MemberWartungsvertraegeViewModel(ISupabaseService supabaseService, MainWindowViewModel mainVm, MemberDTO member)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
            _mainVm = mainVm ?? throw new ArgumentNullException(nameof(mainVm));
            _member = member?.Clone() ?? throw new ArgumentNullException(nameof(member));

            RefreshCommand = new RelayCommand<object?>(_ => _ = LoadAsync(), _ => !IsBusy);
            OpenCommand = new RelayCommand<object?>(_ => _ = OpenSelectedAsync(), _ => SelectedItem != null && !IsBusy);
        }

        public ObservableCollection<MemberWartungsvertragItem> Items { get; } = new();
        public string Title => "↳ Wartungsverträge";
        public string Description => string.IsNullOrWhiteSpace(_member.DisplayName)
            ? "Aktive Wartungsverträge des ausgewählten Mitglieds."
            : $"Aktive Wartungsverträge von {_member.DisplayName}.";
        public bool HasItems => Items.Count > 0;
        public bool HasEmptyState => !IsBusy && Items.Count == 0;
        public string EmptyStateMessage => "Für dieses Mitglied liegen aktuell keine aktiven Wartungsvertragszuordnungen vor.";

        public MemberWartungsvertragItem? SelectedItem
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

        public Task OnNavigatedToAsync() => LoadAsync();
        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private async Task LoadAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;
            try
            {
                StatusMessage = string.Empty;
                var loadedMember = await _supabaseService.GetMitgliedByIdAsync(_member.Id);
                if (loadedMember != null)
                {
                    _member = new MemberDTO
                    {
                        Id = loadedMember.Id,
                        Vorname = loadedMember.Vorname ?? string.Empty,
                        Nachname = loadedMember.Name ?? string.Empty,
                        Email = loadedMember.Email ?? string.Empty,
                        Role = loadedMember.Role ?? string.Empty
                    };
                    OnPropertyChanged(nameof(Description));
                }

                var items = await _supabaseService.GetWartungsvertraegeForMitgliedAsync(_member.Id);

                Items.Clear();
                foreach (var item in items)
                    Items.Add(item);

                SelectedItem = null;
                OnPropertyChanged(nameof(HasItems));
                OnPropertyChanged(nameof(HasEmptyState));
                StatusMessage = items.Count == 0
                    ? "Keine aktiven Wartungsverträge gefunden."
                    : $"{items.Count} aktive Wartungsvertragszuordnung(en) geladen.";
            }
            catch (Exception ex)
            {
                Items.Clear();
                SelectedItem = null;
                OnPropertyChanged(nameof(HasItems));
                OnPropertyChanged(nameof(HasEmptyState));
                StatusMessage = $"Mitgliedsbezogene Wartungsverträge konnten nicht geladen werden: {ex.Message}";
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
