using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Helpers;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Messaging;
using KGV.Messages;

namespace KGV.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly IAuthService _authService;
        private readonly INavigationService _navigationService;
        private readonly ISupabaseService _supabaseService;

        public UserContext UserContext { get; }

        private readonly SemaphoreSlim _navLock = new(1, 1);

        // ======= Saison =======
        public ObservableCollection<string> Seasons { get; } = new();

        private string? _selectedSeason;
        public string? SelectedSeason
        {
            get => _selectedSeason;
            set
            {
                if (_selectedSeason == value) return;
                _selectedSeason = value;
                OnPropertyChanged();
            }
        }

        // ======= Navigation =======
        public ObservableCollection<NavigationItem> NavigationItems { get; } = new();
        public ObservableCollection<NavigationItem> MemberNavigationItems { get; } = new();

        public ICommand NavigateCommand { get; }

        // ======= Rechte =======
        private bool _isAdmin;
        public bool IsAdmin
        {
            get => _isAdmin;
            set
            {
                if (_isAdmin == value) return;
                _isAdmin = value;
                OnPropertyChanged();
                UpdateNavigationVisibility();
                UpdateMemberNavigationVisibility();
            }
        }

        // ======= Current VM (ContentControl) =======
        private BaseViewModel? _currentViewModel;
        public BaseViewModel? CurrentViewModel
        {
            get => _currentViewModel;
            set
            {
                if (_currentViewModel == value) return;
                _currentViewModel = value;
                OnPropertyChanged();
            }
        }

        // ======= Selected Member =======
        private MemberDTO? _selectedMember;
        public MemberDTO? SelectedMember
        {
            get => _selectedMember;
            set
            {
                if (_selectedMember == value) return;
                _selectedMember = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsMemberSelected));
                SelectedParzelle = null;
                BuildMemberNavigation();
                UpdateMemberNavigationVisibility();
            }
        }

        public bool IsMemberSelected => SelectedMember != null;

        private ParzellenBelegungDTO? _selectedParzelle;
        public ParzellenBelegungDTO? SelectedParzelle
        {
            get => _selectedParzelle;
            private set
            {
                if (_selectedParzelle == value) return;
                _selectedParzelle = value;
                OnPropertyChanged();
            }
        }

        public IAuthService AuthService => _authService;
        public ISupabaseService SupabaseService => _supabaseService;

        public MainWindowViewModel(
            IAuthService authService,
            INavigationService navigationService,
            ISupabaseService supabaseService,
            UserContext userContext)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
            UserContext = userContext ?? throw new ArgumentNullException(nameof(userContext));

            NavigateCommand = new RelayCommand<NavigationItem>(item => _ = NavigateByItemAsync(item));

            SeedSeasons();
            BuildNavigation();
            BuildMemberNavigation();
            UpdateNavigationVisibility();
            UpdateMemberNavigationVisibility();

            IsAdmin = UserContext.Has(PermissionFlags.CanEditAllMembers);

            WeakReferenceMessenger.Default.Register<ParzelleSelectedMessage>(this, (_, msg) =>
                _ = OnParzelleSelectedAsync(msg.Belegung));

            WeakReferenceMessenger.Default.Register<ParzelleContextChangedMessage>(this, (_, msg) =>
                OnParzelleContextChanged(msg.Belegung));

            WeakReferenceMessenger.Default.Register<NebenmitgliedSelectedMessage>(this, (_, msg) =>
                _ = OnNebenmitgliedSelectedAsync(msg.Context));

            WeakReferenceMessenger.Default.Register<ArbeitsstundenChangedMessage>(this, (_, _) =>
                _ = RefreshArbeitsstundenPruefungStatusAsync());

            _ = RefreshArbeitsstundenPruefungStatusAsync();
            _ = InitializeHomeStartAsync();
        }

        private async Task InitializeHomeStartAsync()
        {
            try
            {
                if (!UserContext.Has(PermissionFlags.CanSearchMembers))
                    await EnsureCurrentMemberSelectedAsync();

                var created = _navigationService.CreateViewModel(typeof(HomeViewModel), this);
                if (created is BaseViewModel vm)
                    await NavigateToAsync(vm);
            }
            catch
            {
            }
        }

        public async Task<MemberDTO?> EnsureCurrentMemberSelectedAsync()
        {
            try
            {
                if (!UserContext.MitgliedId.HasValue)
                    return SelectedMember;

                if (UserContext.MitgliedId.Value > int.MaxValue)
                    return SelectedMember;

                var myId = (int)UserContext.MitgliedId.Value;

                if (SelectedMember?.Id == myId && !string.IsNullOrWhiteSpace(SelectedMember.Vorname + SelectedMember.Nachname + SelectedMember.Email))
                    return SelectedMember;

                SelectedMember ??= new MemberDTO { Id = myId };

                var rec = await _supabaseService.GetMitgliedByIdAsync(myId);
                if (rec == null)
                    return SelectedMember;

                var dto = MapToDTO(rec);
                SelectedMember = dto.Clone();
                return SelectedMember;
            }
            catch
            {
                return SelectedMember;
            }
        }

        private async Task OnNebenmitgliedSelectedAsync(NebenmitgliedContext ctx)
        {
            var created = _navigationService.CreateViewModel(typeof(NebenmitgliedDetailViewModel), this, ctx);
            if (created is BaseViewModel vm)
                await NavigateToAsync(vm);
        }

        private void OnParzelleContextChanged(ParzellenBelegungDTO? belegung)
        {
            if (SelectedMember == null)
                return;

            SelectedParzelle = belegung;
            BuildMemberNavigation();
            UpdateMemberNavigationVisibility();
        }

        private void SeedSeasons()
        {
            if (Seasons.Count > 0) return;

            Seasons.Add("2024");
            Seasons.Add("2025");
            Seasons.Add("2026");
            SelectedSeason = "2026";
        }

        private void BuildNavigation()
        {
            NavigationItems.Clear();

            NavigationItems.Add(new NavigationItem
            {
                Title = "Startseite",
                ViewModelType = typeof(HomeViewModel),
                IsVisible = true
            });

            NavigationItems.Add(new NavigationItem
            {
                Title = "Impressum",
                ViewModelType = typeof(ImpressumViewModel),
                IsVisible = true
            });

            if (UserContext.Has(PermissionFlags.CanSeeOwnDataOnly))
            {
                NavigationItems.Add(new NavigationItem
                {
                    Title = "Meine Daten",
                    ViewModelType = typeof(MemberDetailViewModel),
                    IsVisible = true
                });
            }

            if (UserContext.Has(PermissionFlags.CanEditAllMembers))
            {
                NavigationItems.Add(new NavigationItem
                {
                    Title = "Parzellenverwaltung",
                    ViewModelType = typeof(ParzellenVerwaltungViewModel),
                    IsVisible = true,
                    IsAdminOnly = true
                });

                NavigationItems.Add(new NavigationItem
                {
                    Title = "Wartungsverträge",
                    ViewModelType = typeof(WartungsvertraegeVerwaltungViewModel),
                    IsVisible = true,
                    IsAdminOnly = true
                });
            }

            if (PermissionChecks.HasAnyMeterAccess(UserContext))
            {
                NavigationItems.Add(new NavigationItem
                {
                    Title = "Ablesen",
                    ViewModelType = typeof(AblesenOverviewViewModel),
                    IsVisible = true,
                    IsAdminOnly = false
                });
            }

            if (UserContext.Has(PermissionFlags.CanManageWorkHours))
            {
                NavigationItems.Add(new NavigationItem
                {
                    Title = "Arbeitsstunden freigeben",
                    ViewModelType = typeof(ArbeitsstundenPruefungViewModel),
                    IsVisible = false,
                    IsAttention = false,
                    BadgeCount = 0
                });
            }

            // Admin-Menü wird nur im Mitglied-Kontext angeboten (siehe MemberNavigationItems)

            // Export (immer sichtbar)
            NavigationItems.Add(new NavigationItem
            {
                Title = "Export",
                ViewModelType = typeof(ExportViewModel),
                IsVisible = true
            });

            if (UserContext.Has(PermissionFlags.CanSearchMembers))
            {
                NavigationItems.Add(new NavigationItem
                {
                    Title = "Mitglieder suchen",
                    ViewModelType = typeof(MemberSearchViewModel),
                    IsVisible = true
                });
            }
        }

        private void BuildMemberNavigation()
        {
            MemberNavigationItems.Clear();

            // Stammdaten bearbeiten (Detail)
            MemberNavigationItems.Add(new NavigationItem
            {
                Title = "↳ Stammdaten",
                ViewModelType = typeof(MemberDetailViewModel),
                IsVisible = SelectedMember != null,
                ButtonMargin = new System.Windows.Thickness(25, 5, 5, 5)
            });

            MemberNavigationItems.Add(new NavigationItem
            {
                Title = "↳ Wartungsverträge",
                ViewModelType = typeof(MemberWartungsvertraegeViewModel),
                IsVisible = SelectedMember != null,
                ButtonMargin = new System.Windows.Thickness(25, 5, 5, 5)
            });

            MemberNavigationItems.Add(new NavigationItem
            {
                Title = "↳ Arbeitsstunden",
                ViewModelType = typeof(ArbeitsstundenViewModel),
                IsVisible = SelectedMember != null && (UserContext.Has(PermissionFlags.CanManageWorkHours) || UserContext.Has(PermissionFlags.CanSeeOwnDataOnly)),
                ButtonMargin = new System.Windows.Thickness(25, 5, 5, 5)
            });

            MemberNavigationItems.Add(new NavigationItem
            {
                Title = "↳ Dokumente",
                ViewModelType = typeof(DokumenteViewModel),
                IsVisible = SelectedMember != null && (UserContext.Has(PermissionFlags.CanManageDocuments) || UserContext.Has(PermissionFlags.CanSeeOwnDataOnly)),
                ButtonMargin = new System.Windows.Thickness(25, 5, 5, 5)
            });

            // Admin-Menü nur im Mitglied-Kontext
            MemberNavigationItems.Add(new NavigationItem
            {
                Title = "↳ Admin-Menü",
                ViewModelType = typeof(AdminRoleViewModel),
                IsAdminOnly = true,
                IsVisible = SelectedMember != null,
                ButtonMargin = new System.Windows.Thickness(25, 5, 5, 5)
            });

            if (SelectedMember == null || SelectedParzelle == null)
                return;

            // Überschrift (nicht klickbar)
            MemberNavigationItems.Add(new NavigationItem
            {
                Title = $"Garten Nr. {SelectedParzelle.GartenNr}",
                ViewModelType = null,
                IsVisible = true,
                ButtonMargin = new System.Windows.Thickness(5, 12, 5, 4)
            });

            MemberNavigationItems.Add(new NavigationItem
            {
                Title = "Strom",
                ViewModelType = typeof(GartenStromViewModel),
                Parameter = SelectedParzelle,
                IsVisible = true,
                ButtonMargin = new System.Windows.Thickness(25, 5, 5, 5)
            });

            MemberNavigationItems.Add(new NavigationItem
            {
                Title = "Wasser",
                ViewModelType = typeof(GartenWasserViewModel),
                Parameter = SelectedParzelle,
                IsVisible = true,
                ButtonMargin = new System.Windows.Thickness(25, 5, 5, 5)
            });

            MemberNavigationItems.Add(new NavigationItem
            {
                Title = "Dokumente",
                ViewModelType = typeof(GartenDokumenteViewModel),
                Parameter = SelectedParzelle,
                IsVisible = true,
                ButtonMargin = new System.Windows.Thickness(25, 5, 5, 5)
            });
        }

        private static MemberDTO MapToDTO(MitgliedRecord m)
        {
            return new MemberDTO
            {
                Id = m.Id,
                Vorname = m.Vorname ?? string.Empty,
                Nachname = m.Name ?? string.Empty,
                Email = m.Email ?? string.Empty,
                Role = m.Role ?? string.Empty
            };
        }

        private async Task OnParzelleSelectedAsync(ParzellenBelegungDTO belegung)
        {
            if (SelectedMember == null)
                return;

            SelectedParzelle = belegung;
            BuildMemberNavigation();
            UpdateMemberNavigationVisibility();

            // Default: nach Doppelklick direkt in Strom-Ansicht springen
            var created = _navigationService.CreateViewModel(typeof(GartenStromViewModel), this, SelectedParzelle);
            if (created is BaseViewModel vm)
                await NavigateToAsync(vm);
        }

        private void UpdateNavigationVisibility()
        {
            foreach (var item in NavigationItems)
            {
                if (item.ViewModelType == typeof(ArbeitsstundenPruefungViewModel))
                    continue;

                item.IsVisible = !item.IsAdminOnly || IsAdmin;
            }

            // Refresh für UI (NavigationItem hat kein INotifyPropertyChanged)
            OnPropertyChanged(nameof(NavigationItems));
        }

        private void UpdateMemberNavigationVisibility()
        {
            foreach (var item in MemberNavigationItems)
            {
                if (SelectedMember == null)
                {
                    item.IsVisible = false;
                    continue;
                }

                var visible = true;

                if (item.IsAdminOnly)
                    visible = IsAdmin;

                if (item.ViewModelType == typeof(ArbeitsstundenViewModel))
                    visible = visible && UserContext.Has(PermissionFlags.CanManageWorkHours);

                if (item.ViewModelType == typeof(DokumenteViewModel))
                    visible = visible && (PermissionChecks.CanManageDocuments(UserContext) || UserContext.Has(PermissionFlags.CanSeeOwnDataOnly));

                if (item.ViewModelType == typeof(GartenStromViewModel) ||
                    item.ViewModelType == typeof(GartenWasserViewModel) ||
                    item.ViewModelType == typeof(GartenDokumenteViewModel))
                    visible = visible && SelectedParzelle != null;

                // Überschrift "Garten Nr..." (nicht klickbar)
                if (item.ViewModelType == null)
                    visible = SelectedParzelle != null;

                item.IsVisible = visible;
            }

            OnPropertyChanged(nameof(MemberNavigationItems));
        }

        private async Task NavigateByItemAsync(NavigationItem? item)
        {
            if (item == null) return;
            if (!item.IsVisible) return;
            if (item.ViewModelType == null) return;

            try
            {

            object? parameter = item.Parameter;

            // MemberDetail braucht MemberDTO
            if (item.ViewModelType == typeof(MemberDetailViewModel))
            {
                if (SelectedMember == null) return;
                parameter = SelectedMember;
            }

            if (item.ViewModelType == typeof(ArbeitsstundenViewModel))
            {
                if (SelectedMember == null) return;
                parameter = SelectedMember;
            }

            if (item.ViewModelType == typeof(MemberWartungsvertraegeViewModel))
            {
                if (SelectedMember == null) return;
                parameter = SelectedMember;
            }

            if (item.ViewModelType == typeof(DokumenteViewModel))
            {
                if (SelectedMember == null) return;
                parameter = new DokumenteContext(SelectedMember, null);
            }

            if (item.ViewModelType == typeof(AdminRoleViewModel))
            {
                if (SelectedMember == null) return;
                parameter = SelectedMember;
            }

            var created = _navigationService.CreateViewModel(item.ViewModelType, this, parameter);
            if (created is not BaseViewModel vm)
            {
                System.Windows.MessageBox.Show(
                    $"Navigation fehlgeschlagen: {item.Title}\nViewModel: {item.ViewModelType.Name}\nParameter: {(parameter == null ? "<null>" : parameter.GetType().Name)}",
                    "Fehler",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                return;
            }

            await NavigateToAsync(vm);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Navigation fehlgeschlagen: {item.Title}\n{ex.Message}",
                    "Fehler",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Navigation inkl. Lifecycle (OnNavigatedFrom/To) wenn ViewModels INavigationAware implementieren.
        /// </summary>
        public async Task NavigateToAsync(BaseViewModel viewModel)
        {
            if (viewModel == null) return;

            await _navLock.WaitAsync();
            try
            {
                if (CurrentViewModel is INavigationAware oldVm)
                    await oldVm.OnNavigatedFromAsync();

                CurrentViewModel = viewModel;

                if (viewModel is INavigationAware newVm)
                    await newVm.OnNavigatedToAsync();
            }
            finally
            {
                _navLock.Release();
            }
        }

        public void NavigateTo(BaseViewModel viewModel)
        {
            _ = NavigateToAsync(viewModel);
        }

        public BaseViewModel? NavigateToArbeitsstundenViewModel(MemberDTO member)
        {
            return _navigationService.CreateViewModel(typeof(ArbeitsstundenViewModel), this, member) as BaseViewModel;
        }

        public BaseViewModel? NavigateToHomeViewModel()
        {
            return _navigationService.CreateViewModel(typeof(HomeViewModel), this) as BaseViewModel;
        }

        public BaseViewModel? NavigateToArbeitsstundenViewModel(ArbeitsstundenNavigationContext context)
        {
            return _navigationService.CreateViewModel(typeof(ArbeitsstundenViewModel), this, context) as BaseViewModel;
        }

        public BaseViewModel? NavigateToArbeitsstundenErfassungViewModel()
        {
            return _navigationService.CreateViewModel(typeof(ArbeitsstundenErfassungViewModel), this) as BaseViewModel;
        }

        public BaseViewModel? NavigateToHomeSectionDetailViewModel(HomeSectionDetailContext context)
        {
            return _navigationService.CreateViewModel(typeof(HomeSectionDetailViewModel), this, context) as BaseViewModel;
        }

        public BaseViewModel? NavigateToArbeitseinsaetzeVerwaltungViewModel()
        {
            return _navigationService.CreateViewModel(typeof(ArbeitseinsaetzeVerwaltungViewModel), this) as BaseViewModel;
        }

        public BaseViewModel? NavigateToTermineVerwaltungViewModel()
        {
            return _navigationService.CreateViewModel(typeof(TermineVerwaltungViewModel), this) as BaseViewModel;
        }

        public BaseViewModel? NavigateToBekanntmachungenVerwaltungViewModel()
        {
            return _navigationService.CreateViewModel(typeof(BekanntmachungenVerwaltungViewModel), this) as BaseViewModel;
        }

        public BaseViewModel? NavigateToAblesenOverviewViewModel()
        {
            return _navigationService.CreateViewModel(typeof(AblesenOverviewViewModel), this) as BaseViewModel;
        }

        public BaseViewModel? NavigateToAblesungErfassenViewModel()
        {
            return _navigationService.CreateViewModel(typeof(AblesungErfassenViewModel), this) as BaseViewModel;
        }

        public BaseViewModel? NavigateToZaehlerwechselScanViewModel()
        {
            return _navigationService.CreateViewModel(typeof(ZaehlerwechselScanViewModel), this) as BaseViewModel;
        }

        public BaseViewModel? NavigateToRfidEinrichtenViewModel()
        {
            return _navigationService.CreateViewModel(typeof(RfidEinrichtenViewModel), this) as BaseViewModel;
        }

        public BaseViewModel? NavigateToFaelligeZaehlerViewModel()
        {
            return _navigationService.CreateViewModel(typeof(FaelligeZaehlerViewModel), this) as BaseViewModel;
        }

        public BaseViewModel? NavigateToAblesungenFreigabeViewModel()
        {
            return _navigationService.CreateViewModel(typeof(AblesungenFreigabeViewModel), this) as BaseViewModel;
        }

        public BaseViewModel? NavigateToFotoUploadTestViewModel()
        {
            return _navigationService.CreateViewModel(typeof(FotoUploadTestViewModel), this) as BaseViewModel;
        }

        private async Task RefreshArbeitsstundenPruefungStatusAsync()
        {
            if (!UserContext.Has(PermissionFlags.CanManageWorkHours))
                return;

            try
            {
                var navItem = NavigationItems.FirstOrDefault(x => x.ViewModelType == typeof(ArbeitsstundenPruefungViewModel));
                if (navItem == null)
                    return;

                var offene = await _supabaseService.GetUnapprovedArbeitsstundenByMitgliedAsync();
                var count = offene.Sum(x => x.Count);

                navItem.Title = count > 0
                    ? $"Arbeitsstunden freigeben ({count})"
                    : "Arbeitsstunden freigeben";
                navItem.BadgeCount = count;
                navItem.IsAttention = count > 0;
                navItem.IsVisible = count > 0;
                OnPropertyChanged(nameof(NavigationItems));
            }
            catch
            {
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}