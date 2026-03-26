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
    private bool _isLoading;

    public HomePage(HomeViewModel viewModel, HomeContextState homeContextState, ArbeitseinsaetzeUserState arbeitseinsaetzeUserState, TermineUserState termineUserState)
    {
        _viewModel = viewModel;
        _homeContextState = homeContextState;
        _arbeitseinsaetzeUserState = arbeitseinsaetzeUserState;
        _termineUserState = termineUserState;
        BindingContext = _viewModel;
        Title = "Startseite";

        var titleLabel = new Label { FontSize = 24, FontAttributes = FontAttributes.Bold };
        titleLabel.SetBinding(Label.TextProperty, nameof(HomeViewModel.Title));

        var descriptionLabel = new Label { LineBreakMode = LineBreakMode.WordWrap, TextColor = Color.FromArgb("#5F6368") };
        descriptionLabel.SetBinding(Label.TextProperty, nameof(HomeViewModel.Description));

        var contextLabel = new Label { TextColor = Color.FromArgb("#7A7F87"), FontSize = 12 };
        contextLabel.SetBinding(Label.TextProperty, nameof(HomeViewModel.UserContextText));

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

        var createWorkHoursEntryButton = new Button { Text = "Arbeitsstunde erfassen" };
        createWorkHoursEntryButton.SetBinding(IsVisibleProperty, nameof(HomeViewModel.CanCreateWorkHoursEntry));
        createWorkHoursEntryButton.Clicked += async (_, _) => await Shell.Current.GoToAsync($"{nameof(ArbeitsstundenEditorPage)}?entryId=0");

        var operationalSection = CreateSectionCard(
            nameof(HomeViewModel.OperationalTitle),
            "Persönlicher Überblick zu offenen und bereits erfassten Arbeitsstunden.",
            operationalAccent,
            operationalBackground,
            CreateWorkHoursSummaryGrid(operationalAccent, operationalBackground),
            CreateWorkHoursInfoLabel(),
            createWorkHoursEntryButton,
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

        var workAssignmentsSection = CreateSectionCard(
            nameof(HomeViewModel.WorkAssignmentsTitle),
            "Anstehende Einsätze mit Zeit, Kurzinfo und direktem Wechsel in die Detailansicht.",
            workAssignmentsAccent,
            workAssignmentsBackground,
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

        var appointmentsSection = CreateSectionCard(
            nameof(HomeViewModel.AppointmentsTitle),
            "Kommende Termine mit ruhiger Zeit- und Kontextdarstellung für den mobilen Überblick.",
            appointmentsAccent,
            appointmentsBackground,
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

        var announcementsSection = CreateSectionCard(
            nameof(HomeViewModel.AnnouncementTitle),
            "Wichtige Vereinsinformationen kompakt getrennt von Terminen und operativen Einträgen.",
            announcementsAccent,
            announcementsBackground,
            announcementsView,
            announcementEmptyLabel);

        var managementHintLabel = new Label { TextColor = Color.FromArgb("#6E737A"), LineBreakMode = LineBreakMode.WordWrap };
        managementHintLabel.SetBinding(Label.TextProperty, nameof(HomeViewModel.ManagementHintText));
        managementHintLabel.SetBinding(IsVisibleProperty, nameof(HomeViewModel.ShowManagementSection));

        var workAssignmentsManagementButton = new Button { Text = "Arbeitseinsätze bearbeiten" };
        workAssignmentsManagementButton.SetBinding(IsVisibleProperty, nameof(HomeViewModel.ShowManagementSection));
        workAssignmentsManagementButton.Clicked += async (_, _) => await Shell.Current.GoToAsync("//management_workassignments");

        var appointmentsManagementButton = new Button { Text = "Termine bearbeiten" };
        appointmentsManagementButton.SetBinding(IsVisibleProperty, nameof(HomeViewModel.ShowManagementSection));
        appointmentsManagementButton.Clicked += async (_, _) => await Shell.Current.GoToAsync("//management_appointments");

        var announcementsManagementButton = new Button { Text = "Bekanntmachungen bearbeiten" };
        announcementsManagementButton.SetBinding(IsVisibleProperty, nameof(HomeViewModel.ShowManagementSection));
        announcementsManagementButton.Clicked += async (_, _) => await Shell.Current.GoToAsync("//management_announcements");

        var managementSection = CreateSectionCard(
            nameof(HomeViewModel.ManagementTitle),
            "Zusätzliche mobile Verwaltungszugänge für Admin und Vorstand.",
            managementAccent,
            managementBackground,
            managementHintLabel,
            workAssignmentsManagementButton,
            appointmentsManagementButton,
            announcementsManagementButton);
        managementSection.SetBinding(IsVisibleProperty, nameof(HomeViewModel.ShowManagementSection));

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 18,
                Children =
                {
                    introCard,
                    operationalSection,
                    workAssignmentsSection,
                    appointmentsSection,
                    announcementsSection,
                    managementSection
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_isLoading)
            return;

        _isLoading = true;
        try
        {
            await _viewModel.InitializeAsync();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private static CollectionView CreateHomeListView<T>(Func<T, Task> openAsync, Func<T, View> templateFactory) where T : class
    {
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

            await openAsync(selected);
            listView.SelectedItem = null;
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
