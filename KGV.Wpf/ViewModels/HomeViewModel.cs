using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Helpers;
using System;
using System.Collections.Generic;
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
        public string OperationalTitle => _overview.OperationalTitle;
        public string OperationalEmptyText => _overview.OperationalEmptyText;
        public string AnnouncementTitle => _overview.AnnouncementTitle;
        public string AnnouncementHintText => _overview.AnnouncementHintText;
        public string AnnouncementEmptyText => _overview.AnnouncementEmptyText;
        public bool HasOperationalItems => OperationalItems.Count > 0;
        public bool ShowOperationalEmptyState => !HasOperationalItems;
        public bool HasAnnouncements => Announcements.Count > 0;
        public bool HasSelectedAnnouncement => SelectedAnnouncement != null;
        public bool ShowAnnouncementHint => HasAnnouncements && !HasSelectedAnnouncement;
        public bool ShowAnnouncementEmptyState => !HasAnnouncements;

        public ObservableCollection<HomeQuickLinkItem> QuickLinks { get; } = new();
        public ObservableCollection<HomeOperationalItem> OperationalItems { get; } = new();
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

        public async Task OnNavigatedToAsync()
        {
            await LoadAsync();
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private async Task LoadAsync()
        {
            _overview = await _mainVm.SupabaseService.GetHomeOverviewAsync(_mainVm.UserContext.Role, ToInt32(_mainVm.UserContext.MitgliedId));

            FillCollection(QuickLinks, _overview.QuickLinks);
            FillCollection(OperationalItems, _overview.OperationalItems);
            FillCollection(Announcements, _overview.Announcements);
            SelectedAnnouncement = null;

            OnPropertyChanged(nameof(Description));
            OnPropertyChanged(nameof(StatusMessage));
            OnPropertyChanged(nameof(QuickLinksTitle));
            OnPropertyChanged(nameof(OperationalTitle));
            OnPropertyChanged(nameof(OperationalEmptyText));
            OnPropertyChanged(nameof(HasOperationalItems));
            OnPropertyChanged(nameof(ShowOperationalEmptyState));
            OnPropertyChanged(nameof(AnnouncementTitle));
            OnPropertyChanged(nameof(AnnouncementHintText));
            OnPropertyChanged(nameof(AnnouncementEmptyText));
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

        private static void FillCollection<T>(ObservableCollection<T> target, IEnumerable<T> source)
        {
            target.Clear();
            foreach (var item in source)
                target.Add(item);
        }

        private static int? ToInt32(long? value)
        {
            return value is > 0 and <= int.MaxValue ? (int)value.Value : null;
        }
    }
}
