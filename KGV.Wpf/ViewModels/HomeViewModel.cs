using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Helpers;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace KGV.ViewModels
{
    public sealed class HomeViewModel : BaseViewModel, INavigationAware
    {
        private readonly MainWindowViewModel _mainVm;

        public string Title => "Startseite";
        public string Description => "Zeigt die aktuell aus Rechten und Navigation belastbar ableitbaren Verwaltungszugänge an.";
        public string UserContextText => $"Kontext: {UserRoles.ToStorageValue(_mainVm.UserContext.Role)}";
        public string StatusMessage => ModuleItems.Count == 0 ? "Für diesen Benutzerkontext sind aktuell keine zusätzlichen Verwaltungszugänge verfügbar." : string.Empty;
        public string AnnouncementHintText => "Bitte eine Bekanntmachung aus der Liste auswählen.";
        public string AnnouncementEmptyText => "Aktuell sind keine Bekanntmachungen vorhanden.";
        public bool HasAnnouncements => Announcements.Count > 0;
        public bool HasSelectedAnnouncement => SelectedAnnouncement != null;
        public bool ShowAnnouncementHint => HasAnnouncements && !HasSelectedAnnouncement;
        public bool ShowAnnouncementEmptyState => !HasAnnouncements;
        public bool CanEditAnnouncements => _mainVm.UserContext.Role is UserRole.Admin or UserRole.Vorstand;

        public ObservableCollection<NavigationItem> ModuleItems { get; } = new();
        public ObservableCollection<HomeAnnouncementItem> Announcements { get; } = new();

        public RelayCommand<NavigationItem> OpenModuleCommand { get; }

        private HomeAnnouncementItem? _selectedAnnouncement;
        public HomeAnnouncementItem? SelectedAnnouncement
        {
            get => _selectedAnnouncement;
            set
            {
                if (_selectedAnnouncement == value)
                    return;

                _selectedAnnouncement = value;
                OnPropertyChanged(nameof(SelectedAnnouncement));
                OnPropertyChanged(nameof(HasSelectedAnnouncement));
                OnPropertyChanged(nameof(ShowAnnouncementHint));
            }
        }

        public HomeViewModel(MainWindowViewModel mainVm)
        {
            _mainVm = mainVm ?? throw new ArgumentNullException(nameof(mainVm));
            OpenModuleCommand = new RelayCommand<NavigationItem>(OpenModule, item => item?.ViewModelType != null && item.IsVisible);
        }

        public Task OnNavigatedToAsync()
        {
            BuildModules();
            LoadAnnouncements();
            return Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private void BuildModules()
        {
            ModuleItems.Clear();

            foreach (var item in _mainVm.NavigationItems
                         .Where(x => x.IsVisible && x.ViewModelType != null && x.ViewModelType != typeof(HomeViewModel)))
            {
                ModuleItems.Add(new NavigationItem
                {
                    Title = item.Title,
                    ViewModelType = item.ViewModelType,
                    Parameter = item.Parameter,
                    IsVisible = item.IsVisible,
                    IsAdminOnly = item.IsAdminOnly,
                    ButtonMargin = item.ButtonMargin
                });
            }

            OnPropertyChanged(nameof(StatusMessage));
        }

        private void LoadAnnouncements()
        {
            Announcements.Clear();
            SelectedAnnouncement = null;

            OnPropertyChanged(nameof(HasAnnouncements));
            OnPropertyChanged(nameof(ShowAnnouncementHint));
            OnPropertyChanged(nameof(ShowAnnouncementEmptyState));
        }

        private void OpenModule(NavigationItem? item)
        {
            if (item == null)
                return;

            _mainVm.NavigateCommand.Execute(item);
        }
    }
}
