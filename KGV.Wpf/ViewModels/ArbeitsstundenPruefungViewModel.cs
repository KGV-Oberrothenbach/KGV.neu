using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Helpers;
using KGV.Messages;
using KGV.Views;
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
        private string? _currentUserId;

        public ObservableCollection<PruefungseintragItem> OffenePruefungen { get; } = new();

        public string Title => "Arbeitsstunden freigeben";
        public string EmptyText => "Aktuell liegen keine offenen Arbeitsstunden zur Freigabe vor.";
        public bool HasEntries => OffenePruefungen.Count > 0;
        public bool ShowEmptyState => _lockAcquired && !HasEntries;
        public bool ShowTable => _lockAcquired && HasEntries;
        public bool ShowLockMessage => !string.IsNullOrWhiteSpace(LockMessage);
        public bool ShowStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);
        public bool HasPendingChanges => OffenePruefungen.Any(x => x.HasChanges);

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

        public RelayCommand<object?> AktualisierenCommand { get; }
        public RelayCommand<object?> SpeichernCommand { get; }
        public RelayCommand<PruefungseintragItem> BearbeitenCommand { get; }

        public ArbeitsstundenPruefungViewModel(ISupabaseService supabaseService, MainWindowViewModel mainWindowViewModel)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
            _mainWindowViewModel = mainWindowViewModel ?? throw new ArgumentNullException(nameof(mainWindowViewModel));

            AktualisierenCommand = new RelayCommand<object?>(_ => _ = ReloadAsync());
            SpeichernCommand = new RelayCommand<object?>(_ => _ = SaveAsync(), _ => _lockAcquired && HasPendingChanges);
            BearbeitenCommand = new RelayCommand<PruefungseintragItem>(item => _ = BearbeitenAsync(item), item => _lockAcquired && item != null);

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
                OnPropertyChanged(nameof(HasEntries));
                OnPropertyChanged(nameof(ShowEmptyState));
                OnPropertyChanged(nameof(ShowTable));
                OnPropertyChanged(nameof(HasPendingChanges));
                SpeichernCommand.RaiseCanExecuteChanged();
                return;
            }

            await LoadEntriesAsync();
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

        private async Task LoadEntriesAsync()
        {
            var entries = await _supabaseService.GetOffeneArbeitsstundenZurFreigabeAsync();

            OffenePruefungen.Clear();
            foreach (var entry in entries)
            {
                var item = new PruefungseintragItem(entry);
                item.PropertyChanged += (_, _) =>
                {
                    OnPropertyChanged(nameof(HasPendingChanges));
                    SpeichernCommand.RaiseCanExecuteChanged();
                };
                OffenePruefungen.Add(item);
            }

            OnPropertyChanged(nameof(HasEntries));
            OnPropertyChanged(nameof(ShowEmptyState));
            OnPropertyChanged(nameof(ShowTable));
            OnPropertyChanged(nameof(HasPendingChanges));
            SpeichernCommand.RaiseCanExecuteChanged();
        }

        private async Task SaveAsync()
        {
            if (!_lockAcquired)
                return;

            var approverId = ResolveApproverId();
            if (!approverId.HasValue)
            {
                StatusMessage = "Freigabe nicht möglich: aktueller Benutzer ist keinem Mitglied zugeordnet.";
                return;
            }

            var changedRows = OffenePruefungen.Where(x => x.HasChanges).ToList();
            if (changedRows.Count == 0)
            {
                StatusMessage = "Aktuell sind keine geänderten oder zur Freigabe markierten Zeilen vorhanden.";
                return;
            }

            var savedCount = 0;
            foreach (var row in changedRows)
            {
                var update = new ArbeitsstundeRecord
                {
                    Id = row.Id,
                    MitgliedId = row.MitgliedId,
                    SaisonId = row.SaisonId,
                    Datum = row.Datum.Date,
                    Stunden = row.Stunden,
                    ArtDerArbeit = row.ArtDerArbeit,
                    Status = string.IsNullOrWhiteSpace(row.StatusText) ? null : row.StatusText.Trim(),
                    Freigegeben = row.Freigeben,
                    GenehmigtAm = row.Freigeben ? DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified) : null,
                    GenehmigtVon = row.Freigeben ? approverId : null,
                    LockedByUserId = _currentUserId,
                    LockedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
                };

                var ok = await _supabaseService.UpdateArbeitsstundeAsync(update);
                if (ok)
                    savedCount++;
            }

            WeakReferenceMessenger.Default.Send(new ArbeitsstundenChangedMessage());
            var allRowsSaved = savedCount == changedRows.Count;
            StatusMessage = allRowsSaved
                ? $"{savedCount} Arbeitsstunden wurden gespeichert/freigegeben."
                : $"{savedCount} von {changedRows.Count} geänderten Arbeitsstunden wurden gespeichert.";

            if (allRowsSaved && savedCount > 0)
            {
                await NavigateHomeAsync();
                return;
            }

            await LoadEntriesAsync();
        }

        private async Task BearbeitenAsync(PruefungseintragItem? item)
        {
            if (!_lockAcquired || item == null)
                return;

            var vm = new ArbeitsstundenErfassungViewModel(
                _supabaseService,
                _mainWindowViewModel,
                new ArbeitsstundenErfassungContext
                {
                    ExistingEntry = item.ToDto(),
                    IsAdminEditMode = true,
                    OpenAsDialog = true
                });

            var window = new ArbeitsstundenErfassungWindow
            {
                Owner = Application.Current?.MainWindow,
                DataContext = vm
            };

            window.ShowDialog();
            await LoadEntriesAsync();
        }

        private async Task NavigateHomeAsync()
        {
            var created = _mainWindowViewModel.NavigateToHomeViewModel();
            if (created != null)
                await _mainWindowViewModel.NavigateToAsync(created);
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
            LockMessage = "Die globale Prüfsperre konnte nicht verlängert werden. Bitte die Seite neu öffnen, bevor weitere Freigaben gespeichert werden.";
            StatusMessage = string.Empty;
            OnPropertyChanged(nameof(ShowTable));
            OnPropertyChanged(nameof(ShowEmptyState));
            OnPropertyChanged(nameof(HasPendingChanges));
            SpeichernCommand.RaiseCanExecuteChanged();
        }

        private int? ResolveApproverId()
        {
            return _mainWindowViewModel.UserContext.MitgliedId is > 0 and <= int.MaxValue
                ? (int)_mainWindowViewModel.UserContext.MitgliedId.Value
                : null;
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
            private readonly string? _originalStatus;
            private bool _freigeben;
            private string _statusText;

            public PruefungseintragItem(ArbeitsstundeDTO dto)
            {
                Id = dto.Id;
                MitgliedId = dto.MitgliedId;
                MitgliedDisplayName = BuildDisplayName(dto.Nachname, dto.Vorname);
                Datum = dto.Datum;
                SaisonId = dto.SaisonId;
                SaisonJahr = dto.SaisonJahr;
                Stunden = dto.Stunden;
                ArtDerArbeit = dto.Beschreibung ?? string.Empty;
                _statusText = dto.Status ?? string.Empty;
                _originalStatus = dto.Status;
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
            public DateTime? FreigegebenAm { get; }
            public string? FreigegebenVonName { get; }

            public string StatusText
            {
                get => _statusText;
                set
                {
                    if (SetProperty(ref _statusText, value))
                        OnPropertyChanged(nameof(HasChanges));
                }
            }

            public bool Freigeben
            {
                get => _freigeben;
                set
                {
                    if (SetProperty(ref _freigeben, value))
                        OnPropertyChanged(nameof(HasChanges));
                }
            }

            public bool HasChanges => Freigeben || !string.Equals(NormalizeStatus(_originalStatus), NormalizeStatus(StatusText), StringComparison.Ordinal);

            public ArbeitsstundeDTO ToDto()
            {
                return new ArbeitsstundeDTO
                {
                    Id = Id,
                    MitgliedId = MitgliedId,
                    Nachname = SplitName(MitgliedDisplayName).nachname,
                    Vorname = SplitName(MitgliedDisplayName).vorname,
                    Datum = Datum,
                    SaisonId = SaisonId,
                    SaisonJahr = SaisonJahr,
                    Stunden = Stunden,
                    Beschreibung = ArtDerArbeit,
                    Status = StatusText,
                    Freigegeben = false,
                    FreigegebenAm = FreigegebenAm,
                    FreigegebenVonName = FreigegebenVonName
                };
            }

            private static string NormalizeStatus(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

            private static string BuildDisplayName(string nachname, string vorname)
            {
                var combined = $"{nachname}, {vorname}".Trim(' ', ',');
                return string.IsNullOrWhiteSpace(combined) ? "Unbekanntes Mitglied" : combined;
            }

            private static (string nachname, string vorname) SplitName(string displayName)
            {
                var parts = (displayName ?? string.Empty).Split(',', 2);
                return parts.Length == 2
                    ? (parts[0].Trim(), parts[1].Trim())
                    : ((displayName ?? string.Empty).Trim(), string.Empty);
            }
        }
    }
}
