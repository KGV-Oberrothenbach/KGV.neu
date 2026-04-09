// File: ViewModels/MemberDetailViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Messaging;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Helpers;
using KGV.Messages;
using KGV.Views;

namespace KGV.ViewModels
{
    public class MemberDetailViewModel : BaseViewModel, INavigationAware
    {
        private readonly ISupabaseService _supabaseService;
        private readonly IAuthService _authService;
        private readonly UserContext _userContext;
        private readonly bool _isNewMode;

        private string? _lockUserId;
        private int? _currentUserMemberId;
        private bool _hasSelectedMemberAppUser;

        public MemberDTO SelectedMember { get; }
        public bool IsNewMode => _isNewMode;
        public bool ShowCancelMembershipButton => !_isNewMode && !IsEditMode && SelectedMember.Id > 0 && SelectedMember.IstHauptmitglied && !SelectedMember.MitgliedEnde.HasValue;
        public bool HasSelectedMemberAppUser
        {
            get => _hasSelectedMemberAppUser;
            private set
            {
                if (!SetProperty(ref _hasSelectedMemberAppUser, value))
                    return;

                OnPropertyChanged(nameof(IsEmailReadOnly));
                OnPropertyChanged(nameof(ShowChangeEmailButton));
                OnPropertyChanged(nameof(ChangeEmailHint));
                ChangeEmailCommand.RaiseCanExecuteChanged();
            }
        }

        public bool IsEmailReadOnly => !IsEditMode || HasSelectedMemberAppUser;
        public bool ShowChangeEmailButton => HasSelectedMemberAppUser && IsEditMode;

        public bool ShowParzellenSection => true;
        public bool ShowNewContractButton => !_isNewMode;

        private MitgliedRecord? _nebenmitgliedRecord;
        private bool _hasNebenmitglied;
        public bool HasNebenmitglied
        {
            get => _hasNebenmitglied;
            private set
            {
                if (SetProperty(ref _hasNebenmitglied, value))
                {
                    OnPropertyChanged(nameof(ShowNebenmitgliedButton));
                    OnPropertyChanged(nameof(NebenmitgliedButtonText));
                }
            }
        }

        public bool ShowNebenmitgliedButton => !_isNewMode && (HasNebenmitglied || IsEditMode);
        public string NebenmitgliedButtonText => HasNebenmitglied ? "Nebenmitglied" : "Nebenmitglied anlegen";

        public bool ShowAdresseUebernehmenButton => false;
        public bool CanEditMemberStammdaten => _isNewMode
            ? PermissionChecks.CanEditAllMembers(_userContext)
            : PermissionChecks.CanWriteStammdatenForMember(_userContext, SelectedMember.Id);

        private MemberDTO _originalSnapshot;

        public ObservableCollection<ParzellenBelegungDTO> ParzellenBelegungen { get; } = new();
        public ObservableCollection<ParzelleRecord> AvailableParzellen { get; } = new();

        private ParzellenBelegungDTO? _selectedBelegung;
        public ParzellenBelegungDTO? SelectedBelegung
        {
            get => _selectedBelegung;
            set
            {
                if (SetProperty(ref _selectedBelegung, value))
                {
                    InvalidateCommands();

                    // Nur Kontext setzen (Sidebar-Menü auf Garten aktivieren), ohne direkt zu navigieren.
                    // WICHTIG: Beim Navigieren weg von der Seite kann WPF `SelectedItem` auf null setzen.
                    // Das darf den globalen Garten-Kontext (Sidebar) nicht wieder "löschen".
                    if (_selectedBelegung != null)
                        WeakReferenceMessenger.Default.Send(new ParzelleContextChangedMessage(_selectedBelegung));
                }
            }
        }

        private ParzelleRecord? _selectedParzelleToAssign;
        public ParzelleRecord? SelectedParzelleToAssign
        {
            get => _selectedParzelleToAssign;
            set
            {
                if (SetProperty(ref _selectedParzelleToAssign, value))
                {
                    InvalidateCommands();
                }
            }
        }

