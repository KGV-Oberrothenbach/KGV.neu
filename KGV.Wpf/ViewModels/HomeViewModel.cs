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
    public sealed class HomeViewModel : BaseViewModel, KGV.Core.Interfaces.INavigationAware
    {
        private readonly MainWindowViewModel _mainVm;
            private HomeOverviewDTO _overview = HomeOverviewFactory.Build(UserRole.User);

        public string Title => "Startseite";
            public string Description => _overview.Description;
        public string UserContextText => $"Kontext: {UserRoles.ToStorageValue(_mainVm.UserContext.Role)}";
            public string StatusMessage => QuickLinks.Count == 0 ? _overview.QuickLinksEmptyText : string.Empty;
            public string QuickLinksTitle => _overview.QuickLinksTitle;
            public string AnnouncementTitle => _overview.AnnouncementTitle;
            public string AnnouncementHintText => _overview.AnnouncementHintText;
            public string AnnouncementEmptyText => _overview.AnnouncementEmptyText;
        public bool HasAnnouncements => Announcements.Count > 0;
        public bool HasSelectedAnnouncement => SelectedAnnouncement != null;
        public bool ShowAnnouncementHint => HasAnnouncements && !HasSelectedAnnouncement;
        public bool ShowAnnouncementEmptyState => !HasAnnouncements;

            public ObservableCollection<HomeQuickLinkItem> QuickLinks { get; } = new();
        public ObservableCollection<HomeAnnouncementItem> Announcements { get; } = new();

            public RelayCommand<HomeQuickLinkItem> OpenModuleCommand { get; }

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
                OpenModuleCommand = new RelayCommand<HomeQuickLinkItem>(OpenModule, item => item != null);
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
            _overview = HomeOverviewFactory.Build(_mainVm.UserContext.Role);

            QuickLinks.Clear();
            foreach (var item in _overview.QuickLinks)
                QuickLinks.Add(item);

            OnPropertyChanged(nameof(StatusMessage));
            OnPropertyChanged(nameof(Description));
            OnPropertyChanged(nameof(QuickLinksTitle));
            OnPropertyChanged(nameof(AnnouncementTitle));
            OnPropertyChanged(nameof(AnnouncementHintText));
            OnPropertyChanged(nameof(AnnouncementEmptyText));
        }

        private void LoadAnnouncements()
        {
            Announcements.Clear();
            SelectedAnnouncement = null;

            OnPropertyChanged(nameof(HasAnnouncements));
            OnPropertyChanged(nameof(ShowAnnouncementHint));
            OnPropertyChanged(nameof(ShowAnnouncementEmptyState));
        }

        private void OpenModule(HomeQuickLinkItem? item)
        {
            if (item == null)
                return;

            var target = item.Key switch
            {
                HomeQuickLinkKey.MemberSearch => _mainVm.NavigationItems.FirstOrDefault(x => x.ViewModelType == typeof(MemberSearchViewModel) && x.IsVisible),
                HomeQuickLinkKey.PlotManagement => _mainVm.NavigationItems.FirstOrDefault(x => x.ViewModelType == typeof(ParzellenVerwaltungViewModel) && x.IsVisible),
                HomeQuickLinkKey.MyProfile => _mainVm.NavigationItems.FirstOrDefault(x => x.ViewModelType == typeof(MemberDetailViewModel) && x.IsVisible),
                _ => null
            };

            if (target != null)
                _mainVm.NavigateCommand.Execute(target);
        }
    }
}
