using System;
using System.Globalization;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Helpers;

namespace KGV.ViewModels
{
    public sealed class AblesungenFreigabeViewModel : BaseViewModel, INavigationAware
    {
        private readonly ISupabaseService _supabaseService;
        private readonly MainWindowViewModel _mainWindowViewModel;

        private AblesungReviewItem? _selectedItem;
        private string _reviewComment = string.Empty;
        private DateTime _editAblesedatum = DateTime.Today;
        private string _editStandText = string.Empty;
        private string _statusMessage = string.Empty;
        private bool _isBusy;

        public ObservableCollection<AblesungReviewItem> Items { get; } = new();

        public string Title => "Eingereichte Ablesungen prüfen";
        public string Description => "Offene Einreichungen werden zentral über den Shared-Service geladen. Entscheidungen erfordern immer einen Prüfkommentar.";
        public bool HasItems => Items.Count > 0;
        public bool HasSelection => SelectedItem != null;
        public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);
        public bool ShowEmptyState => !IsBusy && !HasItems;
        public string EmptyText => "Aktuell liegen keine eingereichten Ablesungen zur Prüfung vor.";

        public AblesungReviewItem? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (!SetProperty(ref _selectedItem, value))
                    return;

                ReviewComment = value?.Pruefkommentar ?? string.Empty;
                EditAblesedatum = value?.Ablesedatum.Date ?? DateTime.Today;
                EditStandText = value != null
                    ? value.Stand.ToString("0.##", CultureInfo.CurrentCulture)
                    : string.Empty;
                OnPropertyChanged(nameof(HasSelection));
                FreigebenCommand.RaiseCanExecuteChanged();
                AblehnenCommand.RaiseCanExecuteChanged();
                KorrigierenCommand.RaiseCanExecuteChanged();
                LoeschenCommand.RaiseCanExecuteChanged();
            }
        }

        public string ReviewComment
        {
            get => _reviewComment;
            set => SetProperty(ref _reviewComment, value);
        }

        public DateTime EditAblesedatum
        {
            get => _editAblesedatum;
            set => SetProperty(ref _editAblesedatum, value);
        }

        public string EditStandText
        {
            get => _editStandText;
            set => SetProperty(ref _editStandText, value);
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
                if (!SetProperty(ref _isBusy, value))
                    return;

                OnPropertyChanged(nameof(ShowEmptyState));
                AktualisierenCommand.RaiseCanExecuteChanged();
                FreigebenCommand.RaiseCanExecuteChanged();
                AblehnenCommand.RaiseCanExecuteChanged();
                KorrigierenCommand.RaiseCanExecuteChanged();
                LoeschenCommand.RaiseCanExecuteChanged();
            }
        }

        public RelayCommand<object?> AktualisierenCommand { get; }
        public RelayCommand<object?> FreigebenCommand { get; }
        public RelayCommand<object?> AblehnenCommand { get; }
        public RelayCommand<object?> KorrigierenCommand { get; }
        public RelayCommand<object?> LoeschenCommand { get; }

        public AblesungenFreigabeViewModel(ISupabaseService supabaseService, MainWindowViewModel mainWindowViewModel)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
            _mainWindowViewModel = mainWindowViewModel ?? throw new ArgumentNullException(nameof(mainWindowViewModel));

            AktualisierenCommand = new RelayCommand<object?>(_ => _ = LoadAsync(), _ => !IsBusy);
            FreigebenCommand = new RelayCommand<object?>(_ => _ = EntscheidenAsync(AblesungPruefstatus.Freigegeben), _ => SelectedItem != null && !IsBusy);
            AblehnenCommand = new RelayCommand<object?>(_ => _ = EntscheidenAsync(AblesungPruefstatus.Abgelehnt), _ => SelectedItem != null && !IsBusy);
            KorrigierenCommand = new RelayCommand<object?>(_ => _ = KorrigierenAsync(), _ => SelectedItem != null && !IsBusy);
            LoeschenCommand = new RelayCommand<object?>(_ => _ = LoeschenAsync(), _ => SelectedItem != null && !IsBusy);
        }

        public Task OnNavigatedToAsync() => LoadAsync();

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private async Task LoadAsync()
        {
            if (IsBusy)
                return;

            if (!_mainWindowViewModel.UserContext.Has(KGV.Core.Security.PermissionFlags.CanApproveMeterReadings))
            {
                Items.Clear();
                SelectedItem = null;
                StatusMessage = "Mit den aktuellen Rechten ist keine Ablesungsfreigabe möglich.";
                OnPropertyChanged(nameof(HasItems));
                OnPropertyChanged(nameof(ShowEmptyState));
                return;
            }

            IsBusy = true;
            StatusMessage = string.Empty;
            var selectedId = SelectedItem?.AblesungId;

            try
            {
                var items = await _supabaseService.GetOffeneAblesungenZurFreigabeAsync();
                Items.Clear();
                foreach (var item in items)
                    Items.Add(item);

                SelectedItem = selectedId.HasValue
                    ? Items.FirstOrDefault(x => x.AblesungId == selectedId.Value) ?? Items.FirstOrDefault()
                    : Items.FirstOrDefault();

                if (Items.Count == 0)
                    StatusMessage = "Aktuell liegen keine eingereichten Ablesungen zur Prüfung vor.";
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
            finally
            {
                OnPropertyChanged(nameof(HasItems));
                OnPropertyChanged(nameof(ShowEmptyState));
                IsBusy = false;
            }
        }

        private async Task KorrigierenAsync()
        {
            var selected = SelectedItem;
            if (selected == null || IsBusy)
                return;

            var approverId = ResolveApproverMitgliedId();
            if (!approverId.HasValue)
            {
                StatusMessage = "Korrektur ist nur mit gültigem Mitgliedskontext möglich.";
                return;
            }

            var kommentar = ReviewComment?.Trim();
            if (string.IsNullOrWhiteSpace(kommentar))
            {
                StatusMessage = "Bitte einen Korrekturkommentar eingeben, bevor die Ablesung korrigiert wird.";
                return;
            }

            if (!TryParseStand(EditStandText, out var stand))
            {
                StatusMessage = "Bitte einen gültigen Zählerstand für die Korrektur eingeben.";
                return;
            }

            if (MessageBox.Show(
                    "Die eingereichte Ablesung wird mit den geänderten Werten korrigiert und direkt freigegeben. Fortfahren?",
                    "Ablesung korrigieren",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            IsBusy = true;
            StatusMessage = string.Empty;

            try
            {
                var ok = await _supabaseService.CorrectAblesungImPruefprozessAsync(
                    selected.AblesungId,
                    EditAblesedatum.Date,
                    stand,
                    kommentar,
                    approverId.Value,
                    DateTime.UtcNow);

                StatusMessage = ok
                    ? "Ablesung wurde korrigiert und direkt freigegeben."
                    : "Die Korrektur konnte nicht gespeichert werden.";

                await LoadAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoeschenAsync()
        {
            var selected = SelectedItem;
            if (selected == null || IsBusy)
                return;

            var approverId = ResolveApproverMitgliedId();
            if (!approverId.HasValue)
            {
                StatusMessage = "Löschen ist nur mit gültigem Mitgliedskontext möglich.";
                return;
            }

            var begruendung = ReviewComment?.Trim();
            if (string.IsNullOrWhiteSpace(begruendung))
            {
                StatusMessage = "Bitte eine Löschbegründung eingeben, bevor die Ablesung entfernt wird.";
                return;
            }

            if (MessageBox.Show(
                    "Die Ablesung wird mit Begründung aus dem aktiven Prüfprozess entfernt. Fortfahren?",
                    "Ablesung löschen",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }

            IsBusy = true;
            StatusMessage = string.Empty;

            try
            {
                var ok = await _supabaseService.RemoveAblesungImPruefprozessAsync(
                    selected.AblesungId,
                    begruendung,
                    approverId.Value,
                    DateTime.UtcNow);

                StatusMessage = ok
                    ? "Ablesung wurde aus dem aktiven Prüfprozess entfernt."
                    : "Die Ablesung konnte nicht aus dem Prüfprozess entfernt werden.";

                await LoadAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task EntscheidenAsync(string pruefstatus)
        {
            var selected = SelectedItem;
            if (selected == null || IsBusy)
                return;

            var approverId = ResolveApproverMitgliedId();
            if (!approverId.HasValue)
            {
                StatusMessage = "Freigabe oder Ablehnung ist nur mit gültigem Mitgliedskontext möglich.";
                return;
            }

            var kommentar = ReviewComment?.Trim();
            if (string.IsNullOrWhiteSpace(kommentar))
            {
                StatusMessage = "Bitte einen Prüfkommentar eingeben, bevor die Entscheidung gespeichert wird.";
                return;
            }

            IsBusy = true;
            StatusMessage = string.Empty;

            try
            {
                var ok = await _supabaseService.UpdateAblesungPruefstatusAsync(
                    selected.AblesungId,
                    pruefstatus,
                    kommentar,
                    approverId,
                    DateTime.UtcNow);

                StatusMessage = ok
                    ? (pruefstatus == AblesungPruefstatus.Freigegeben
                        ? "Ablesung wurde freigegeben."
                        : "Ablesung wurde abgelehnt.")
                    : "Die Entscheidung konnte nicht gespeichert werden.";

                await LoadAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private int? ResolveApproverMitgliedId()
        {
            var mitgliedId = _mainWindowViewModel.UserContext.MitgliedId;
            if (!mitgliedId.HasValue || mitgliedId.Value <= 0 || mitgliedId.Value > int.MaxValue)
                return null;

            return (int)mitgliedId.Value;
        }

        private static bool TryParseStand(string? input, out decimal stand)
        {
            var normalized = input?.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                stand = 0;
                return false;
            }

            var styles = NumberStyles.Number;
            if (decimal.TryParse(normalized, styles, CultureInfo.CurrentCulture, out stand))
                return stand >= 0;

            if (decimal.TryParse(normalized, styles, CultureInfo.InvariantCulture, out stand))
                return stand >= 0;

            stand = 0;
            return false;
        }
    }
}