        private DateTime? _assignVonDatum = DateTime.Today;
        public DateTime? AssignVonDatum
        {
            get => _assignVonDatum;
            set
            {
                if (SetProperty(ref _assignVonDatum, value?.Date))
                {
                    InvalidateCommands();
                }
            }
        }

        private bool _isEditMode;
        public bool IsEditMode
        {
            get => _isEditMode;
            private set
            {
                if (!SetProperty(ref _isEditMode, value))
                    return;

                OnPropertyChanged(nameof(IsEmailReadOnly));
                OnPropertyChanged(nameof(ShowChangeEmailButton));
                OnPropertyChanged(nameof(CanChangeEmail));
                OnPropertyChanged(nameof(ChangeEmailHint));
            }
        }

        private bool _isDirty;
        public bool IsDirty
        {
            get => _isDirty;
            private set => SetProperty(ref _isDirty, value);
        }

        public RelayCommand<object?> ToggleEditCommand { get; }
        public RelayCommand<object?> SaveCommand { get; }
        public RelayCommand<object?> CancelCommand { get; }
        public RelayCommand<object?> ChangeEmailCommand { get; }
        public RelayCommand<object?> CreateMitgliedsantragCommand { get; }
        public RelayCommand<object?> NebenmitgliedCommand { get; }
        public RelayCommand<object?> CopyAddressFromHauptmitgliedCommand { get; }

        // noch nicht implementiert (Binding existiert in View)
        public RelayCommand<object?> NewContractCommand { get; }
        public RelayCommand<object?> CancelMembershipCommand { get; }
        public RelayCommand<object?> AssignParzelleCommand { get; }
        public RelayCommand<object?> EndBelegungCommand { get; }
        public RelayCommand<object?> OpenSelectedParzelleCommand { get; }

        public bool ShowMitgliedsantragButton => !_isNewMode && PermissionChecks.CanManageDocuments(_userContext);
        public bool CanCreateMitgliedsantrag => ShowMitgliedsantragButton && SelectedMember.Id > 0 && !IsEditMode;
        public bool CanChangeEmail => HasSelectedMemberAppUser && IsEditMode && _currentUserMemberId == SelectedMember.Id;
        public string ChangeEmailHint => !HasSelectedMemberAppUser
            ? "Mailadresse kann direkt in den Stammdaten bearbeitet werden, solange noch kein App-User verknüpft ist."
            : CanChangeEmail
                ? "Mailadresse wird separat per OTP-Code geändert und nicht über das normale Stammdaten-Speichern."
                : "Mailadresse kann nur vom aktuell angemeldeten Benutzer über den separaten OTP-Flow geändert werden.";

