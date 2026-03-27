using System;
using System.Threading.Tasks;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Helpers;

namespace KGV.ViewModels
{
    public sealed class WartungsvertragDetailViewModel : BaseViewModel, INavigationAware
    {
        private readonly ISupabaseService _supabaseService;
        private readonly MainWindowViewModel _mainVm;
        private readonly long _wartungsvertragId;
        private readonly BaseViewModel? _backTarget;
        private WartungsvertragDetailItem? _detail;
        private string _statusMessage = string.Empty;
        private bool _isBusy;

        public WartungsvertragDetailViewModel(ISupabaseService supabaseService, MainWindowViewModel mainVm, long wartungsvertragId, BaseViewModel? backTarget = null)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
            _mainVm = mainVm ?? throw new ArgumentNullException(nameof(mainVm));
            _wartungsvertragId = wartungsvertragId;
            _backTarget = backTarget;

            RefreshCommand = new RelayCommand<object?>(_ => _ = LoadAsync(), _ => !IsBusy);
            BackCommand = new RelayCommand<object?>(_ => _ = NavigateBackAsync(), _ => _backTarget != null && !IsBusy);
        }

        public string PageTitle => "Wartungsvertrag";
        public string Description => "ReadOnly-Detailansicht eines Wartungsvertrags mit Kontingent, Belegung und den aktuell zugeordneten Mitgliedern.";
        public WartungsvertragDetailItem? Detail
        {
            get => _detail;
            private set
            {
                if (SetProperty(ref _detail, value))
                {
                    OnPropertyChanged(nameof(HasDetail));
                    OnPropertyChanged(nameof(HasAssignedMembers));
                }
            }
        }

        public bool HasDetail => Detail != null;
        public bool HasAssignedMembers => Detail?.ZugeordneteMitglieder?.Count > 0;
        public string EmptyMembersMessage => "Aktuell sind keine aktiven Mitgliedszuordnungen vorhanden.";

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
                    BackCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public bool HasBackTarget => _backTarget != null;
        public RelayCommand<object?> RefreshCommand { get; }
        public RelayCommand<object?> BackCommand { get; }

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
                Detail = await _supabaseService.GetWartungsvertragDetailAsync(_wartungsvertragId);
                StatusMessage = Detail == null
                    ? "Der ausgewählte Wartungsvertrag konnte nicht geladen werden."
                    : $"{Detail.ZugeordneteMitglieder.Count} aktive Zuordnung(en) geladen.";
            }
            catch (Exception ex)
            {
                Detail = null;
                StatusMessage = $"Wartungsvertragsdetails konnten nicht geladen werden: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task NavigateBackAsync()
        {
            if (_backTarget == null)
                return;

            await _mainVm.NavigateToAsync(_backTarget);
        }
    }
}
