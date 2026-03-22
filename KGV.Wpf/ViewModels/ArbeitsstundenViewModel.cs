using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Helpers;
using KGV.Messages;
using KGV.Views;

namespace KGV.ViewModels
{
    public sealed class ArbeitsstundenViewModel : BaseViewModel, INavigationAware
    {
        private readonly ISupabaseService _supabaseService;
        private readonly IAuthService _authService;
        private readonly bool _reviewMode;
        private readonly bool _showOnlyPending;
        private readonly bool _includeNebenmitglied;

        public MemberDTO Hauptmitglied { get; }

        private MemberDTO? _nebenmitglied;
        public MemberDTO? Nebenmitglied
        {
            get => _nebenmitglied;
            private set => SetProperty(ref _nebenmitglied, value);
        }

        public ObservableCollection<ArbeitsstundeDTO> Arbeitsstunden { get; } = new();

        private ArbeitsstundeDTO? _selectedArbeitsstunde;
        public ArbeitsstundeDTO? SelectedArbeitsstunde
        {
            get => _selectedArbeitsstunde;
            set
            {
                if (SetProperty(ref _selectedArbeitsstunde, value))
                {
                    BearbeitenCommand.RaiseCanExecuteChanged();
                    FreigebenCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public ObservableCollection<SaisonRecord> Saisons { get; } = new();

        public string Title => _reviewMode ? "Arbeitsstunden freigeben" : "Arbeitsstunden";
        public bool IsReviewMode => _reviewMode;
        public bool ShowNeuButton => !_reviewMode;
        public bool ShowFreigebenButton => _reviewMode;
        public bool HasArbeitsstunden => Arbeitsstunden.Count > 0;
        public bool ShowEmptyState => !HasArbeitsstunden;
        public string EmptyText => _reviewMode
            ? "Für dieses Mitglied liegen aktuell keine offenen Arbeitsstunden mehr zur Freigabe vor."
            : "Für dieses Mitglied liegen aktuell keine Arbeitsstunden vor.";
        public string HintText => _reviewMode
            ? "Öffne einen Eintrag per Doppelklick oder gib den ausgewählten Datensatz direkt frei."
            : "Doppelklick öffnet den ausgewählten Datensatz zur Bearbeitung.";

        public RelayCommand<object?> NeueArbeitsstundeCommand { get; }
        public RelayCommand<object?> BearbeitenCommand { get; }
        public RelayCommand<object?> FreigebenCommand { get; }

        private int? _currentUserMitgliedId;

        public ArbeitsstundenViewModel(ISupabaseService supabaseService, IAuthService authService, MemberDTO hauptmitglied)
            : this(supabaseService, authService, new ArbeitsstundenNavigationContext
            {
                Member = hauptmitglied,
                IncludeNebenmitglied = true,
                ReviewMode = false,
                ShowOnlyPending = false
            })
        {
        }

        public ArbeitsstundenViewModel(ISupabaseService supabaseService, IAuthService authService, ArbeitsstundenNavigationContext context)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            if (context == null) throw new ArgumentNullException(nameof(context));
            Hauptmitglied = context.Member ?? throw new ArgumentNullException(nameof(context.Member));
            _includeNebenmitglied = context.IncludeNebenmitglied;
            _reviewMode = context.ReviewMode;
            _showOnlyPending = context.ShowOnlyPending;

            NeueArbeitsstundeCommand = new RelayCommand<object?>(_ => _ = NeueArbeitsstundeAsync(), _ => ShowNeuButton);
            BearbeitenCommand = new RelayCommand<object?>(_ => _ = BearbeitenAsync(), _ => SelectedArbeitsstunde != null);
            FreigebenCommand = new RelayCommand<object?>(_ => _ = FreigebenAsync(), _ => CanFreigebenSelected());
        }

        public async Task OnNavigatedToAsync()
        {
            await LoadAsync();
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private async Task LoadAsync()
        {
            if (_includeNebenmitglied)
            {
                var nebenRec = await _supabaseService.GetNebenmitgliedByHauptmitgliedIdAsync(Hauptmitglied.Id);
                Nebenmitglied = nebenRec != null ? new MemberDTO { Id = nebenRec.Id, Vorname = nebenRec.Vorname ?? "", Nachname = nebenRec.Name ?? "" } : null;
            }
            else
            {
                Nebenmitglied = null;
            }

            var saisonen = await _supabaseService.GetSaisonRecordsAsync();
            Saisons.Clear();
            foreach (var s in saisonen.OrderByDescending(x => x.Jahr))
                Saisons.Add(s);

            var ids = new List<int> { Hauptmitglied.Id };
            if (_includeNebenmitglied && Nebenmitglied != null)
                ids.Add(Nebenmitglied.Id);

            var items = await _supabaseService.GetArbeitsstundenAsync(ids.ToArray());
            if (_showOnlyPending)
            {
                items = items
                    .Where(IsPending)
                    .OrderBy(x => x.Datum)
                    .ThenBy(x => x.Id)
                    .ToList();
            }

            Arbeitsstunden.Clear();
            foreach (var i in items)
                Arbeitsstunden.Add(i);

            SelectedArbeitsstunde = Arbeitsstunden.FirstOrDefault();
            await EnsureCurrentUserMitgliedIdAsync();
            OnPropertyChanged(nameof(HasArbeitsstunden));
            OnPropertyChanged(nameof(ShowEmptyState));
            OnPropertyChanged(nameof(EmptyText));
            OnPropertyChanged(nameof(HintText));
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(ShowNeuButton));
            OnPropertyChanged(nameof(ShowFreigebenButton));
            NeueArbeitsstundeCommand.RaiseCanExecuteChanged();
            FreigebenCommand.RaiseCanExecuteChanged();
        }

        private async Task EnsureCurrentUserMitgliedIdAsync()
        {
            if (_currentUserMitgliedId.HasValue)
                return;

            var authId = _authService.CurrentUserId;
            if (string.IsNullOrWhiteSpace(authId))
                return;

            var rec = await _supabaseService.GetMitgliedByAuthUserIdAsync(authId);
            _currentUserMitgliedId = rec?.Id;
        }

        private async Task NeueArbeitsstundeAsync()
        {
            var dlg = new ArbeitsstundeDialog
            {
                Owner = Application.Current?.MainWindow
            };

            var memberOptions = new List<MemberDTO> { Hauptmitglied };
            if (_includeNebenmitglied && Nebenmitglied != null) memberOptions.Add(Nebenmitglied);

            dlg.SetOptions(memberOptions, Saisons.ToList());
            var today = DateTime.Today;
            var saisonIdByYear = Saisons.FirstOrDefault(s => s.Jahr == today.Year)?.Id;
            var defaultSaisonId = saisonIdByYear ?? Saisons.FirstOrDefault()?.Id;
            dlg.SetInitialValues(memberId: Hauptmitglied.Id, saisonId: defaultSaisonId, datum: today, stunden: null, beschreibung: null);
            // Erfassung durch Vorstand/Admin: sofort freigegeben (Checkbox nicht änderbar)
            var isPrivileged = _authService.IsAdmin || _authService.IsVorstand;
            dlg.SetFreigabeMode(canApprove: !isPrivileged, defaultFreigegeben: isPrivileged);
            dlg.SetDeleteEnabled(false);

            if (dlg.ShowDialog() != true)
                return;

            if (!dlg.Datum.HasValue || !dlg.Stunden.HasValue || dlg.SelectedMitgliedId == null || dlg.SelectedSaisonId == null || string.IsNullOrWhiteSpace(dlg.Beschreibung))
            {
                MessageBox.Show("Bitte Mitglied, Datum, Saison, Stunden und Beschreibung angeben.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var approveImmediately = isPrivileged;
            int? approverId = null;
            DateTime? approvedAt = null;

            if (approveImmediately)
            {
                await EnsureCurrentUserMitgliedIdAsync();
                if (!_currentUserMitgliedId.HasValue)
                {
                    MessageBox.Show("Freigabe nicht möglich: aktueller Benutzer ist keinem Mitglied zugeordnet (auth_user_id fehlt).", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                approverId = _currentUserMitgliedId;
                approvedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
            }

            var rec = new ArbeitsstundeRecord
            {
                MitgliedId = dlg.SelectedMitgliedId.Value,
                SaisonId = dlg.SelectedSaisonId.Value,
                Datum = dlg.Datum.Value.Date,
                Stunden = dlg.Stunden.Value,
                ArtDerArbeit = dlg.Beschreibung ?? string.Empty,
                Freigegeben = approveImmediately,
                GenehmigtVon = approverId,
                GenehmigtAm = approvedAt,
                Status = "offen"
            };

            var ok = await _supabaseService.AddArbeitsstundeAsync(rec);
            if (!ok)
            {
                MessageBox.Show("Arbeitsstunde konnte nicht gespeichert werden.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            WeakReferenceMessenger.Default.Send(new ArbeitsstundenChangedMessage());
            await LoadAsync();
        }

        private async Task BearbeitenAsync()
        {
            if (SelectedArbeitsstunde == null)
                return;

            var dlg = new ArbeitsstundeDialog
            {
                Owner = Application.Current?.MainWindow
            };

            var memberOptions = new List<MemberDTO> { Hauptmitglied };
            if (_includeNebenmitglied && Nebenmitglied != null) memberOptions.Add(Nebenmitglied);

            dlg.SetOptions(memberOptions, Saisons.ToList());
            dlg.SetInitialValues(
                memberId: SelectedArbeitsstunde.MitgliedId,
                saisonId: SelectedArbeitsstunde.SaisonId,
                datum: SelectedArbeitsstunde.Datum,
                stunden: SelectedArbeitsstunde.Stunden,
                beschreibung: SelectedArbeitsstunde.Beschreibung);

            var canApprove = _authService.IsAdmin || _authService.IsVorstand;
            dlg.SetFreigabeMode(canApprove: canApprove, defaultFreigegeben: SelectedArbeitsstunde.Freigegeben);
            dlg.SetDeleteEnabled(canApprove);

            if (dlg.ShowDialog() != true)
                return;

            if (dlg.DeleteRequested)
            {
                var okDel = await _supabaseService.DeleteArbeitsstundeAsync(SelectedArbeitsstunde.Id);
                if (!okDel)
                {
                    MessageBox.Show("Datensatz konnte nicht gelöscht werden.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                WeakReferenceMessenger.Default.Send(new ArbeitsstundenChangedMessage());
                await LoadAsync();
                return;
            }

            if (!dlg.Datum.HasValue || !dlg.Stunden.HasValue || dlg.SelectedMitgliedId == null || dlg.SelectedSaisonId == null || string.IsNullOrWhiteSpace(dlg.Beschreibung))
            {
                MessageBox.Show("Bitte Mitglied, Datum, Saison, Stunden und Beschreibung angeben.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            await EnsureCurrentUserMitgliedIdAsync();

            var freigegeben = dlg.Freigegeben;
            DateTime? genehmigtAm = SelectedArbeitsstunde.FreigegebenAm;
            int? genehmigtVon = SelectedArbeitsstunde.FreigegebenVonId;

            if (!SelectedArbeitsstunde.Freigegeben && freigegeben)
            {
                if (!canApprove)
                {
                    MessageBox.Show("Keine Berechtigung zum Freigeben.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                genehmigtAm = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
                if (!_currentUserMitgliedId.HasValue)
                {
                    MessageBox.Show("Freigabe nicht möglich: aktueller Benutzer ist keinem Mitglied zugeordnet (auth_user_id fehlt).", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                genehmigtVon = _currentUserMitgliedId;
            }

            var update = new ArbeitsstundeRecord
            {
                Id = SelectedArbeitsstunde.Id,
                MitgliedId = dlg.SelectedMitgliedId.Value,
                SaisonId = dlg.SelectedSaisonId.Value,
                Datum = dlg.Datum.Value.Date,
                Stunden = dlg.Stunden.Value,
                ArtDerArbeit = dlg.Beschreibung ?? string.Empty,
                Freigegeben = freigegeben,
                GenehmigtAm = genehmigtAm,
                GenehmigtVon = genehmigtVon,
                Status = string.IsNullOrWhiteSpace(SelectedArbeitsstunde.Status) ? "offen" : SelectedArbeitsstunde.Status
            };

            var ok = await _supabaseService.UpdateArbeitsstundeAsync(update);
            if (!ok)
            {
                MessageBox.Show("Arbeitsstunde konnte nicht gespeichert werden.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            WeakReferenceMessenger.Default.Send(new ArbeitsstundenChangedMessage());
            await LoadAsync();
        }

        private async Task FreigebenAsync()
        {
            if (SelectedArbeitsstunde == null)
                return;

            if (!_authService.IsAdmin && !_authService.IsVorstand)
                return;

            await EnsureCurrentUserMitgliedIdAsync();
            if (!_currentUserMitgliedId.HasValue)
            {
                MessageBox.Show("Freigabe nicht möglich: aktueller Benutzer ist keinem Mitglied zugeordnet (auth_user_id fehlt).", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var update = new ArbeitsstundeRecord
            {
                Id = SelectedArbeitsstunde.Id,
                MitgliedId = SelectedArbeitsstunde.MitgliedId,
                SaisonId = SelectedArbeitsstunde.SaisonId,
                Datum = SelectedArbeitsstunde.Datum.Date,
                Stunden = SelectedArbeitsstunde.Stunden,
                ArtDerArbeit = SelectedArbeitsstunde.Beschreibung,
                Freigegeben = true,
                GenehmigtAm = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified),
                GenehmigtVon = _currentUserMitgliedId,
                Status = string.IsNullOrWhiteSpace(SelectedArbeitsstunde.Status) ? "offen" : SelectedArbeitsstunde.Status
            };

            var ok = await _supabaseService.UpdateArbeitsstundeAsync(update);
            if (!ok)
            {
                MessageBox.Show("Arbeitsstunde konnte nicht freigegeben werden.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            WeakReferenceMessenger.Default.Send(new ArbeitsstundenChangedMessage());
            await LoadAsync();
        }

        private bool CanFreigebenSelected()
        {
            return _reviewMode
                   && SelectedArbeitsstunde != null
                   && !SelectedArbeitsstunde.Freigegeben
                   && (_authService.IsAdmin || _authService.IsVorstand);
        }

        private static bool IsPending(ArbeitsstundeDTO item)
        {
            return !item.Freigegeben && (string.IsNullOrWhiteSpace(item.Status) || item.Status.Equals("offen", StringComparison.OrdinalIgnoreCase));
        }
    }
}
