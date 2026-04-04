using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Helpers;

namespace KGV.ViewModels
{
    public sealed class AdminRoleViewModel : BaseViewModel, INavigationAware
    {
        private readonly ISupabaseService _supabaseService;
        private readonly IAuthService _authService;
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly INavigationService _navigationService;

        private string? _lockUserId;
        private bool _allowUserMeterReadingSubmissions;
        private bool _initialAllowUserMeterReadingSubmissions;

        public MemberDTO SelectedMember { get; }

        public ObservableCollection<string> Roles { get; } = new(UserRoles.AssignableRoles);

        private string _selectedRole = "user";
        public string SelectedRole
        {
            get => _selectedRole;
            set
            {
                if (SetProperty(ref _selectedRole, value ?? "user"))
                {
                    IsDirty = true;
                    SaveCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private bool _isDirty;
        public bool IsDirty
        {
            get => _isDirty;
            private set => SetProperty(ref _isDirty, value);
        }

        public bool IsRoleEditable => SelectedMember.Id != 7;
        public bool CanOpenUserManagement => _authService.IsAdmin && SelectedMember.Id > 0;
        public bool CanManageUserMeterReadingSubmissions => _authService.IsAdmin || _authService.IsVorstand;

        public bool AllowUserMeterReadingSubmissions
        {
            get => _allowUserMeterReadingSubmissions;
            set
            {
                if (!SetProperty(ref _allowUserMeterReadingSubmissions, value))
                    return;

                OnPropertyChanged(nameof(IsUserMeterReadingSubmissionSettingDirty));
                SaveCommand.RaiseCanExecuteChanged();
            }
        }

        public bool IsUserMeterReadingSubmissionSettingDirty => AllowUserMeterReadingSubmissions != _initialAllowUserMeterReadingSubmissions;

        public RelayCommand<object?> SaveCommand { get; }
        public RelayCommand<object?> OpenUserManagementCommand { get; }

        public AdminRoleViewModel(ISupabaseService supabaseService, IAuthService authService, MemberDTO member, MainWindowViewModel mainWindowViewModel, INavigationService navigationService)
        {
            _supabaseService = supabaseService;
            _authService = authService;
            SelectedMember = member;
            _mainWindowViewModel = mainWindowViewModel;
            _navigationService = navigationService;

            SaveCommand = new RelayCommand<object?>(_ => _ = SaveAsync(), _ => CanSave());
            OpenUserManagementCommand = new RelayCommand<object?>(_ => _ = OpenUserManagementAsync(), _ => CanOpenUserManagement);
        }

        public async Task OnNavigatedToAsync()
        {
            await LoadAsync();
            IsDirty = false;
            SaveCommand.RaiseCanExecuteChanged();
        }

        public async Task OnNavigatedFromAsync()
        {
            if (!string.IsNullOrEmpty(_lockUserId))
            {
                await _supabaseService.ReleaseLockMitgliedAsync(SelectedMember.Id, _lockUserId, force: false);
                _lockUserId = null;
            }
        }

        private async Task LoadAsync()
        {
            var rec = await _supabaseService.GetMitgliedByIdAsync(SelectedMember.Id);
            if (rec == null)
                return;

            SelectedMember.Vorname = rec.Vorname ?? string.Empty;
            SelectedMember.Nachname = rec.Name ?? string.Empty;
            SelectedMember.Mobilnummer = rec.Handy ?? string.Empty;
            SelectedMember.Role = rec.Role ?? "user";

            SelectedRole = SelectedMember.Role;
            AllowUserMeterReadingSubmissions = await _supabaseService.GetAllowUserMeterReadingSubmissionsAsync();
            _initialAllowUserMeterReadingSubmissions = AllowUserMeterReadingSubmissions;
            IsDirty = false;
            OnPropertyChanged(nameof(IsUserMeterReadingSubmissionSettingDirty));
            SaveCommand.RaiseCanExecuteChanged();
        }

        private bool CanSave()
        {
            var canSaveRole = _authService.IsAdmin && IsDirty && IsRoleEditable;
            var canSaveSetting = CanManageUserMeterReadingSubmissions && IsUserMeterReadingSubmissionSettingDirty;
            return canSaveRole || canSaveSetting;
        }

        private async Task SaveAsync()
        {
            try
            {
                if (_authService.IsAdmin && IsDirty && !IsRoleEditable)
                {
                    MessageBox.Show("Für dieses Mitglied ist die Rollenbearbeitung gesperrt.", "Gesperrt", MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                if (!CanSave())
                    return;

                var savedParts = new List<string>();

                if (_authService.IsAdmin && IsDirty)
                {
                    var userId = _authService.CurrentUserId;
                    if (string.IsNullOrWhiteSpace(userId))
                    {
                        MessageBox.Show("Nicht angemeldet. Bitte erneut einloggen.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
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

                    var rec = await _supabaseService.GetMitgliedByIdAsync(SelectedMember.Id);
                    if (rec == null)
                    {
                        MessageBox.Show("Mitglied konnte nicht geladen werden.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var dto = new MemberDTO
                    {
                        Id = rec.Id,
                        Vorname = rec.Vorname ?? string.Empty,
                        Nachname = rec.Name ?? string.Empty,
                        Email = rec.Email ?? string.Empty,
                        Role = SelectedRole,

                        Geburtsdatum = rec.Geburtsdatum,
                        Strasse = rec.Adresse ?? string.Empty,
                        PLZ = rec.Plz ?? string.Empty,
                        Ort = rec.Ort ?? string.Empty,
                        Telefon = rec.Telefon ?? string.Empty,
                        Mobilnummer = rec.Handy ?? string.Empty,
                        Bemerkungen = rec.Bemerkung ?? string.Empty,
                        WhatsappEinwilligung = rec.WhatsappEinwilligung,
                        MitgliedSeit = rec.MitgliedSeit,
                        MitgliedEnde = rec.MitgliedEnde
                    };

                    var ok = await _supabaseService.UpdateMitgliedAsync(dto, userId);
                    if (!ok)
                    {
                        MessageBox.Show("Speichern fehlgeschlagen (ggf. Lock verloren oder keine Berechtigung).", "Fehler",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    SelectedMember.Role = SelectedRole;
                    IsDirty = false;
                    savedParts.Add("Rolle");

                    await _supabaseService.ReleaseLockMitgliedAsync(SelectedMember.Id, userId, force: false);
                    _lockUserId = null;
                }

                if (CanManageUserMeterReadingSubmissions && IsUserMeterReadingSubmissionSettingDirty)
                {
                    var ok = await _supabaseService.SetAllowUserMeterReadingSubmissionsAsync(AllowUserMeterReadingSubmissions);
                    if (!ok)
                    {
                        MessageBox.Show("Die globale Ablesungs-Einstellung konnte nicht gespeichert werden.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    _initialAllowUserMeterReadingSubmissions = AllowUserMeterReadingSubmissions;
                    OnPropertyChanged(nameof(IsUserMeterReadingSubmissionSettingDirty));
                    savedParts.Add("Ablesungs-Einstellung");
                }

                SaveCommand.RaiseCanExecuteChanged();
                MessageBox.Show(savedParts.Count == 0 ? "Keine Änderungen gespeichert." : $"Gespeichert: {string.Join(", ", savedParts)}.", "OK", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Speichern: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task OpenUserManagementAsync()
        {
            if (!CanOpenUserManagement)
                return;

            var created = _navigationService.CreateViewModel(typeof(UserManagementViewModel), _mainWindowViewModel, SelectedMember);
            if (created is not BaseViewModel vm)
            {
                MessageBox.Show("Benutzerverwaltung konnte nicht geöffnet werden.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            await _mainWindowViewModel.NavigateToAsync(vm);
        }
    }
}
