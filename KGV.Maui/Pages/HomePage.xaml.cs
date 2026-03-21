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

        var contextLabel = new Label { TextColor = Colors.Gray, FontSize = 12 };
        contextLabel.SetBinding(Label.TextProperty, nameof(HomeViewModel.UserContextText));

        var sectionLabel = new Label { Text = "Bekanntmachungen", FontSize = 18, FontAttributes = FontAttributes.Bold };

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
        var editButton = new Button { Text = "Bearbeiten", IsEnabled = false, HorizontalOptions = LayoutOptions.End };
        editButton.SetBinding(IsVisibleProperty, nameof(HomeViewModel.CanEditAnnouncements));

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
        detailHeader.Children.Add(detailLabel);
        Grid.SetColumn(detailLabel, 0);
        detailHeader.Children.Add(editButton);
        Grid.SetColumn(editButton, 1);

        Content = new VerticalStackLayout
        {
            Padding = 24,
            Spacing = 12,
            Children =
            {
                titleLabel,
                contextLabel,
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

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_initialized)
            return;

        _viewModel.Initialize();
        _initialized = true;
    }
}
