using KGV.Maui.ViewModels;
using KGV.Maui.State;
using KGV.Maui.Services.Diagnostics;
using KGV.Core.Security;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System.Linq;

namespace KGV.Maui.Pages;

public partial class MemberSearchPage : ContentPage
{
    private readonly MemberSearchViewModel _vm;
    private readonly MemberContextState _memberContextState;
    private readonly MemberSearchRefreshState _memberSearchRefreshState;
    private readonly UserContextState _userContextState;
    private CollectionView? _resultsCollectionView;
    private bool _memberSwitchInProgress;

    public MemberSearchPage(MemberSearchViewModel vm, MemberContextState memberContextState, MemberSearchRefreshState memberSearchRefreshState, UserContextState userContextState)
    {
        _vm = vm;
        _memberContextState = memberContextState;
        _memberSearchRefreshState = memberSearchRefreshState;
        _userContextState = userContextState;
        BindingContext = _vm;
        Title = "Mitgliedersuche";

        Content = BuildContent();

        Appearing += MemberSearchPage_Appearing;
    }

    private View BuildContent()
    {
        var titleLabel = new Label
        {
            Text = "Mitgliedersuche",
            FontSize = 24,
            FontAttributes = FontAttributes.Bold
        };

        var searchEntry = new Entry
        {
            Placeholder = "Suche"
        };
        searchEntry.SetBinding(Entry.TextProperty, nameof(MemberSearchViewModel.SearchText));

        var searchButton = new Button
        {
            Text = "Suchen"
        };
        searchButton.SetBinding(Button.CommandProperty, nameof(MemberSearchViewModel.SearchCommand));

        var searchGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 12,
            Children = { searchEntry, searchButton }
        };
        Grid.SetColumn(searchButton, 1);

        var searchByParzelleCheckBox = new CheckBox();
        searchByParzelleCheckBox.SetBinding(CheckBox.IsCheckedProperty, nameof(MemberSearchViewModel.SearchByParzelle));

        var searchByParzelleLabel = new Label
        {
            Text = "nach Gartennummer suchen",
            VerticalTextAlignment = TextAlignment.Center
        };

        var optionsLayout = new HorizontalStackLayout
        {
            Spacing = 8,
            Children = { searchByParzelleCheckBox, searchByParzelleLabel }
        };

        var activityIndicator = new ActivityIndicator();
        activityIndicator.SetBinding(ActivityIndicator.IsRunningProperty, nameof(MemberSearchViewModel.IsBusy));
        activityIndicator.SetBinding(ActivityIndicator.IsVisibleProperty, nameof(MemberSearchViewModel.IsBusy));

