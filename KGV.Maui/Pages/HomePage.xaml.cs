using KGV.Core.Models;
using KGV.Maui.State;
using KGV.Maui.ViewModels;
using System.Linq;

namespace KGV.Maui.Pages;

public class HomePage : ContentPage
{
    private readonly HomeViewModel _viewModel;
    private readonly HomeContextState _homeContextState;
    private bool _initialized;

    public HomePage(HomeViewModel viewModel, HomeContextState homeContextState)
    {
        _viewModel = viewModel;
        _homeContextState = homeContextState;
        BindingContext = _viewModel;
        Title = "Startseite";

        var titleLabel = new Label { FontSize = 24, FontAttributes = FontAttributes.Bold };
        titleLabel.SetBinding(Label.TextProperty, nameof(HomeViewModel.Title));

        var descriptionLabel = new Label { LineBreakMode = LineBreakMode.WordWrap };
        descriptionLabel.SetBinding(Label.TextProperty, nameof(HomeViewModel.Description));

        var contextLabel = new Label { TextColor = Colors.Gray, FontSize = 12 };
        contextLabel.SetBinding(Label.TextProperty, nameof(HomeViewModel.UserContextText));

        var quickLinksLabel = new Label { FontSize = 18, FontAttributes = FontAttributes.Bold };
        quickLinksLabel.SetBinding(Label.TextProperty, nameof(HomeViewModel.QuickLinksTitle));

        var quickLinksView = new CollectionView
        {
            SelectionMode = SelectionMode.Single,
            HeightRequest = 180,
            ItemTemplate = new DataTemplate(() =>
            {
                var itemTitle = new Label { FontAttributes = FontAttributes.Bold };
                itemTitle.SetBinding(Label.TextProperty, nameof(HomeQuickLinkItem.Title));

                var itemDescription = new Label { FontSize = 12, TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
                itemDescription.SetBinding(Label.TextProperty, nameof(HomeQuickLinkItem.Description));

                return new VerticalStackLayout
                {
                    Padding = new Thickness(0, 8),
                    Children = { itemTitle, itemDescription }
                };
            })
        };
        quickLinksView.SetBinding(ItemsView.ItemsSourceProperty, nameof(HomeViewModel.QuickLinks));
        quickLinksView.SetBinding(IsVisibleProperty, nameof(HomeViewModel.HasQuickLinks));
        quickLinksView.SelectionChanged += async (_, e) =>
        {
            var selected = e.CurrentSelection?.FirstOrDefault() as HomeQuickLinkItem;
            if (selected == null)
                return;

            if (TryGetRoute(selected, out var route))
                await Shell.Current.GoToAsync($"//{route}");

            quickLinksView.SelectedItem = null;
        };

        var quickLinksEmptyLabel = new Label { TextColor = Colors.Gray };
        quickLinksEmptyLabel.SetBinding(Label.TextProperty, nameof(HomeViewModel.QuickLinksEmptyText));
        quickLinksEmptyLabel.SetBinding(IsVisibleProperty, nameof(HomeViewModel.HasQuickLinks), converter: new InverseBooleanConverter());

        var operationalLabel = new Label { FontSize = 18, FontAttributes = FontAttributes.Bold };
        operationalLabel.SetBinding(Label.TextProperty, nameof(HomeViewModel.OperationalTitle));

        var operationalView = new CollectionView
        {
            SelectionMode = SelectionMode.None,
            HeightRequest = 160,
            ItemTemplate = new DataTemplate(() =>
            {
                var itemTitle = new Label { FontAttributes = FontAttributes.Bold };
                itemTitle.SetBinding(Label.TextProperty, nameof(HomeOperationalItem.Title));

                var itemMessage = new Label { FontSize = 12, LineBreakMode = LineBreakMode.WordWrap };
                itemMessage.SetBinding(Label.TextProperty, nameof(HomeOperationalItem.Message));

                return new Border
                {
                    Padding = 12,
                    Stroke = Colors.LightGray,
                    Content = new VerticalStackLayout
                    {
                        Spacing = 4,
                        Children = { itemTitle, itemMessage }
                    }
                };
            })
        };
        operationalView.SetBinding(ItemsView.ItemsSourceProperty, nameof(HomeViewModel.OperationalItems));
        operationalView.SetBinding(IsVisibleProperty, nameof(HomeViewModel.HasOperationalItems));

        var operationalEmptyLabel = new Label { TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
        operationalEmptyLabel.SetBinding(Label.TextProperty, nameof(HomeViewModel.OperationalEmptyText));
        operationalEmptyLabel.SetBinding(IsVisibleProperty, nameof(HomeViewModel.HasOperationalItems), converter: new InverseBooleanConverter());

        var workAssignmentsLabel = new Label { FontSize = 18, FontAttributes = FontAttributes.Bold };
        workAssignmentsLabel.SetBinding(Label.TextProperty, nameof(HomeViewModel.WorkAssignmentsTitle));

        var workAssignmentsView = CreateHomeListView<HomeWorkAssignmentItem>(item =>
        {
            _homeContextState.SetWorkAssignment(item);
            return Shell.Current.GoToAsync(nameof(HomeSectionDetailPage));
        }, item =>
        {
            var title = new Label { FontAttributes = FontAttributes.Bold };
            title.SetBinding(Label.TextProperty, nameof(HomeWorkAssignmentItem.Title));

            var subtitle = new Label { FontSize = 12, TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
            subtitle.SetBinding(Label.TextProperty, nameof(HomeWorkAssignmentItem.Subtitle));

            var time = new Label { FontSize = 12, TextColor = Colors.Gray };
            time.SetBinding(Label.TextProperty, nameof(HomeWorkAssignmentItem.TimeText));
            time.SetBinding(IsVisibleProperty, nameof(HomeWorkAssignmentItem.HasTimeText));

            var registration = new Label { FontSize = 12, TextColor = Colors.DarkSlateBlue, LineBreakMode = LineBreakMode.WordWrap };
            registration.SetBinding(Label.TextProperty, nameof(HomeWorkAssignmentItem.RegistrationInfo));
            registration.SetBinding(IsVisibleProperty, nameof(HomeWorkAssignmentItem.HasRegistrationInfo));

            return new VerticalStackLayout
            {
                Padding = new Thickness(0, 8),
                Children = { title, subtitle, time, registration }
            };
        });
        workAssignmentsView.SetBinding(ItemsView.ItemsSourceProperty, nameof(HomeViewModel.WorkAssignments));
        workAssignmentsView.SetBinding(IsVisibleProperty, nameof(HomeViewModel.HasWorkAssignments));

        var workAssignmentsEmptyLabel = new Label { TextColor = Colors.Gray };
        workAssignmentsEmptyLabel.SetBinding(Label.TextProperty, nameof(HomeViewModel.WorkAssignmentsEmptyText));
        workAssignmentsEmptyLabel.SetBinding(IsVisibleProperty, nameof(HomeViewModel.ShowWorkAssignmentsEmptyState));

        var appointmentsLabel = new Label { FontSize = 18, FontAttributes = FontAttributes.Bold };
        appointmentsLabel.SetBinding(Label.TextProperty, nameof(HomeViewModel.AppointmentsTitle));

        var appointmentsView = CreateHomeListView<HomeAppointmentItem>(item =>
        {
            _homeContextState.SetAppointment(item);
            return Shell.Current.GoToAsync(nameof(HomeSectionDetailPage));
        }, item =>
        {
            var title = new Label { FontAttributes = FontAttributes.Bold };
            title.SetBinding(Label.TextProperty, nameof(HomeAppointmentItem.Title));

            var subtitle = new Label { FontSize = 12, TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
            subtitle.SetBinding(Label.TextProperty, nameof(HomeAppointmentItem.Subtitle));

            var time = new Label { FontSize = 12, TextColor = Colors.Gray };
            time.SetBinding(Label.TextProperty, nameof(HomeAppointmentItem.TimeText));
            time.SetBinding(IsVisibleProperty, nameof(HomeAppointmentItem.HasTimeText));

            return new VerticalStackLayout
            {
                Padding = new Thickness(0, 8),
                Children = { title, subtitle, time }
            };
        });
        appointmentsView.SetBinding(ItemsView.ItemsSourceProperty, nameof(HomeViewModel.Appointments));
        appointmentsView.SetBinding(IsVisibleProperty, nameof(HomeViewModel.HasAppointments));

        var appointmentsEmptyLabel = new Label { TextColor = Colors.Gray };
        appointmentsEmptyLabel.SetBinding(Label.TextProperty, nameof(HomeViewModel.AppointmentsEmptyText));
        appointmentsEmptyLabel.SetBinding(IsVisibleProperty, nameof(HomeViewModel.ShowAppointmentsEmptyState));

        var announcementsLabel = new Label { FontSize = 18, FontAttributes = FontAttributes.Bold };
        announcementsLabel.SetBinding(Label.TextProperty, nameof(HomeViewModel.AnnouncementTitle));

        var announcementsView = CreateHomeListView<HomeAnnouncementItem>(item =>
        {
            _homeContextState.SetAnnouncement(item);
            return Shell.Current.GoToAsync(nameof(HomeSectionDetailPage));
        }, item =>
        {
            var title = new Label { FontAttributes = FontAttributes.Bold };
            title.SetBinding(Label.TextProperty, nameof(HomeAnnouncementItem.Title));

            var subtitle = new Label { FontSize = 12, TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
            subtitle.SetBinding(Label.TextProperty, nameof(HomeAnnouncementItem.Subtitle));

            return new VerticalStackLayout
            {
                Padding = new Thickness(0, 8),
                Children = { title, subtitle }
            };
        });
        announcementsView.SetBinding(ItemsView.ItemsSourceProperty, nameof(HomeViewModel.Announcements));
        announcementsView.SetBinding(IsVisibleProperty, nameof(HomeViewModel.HasAnnouncements));

        var announcementEmptyLabel = new Label { TextColor = Colors.Gray };
        announcementEmptyLabel.SetBinding(Label.TextProperty, nameof(HomeViewModel.AnnouncementEmptyText));
        announcementEmptyLabel.SetBinding(IsVisibleProperty, nameof(HomeViewModel.ShowAnnouncementEmptyState));

        var managementTitleLabel = new Label { FontSize = 18, FontAttributes = FontAttributes.Bold };
        managementTitleLabel.SetBinding(Label.TextProperty, nameof(HomeViewModel.ManagementTitle));
        managementTitleLabel.SetBinding(IsVisibleProperty, nameof(HomeViewModel.ShowManagementSection));

        var managementHintLabel = new Label { TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
        managementHintLabel.SetBinding(Label.TextProperty, nameof(HomeViewModel.ManagementHintText));
        managementHintLabel.SetBinding(IsVisibleProperty, nameof(HomeViewModel.ShowManagementSection));

        var workAssignmentsManagementButton = new Button { Text = "Arbeitseinsätze bearbeiten" };
        workAssignmentsManagementButton.SetBinding(IsVisibleProperty, nameof(HomeViewModel.ShowManagementSection));
        workAssignmentsManagementButton.Clicked += async (_, _) => await Shell.Current.GoToAsync($"{nameof(HomeManagementPage)}?section=workassignments");

        var appointmentsManagementButton = new Button { Text = "Termine bearbeiten" };
        appointmentsManagementButton.SetBinding(IsVisibleProperty, nameof(HomeViewModel.ShowManagementSection));
        appointmentsManagementButton.Clicked += async (_, _) => await Shell.Current.GoToAsync($"{nameof(HomeManagementPage)}?section=appointments");

        var announcementsManagementButton = new Button { Text = "Bekanntmachungen bearbeiten" };
        announcementsManagementButton.SetBinding(IsVisibleProperty, nameof(HomeViewModel.ShowManagementSection));
        announcementsManagementButton.Clicked += async (_, _) => await Shell.Current.GoToAsync($"{nameof(HomeManagementPage)}?section=announcements");

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 12,
                Children =
                {
                    titleLabel,
                    descriptionLabel,
                    contextLabel,
                    quickLinksLabel,
                    quickLinksView,
                    quickLinksEmptyLabel,
                    operationalLabel,
                    operationalView,
                    operationalEmptyLabel,
                    workAssignmentsLabel,
                    workAssignmentsView,
                    workAssignmentsEmptyLabel,
                    appointmentsLabel,
                    appointmentsView,
                    appointmentsEmptyLabel,
                    announcementsLabel,
                    announcementsView,
                    announcementEmptyLabel,
                    managementTitleLabel,
                    managementHintLabel,
                    workAssignmentsManagementButton,
                    appointmentsManagementButton,
                    announcementsManagementButton
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_initialized)
            return;

        await _viewModel.InitializeAsync();
        _initialized = true;
    }

    private static CollectionView CreateHomeListView<T>(Func<T, Task> openAsync, Func<T, View> templateFactory) where T : class
    {
        var listView = new CollectionView
        {
            SelectionMode = SelectionMode.Single,
            HeightRequest = 180,
            ItemTemplate = new DataTemplate(() =>
            {
                var container = new ContentView();
                container.BindingContextChanged += (_, _) =>
                {
                    if (container.BindingContext is T item)
                        container.Content = templateFactory(item);
                };
                return container;
            })
        };

        listView.SelectionChanged += async (_, e) =>
        {
            var selected = e.CurrentSelection?.FirstOrDefault() as T;
            if (selected == null)
                return;

            await openAsync(selected);
            listView.SelectedItem = null;
        };

        return listView;
    }

    private static bool TryGetRoute(HomeQuickLinkItem item, out string route)
    {
        route = item.Key switch
        {
            HomeQuickLinkKey.MemberSearch => "membersearch",
            HomeQuickLinkKey.PlotManagement => "parzellen",
            HomeQuickLinkKey.MyProfile => "myprofile",
            HomeQuickLinkKey.MyWorkHours => "workhours",
            _ => string.Empty
        };

        return !string.IsNullOrWhiteSpace(route);
    }

    private sealed class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
            => value is bool b ? !b : true;

        public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
            => throw new NotSupportedException();
    }
}
