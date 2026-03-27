using KGV.Core.Models;
using KGV.Maui.State;
using KGV.Maui.ViewModels;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System.Linq;

namespace KGV.Maui.Pages;

public class HomePage : ContentPage
{
    private readonly HomeViewModel _viewModel;
    private readonly HomeContextState _homeContextState;
    private readonly ArbeitseinsaetzeUserState _arbeitseinsaetzeUserState;
    private readonly TermineUserState _termineUserState;
    private readonly MemberContextState _memberContextState;
    private readonly Label _statusLabel;
    private readonly Button _createWorkHoursEntryButton;
    private readonly Button _newWorkAssignmentButton;
    private readonly Button _newAppointmentButton;
    private readonly Button _newAnnouncementButton;
    private bool _isLoading;
    private bool _loadScheduled;
    private bool _isSubscribed;

    public HomePage(HomeViewModel viewModel, HomeContextState homeContextState, ArbeitseinsaetzeUserState arbeitseinsaetzeUserState, TermineUserState termineUserState, MemberContextState memberContextState)
    {
        _viewModel = viewModel;
        _homeContextState = homeContextState;
        _arbeitseinsaetzeUserState = arbeitseinsaetzeUserState;
        _termineUserState = termineUserState;
        _memberContextState = memberContextState;
        BindingContext = _viewModel;
        Title = "Startseite";

        var titleLabel = new Label { FontSize = 24, FontAttributes = FontAttributes.Bold };
        titleLabel.SetBinding(Label.TextProperty, nameof(HomeViewModel.Title));

        var descriptionLabel = new Label { LineBreakMode = LineBreakMode.WordWrap, TextColor = Color.FromArgb("#5F6368") };
        descriptionLabel.SetBinding(Label.TextProperty, nameof(HomeViewModel.Description));

        var contextLabel = new Label { TextColor = Color.FromArgb("#7A7F87"), FontSize = 12 };
        contextLabel.SetBinding(Label.TextProperty, nameof(HomeViewModel.UserContextText));

        _statusLabel = new Label { TextColor = Color.FromArgb("#446B8A"), LineBreakMode = LineBreakMode.WordWrap };

        var introCard = new Border
        {
            BackgroundColor = Colors.White,
            Stroke = Color.FromArgb("#E3E7EB"),
            StrokeThickness = 1,
            Padding = 18,
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    titleLabel,
                    descriptionLabel,
                    contextLabel
                }
            }
        };

        var operationalAccent = Color.FromArgb("#4F7A5C");
        var operationalBackground = Color.FromArgb("#F2F7F3");
        var workAssignmentsAccent = Color.FromArgb("#A06B2C");
        var workAssignmentsBackground = Color.FromArgb("#FBF5ED");
        var appointmentsAccent = Color.FromArgb("#446B8A");
        var appointmentsBackground = Color.FromArgb("#F1F6FA");
        var announcementsAccent = Color.FromArgb("#6E4D77");
        var announcementsBackground = Color.FromArgb("#F5F1F7");
        var managementAccent = Color.FromArgb("#5F6368");
        var managementBackground = Color.FromArgb("#F7F8FA");

        var operationalView = new CollectionView
        {
            SelectionMode = SelectionMode.None,
            HeightRequest = 180,
            VerticalOptions = LayoutOptions.Start,
            ItemTemplate = new DataTemplate(() =>
            {
                var itemTitle = new Label { FontAttributes = FontAttributes.Bold };
                itemTitle.SetBinding(Label.TextProperty, nameof(HomeOperationalItem.Title));

                var itemMessage = new Label { FontSize = 12, TextColor = Color.FromArgb("#5F6368"), LineBreakMode = LineBreakMode.WordWrap };
                itemMessage.SetBinding(Label.TextProperty, nameof(HomeOperationalItem.Message));

                return CreateSectionItemBorder(operationalAccent, itemTitle, itemMessage);
            })
        };
        operationalView.SetBinding(ItemsView.ItemsSourceProperty, nameof(HomeViewModel.OperationalItems));
        operationalView.SetBinding(IsVisibleProperty, nameof(HomeViewModel.ShowOperationalFallbackList));

        var operationalEmptyLabel = new Label { TextColor = Color.FromArgb("#6E737A"), LineBreakMode = LineBreakMode.WordWrap };
        operationalEmptyLabel.SetBinding(Label.TextProperty, nameof(HomeViewModel.OperationalEmptyText));
        operationalEmptyLabel.SetBinding(IsVisibleProperty, nameof(HomeViewModel.ShowOperationalEmptyState));

        _createWorkHoursEntryButton = new Button { Text = "Arbeitsstunde erfassen" };
        _createWorkHoursEntryButton.SetBinding(IsVisibleProperty, nameof(HomeViewModel.CanCreateWorkHoursEntry));
        _createWorkHoursEntryButton.Clicked += async (_, _) => await Shell.Current.GoToAsync(nameof(ArbeitsstundenEditorPage));

        var operationalSection = CreateSectionCard(
            nameof(HomeViewModel.OperationalTitle),
            "Persönlicher Überblick zu offenen und bereits erfassten Arbeitsstunden.",
            operationalAccent,
            operationalBackground,
            CreateWorkHoursSummaryGrid(operationalAccent, operationalBackground),
            CreateWorkHoursInfoLabel(),
            _createWorkHoursEntryButton,
            operationalView,
            operationalEmptyLabel);

        var workAssignmentsView = CreateHomeListView<HomeWorkAssignmentItem>(item =>
        {
            _arbeitseinsaetzeUserState.SetEntries(_viewModel.WorkAssignments.ToList(), item.Id);
            _homeContextState.SetWorkAssignment(item);
            return Shell.Current.GoToAsync(nameof(HomeSectionDetailPage));
        }, item =>
        {
            var title = new Label { FontAttributes = FontAttributes.Bold };
            title.SetBinding(Label.TextProperty, nameof(HomeWorkAssignmentItem.Title));

            var subtitle = new Label { FontSize = 12, TextColor = Color.FromArgb("#5F6368"), LineBreakMode = LineBreakMode.WordWrap };
            subtitle.SetBinding(Label.TextProperty, nameof(HomeWorkAssignmentItem.Subtitle));

            var time = new Label { FontSize = 12, TextColor = Color.FromArgb("#6E737A") };
            time.SetBinding(Label.TextProperty, nameof(HomeWorkAssignmentItem.TimeText));
            time.SetBinding(IsVisibleProperty, nameof(HomeWorkAssignmentItem.HasTimeText));

            var registration = new Label { FontSize = 12, TextColor = workAssignmentsAccent, LineBreakMode = LineBreakMode.WordWrap };
            registration.SetBinding(Label.TextProperty, nameof(HomeWorkAssignmentItem.RegistrationInfo));
            registration.SetBinding(IsVisibleProperty, nameof(HomeWorkAssignmentItem.HasRegistrationInfo));

            return CreateSectionItemBorder(workAssignmentsAccent, title, subtitle, time, registration);
        });
        workAssignmentsView.SetBinding(ItemsView.ItemsSourceProperty, nameof(HomeViewModel.WorkAssignments));
        workAssignmentsView.SetBinding(IsVisibleProperty, nameof(HomeViewModel.HasWorkAssignments));

        var workAssignmentsEmptyLabel = new Label { TextColor = Color.FromArgb("#6E737A") };
        workAssignmentsEmptyLabel.SetBinding(Label.TextProperty, nameof(HomeViewModel.WorkAssignmentsEmptyText));
        workAssignmentsEmptyLabel.SetBinding(IsVisibleProperty, nameof(HomeViewModel.ShowWorkAssignmentsEmptyState));

        _newWorkAssignmentButton = new Button { Text = "Neu", HorizontalOptions = LayoutOptions.End, IsVisible = false };
        _newWorkAssignmentButton.SetBinding(IsVisibleProperty, nameof(HomeViewModel.ShowManagementSection));
        _newWorkAssignmentButton.Clicked += async (_, _) => await Shell.Current.GoToAsync(nameof(ArbeitseinsaetzeEditorPage));

        var workAssignmentsSection = CreateSectionCard(
            nameof(HomeViewModel.WorkAssignmentsTitle),
            "Anstehende Einsätze mit Zeit, Kurzinfo und direktem Wechsel in die Detailansicht.",
            workAssignmentsAccent,
            workAssignmentsBackground,
            _newWorkAssignmentButton,
            workAssignmentsView,
            workAssignmentsEmptyLabel);

        var appointmentsView = CreateHomeListView<HomeAppointmentItem>(item =>
        {
            _termineUserState.SetEntries(_viewModel.Appointments.ToList(), item.Id);
            _homeContextState.SetAppointment(item);
            return Shell.Current.GoToAsync(nameof(HomeSectionDetailPage));
        }, item =>
        {
            var title = new Label { FontAttributes = FontAttributes.Bold };
            title.SetBinding(Label.TextProperty, nameof(HomeAppointmentItem.Title));

            var subtitle = new Label { FontSize = 12, TextColor = Color.FromArgb("#5F6368"), LineBreakMode = LineBreakMode.WordWrap };
            subtitle.SetBinding(Label.TextProperty, nameof(HomeAppointmentItem.Subtitle));

            var time = new Label { FontSize = 12, TextColor = appointmentsAccent };
            time.SetBinding(Label.TextProperty, nameof(HomeAppointmentItem.TimeText));
            time.SetBinding(IsVisibleProperty, nameof(HomeAppointmentItem.HasTimeText));

            return CreateSectionItemBorder(appointmentsAccent, title, subtitle, time);
        });
        appointmentsView.SetBinding(ItemsView.ItemsSourceProperty, nameof(HomeViewModel.Appointments));
        appointmentsView.SetBinding(IsVisibleProperty, nameof(HomeViewModel.HasAppointments));

        var appointmentsEmptyLabel = new Label { TextColor = Color.FromArgb("#6E737A") };
        appointmentsEmptyLabel.SetBinding(Label.TextProperty, nameof(HomeViewModel.AppointmentsEmptyText));
        appointmentsEmptyLabel.SetBinding(IsVisibleProperty, nameof(HomeViewModel.ShowAppointmentsEmptyState));

        _newAppointmentButton = new Button { Text = "Neu", HorizontalOptions = LayoutOptions.End, IsVisible = false };
        _newAppointmentButton.SetBinding(IsVisibleProperty, nameof(HomeViewModel.ShowManagementSection));
        _newAppointmentButton.Clicked += async (_, _) => await Shell.Current.GoToAsync(nameof(TermineEditorPage));

        var appointmentsSection = CreateSectionCard(
            nameof(HomeViewModel.AppointmentsTitle),
            "Kommende Termine mit ruhiger Zeit- und Kontextdarstellung für den mobilen Überblick.",
            appointmentsAccent,
            appointmentsBackground,
            _newAppointmentButton,
            appointmentsView,
            appointmentsEmptyLabel);

        var announcementsView = CreateHomeListView<HomeAnnouncementItem>(item =>
        {
            _homeContextState.SetAnnouncement(item);
            return Shell.Current.GoToAsync(nameof(HomeSectionDetailPage));
        }, item =>
        {
            var title = new Label { FontAttributes = FontAttributes.Bold };
            title.SetBinding(Label.TextProperty, nameof(HomeAnnouncementItem.Title));

            var subtitle = new Label { FontSize = 12, TextColor = Color.FromArgb("#5F6368"), LineBreakMode = LineBreakMode.WordWrap };
            subtitle.SetBinding(Label.TextProperty, nameof(HomeAnnouncementItem.Subtitle));

            return CreateSectionItemBorder(announcementsAccent, title, subtitle);
        });
        announcementsView.SetBinding(ItemsView.ItemsSourceProperty, nameof(HomeViewModel.Announcements));
        announcementsView.SetBinding(IsVisibleProperty, nameof(HomeViewModel.HasAnnouncements));

        var announcementEmptyLabel = new Label { TextColor = Color.FromArgb("#6E737A") };
        announcementEmptyLabel.SetBinding(Label.TextProperty, nameof(HomeViewModel.AnnouncementEmptyText));
        announcementEmptyLabel.SetBinding(IsVisibleProperty, nameof(HomeViewModel.ShowAnnouncementEmptyState));

        _newAnnouncementButton = new Button { Text = "Neu", HorizontalOptions = LayoutOptions.End, IsVisible = false };
        _newAnnouncementButton.SetBinding(IsVisibleProperty, nameof(HomeViewModel.ShowManagementSection));
        _newAnnouncementButton.Clicked += async (_, _) => await Shell.Current.GoToAsync(nameof(BekanntmachungEditorPage));

        var announcementsSection = CreateSectionCard(
            nameof(HomeViewModel.AnnouncementTitle),
            "Wichtige Vereinsinformationen kompakt getrennt von Terminen und operativen Einträgen.",
            announcementsAccent,
            announcementsBackground,
            _newAnnouncementButton,
            announcementsView,
            announcementEmptyLabel);

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 18,
                Children =
                {
                    introCard,
                    _statusLabel,
                    operationalSection,
                    workAssignmentsSection,
                    appointmentsSection,
                    announcementsSection
                }
            }
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (!_isSubscribed)
        {
            _memberContextState.Changed += OnMemberContextChanged;
            _isSubscribed = true;
        }

        ScheduleLoad();
    }

    protected override void OnDisappearing()
    {
        if (_isSubscribed)
        {
            _memberContextState.Changed -= OnMemberContextChanged;
            _isSubscribed = false;
        }

        base.OnDisappearing();
    }

    private void OnMemberContextChanged(object? sender, EventArgs e)
    {
        _viewModel.Invalidate();
        if (IsVisible)
            ScheduleLoad();
    }

    private void ScheduleLoad()
    {
        if (_isLoading || _loadScheduled)
            return;

        _loadScheduled = true;
        SetLoadingState(true, "Daten werden geladen.");
        Dispatcher.Dispatch(async () =>
        {
            await Task.Yield();

            _loadScheduled = false;
            if (_isLoading)
                return;

            _isLoading = true;
            var loadFailed = false;
            try
            {
                await _viewModel.InitializeAsync();
            }
            catch (Exception ex)
            {
                loadFailed = true;
                SetLoadingState(false, ex.Message, isError: true);
            }
            finally
            {
                if (!loadFailed)
                    SetLoadingState(false);

                _isLoading = false;
            }
        });
    }

    private void SetLoadingState(bool isLoading, string? statusText = null, bool isError = false)
    {
        _statusLabel.TextColor = isError ? Colors.IndianRed : Color.FromArgb("#446B8A");

        if (!string.IsNullOrWhiteSpace(statusText))
            _statusLabel.Text = statusText;
        else if (!isLoading)
            _statusLabel.Text = string.Empty;

        _createWorkHoursEntryButton.IsEnabled = !isLoading;
        _newWorkAssignmentButton.IsEnabled = !isLoading;
        _newAppointmentButton.IsEnabled = !isLoading;
        _newAnnouncementButton.IsEnabled = !isLoading;
    }

    private static CollectionView CreateHomeListView<T>(Func<T, Task> openAsync, Func<T, View> templateFactory) where T : class
    {
        var isNavigating = false;
        var listView = new CollectionView
        {
            SelectionMode = SelectionMode.Single,
            HeightRequest = 220,
            VerticalOptions = LayoutOptions.Start,
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

            if (isNavigating)
            {
                listView.SelectedItem = null;
                return;
            }

            isNavigating = true;
            try
            {
                await openAsync(selected);
            }
            finally
            {
                listView.SelectedItem = null;
                isNavigating = false;
            }
        };

        return listView;
    }

    private static Border CreateSectionCard(string titleBindingPath, string subtitleText, Color accentColor, Color backgroundColor, params View[] content)
    {
        var titleLabel = new Label
        {
            FontSize = 19,
            FontAttributes = FontAttributes.Bold,
            TextColor = accentColor
        };
        titleLabel.SetBinding(Label.TextProperty, titleBindingPath);

        var subtitleLabel = new Label
        {
            FontSize = 12,
            TextColor = Color.FromArgb("#6E737A"),
            LineBreakMode = LineBreakMode.WordWrap,
            Text = subtitleText
        };

        var badge = new Border
        {
            BackgroundColor = accentColor.WithAlpha(0.12f),
            StrokeThickness = 0,
            Padding = new Thickness(10, 4),
            HorizontalOptions = LayoutOptions.Start,
            Content = new Label
            {
                Text = "Bereich",
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                TextColor = accentColor
            }
        };

        var stack = new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                badge,
                titleLabel,
                subtitleLabel,
                new BoxView
                {
                    HeightRequest = 1,
                    Color = accentColor.WithAlpha(0.20f)
                }
            }
        };

        foreach (var view in content)
            stack.Children.Add(view);

        return new Border
        {
            BackgroundColor = backgroundColor,
            Stroke = accentColor.WithAlpha(0.28f),
            StrokeThickness = 1,
            Padding = 18,
            Content = stack
        };
    }

    private static Border CreateSectionItemBorder(Color accentColor, params View[] children)
    {
        var stack = new VerticalStackLayout
        {
            Spacing = 4,
            Children = { }
        };

        foreach (var child in children)
            stack.Children.Add(child);

        var contentView = new ContentView
        {
            Content = stack
        };
        Grid.SetColumn(contentView, 1);

        return new Border
        {
            BackgroundColor = Colors.White,
            Stroke = accentColor.WithAlpha(0.24f),
            StrokeThickness = 1,
            Padding = 14,
            Margin = new Thickness(0, 0, 0, 10),
            Content = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = 4 },
                    new ColumnDefinition { Width = GridLength.Star }
                },
                ColumnSpacing = 12,
                Children =
                {
                    new BoxView
                    {
                        Color = accentColor,
                        CornerRadius = 3,
                        VerticalOptions = LayoutOptions.Fill
                    },
                    contentView
                }
            }
        };
    }

    private static Grid CreateWorkHoursSummaryGrid(Color accentColor, Color backgroundColor)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 10,
            Margin = new Thickness(0, 2, 0, 0)
        };

        var requiredCard = CreateWorkHoursMetricCard("Soll", nameof(HomeViewModel.RequiredHoursValue), accentColor);
        var workedCard = CreateWorkHoursMetricCard("Geleistet", nameof(HomeViewModel.WorkedHoursValue), accentColor);
        var openCard = CreateWorkHoursMetricCard("Offen", nameof(HomeViewModel.OpenHoursValue), accentColor);

        Grid.SetColumn(workedCard, 1);
        Grid.SetColumn(openCard, 2);

        grid.Children.Add(requiredCard);
        grid.Children.Add(workedCard);
        grid.Children.Add(openCard);
        grid.SetBinding(IsVisibleProperty, nameof(HomeViewModel.HasWorkHoursSummary));

        return grid;
    }

    private static Border CreateWorkHoursMetricCard(string labelText, string valueBindingPath, Color accentColor)
    {
        var titleLabel = new Label
        {
            Text = labelText,
            FontSize = 12,
            TextColor = accentColor,
            HorizontalTextAlignment = TextAlignment.Center
        };

        var valueLabel = new Label
        {
            FontSize = 20,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center
        };
        valueLabel.SetBinding(Label.TextProperty, valueBindingPath);

        return new Border
        {
            BackgroundColor = Colors.White,
            Stroke = accentColor.WithAlpha(0.22f),
            StrokeThickness = 1,
            Padding = new Thickness(10, 12),
            Content = new VerticalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    titleLabel,
                    valueLabel
                }
            }
        };
    }

    private static Label CreateWorkHoursInfoLabel()
    {
        var label = new Label
        {
            FontSize = 12,
            TextColor = Color.FromArgb("#5F6368"),
            LineBreakMode = LineBreakMode.WordWrap
        };
        label.SetBinding(Label.TextProperty, nameof(HomeViewModel.WorkHoursInfoText));
        label.SetBinding(IsVisibleProperty, nameof(HomeViewModel.HasWorkHoursSummary));
        return label;
    }

}