        var searchSection = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                searchGrid,
                new Button
                {
                    Text = "Mitglied neu anlegen",
                    IsVisible = PermissionChecks.CanEditAllMembers(_userContextState.CurrentUserContext),
                    Command = new Command(async () => await Shell.Current.GoToAsync($"{nameof(MemberDetailPage)}?mode=new"))
                },
                optionsLayout,
                activityIndicator
            }
        };

        _resultsCollectionView = new CollectionView
        {
            SelectionMode = SelectionMode.Single,
            ItemTemplate = new DataTemplate(() =>
            {
                var title = new Label
                {
                    FontAttributes = FontAttributes.Bold,
                    LineBreakMode = LineBreakMode.TailTruncation
                };
                title.SetBinding(Label.TextProperty, nameof(MemberSearchResultItem.Title));

                var subtitle = new Label
                {
                    FontSize = 12,
                    Opacity = 0.78
                };
                subtitle.SetBinding(Label.TextProperty, nameof(MemberSearchResultItem.Subtitle));
                subtitle.SetBinding(IsVisibleProperty, nameof(MemberSearchResultItem.HasSubtitle));

                var gardenLabel = new Label
                {
                    FontSize = 12,
                    TextColor = Color.FromArgb("#446B8A"),
                    HorizontalTextAlignment = TextAlignment.Center
                };
                gardenLabel.SetBinding(Label.TextProperty, new Binding(nameof(MemberSearchResultItem.GartenNummernText), stringFormat: "Garten {0}"));

                var gardenBorder = new Border
                {
                    StrokeThickness = 0,
                    BackgroundColor = Color.FromArgb("#F1F6FA"),
                    Padding = new Thickness(10, 4),
                    Content = gardenLabel
                };
                gardenBorder.SetBinding(IsVisibleProperty, nameof(MemberSearchResultItem.HasGartenNummern));

                var hauptmitgliedLabel = new Label
                {
                    Text = "Hauptmitglied",
                    FontSize = 11,
                    TextColor = Color.FromArgb("#6E4D77"),
                    HorizontalTextAlignment = TextAlignment.Center
                };

                var hauptmitgliedBorder = new Border
                {
                    StrokeThickness = 0,
                    BackgroundColor = Color.FromArgb("#F5F1F7"),
                    Padding = new Thickness(10, 4),
                    HorizontalOptions = LayoutOptions.End,
                    Content = hauptmitgliedLabel
                };
                hauptmitgliedBorder.SetBinding(IsVisibleProperty, nameof(MemberSearchResultItem.IstHauptmitglied));

                var rightStack = new VerticalStackLayout
                {
                    Spacing = 6,
                    HorizontalOptions = LayoutOptions.End,
                    Children = { gardenBorder, hauptmitgliedBorder }
                };

                var separator = new BoxView
                {
                    HeightRequest = 1,
                    Color = Color.FromArgb("#EEF1F4")
                };
                separator.SetBinding(IsVisibleProperty, nameof(MemberSearchResultItem.HasGartenNummern));

                var itemGrid = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition(GridLength.Star),
                        new ColumnDefinition(GridLength.Auto)
                    },
                    RowDefinitions =
                    {
                        new RowDefinition(GridLength.Auto),
                        new RowDefinition(GridLength.Auto)
                    },
                    ColumnSpacing = 12,
                    RowSpacing = 6
                };

                itemGrid.Children.Add(new VerticalStackLayout
                {
                    Spacing = 3,
                    Children = { title, subtitle }
                });
                itemGrid.Children.Add(rightStack);
                Grid.SetColumn(rightStack, 1);
                itemGrid.Children.Add(separator);
                Grid.SetRow(separator, 1);
                Grid.SetColumnSpan(separator, 2);

                return new Border
                {
                    Stroke = Color.FromArgb("#E3E7EB"),
                    StrokeThickness = 1,
                    BackgroundColor = Colors.White,
                    Padding = 14,
                    Margin = new Thickness(0, 0, 0, 10),
                    Content = itemGrid
                };
            })
        };
        _resultsCollectionView.SetBinding(ItemsView.ItemsSourceProperty, nameof(MemberSearchViewModel.Results));
        _resultsCollectionView.SelectionChanged += ResultsCollectionView_SelectionChanged;

        var refreshView = new RefreshView
        {
            Content = _resultsCollectionView
        };
        refreshView.SetBinding(RefreshView.CommandProperty, nameof(MemberSearchViewModel.RefreshCommand));
        refreshView.SetBinding(RefreshView.IsRefreshingProperty, nameof(MemberSearchViewModel.IsRefreshing), mode: BindingMode.TwoWay);

        var debugCollectionView = new CollectionView
        {
            HeightRequest = 120,
            ItemTemplate = new DataTemplate(() =>
            {
                var label = new Label
                {
                    FontSize = 12
                };
                label.SetBinding(Label.TextProperty, ".");
                return label;
            })
        };
        debugCollectionView.SetBinding(ItemsView.ItemsSourceProperty, nameof(MemberSearchViewModel.DebugMessages));

        var rootGrid = new Grid
        {
            Padding = 24,
            RowSpacing = 12,
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto)
            }
        };

        rootGrid.Children.Add(titleLabel);
        rootGrid.Children.Add(searchSection);
        Grid.SetRow(searchSection, 1);
        rootGrid.Children.Add(refreshView);
        Grid.SetRow(refreshView, 2);
        rootGrid.Children.Add(debugCollectionView);
        Grid.SetRow(debugCollectionView, 3);

        return rootGrid;
    }

    private async void MemberSearchPage_Appearing(object? sender, EventArgs e)
    {
        await _vm.InitializeAsync(_memberSearchRefreshState.ConsumeReloadRequest());
    }

    private async void ResultsCollectionView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var item = e.CurrentSelection?.FirstOrDefault() as MemberSearchResultItem;

        if (sender is CollectionView cv)
            cv.SelectedItem = null;

        if (item == null)
            return;

        if (_memberSwitchInProgress)
        {
            AppFileLog.Warning("KGV.Navigation", "Mitgliedswechsel unterdrückt: mitgliedersuche -> stammdaten. Bereits aktiv: local-member-switch.");
            return;
        }

        _memberSwitchInProgress = true;
        if (_resultsCollectionView != null)
            _resultsCollectionView.IsEnabled = false;

        try
        {
            using var navigationScope = NavigationCoordinator.TryBegin(
                NavigationCoordinator.MemberSwitchScope,
                "mitgliedersuche -> stammdaten",
                NavigationCoordinator.RootSwitchScope);

            if (navigationScope == null)
                return;

            var member = await _vm.SelectResultAsync(item);
            if (member == null)
                return;

            _memberContextState.SetSelectedMember(member);

            if (Shell.Current is Shell shell && ShellNavigationHelper.HasVisibleShellContentRoute(shell, "memberdetails"))
            {
                ShellNavigationHelper.EnsureActiveShellItem(shell, "memberdetails");

                if (string.Equals(ShellNavigationHelper.GetActiveShellContentRoute(shell), "memberdetails", StringComparison.OrdinalIgnoreCase))
                    return;
            }

            await Shell.Current.GoToAsync(nameof(MeineDatenPage));
        }
        finally
        {
            _memberSwitchInProgress = false;
            if (_resultsCollectionView != null)
                _resultsCollectionView.IsEnabled = true;
        }
    }
}