        public MemberDetailViewModel(ISupabaseService supabaseService, IAuthService authService, UserContext userContext, MemberDTO member, bool isNewMode = false)
        {
            _supabaseService = supabaseService;
            _authService = authService;
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _isNewMode = isNewMode;
            SelectedMember = member;

            _originalSnapshot = SelectedMember.Clone();

            SelectedMember.PropertyChanged += (_, __) =>
            {
                if (!IsEditMode)
                    return;

                IsDirty = !SelectedMember.ValueEquals(_originalSnapshot);
                InvalidateCommands();
            };

            ToggleEditCommand = new RelayCommand<object?>(_ => _ = ToggleEditAsync(), _ => CanToggleEdit());
            SaveCommand = new RelayCommand<object?>(_ => _ = SaveAsync(), _ => CanSave());
            CancelCommand = new RelayCommand<object?>(_ => _ = CancelAsync(), _ => CanCancel());
            ChangeEmailCommand = new RelayCommand<object?>(_ => _ = ChangeEmailAsync(), _ => CanChangeEmail);
            CreateMitgliedsantragCommand = new RelayCommand<object?>(_ => _ = CreateMitgliedsantragAsync(), _ => CanCreateMitgliedsantrag);
            AssignParzelleCommand = new RelayCommand<object?>(_ => _ = AssignParzelleAsync(), _ => CanAssignParzelle());
            EndBelegungCommand = new RelayCommand<object?>(_ => _ = EndBelegungAsync(), _ => CanEndBelegung());
            OpenSelectedParzelleCommand = new RelayCommand<object?>(_ => OpenSelectedParzelle(), _ => SelectedBelegung != null);

            NebenmitgliedCommand = new RelayCommand<object?>(_ => _ = NebenmitgliedAsync(), _ => ShowNebenmitgliedButton);
            CopyAddressFromHauptmitgliedCommand = new RelayCommand<object?>(_ => { }, _ => false);

            NewContractCommand = new RelayCommand<object?>(_ => MessageBox.Show("Noch nicht implementiert.", "Info", MessageBoxButton.OK, MessageBoxImage.Information));
            CancelMembershipCommand = new RelayCommand<object?>(_ => _ = CancelMembershipAsync(), _ => CanCancelMembership());
        }

        private void OpenSelectedParzelle()
        {
            if (SelectedBelegung == null)
                return;

            WeakReferenceMessenger.Default.Send(new ParzelleSelectedMessage(SelectedBelegung));
        }

        public async Task OnNavigatedToAsync()
        {
            await LoadCurrentUserMemberAsync();
            if (_isNewMode)
            {
                SelectedMember.IstHauptmitglied = true;
                SelectedMember.Aktiv = true;
                if (!SelectedMember.MitgliedSeit.HasValue)
                    SelectedMember.MitgliedSeit = DateTime.Today;

                _originalSnapshot = SelectedMember.Clone();
                IsEditMode = true;
                IsDirty = false;
                OnPropertyChanged(nameof(CanEditMemberStammdaten));
                InvalidateCommands();
                return;
            }

            await LoadMemberAsync();
            await LoadParzellenAsync();
            await RefreshNebenmitgliedAsync();

            IsEditMode = false;
            IsDirty = false;
            OnPropertyChanged(nameof(CanEditMemberStammdaten));
            InvalidateCommands();
        }

        public async Task OnNavigatedFromAsync()
        {
            if (!_isNewMode && IsEditMode && !string.IsNullOrEmpty(_lockUserId))
            {
                await _supabaseService.ReleaseLockMitgliedAsync(SelectedMember.Id, _lockUserId, force: false);
                _lockUserId = null;
            }

            IsEditMode = false;
            IsDirty = false;
        }

        private async Task RefreshNebenmitgliedAsync()
        {
            if (_isNewMode || SelectedMember.Id <= 0)
            {
                _nebenmitgliedRecord = null;
                HasNebenmitglied = false;
                NebenmitgliedCommand.RaiseCanExecuteChanged();
                return;
            }

            _nebenmitgliedRecord = await _supabaseService.GetNebenmitgliedByHauptmitgliedIdAsync(SelectedMember.Id);
            HasNebenmitglied = _nebenmitgliedRecord != null;
            NebenmitgliedCommand.RaiseCanExecuteChanged();
        }

        private static MemberDTO ToMemberDto(MitgliedRecord rec)
        {
            return new MemberDTO
            {
                Id = rec.Id,
                Vorname = rec.Vorname ?? string.Empty,
                Nachname = rec.Name ?? string.Empty,
                Geburtsdatum = rec.Geburtsdatum,
                Strasse = rec.Adresse ?? string.Empty,
                PLZ = rec.Plz ?? string.Empty,
                Ort = rec.Ort ?? string.Empty,
                Telefon = rec.Telefon ?? string.Empty,
                Mobilnummer = rec.Handy ?? string.Empty,
                Email = rec.Email ?? string.Empty,
                Bemerkungen = rec.Bemerkung ?? string.Empty,
                WhatsappEinwilligung = rec.WhatsappEinwilligung,
                MitgliedSeit = rec.MitgliedSeit,
                MitgliedEnde = rec.MitgliedEnde,
                Role = rec.Role ?? string.Empty,
                IstHauptmitglied = !rec.HauptmitgliedId.HasValue || rec.HauptmitgliedId.Value <= 0
            };
        }

