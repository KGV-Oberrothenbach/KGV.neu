using KGV.Core.Models;
using KGV.Maui.ViewModels;
using System.Linq;

namespace KGV.Maui.Pages;

public class HomePage : ContentPage
{
    private readonly HomeViewModel _viewModel;
    private bool _initialized;

    public HomePage(HomeViewModel viewModel)
    {
        _viewModel = viewModel;
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

        var sectionLabel = new Label { FontSize = 18, FontAttributes = FontAttributes.Bold };
        sectionLabel.SetBinding(Label.TextProperty, nameof(HomeViewModel.AnnouncementTitle));

        var announcementsView = new CollectionView
        {
            SelectionMode = SelectionMode.Single,
            HeightRequest = 180,
            ItemTemplate = new DataTemplate(() =>
            {
                var itemTitle = new Label { FontAttributes = FontAttributes.Bold };
                itemTitle.SetBinding(Label.TextProperty, nameof(HomeAnnouncementItem.Title));

                return new VerticalStackLayout
                {
                    Padding = new Thickness(0, 8),
                    Children = { itemTitle }
                };
            })
        };
        announcementsView.SetBinding(ItemsView.ItemsSourceProperty, nameof(HomeViewModel.Announcements));
        announcementsView.SetBinding(IsVisibleProperty, nameof(HomeViewModel.HasAnnouncements));
        announcementsView.SelectionChanged += (_, e) =>
        {
            _viewModel.SelectedAnnouncement = e.CurrentSelection?.FirstOrDefault() as HomeAnnouncementItem;
        };

        var emptyLabel = new Label { TextColor = Colors.Gray };
        emptyLabel.SetBinding(Label.TextProperty, nameof(HomeViewModel.AnnouncementEmptyText));
        emptyLabel.SetBinding(IsVisibleProperty, nameof(HomeViewModel.ShowAnnouncementEmptyState));

        var detailLabel = new Label { Text = "Detail", FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center };

        var selectedTitle = new Label { FontAttributes = FontAttributes.Bold, FontSize = 16 };
        selectedTitle.SetBinding(Label.TextProperty, "SelectedAnnouncement.Title");
        selectedTitle.SetBinding(IsVisibleProperty, nameof(HomeViewModel.HasSelectedAnnouncement));

        var selectedContent = new Label { LineBreakMode = LineBreakMode.WordWrap };
        selectedContent.SetBinding(Label.TextProperty, "SelectedAnnouncement.Content");
        selectedContent.SetBinding(IsVisibleProperty, nameof(HomeViewModel.HasSelectedAnnouncement));

        var hintLabel = new Label { TextColor = Colors.Gray };
        hintLabel.SetBinding(Label.TextProperty, nameof(HomeViewModel.AnnouncementHintText));
        hintLabel.SetBinding(IsVisibleProperty, nameof(HomeViewModel.ShowAnnouncementHint));

        var detailHeader = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 12
        };
        detailHeader.SetBinding(IsVisibleProperty, nameof(HomeViewModel.ShowAnnouncementDetail));
        detailHeader.Children.Add(detailLabel);
        Grid.SetColumn(detailLabel, 0);
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
                sectionLabel,
                announcementsView,
                emptyLabel,
                detailHeader,
                selectedTitle,
                selectedContent,
                hintLabel
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
