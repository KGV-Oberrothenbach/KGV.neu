using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Helpers;
using KGV.Messages;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace KGV.ViewModels
{
    public sealed class ArbeitsstundenPruefungViewModel : BaseViewModel, INavigationAware
    {
        private const int LockTimeoutMinutes = 10;
        private static readonly TimeSpan LockHeartbeatInterval = TimeSpan.FromMinutes(3);

        private readonly ISupabaseService _supabaseService;
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly DispatcherTimer _lockHeartbeatTimer;

        private bool _lockAcquired;
        private bool _isBusy;
        private bool _isLoadingHistory;
        private string? _currentUserId;
        private PruefungseintragItem? _selectedEntry;
        private string _reviewComment = string.Empty;
        private bool _isReviewCommentInvalid;
        private DateTime? _correctionDate;
        private string _correctionHoursText = string.Empty;
        private string _correctionWorkType = string.Empty;
        private ObservableCollection<ArbeitsstundenPruefverlaufItem> _verlauf = new();

        public ObservableCollection<PruefungseintragItem> OffenePruefungen { get; } = new();

        public string Title => "Arbeitsstunden freigeben";
        public string EmptyText => "Aktuell liegen keine offenen Arbeitsstunden zur Prüfung vor.";
        public bool HasEntries => OffenePruefungen.Count > 0;
        public bool ShowEmptyState => _lockAcquired && !HasEntries;
        public bool ShowTable => _lockAcquired && HasEntries;
        public bool ShowLockMessage => !string.IsNullOrWhiteSpace(LockMessage);
        public bool ShowStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);
        public bool ShowActionPanel => _lockAcquired && SelectedEntry != null;
        public bool ShowHistory => ShowActionPanel && Verlauf.Count > 0;
        public bool ShowHistoryEmptyState => ShowActionPanel && !_isLoadingHistory && Verlauf.Count == 0;
        public bool ShowHistoryLoading => ShowActionPanel && _isLoadingHistory;
        public bool IsBusy => _isBusy;

        private string _lockMessage = string.Empty;
        public string LockMessage
        {
            get => _lockMessage;
            private set
            {
                if (SetProperty(ref _lockMessage, value))
                    OnPropertyChanged(nameof(ShowLockMessage));
            }
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            private set
            {
                if (SetProperty(ref _statusMessage, value))
                    OnPropertyChanged(nameof(ShowStatusMessage));
            }
        }

        public PruefungseintragItem? SelectedEntry
        {
            get => _selectedEntry;
            set
            {
                if (!SetProperty(ref _selectedEntry, value))
                    return;

                OnPropertyChanged(nameof(ShowActionPanel));
                OnPropertyChanged(nameof(ShowHistory));
                OnPropertyChanged(nameof(ShowHistoryEmptyState));
                OnPropertyChanged(nameof(ShowHistoryLoading));
                _ = ApplySelectedEntryAsync();
            }
        }

        public string ReviewComment
        {
            get => _reviewComment;
            set
            {
                if (!SetProperty(ref _reviewComment, value))
                    return;

                IsReviewCommentInvalid = false;
                RaiseActionCommandStates();
            }
        }

        public bool IsReviewCommentInvalid
        {
            get => _isReviewCommentInvalid;
            private set => SetProperty(ref _isReviewCommentInvalid, value);
        }

        public DateTime? CorrectionDate
        {
            get => _correctionDate;
            set => SetProperty(ref _correctionDate, value);
        }

        public string CorrectionHoursText
        {
            get => _correctionHoursText;
            set => SetProperty(ref _correctionHoursText, value);
        }

        public string CorrectionWorkType
        {
            get => _correctionWorkType;
            set => SetProperty(ref _correctionWorkType, value);
        }

        public ObservableCollection<ArbeitsstundenPruefverlaufItem> Verlauf
        {
            get => _verlauf;
            private set
            {
                if (SetProperty(ref _verlauf, value))
                {
                    OnPropertyChanged(nameof(ShowHistory));
                    OnPropertyChanged(nameof(ShowHistoryEmptyState));
                }
            }
        }

        public RelayCommand<object?> AktualisierenCommand { get; }
        public RelayCommand<object?> FreigebenCommand { get; }
        public RelayCommand<object?> AblehnenCommand { get; }
        public RelayCommand<object?> KorrigierenCommand { get; }
        public RelayCommand<object?> LoeschenCommand { get; }

        public ArbeitsstundenPruefungViewModel(ISupabaseService supabaseService, MainWindowViewModel mainWindowViewModel)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
            _mainWindowViewModel = mainWindowViewModel ?? throw new ArgumentNullException(nameof(mainWindowViewModel));

            AktualisierenCommand = new RelayCommand<object?>(_ => _ = ReloadAsync());
            FreigebenCommand = new RelayCommand<object?>(_ => _ = FreigebenAsync(), _ => CanExecuteReviewAction());
            AblehnenCommand = new RelayCommand<object?>(_ => _ = AblehnenAsync(), _ => CanExecuteReviewAction());
            KorrigierenCommand = new RelayCommand<object?>(_ => _ = KorrigierenAsync(), _ => CanExecuteReviewAction());
            LoeschenCommand = new RelayCommand<object?>(_ => _ = LoeschenAsync(), _ => CanExecuteReviewAction());

            _lockHeartbeatTimer = new DispatcherTimer { Interval = LockHeartbeatInterval };
            _lockHeartbeatTimer.Tick += OnLockHeartbeat;
        }

        public async Task OnNavigatedToAsync()
        {
            _currentUserId = _mainWindowViewModel.AuthService.CurrentUserId;
            await ReloadAsync();
        }

        public async Task OnNavigatedFromAsync()
        {
            _lockHeartbeatTimer.Stop();
            if (_lockAcquired && !string.IsNullOrWhiteSpace(_currentUserId))
                await _supabaseService.ReleaseArbeitsstundenReviewLockAsync(_currentUserId);

            _lockAcquired = false;
        }

        private async Task ReloadAsync()
        {
            StatusMessage = string.Empty;
            LockMessage = string.Empty;

            var lockResult = await EnsureReviewLockAsync();
            if (!lockResult.Acquired)
            {
                OffenePruefungen.Clear();
                SelectedEntry = null;
                Verlauf.Clear();
                ReviewComment = string.Empty;
                RaiseCollectionStateChanged();
                RaiseActionCommandStates();
                return;
            }

            await LoadEntriesAsync(SelectedEntry?.Id);
        }

        private async Task<ArbeitsstundenReviewLockResult> EnsureReviewLockAsync()
        {
            if (string.IsNullOrWhiteSpace(_currentUserId))
            {
                _lockAcquired = false;
                LockMessage = "Die Prüfsperre konnte nicht gesetzt werden, weil keine aktuelle Benutzer-ID verfügbar ist.";
                return new ArbeitsstundenReviewLockResult();
            }

            var result = await _supabaseService.TryAcquireArbeitsstundenReviewLockAsync(_currentUserId, LockTimeoutMinutes);
            _lockAcquired = result.Acquired;

            if (_lockAcquired)
            {
                LockMessage = "Prüfsitzung aktiv. Offene Arbeitsstunden sind während dieser Sitzung global für andere Prüfer gesperrt. Hängende Sperren werden nach Timeout automatisch übersteuerbar.";
                StartLockHeartbeat();
            }
            else
            {
                _lockHeartbeatTimer.Stop();
                LockMessage = BuildForeignLockMessage(result);
            }

            OnPropertyChanged(nameof(ShowLockMessage));
            return result;
        }

        private async Task LoadEntriesAsync(int? selectedEntryId = null, int? preferredIndex = null)
        {
            var entries = await _supabaseService.GetOffeneArbeitsstundenZurFreigabeAsync();
            var prepared = entries
                .Where(x => x.IstOffenerPrueffall)
                .OrderBy(x => x.Datum)
                .ThenBy(x => x.Id)
                .Select(x => new PruefungseintragItem(x))
                .ToList();

            OffenePruefungen.Clear();
            foreach (var item in prepared)
                OffenePruefungen.Add(item);

            var nextSelection = ResolveSelection(prepared, selectedEntryId, preferredIndex);
            RaiseCollectionStateChanged();
            SelectedEntry = nextSelection;
        }

        private async Task ApplySelectedEntryAsync()
        {
            if (SelectedEntry == null)
            {
                ReviewComment = string.Empty;
                CorrectionDate = null;
                CorrectionHoursText = string.Empty;
                CorrectionWorkType = string.Empty;
                Verlauf.Clear();
                OnPropertyChanged(nameof(ShowHistory));
                OnPropertyChanged(nameof(ShowHistoryEmptyState));
                RaiseActionCommandStates();
                return;
            }

            ReviewComment = string.Empty;
            CorrectionDate = SelectedEntry.Datum.Date;
            CorrectionHoursText = SelectedEntry.Stunden.ToString("0.##", CultureInfo.CurrentCulture);
            CorrectionWorkType = SelectedEntry.ArtDerArbeit;
            await LoadHistoryAsync(SelectedEntry.Id);
            RaiseActionCommandStates();
        }

        private async Task LoadHistoryAsync(int arbeitsstundeId)
        {
            _isLoadingHistory = true;
            OnPropertyChanged(nameof(ShowHistoryLoading));
            OnPropertyChanged(nameof(ShowHistoryEmptyState));

            try
            {
                var items = await _supabaseService.GetArbeitsstundenPruefverlaufAsync(arbeitsstundeId);
                Verlauf.Clear();
                foreach (var item in items)
                    Verlauf.Add(item);
            }
            finally
            {
                _isLoadingHistory = false;
                OnPropertyChanged(nameof(ShowHistory));
                OnPropertyChanged(nameof(ShowHistoryLoading));
                OnPropertyChanged(nameof(ShowHistoryEmptyState));
            }
        }

        private async Task FreigebenAsync()
        {
            if (!TryGetReviewKommentar(out var kommentar) || !TryResolveApproverId(out var approverId))
                return;

            await ExecuteReviewActionAsync(
                async () => await _supabaseService.ApproveArbeitsstundeImPruefprozessAsync(SelectedEntry!.Id, kommentar, approverId),
                "Arbeitsstunde wurde freigegeben.");
        }

        private async Task AblehnenAsync()
        {
            if (!TryGetReviewKommentar(out var kommentar) || !TryResolveApproverId(out var approverId))
                return;

            await ExecuteReviewActionAsync(
                async () => await _supabaseService.RejectArbeitsstundeImPruefprozessAsync(SelectedEntry!.Id, kommentar, approverId),
                "Arbeitsstunde wurde abgelehnt und aus der offenen Prüfliste entfernt.");
        }

        private async Task KorrigierenAsync()
        {
            if (!TryGetReviewKommentar(out var kommentar) || !TryResolveApproverId(out var approverId))
                return;

            if (SelectedEntry == null)
                return;

            if (!CorrectionDate.HasValue)
            {
                StatusMessage = "Für die Korrektur ist ein Datum erforderlich.";
                return;
            }

            if (!TryParseHours(CorrectionHoursText, out var stunden) || stunden <= 0)
            {
                StatusMessage = "Für die Korrektur müssen Stunden größer als 0 angegeben werden.";
                return;
            }

            if (string.IsNullOrWhiteSpace(CorrectionWorkType))
            {
                StatusMessage = "Für die Korrektur ist die Art der Arbeit erforderlich.";
                return;
            }

            var request = new ArbeitsstundenPruefkorrekturRequest
            {
                ArbeitsstundeId = SelectedEntry.Id,
                Datum = CorrectionDate.Value.Date,
                Stunden = stunden,
                ArtDerArbeit = CorrectionWorkType.Trim(),
                Begruendung = kommentar,
                GeprueftVon = approverId
            };

            await ExecuteReviewActionAsync(
                async () => await _supabaseService.CorrectArbeitsstundeImPruefprozessAsync(request),
                "Arbeitsstunde wurde korrigiert, freigegeben und im Verlauf dokumentiert.");
        }

        private async Task LoeschenAsync()
        {
            if (!TryGetReviewKommentar(out var kommentar) || !TryResolveApproverId(out var approverId))
                return;

            if (SelectedEntry == null)
                return;

            var confirm = MessageBox.Show(
                $"Soll die Arbeitsstunde von {SelectedEntry.MitgliedDisplayName} wirklich im Prüfprozess gelöscht werden?",
                "Arbeitsstunde löschen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (confirm != MessageBoxResult.Yes)
                return;

            await ExecuteReviewActionAsync(
                async () => await _supabaseService.DeleteArbeitsstundeImPruefprozessAsync(SelectedEntry.Id, kommentar, approverId),
                "Arbeitsstunde wurde im Prüfprozess gelöscht. Der Verlauf bleibt nachvollziehbar erhalten.");
        }

        private async Task ExecuteReviewActionAsync(Func<Task<bool>> action, string successMessage)
        {
            if (SelectedEntry == null || _isBusy)
                return;

            var currentId = SelectedEntry.Id;
            var currentIndex = OffenePruefungen.IndexOf(SelectedEntry);
            SetBusy(true);
            StatusMessage = string.Empty;

            try
            {
                var success = await action();
                if (!success)
                {
                    StatusMessage = "Die Prüfaktion konnte nicht ausgeführt werden. Details stehen im Anwendungslog oder der Datensatz ist nicht mehr offen.";
                    return;
                }

                WeakReferenceMessenger.Default.Send(new ArbeitsstundenChangedMessage());
                ReviewComment = string.Empty;
                StatusMessage = successMessage;
                await LoadEntriesAsync(preferredIndex: currentIndex);
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
                await LoadEntriesAsync(selectedEntryId: currentId);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void StartLockHeartbeat()
        {
            _lockHeartbeatTimer.Stop();
            _lockHeartbeatTimer.Start();
        }

        private async void OnLockHeartbeat(object? sender, EventArgs e)
        {
            if (!_lockAcquired || string.IsNullOrWhiteSpace(_currentUserId))
                return;

            var ok = await _supabaseService.RefreshArbeitsstundenReviewLockAsync(_currentUserId, LockTimeoutMinutes);
            if (ok)
                return;

            _lockHeartbeatTimer.Stop();
            _lockAcquired = false;
            LockMessage = "Die globale Prüfsperre konnte nicht verlängert werden. Bitte die Seite neu öffnen, bevor weitere Aktionen ausgeführt werden.";
            StatusMessage = string.Empty;
            RaiseCollectionStateChanged();
            RaiseActionCommandStates();
        }

        private bool TryGetReviewKommentar(out string kommentar)
        {
            kommentar = ArbeitsstundenPruefprozess.NormalizeKommentar(ReviewComment);
            if (ArbeitsstundenPruefprozess.HasRequiredKommentar(kommentar))
                return true;

            IsReviewCommentInvalid = true;
            StatusMessage = "Für Freigeben, Ablehnen, Korrigieren und Löschen ist ein Prüfkommentar verpflichtend.";
            return false;
        }

        private bool TryResolveApproverId(out int approverId)
        {
            approverId = 0;
            var mitgliedId = _mainWindowViewModel.UserContext.MitgliedId;
            if (mitgliedId.HasValue && mitgliedId.Value > 0 && mitgliedId.Value <= int.MaxValue)
            {
                approverId = (int)mitgliedId.Value;
                return true;
            }

            StatusMessage = "Prüfaktion nicht möglich: aktueller Benutzer ist keinem Mitglied zugeordnet.";
            return false;
        }

        private bool TryParseHours(string? value, out decimal stunden)
        {
            var normalized = string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().Replace(',', '.');

            return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out stunden)
                   || decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.CurrentCulture, out stunden);
        }

        private bool CanExecuteReviewAction()
        {
            return _lockAcquired && SelectedEntry != null && !_isBusy;
        }

        private void SetBusy(bool value)
        {
            if (_isBusy == value)
                return;

            _isBusy = value;
            OnPropertyChanged(nameof(IsBusy));
            RaiseActionCommandStates();
        }

        private void RaiseCollectionStateChanged()
        {
            OnPropertyChanged(nameof(HasEntries));
            OnPropertyChanged(nameof(ShowEmptyState));
            OnPropertyChanged(nameof(ShowTable));
            OnPropertyChanged(nameof(ShowActionPanel));
        }

        private void RaiseActionCommandStates()
        {
            FreigebenCommand.RaiseCanExecuteChanged();
            AblehnenCommand.RaiseCanExecuteChanged();
            KorrigierenCommand.RaiseCanExecuteChanged();
            LoeschenCommand.RaiseCanExecuteChanged();
        }

        private static PruefungseintragItem? ResolveSelection(IReadOnlyList<PruefungseintragItem> entries, int? selectedEntryId, int? preferredIndex)
        {
            if (entries.Count == 0)
                return null;

            if (selectedEntryId.HasValue)
            {
                var exact = entries.FirstOrDefault(x => x.Id == selectedEntryId.Value);
                if (exact != null)
                    return exact;
            }

            if (preferredIndex.HasValue)
            {
                var clamped = Math.Clamp(preferredIndex.Value, 0, entries.Count - 1);
                return entries[clamped];
            }

            return entries[0];
        }

        private static string BuildForeignLockMessage(ArbeitsstundenReviewLockResult result)
        {
            var lockedBy = string.IsNullOrWhiteSpace(result.LockedByDisplayName)
                ? (!string.IsNullOrWhiteSpace(result.LockedByUserId) ? result.LockedByUserId : "einen anderen Prüfer")
                : result.LockedByDisplayName;

            if (result.LockedAt.HasValue)
                return $"Die Freigabeansicht ist aktuell global durch {lockedBy} gesperrt (seit {result.LockedAt.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.CurrentCulture)}). Bitte warte auf die Freigabe oder auf das Timeout einer hängenden Sitzung.";

            return $"Die Freigabeansicht ist aktuell global durch {lockedBy} gesperrt. Bitte warte auf die Freigabe oder auf das Timeout einer hängenden Sitzung.";
        }

        public sealed class PruefungseintragItem : BaseViewModel
        {
            public PruefungseintragItem(ArbeitsstundeDTO dto)
            {
                Id = dto.Id;
                MitgliedId = dto.MitgliedId;
                Datum = dto.Datum;
                SaisonId = dto.SaisonId;
                SaisonJahr = dto.SaisonJahr;
                Stunden = dto.Stunden;
                ArtDerArbeit = dto.Beschreibung ?? string.Empty;
                MitgliedDisplayName = BuildDisplayName(dto.Nachname, dto.Vorname);
                PruefstatusDisplay = dto.PruefstatusDisplay;
                FreigegebenAm = dto.FreigegebenAm;
                FreigegebenVonName = dto.FreigegebenVonName;
            }

            public int Id { get; }
            public int MitgliedId { get; }
            public DateTime Datum { get; }
            public int SaisonId { get; }
            public int SaisonJahr { get; }
            public decimal Stunden { get; }
            public string ArtDerArbeit { get; }
            public string MitgliedDisplayName { get; }
            public string PruefstatusDisplay { get; }
            public DateTime? FreigegebenAm { get; }
            public string? FreigegebenVonName { get; }
            public string FreigabeInfo => FreigegebenAm.HasValue
                ? $"Freigegeben am {FreigegebenAm.Value:dd.MM.yyyy HH:mm}{(string.IsNullOrWhiteSpace(FreigegebenVonName) ? string.Empty : $" · {FreigegebenVonName}")}"
                : "Offener Prüffall";

            private static string BuildDisplayName(string nachname, string vorname)
            {
                var combined = $"{nachname}, {vorname}".Trim(' ', ',');
                return string.IsNullOrWhiteSpace(combined) ? "Unbekanntes Mitglied" : combined;
            }
        }
    }
}