        private bool CanCancelMembership()
            => ShowCancelMembershipButton && PermissionChecks.CanWriteStammdatenForMember(_userContext, SelectedMember.Id);

        private async Task CancelMembershipAsync()
        {
            var userId = _authService.CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                MessageBox.Show("Nicht angemeldet. Bitte erneut einloggen.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var secondaryMember = await _supabaseService.GetNebenmitgliedByHauptmitgliedIdAsync(SelectedMember.Id);
            MembershipEndDecision? decision = null;
            if (secondaryMember != null)
            {
                var choice = MessageBox.Show(
                    "Ja = Nebenmitglied ebenfalls beenden.\nNein = Nebenmitglied zum Hauptmitglied machen.\nAbbrechen = keine Änderung.",
                    "Folgeentscheid für Nebenmitglied",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (choice == MessageBoxResult.Cancel)
                    return;

                decision = choice == MessageBoxResult.Yes
                    ? MembershipEndDecision.EndSecondaryMember
                    : MembershipEndDecision.PromoteSecondaryMember;
            }

            if (MessageBox.Show(
                    $"Soll die Mitgliedschaft zum {DateTime.Today:dd.MM.yyyy} beendet werden?",
                    "Mitgliedschaft beenden",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }

            var lockAcquired = await _supabaseService.TryLockMitgliedAsync(SelectedMember.Id, userId);
            if (!lockAcquired)
            {
                MessageBox.Show("Datensatz ist aktuell gesperrt. Bitte später erneut versuchen.", "Gesperrt", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var result = await _supabaseService.EndMembershipAsync(SelectedMember.Id, DateTime.Today, decision, userId);
                if (!result.Success || result.UpdatedMainMember == null)
                {
                    MessageBox.Show(string.IsNullOrWhiteSpace(result.Message) ? "Mitgliedschaft konnte nicht beendet werden." : result.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                SelectedMember.CopyFrom(ToMemberDto(result.UpdatedMainMember));
                _originalSnapshot = SelectedMember.Clone();
                HasNebenmitglied = result.AppliedDecision != MembershipEndDecision.PromoteSecondaryMember && secondaryMember != null;
                OnPropertyChanged(nameof(ShowCancelMembershipButton));
                InvalidateCommands();
                WeakReferenceMessenger.Default.Send(new MemberSavedMessage(SelectedMember.Clone()));
                if (result.UpdatedSecondaryMember != null)
                    WeakReferenceMessenger.Default.Send(new MemberSavedMessage(ToMemberDto(result.UpdatedSecondaryMember)));

                MessageBox.Show(result.Message, "OK", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            finally
            {
                await _supabaseService.ReleaseLockMitgliedAsync(SelectedMember.Id, userId, force: false);
            }
        }

        private async Task NebenmitgliedAsync()
        {
            if (HasNebenmitglied)
            {
                if (_nebenmitgliedRecord == null)
                    _nebenmitgliedRecord = await _supabaseService.GetNebenmitgliedByHauptmitgliedIdAsync(SelectedMember.Id);

                if (_nebenmitgliedRecord == null)
                {
                    await RefreshNebenmitgliedAsync();
                    return;
                }

                var ctx = new NebenmitgliedContext(SelectedMember.Clone(), ToMemberDto(_nebenmitgliedRecord));
                WeakReferenceMessenger.Default.Send(new NebenmitgliedSelectedMessage(ctx));
                return;
            }

            if (!IsEditMode)
                return;

            await CreateAndOpenNebenmitgliedAsync();
        }

        private async Task CreateAndOpenNebenmitgliedAsync()
        {
            var created = await PromptCreateNebenmitgliedAsync();
            if (created == null)
                return;

            OpenNebenmitgliedDetail(created);
        }

        private async Task<MitgliedRecord?> PromptCreateNebenmitgliedAsync()
        {
            var dlg = new NebenmitgliedDialog
            {
                Owner = Application.Current?.MainWindow
            };

            dlg.SetInitialValues(vorname: string.Empty, nachname: SelectedMember.Nachname, adresseUebernehmen: true);

            if (dlg.ShowDialog() != true)
                return null;

            if (string.IsNullOrWhiteSpace(dlg.Vorname) || string.IsNullOrWhiteSpace(dlg.Nachname))
            {
                MessageBox.Show("Bitte Vorname und Nachname angeben.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            var created = await _supabaseService.CreateNebenmitgliedAsync(new NebenmitgliedCreateDTO
            {
                HauptmitgliedId = SelectedMember.Id,
                Vorname = dlg.Vorname.Trim(),
                Nachname = dlg.Nachname.Trim(),
                AdresseUebernehmen = dlg.AdresseUebernehmen
            });

            if (created == null)
            {
                MessageBox.Show("Nebenmitglied konnte nicht angelegt werden.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            await RefreshNebenmitgliedAsync();
            return created;
        }

        private void OpenNebenmitgliedDetail(MitgliedRecord created)
        {
            var context = new NebenmitgliedContext(SelectedMember.Clone(), ToMemberDto(created));
            WeakReferenceMessenger.Default.Send(new NebenmitgliedSelectedMessage(context));
        }

        private async Task LoadMemberAsync()
        {
            if (_isNewMode || SelectedMember.Id <= 0)
                return;

            var rec = await _supabaseService.GetMitgliedByIdAsync(SelectedMember.Id);
            if (rec == null)
                return;

            SelectedMember.Vorname = rec.Vorname ?? "";
            SelectedMember.Nachname = rec.Name ?? "";
            SelectedMember.Geburtsdatum = rec.Geburtsdatum;

            SelectedMember.Strasse = rec.Adresse ?? "";
            SelectedMember.PLZ = rec.Plz ?? "";
            SelectedMember.Ort = rec.Ort ?? "";

            SelectedMember.Telefon = rec.Telefon ?? "";
            SelectedMember.Mobilnummer = rec.Handy ?? "";
            SelectedMember.Email = rec.Email ?? "";

            SelectedMember.Bemerkungen = rec.Bemerkung ?? "";
            SelectedMember.WhatsappEinwilligung = rec.WhatsappEinwilligung;

            SelectedMember.MitgliedSeit = rec.MitgliedSeit;
            SelectedMember.MitgliedEnde = rec.MitgliedEnde;

            SelectedMember.Role = rec.Role ?? "";
            _originalSnapshot = SelectedMember.Clone();
            OnPropertyChanged(nameof(ChangeEmailHint));
        }

        private async Task LoadCurrentUserMemberAsync()
        {
            _currentUserMemberId = null;

            if (string.IsNullOrWhiteSpace(_authService.CurrentUserId))
                return;

            var currentMember = await _supabaseService.GetMitgliedByAuthUserIdAsync(_authService.CurrentUserId);
            _currentUserMemberId = currentMember?.Id;
            OnPropertyChanged(nameof(CanChangeEmail));
            OnPropertyChanged(nameof(ChangeEmailHint));
            ChangeEmailCommand.RaiseCanExecuteChanged();
        }

        private async Task LoadParzellenAsync()
        {
            ParzellenBelegungen.Clear();
            AvailableParzellen.Clear();
            SelectedBelegung = null;
            SelectedParzelleToAssign = null;
            AssignVonDatum = DateTime.Today;

            if (_isNewMode || SelectedMember.Id <= 0)
            {
                InvalidateCommands();
                return;
            }

            var parzellen = await _supabaseService.GetAllParzellenAsync();
            var memberBelegungen = await _supabaseService.GetBelegungenForMitgliedAsync(SelectedMember.Id);
            var allBelegungen = await _supabaseService.GetAllParzellenBelegungenAsync();

            var parzById = parzellen.ToDictionary(p => p.Id, p => p);

            foreach (var b in memberBelegungen
                         .OrderByDescending(x => x.BisDatum == null)
                         .ThenByDescending(x => x.VonDatum ?? DateTime.MinValue))
            {
                parzById.TryGetValue(b.ParzelleId, out var p);

                ParzellenBelegungen.Add(new ParzellenBelegungDTO
                {
                    BelegungId = b.Id,
                    ParzelleId = b.ParzelleId,
                    MitgliedId = b.MitgliedId,
                    GartenNr = p?.GartenNr ?? $"#{b.ParzelleId}",
                    Anlage = p?.Anlage ?? "",
                    VonDatum = b.VonDatum?.Date,
                    BisDatum = b.BisDatum?.Date
                });
            }

            // Regel:
            // Frei = keine aktive Belegung heute ODER aktive Belegung hat BisDatum (auch in Zukunft)
            var today = DateTime.Today;

            var activeToday = allBelegungen
                .GroupBy(b => b.ParzelleId)
                .Select(g => g.Where(x =>
                        (x.VonDatum ?? DateTime.MinValue).Date <= today &&
                        (x.BisDatum == null || x.BisDatum.Value.Date >= today))
                    .OrderByDescending(x => x.VonDatum ?? DateTime.MinValue)
                    .FirstOrDefault())
                .Where(x => x != null)
                .ToDictionary(x => x!.ParzelleId, x => x!);

            foreach (var p in parzellen
                         .OrderBy(x => GetGartenNrSortKey(x.GartenNr))
                         .ThenBy(x => x.GartenNr, StringComparer.CurrentCultureIgnoreCase))
            {
                if (!activeToday.TryGetValue(p.Id, out var akt))
                {
                    AvailableParzellen.Add(p);
                    continue;
                }
            }

            InvalidateCommands();
        }

        private async Task ToggleEditAsync()
        {
            if (_isNewMode)
                return;

            if (!CanEditMemberStammdaten)
                return;

            if (!IsEditMode)
            {
                var userId = _authService.CurrentUserId;
                if (string.IsNullOrWhiteSpace(userId))
                {
                    MessageBox.Show("Nicht angemeldet. Bitte erneut einloggen.", "Fehler",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var locked = await _supabaseService.TryLockMitgliedAsync(SelectedMember.Id, userId);
                if (!locked)
                {
                    MessageBox.Show("Datensatz ist aktuell gesperrt. Bitte später erneut versuchen.", "Gesperrt",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                _lockUserId = userId;

                IsEditMode = true;
                _originalSnapshot = SelectedMember.Clone();
                IsDirty = false;

                OnPropertyChanged(nameof(ShowNebenmitgliedButton));
                OnPropertyChanged(nameof(ShowCancelMembershipButton));
                OnPropertyChanged(nameof(CanChangeEmail));
                OnPropertyChanged(nameof(ChangeEmailHint));
                ChangeEmailCommand.RaiseCanExecuteChanged();
                NebenmitgliedCommand.RaiseCanExecuteChanged();
            }
            else
            {
                await CancelAsync();
            }

            InvalidateCommands();
        }

        private bool CanSave() => IsEditMode && IsDirty;
        private bool CanCancel() => IsEditMode;

        private bool CanToggleEdit() => !IsEditMode && CanEditMemberStammdaten || IsEditMode;

        private async Task SaveAsync()
        {
            try
            {
                if (_isNewMode)
                {
                    var created = await _supabaseService.CreateMitgliedAsync(SelectedMember);
                    if (created == null)
                    {
                        MessageBox.Show("Mitglied konnte nicht angelegt werden.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    SelectedMember.Id = created.Id;
                    SelectedMember.Vorname = created.Vorname ?? string.Empty;
                    SelectedMember.Nachname = created.Name ?? string.Empty;
                    SelectedMember.Email = created.Email ?? string.Empty;
                    SelectedMember.Strasse = created.Adresse ?? string.Empty;
                    SelectedMember.PLZ = created.Plz ?? string.Empty;
                    SelectedMember.Ort = created.Ort ?? string.Empty;
                    SelectedMember.Telefon = created.Telefon ?? string.Empty;
                    SelectedMember.Mobilnummer = created.Handy ?? string.Empty;
                    SelectedMember.Bemerkungen = created.Bemerkung ?? string.Empty;
                    SelectedMember.WhatsappEinwilligung = created.WhatsappEinwilligung;
                    SelectedMember.MitgliedSeit = created.MitgliedSeit;
                    SelectedMember.MitgliedEnde = created.MitgliedEnde;
                    SelectedMember.Aktiv = created.Aktiv;
                    SelectedMember.IstHauptmitglied = !created.HauptmitgliedId.HasValue || created.HauptmitgliedId.Value <= 0;
                    SelectedMember.Role = created.Role ?? string.Empty;

                    _originalSnapshot = SelectedMember.Clone();
                    IsDirty = false;
                    IsEditMode = false;
                    OnPropertyChanged(nameof(CanEditMemberStammdaten));
                    WeakReferenceMessenger.Default.Send(new MemberSavedMessage(SelectedMember.Clone()));

                    var createNebenmitglied = MessageBox.Show(
                        "Mitglied angelegt. Soll jetzt ein Nebenmitglied angelegt werden?",
                        "Nebenmitglied anlegen",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (createNebenmitglied == MessageBoxResult.Yes)
                        await CreateAndOpenNebenmitgliedAsync();

                    return;
                }

                if (string.IsNullOrWhiteSpace(_lockUserId))
                {
                    MessageBox.Show("Kein Lock aktiv. Bitte Bearbeiten erneut starten.", "Fehler",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var ok = await _supabaseService.UpdateMitgliedAsync(SelectedMember, _lockUserId);
                if (!ok)
                {
                    MessageBox.Show("Speichern fehlgeschlagen (ggf. Lock verloren oder keine Berechtigung).", "Fehler",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _originalSnapshot = SelectedMember.Clone();
                IsDirty = false;

                if (!string.IsNullOrEmpty(_lockUserId))
                {
                    await _supabaseService.ReleaseLockMitgliedAsync(SelectedMember.Id, _lockUserId, force: false);
                    _lockUserId = null;
                }

                IsEditMode = false;
                InvalidateCommands();
                OnPropertyChanged(nameof(CanChangeEmail));
                OnPropertyChanged(nameof(ChangeEmailHint));
                ChangeEmailCommand.RaiseCanExecuteChanged();

                WeakReferenceMessenger.Default.Send(new MemberSavedMessage(SelectedMember.Clone()));

                MessageBox.Show("Mitglied gespeichert.", "OK", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Speichern: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task CancelAsync()
        {
            try
            {
                if (_isNewMode)
                {
                    SelectedMember.CopyFrom(_originalSnapshot);
                    IsDirty = false;
                    return;
                }

                SelectedMember.CopyFrom(_originalSnapshot);

                if (!string.IsNullOrEmpty(_lockUserId))
                {
                    await _supabaseService.ReleaseLockMitgliedAsync(SelectedMember.Id, _lockUserId, force: false);
                    _lockUserId = null;
                }

                IsEditMode = false;
                IsDirty = false;
                InvalidateCommands();

                OnPropertyChanged(nameof(ShowNebenmitgliedButton));
                OnPropertyChanged(nameof(ShowCancelMembershipButton));
                OnPropertyChanged(nameof(CanChangeEmail));
                OnPropertyChanged(nameof(ChangeEmailHint));
                ChangeEmailCommand.RaiseCanExecuteChanged();
                NebenmitgliedCommand.RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Abbrechen: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanAssignParzelle()
        {
            if (!IsEditMode)
                return false;

            if (SelectedParzelleToAssign == null)
                return false;

            if (!AssignVonDatum.HasValue)
                return false;

            return true;
        }

        private async Task AssignParzelleAsync()
        {
            if (SelectedParzelleToAssign == null)
                return;

            try
            {
                var start = (AssignVonDatum ?? DateTime.Today).Date;

                var ok = await _supabaseService.AssignParzelleToMitgliedAsync(
                    SelectedMember.Id,
                    SelectedParzelleToAssign.Id,
                    start);

                if (!ok)
                {
                    MessageBox.Show(
                        "Zuweisung fehlgeschlagen. Der Datensatz konnte nicht gespeichert werden (keine Details von der Datenbank).",
                        "Fehler",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                await LoadParzellenAsync();

                MessageBox.Show("Parzelle zugewiesen.", "OK", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Zuweisen: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanEndBelegung()
        {
            if (!IsEditMode)
                return false;

            if (SelectedBelegung == null)
                return false;

            if (SelectedBelegung.BisDatum.HasValue)
                return false;

            return true;
        }

        private async Task EndBelegungAsync()
        {
            if (SelectedBelegung == null)
                return;

            try
            {
                var today = DateTime.Today;

                var ok = await _supabaseService.EndParzellenBelegungAsync(SelectedBelegung.BelegungId, today);
                if (!ok)
                {
                    MessageBox.Show("Belegung konnte nicht beendet werden.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                await LoadParzellenAsync();

                MessageBox.Show("Belegung beendet.", "OK", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Beenden: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InvalidateCommands()
        {
            SaveCommand.RaiseCanExecuteChanged();
            CancelCommand.RaiseCanExecuteChanged();
            ChangeEmailCommand.RaiseCanExecuteChanged();
            CreateMitgliedsantragCommand.RaiseCanExecuteChanged();
            AssignParzelleCommand.RaiseCanExecuteChanged();
            EndBelegungCommand.RaiseCanExecuteChanged();
            OpenSelectedParzelleCommand.RaiseCanExecuteChanged();
            NebenmitgliedCommand.RaiseCanExecuteChanged();
        }

        private async Task CreateMitgliedsantragAsync()
        {
            if (!CanCreateMitgliedsantrag)
                return;

            var result = await _supabaseService.CreateMitgliedsantragDokumentAsync(SelectedMember.Id, FormularDokumentStatus.Unsigniert);
            if (!result.Success)
            {
                MessageBox.Show(result.Message, "Mitgliedsantrag", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var document = result.Document;
            if (document?.CanOpen != true)
            {
                MessageBox.Show("Mitgliedsantrag wurde als Dokument abgelegt.", "Mitgliedsantrag", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var url = await _supabaseService.ResolveDokumentOpenUrlAsync(document, 3600);
            if (string.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show("Mitgliedsantrag wurde gespeichert, konnte aber nicht direkt geöffnet werden.", "Mitgliedsantrag", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        private async Task ChangeEmailAsync()
        {
            var vm = new ChangeEmailViewModel(_authService, SelectedMember.Email, CanChangeEmail);
            var window = new ChangeEmailWindow(vm)
            {
                Owner = Application.Current?.MainWindow
            };

            window.ShowDialog();
            await LoadMemberAsync();
        }

        private static int GetGartenNrSortKey(string? gartenNr)
        {
            if (string.IsNullOrWhiteSpace(gartenNr))
                return int.MaxValue;

            var digits = new string(gartenNr.TakeWhile(char.IsDigit).ToArray());
            return int.TryParse(digits, out var n) ? n : int.MaxValue;
        }

    }
}