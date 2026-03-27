using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Helpers;
using System.Windows;

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
        private bool _isAssignMode;
        private DateTime _gueltigAb = DateTime.Today;

        public MemberWartungsvertraegeViewModel(ISupabaseService supabaseService, MainWindowViewModel mainVm, MemberDTO member)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
            _mainVm = mainVm ?? throw new ArgumentNullException(nameof(mainVm));
            _member = member?.Clone() ?? throw new ArgumentNullException(nameof(member));

            RefreshCommand = new RelayCommand<object?>(_ => _ = LoadAsync(), _ => !IsBusy);
            OpenCommand = new RelayCommand<object?>(_ => _ = OpenSelectedAsync(), _ => SelectedItem != null && !IsBusy && !IsAssignMode);
            StartAssignCommand = new RelayCommand<object?>(_ => _ = BeginAssignAsync(), _ => CanManageMemberAssignments && !IsBusy && !IsAssignMode);
            SaveAssignCommand = new RelayCommand<object?>(_ => _ = SaveAssignmentsAsync(), _ => CanManageMemberAssignments && !IsBusy && IsAssignMode && AssignableItems.Any(x => x.IsSelected));
            CancelAssignCommand = new RelayCommand<object?>(_ => CancelAssignMode(), _ => !IsBusy && IsAssignMode);
            EndAssignmentCommand = new RelayCommand<object?>(parameter => _ = EndAssignmentAsync(parameter as MemberWartungsvertragItem), _ => CanManageMemberAssignments && !IsBusy);
        }

        public ObservableCollection<MemberWartungsvertragItem> Items { get; } = new();
        public ObservableCollection<SelectableWartungsvertragItem> AssignableItems { get; } = new();
        public string Title => "↳ Wartungsverträge";
        public string Description => string.IsNullOrWhiteSpace(_member.DisplayName)
            ? "Aktive Wartungsverträge des ausgewählten Mitglieds."
            : $"Aktive Wartungsverträge von {_member.DisplayName}.";
        public bool HasItems => Items.Count > 0;
        public bool HasEmptyState => !IsBusy && Items.Count == 0;
        public string EmptyStateMessage => "Für dieses Mitglied liegen aktuell keine aktiven Wartungsvertragszuordnungen vor.";
        public bool HasAssignableItems => AssignableItems.Count > 0;
        public bool CanManageMemberAssignments => _mainVm.UserContext.Has(PermissionFlags.CanEditAllMembers);

        public bool IsAssignMode
        {
            get => _isAssignMode;
            private set
            {
                if (SetProperty(ref _isAssignMode, value))
                {
                    OnPropertyChanged(nameof(HasAssignableItems));
                    OpenCommand.RaiseCanExecuteChanged();
                    StartAssignCommand.RaiseCanExecuteChanged();
                    SaveAssignCommand.RaiseCanExecuteChanged();
                    CancelAssignCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public DateTime GueltigAb
        {
            get => _gueltigAb;
            set => SetProperty(ref _gueltigAb, value.Date);
        }

        public MemberWartungsvertragItem? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                {
                    OpenCommand.RaiseCanExecuteChanged();
                    EndAssignmentCommand.RaiseCanExecuteChanged();
                }
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
                    StartAssignCommand.RaiseCanExecuteChanged();
                    SaveAssignCommand.RaiseCanExecuteChanged();
                    CancelAssignCommand.RaiseCanExecuteChanged();
                    EndAssignmentCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public RelayCommand<object?> RefreshCommand { get; }
        public RelayCommand<object?> OpenCommand { get; }
        public RelayCommand<object?> StartAssignCommand { get; }
        public RelayCommand<object?> SaveAssignCommand { get; }
        public RelayCommand<object?> CancelAssignCommand { get; }
        public RelayCommand<object?> EndAssignmentCommand { get; }

        public Task OnNavigatedToAsync() => LoadAsync();
        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private async Task LoadAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;
            try
            {
                StatusMessage = "Wartungsverträge werden geladen.";
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
                if (!IsAssignMode)
                    AssignableItems.Clear();
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

        private async Task BeginAssignAsync()
        {
            if (!CanManageMemberAssignments || IsBusy)
                return;

            IsBusy = true;
            try
            {
                StatusMessage = "Wartungsverträge werden geladen.";
                var assignableContracts = await _supabaseService.GetAssignableWartungsvertraegeForMitgliedAsync(_member.Id);

                AssignableItems.Clear();
                foreach (var item in assignableContracts)
                    AssignableItems.Add(new SelectableWartungsvertragItem(item, UpdateAssignableSelectionState));

                IsAssignMode = true;
                OnPropertyChanged(nameof(HasAssignableItems));
                StatusMessage = assignableContracts.Count == 0
                    ? "Für dieses Mitglied sind aktuell keine freien zusätzlichen Wartungsverträge verfügbar."
                    : string.Empty;
                UpdateAssignableSelectionState();
            }
            catch (Exception ex)
            {
                AssignableItems.Clear();
                IsAssignMode = true;
                StatusMessage = $"Zuweisbare Wartungsverträge konnten nicht geladen werden: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SaveAssignmentsAsync()
        {
            if (!CanManageMemberAssignments || IsBusy)
                return;

            var selectedIds = AssignableItems
                .Where(x => x.IsSelected)
                .Select(x => x.Id)
                .ToList();
            if (selectedIds.Count == 0)
            {
                StatusMessage = "Bitte mindestens einen Wartungsvertrag auswählen.";
                return;
            }

            IsBusy = true;
            try
            {
                StatusMessage = "Zuordnung wird gespeichert.";
                var result = await _supabaseService.AssignWartungsvertraegeToMitgliedAsync(_member.Id, GueltigAb, selectedIds);
                if (!result.Success)
                {
                    StatusMessage = result.Message;
                    return;
                }

                CancelAssignMode();
                IsBusy = false;
                await LoadAsync();
                StatusMessage = result.Message;
                return;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Zuordnung konnte nicht gespeichert werden: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void CancelAssignMode()
        {
            AssignableItems.Clear();
            IsAssignMode = false;
            OnPropertyChanged(nameof(HasAssignableItems));
            StatusMessage = string.Empty;
            UpdateAssignableSelectionState();
        }

        private async Task EndAssignmentAsync(MemberWartungsvertragItem? item)
        {
            if (!CanManageMemberAssignments || IsBusy || item?.ZuordnungId <= 0)
                return;

            var assignment = item!;
            var confirmed = MessageBox.Show(
                $"Die aktive Zuordnung von '{assignment.Titel}' für dieses Mitglied wird beendet. Fortfahren?",
                "Wartungsvertrag beenden",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes;

            if (!confirmed)
                return;

            IsBusy = true;
            try
            {
                StatusMessage = "Zuordnung wird beendet.";
                var success = await _supabaseService.EndWartungsvertragZuordnungAsync(assignment.ZuordnungId, DateTime.Today);
                if (!success)
                {
                    StatusMessage = "Die aktive Zuordnung konnte nicht beendet werden.";
                    return;
                }

                IsBusy = false;
                await LoadAsync();
                StatusMessage = "Zuordnung wurde beendet.";
                return;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Zuordnung konnte nicht beendet werden: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void UpdateAssignableSelectionState()
        {
            SaveAssignCommand.RaiseCanExecuteChanged();
        }

        public sealed class SelectableWartungsvertragItem : BaseViewModel
        {
            private readonly Action _selectionChanged;
            private bool _isSelected;

            public SelectableWartungsvertragItem(WartungsvertragOverviewItem source, Action selectionChanged)
            {
                Id = source.Id;
                Titel = source.Titel;
                Kurzbeschreibung = source.Kurzbeschreibung;
                MaxKontingent = source.MaxKontingent;
                Belegt = source.Belegt;
                Frei = source.Frei;
                _selectionChanged = selectionChanged;
            }

            public long Id { get; }
            public string Titel { get; }
            public string Kurzbeschreibung { get; }
            public int MaxKontingent { get; }
            public int Belegt { get; }
            public int Frei { get; }
            public string BelegungText => $"{Belegt} von {MaxKontingent} belegt · {Frei} frei";

            public bool IsSelected
            {
                get => _isSelected;
                set
                {
                    if (SetProperty(ref _isSelected, value))
                        _selectionChanged();
                }
            }
        }
    }
}
