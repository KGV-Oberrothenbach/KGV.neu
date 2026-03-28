using KGV.Core.Models;
using KGV.Maui;
using KGV.Maui.State;
using KGV.Maui.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace KGV.Maui.Pages;

public sealed class ParzellenPage : ContentPage
{
    private readonly ParzellenViewModel _viewModel;
    private readonly MemberContextState _memberContextState;
    private bool _initialized;

    public ParzellenPage(ParzellenViewModel viewModel, MemberContextState memberContextState)
    {
        _viewModel = viewModel;
        _memberContextState = memberContextState;
        BindingContext = _viewModel;
        Title = "Parzellen";

        var titleLabel = new Label { FontSize = 24, FontAttributes = FontAttributes.Bold };
        titleLabel.SetBinding(Label.TextProperty, nameof(ParzellenViewModel.Title));

        var descriptionLabel = new Label { LineBreakMode = Microsoft.Maui.LineBreakMode.WordWrap };
        descriptionLabel.SetBinding(Label.TextProperty, nameof(ParzellenViewModel.Description));

        var hintLabel = new Label { TextColor = Colors.Gray, LineBreakMode = Microsoft.Maui.LineBreakMode.WordWrap };
        hintLabel.SetBinding(Label.TextProperty, nameof(ParzellenViewModel.DetailHint));

        var refreshButton = new Button { Text = "Aktualisieren" };
        refreshButton.Clicked += async (_, _) => await _viewModel.RefreshAsync();

        var searchBar = new SearchBar { Placeholder = "Nach Garten Nr oder Pächter suchen" };
        searchBar.SetBinding(SearchBar.TextProperty, nameof(ParzellenViewModel.SearchText), BindingMode.TwoWay);

        var listEmptyLabel = new Label
        {
            Text = "Keine Parzellen für die aktuelle Suche gefunden.",
            TextColor = Colors.Gray,
            HorizontalTextAlignment = Microsoft.Maui.TextAlignment.Center,
            VerticalTextAlignment = Microsoft.Maui.TextAlignment.Center
        };

        var parzellenList = new CollectionView
        {
            SelectionMode = SelectionMode.Single,
            HeightRequest = 260,
            EmptyView = listEmptyLabel,
            ItemTemplate = new DataTemplate(() =>
            {
                var gartenNrLabel = new Label { FontAttributes = FontAttributes.Bold, FontSize = 16 };
                gartenNrLabel.SetBinding(Label.TextProperty, nameof(ParzelleVerwaltungItem.GartenNr));

                var paechterLabel = new Label { TextColor = Colors.Gray, LineBreakMode = Microsoft.Maui.LineBreakMode.WordWrap };
                paechterLabel.SetBinding(Label.TextProperty, nameof(ParzelleVerwaltungItem.PaechterDisplayText));

                return new Border
                {
                    Stroke = Colors.LightGray,
                    Padding = 12,
                    Margin = new Microsoft.Maui.Thickness(0, 0, 0, 8),
                    Content = new VerticalStackLayout
                    {
                        Spacing = 4,
                        Children =
                        {
                            gartenNrLabel,
                            paechterLabel
                        }
                    }
                };
            })
        };
        parzellenList.SetBinding(ItemsView.ItemsSourceProperty, nameof(ParzellenViewModel.FilteredItems));
        parzellenList.SetBinding(SelectableItemsView.SelectedItemProperty, nameof(ParzellenViewModel.SelectedItem), BindingMode.TwoWay);

        var listSection = CreateSection("Parzellenübersicht", searchBar, parzellenList);

        var backToMemberButton = new Button { Text = "Zur Stammdatenansicht" };
        backToMemberButton.SetBinding(IsVisibleProperty, nameof(ParzellenViewModel.IsContextBound));
        backToMemberButton.Clicked += async (_, _) => await Shell.Current.GoToAsync(nameof(MeineDatenPage));

        var clearContextButton = new Button { Text = "Zur globalen Parzellenübersicht" };
        clearContextButton.SetBinding(IsVisibleProperty, nameof(ParzellenViewModel.IsContextBound));
        clearContextButton.Clicked += async (_, _) => await _viewModel.ClearRequestedContextAsync();

        var selectionHint = new Label { Text = "Keine Parzelle ausgewählt.", TextColor = Colors.Gray };
        selectionHint.SetBinding(IsVisibleProperty, nameof(ParzellenViewModel.ShowSelectionHint));

        var detailContainer = new VerticalStackLayout { Spacing = 12 };
        detailContainer.SetBinding(IsVisibleProperty, nameof(ParzellenViewModel.HasSelectedDetail));
        detailContainer.GestureRecognizers.Add(new SwipeGestureRecognizer
        {
            Direction = Microsoft.Maui.SwipeDirection.Left,
            Command = new Command(async () => await _viewModel.SelectNextAsync())
        });
        detailContainer.GestureRecognizers.Add(new SwipeGestureRecognizer
        {
            Direction = Microsoft.Maui.SwipeDirection.Right,
            Command = new Command(async () => await _viewModel.SelectPreviousAsync())
        });

        var currentParzelleLabel = new Label { FontSize = 20, FontAttributes = FontAttributes.Bold };
        currentParzelleLabel.SetBinding(Label.TextProperty, nameof(ParzellenViewModel.SelectedParzelleDisplayName));

        var editButton = new Button { Text = "Stammdaten bearbeiten" };
        editButton.SetBinding(IsEnabledProperty, nameof(ParzellenViewModel.CanManageAssignment));
        editButton.Clicked += (_, _) => _viewModel.BeginEditMode();

        detailContainer.Children.Add(new Border
        {
            Stroke = Colors.LightGray,
            Padding = 12,
            Content = new VerticalStackLayout
            {
                Spacing = 10,
                Children =
                {
                    currentParzelleLabel,
                    CreateSection("Stammdaten",
                        CreateValueLabel("ID", "SelectedDetail.ParzelleId"),
                        CreateValueLabel("Garten Nr", "SelectedDetail.GartenNr"),
                        CreateValueLabel("Fläche", "SelectedDetail.FlaecheText"),
                        CreateValueLabel("hat Wasser", "SelectedDetail.HatWasserText"),
                        CreateValueLabel("hat Strom", "SelectedDetail.HatStromText"),
                        CreateValueLabel("rfid Wasser", "SelectedDetail.RfidWasserText"),
                        CreateValueLabel("rfid Strom", "SelectedDetail.RfidStromText"),
                        CreateValueLabel("Anlage", "SelectedDetail.Anlage"),
                        editButton)
                }
            }
        });

        var editSection = CreateSection("Stammdaten bearbeiten",
            CreateEditorEntry("Garten Nr", nameof(ParzellenViewModel.EditGartenNr)),
            CreateEditorEntry("Fläche", nameof(ParzellenViewModel.EditFlaeche)),
            CreateEditorSwitch("hat Wasser", nameof(ParzellenViewModel.EditHatWasser)),
            CreateEditorSwitch("hat Strom", nameof(ParzellenViewModel.EditHatStrom)),
            CreateEditorEntry("rfid Wasser", nameof(ParzellenViewModel.EditRfidWasser)),
            CreateEditorEntry("rfid Strom", nameof(ParzellenViewModel.EditRfidStrom)),
            CreateEditorEntry("Anlage", nameof(ParzellenViewModel.EditAnlage)));
        editSection.SetBinding(IsVisibleProperty, nameof(ParzellenViewModel.IsEditMode));

        var saveStammdatenButton = new Button { Text = "Stammdaten speichern" };
        saveStammdatenButton.SetBinding(IsEnabledProperty, nameof(ParzellenViewModel.CanSaveStammdaten));
        saveStammdatenButton.Clicked += async (_, _) => await SaveStammdatenAsync();

        var cancelEditButton = new Button { Text = "Bearbeiten abbrechen" };
        cancelEditButton.Clicked += (_, _) => _viewModel.CancelEditMode();

        if (editSection.Content is VerticalStackLayout editSectionLayout)
        {
            editSectionLayout.Children.Add(new HorizontalStackLayout
            {
                Spacing = 8,
                Children = { cancelEditButton, saveStammdatenButton }
            });
        }
        detailContainer.Children.Add(editSection);

        var assignedMemberButton = new Button
        {
            BackgroundColor = Colors.Transparent,
            TextColor = Colors.DarkBlue,
            HorizontalOptions = LayoutOptions.Start,
            Padding = new Microsoft.Maui.Thickness(0)
        };
        assignedMemberButton.SetBinding(Button.TextProperty, "SelectedDetail.MitgliedDisplayText");
        assignedMemberButton.SetBinding(IsVisibleProperty, nameof(ParzellenViewModel.HasAssignedMember));
        assignedMemberButton.SetBinding(IsEnabledProperty, nameof(ParzellenViewModel.CanOpenAssignedMember));
        assignedMemberButton.Clicked += async (_, _) => await OpenAssignedMemberAsync();

        var unassignedLayout = new VerticalStackLayout { Spacing = 8 };
        unassignedLayout.SetBinding(IsVisibleProperty, nameof(ParzellenViewModel.HasAssignedMember), converter: new InverseBooleanConverter());
        var startAssignButton = new Button { Text = "Zuweisen" };
        startAssignButton.SetBinding(IsEnabledProperty, nameof(ParzellenViewModel.CanStartAssign));
        startAssignButton.Clicked += (_, _) => _viewModel.BeginAssignMode();
        unassignedLayout.Children.Add(new Label { Text = "Aktuell keiner Person zugeordnet.", LineBreakMode = Microsoft.Maui.LineBreakMode.WordWrap });
        unassignedLayout.Children.Add(startAssignButton);

        var assignedLayout = new VerticalStackLayout { Spacing = 8 };
        assignedLayout.SetBinding(IsVisibleProperty, nameof(ParzellenViewModel.HasAssignedMember));
        assignedLayout.Children.Add(assignedMemberButton);
        assignedLayout.Children.Add(CreateBoundLabel("SelectedDetail.BelegungText"));

        var endAssignmentButton = new Button { Text = "Belegung beenden" };
        endAssignmentButton.SetBinding(IsEnabledProperty, nameof(ParzellenViewModel.CanEndAssignment));
        endAssignmentButton.Clicked += async (_, _) =>
        {
            var ok = await _viewModel.EndAssignmentAsync();
            if (ok)
                await DisplayAlert("OK", "Aktive Belegung beendet.", "OK");
        };
        assignedLayout.Children.Add(endAssignmentButton);

        var assignPicker = new Picker { Title = "Mitglied auswählen" };
        assignPicker.SetBinding(Picker.ItemsSourceProperty, nameof(ParzellenViewModel.AssignableMembers));
        assignPicker.SetBinding(Picker.SelectedItemProperty, nameof(ParzellenViewModel.SelectedAssignMember), BindingMode.TwoWay);
        assignPicker.ItemDisplayBinding = new Binding(nameof(MemberDTO.DisplayName));
        assignPicker.SetBinding(IsEnabledProperty, nameof(ParzellenViewModel.CanManageAssignment));

        var assignDatePicker = new DatePicker();
        assignDatePicker.SetBinding(DatePicker.DateProperty, nameof(ParzellenViewModel.AssignVonDatum), BindingMode.TwoWay);
        assignDatePicker.SetBinding(IsEnabledProperty, nameof(ParzellenViewModel.CanManageAssignment));

        var saveAssignButton = new Button { Text = "Zuweisung speichern" };
        saveAssignButton.SetBinding(IsEnabledProperty, nameof(ParzellenViewModel.CanAssign));
        saveAssignButton.Clicked += async (_, _) =>
        {
            var ok = await _viewModel.AssignAsync();
            if (ok)
                await DisplayAlert("OK", "Parzelle erfolgreich zugeordnet.", "OK");
        };

        var cancelAssignButton = new Button { Text = "Zuweisen abbrechen" };
        cancelAssignButton.Clicked += (_, _) => _viewModel.CancelAssignMode();

        var assignSection = CreateSection("Belegung zuweisen",
            CreateEditorField("Mitglied", assignPicker),
            CreateEditorField("Start", assignDatePicker),
            cancelAssignButton,
            saveAssignButton);
        assignSection.SetBinding(IsVisibleProperty, nameof(ParzellenViewModel.IsAssignMode));

        detailContainer.Children.Add(CreateSection("Belegung",
            CreateValueLabel("Status", "SelectedDetail.StatusText"),
            assignedLayout,
            unassignedLayout,
            assignSection));

        var previousButton = new Button { Text = "Vorherige" };
        previousButton.SetBinding(IsEnabledProperty, nameof(ParzellenViewModel.CanSelectPrevious));
        previousButton.Clicked += async (_, _) => await _viewModel.SelectPreviousAsync();

        var navigationLabel = new Label
        {
            HorizontalOptions = LayoutOptions.Center,
            VerticalTextAlignment = Microsoft.Maui.TextAlignment.Center,
            HorizontalTextAlignment = Microsoft.Maui.TextAlignment.Center
        };
        navigationLabel.SetBinding(Label.TextProperty, nameof(ParzellenViewModel.NavigationText));

        var nextButton = new Button { Text = "Nächste" };
        nextButton.SetBinding(IsEnabledProperty, nameof(ParzellenViewModel.CanSelectNext));
        nextButton.Clicked += async (_, _) => await _viewModel.SelectNextAsync();

        var navigationGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = Microsoft.Maui.GridLength.Star },
                new ColumnDefinition { Width = Microsoft.Maui.GridLength.Auto },
                new ColumnDefinition { Width = Microsoft.Maui.GridLength.Star }
            },
            ColumnSpacing = 8
        };
        navigationGrid.Children.Add(previousButton);
        Grid.SetColumn(previousButton, 0);
        navigationGrid.Children.Add(navigationLabel);
        Grid.SetColumn(navigationLabel, 1);
        navigationGrid.Children.Add(nextButton);
        Grid.SetColumn(nextButton, 2);

        detailContainer.Children.Add(CreateSection("Navigation",
            new Label
            {
                Text = "Per Buttons oder Wischgeste zwischen den Parzellen wechseln.",
                TextColor = Colors.Gray,
                LineBreakMode = Microsoft.Maui.LineBreakMode.WordWrap
            },
            navigationGrid));

        var statusLabel = new Label { TextColor = Colors.DarkSlateBlue, LineBreakMode = Microsoft.Maui.LineBreakMode.WordWrap };
        statusLabel.SetBinding(Label.TextProperty, nameof(ParzellenViewModel.StatusMessage));
        statusLabel.SetBinding(IsVisibleProperty, nameof(ParzellenViewModel.HasStatusMessage));

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
                    hintLabel,
                    backToMemberButton,
                    clearContextButton,
                    refreshButton,
                    listSection,
                    selectionHint,
                    detailContainer,
                    statusLabel
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!_initialized)
        {
            await _viewModel.InitializeAsync();
            _initialized = true;
            return;
        }

        await _viewModel.ApplyRequestedContextAsync();
        await _viewModel.RefreshSelectedDetailAsync();
    }

    private async Task SaveStammdatenAsync()
    {
        if (_viewModel.HasFlaecheChanged())
        {
            var confirm = await DisplayAlert(
                "Fläche wirklich ändern?",
                "Die Fläche wirkt fachlich in weitere Auswertungen hinein. Soll die neue Fläche wirklich gespeichert werden?",
                "Ja, speichern",
                "Abbrechen");

            if (!confirm)
                return;
        }

        var ok = await _viewModel.SaveStammdatenAsync();
        if (ok)
            await DisplayAlert("OK", "Parzellen-Stammdaten gespeichert.", "OK");
    }

    private async Task OpenAssignedMemberAsync()
    {
        var member = await _viewModel.LoadAssignedMemberAsync();
        if (member == null)
        {
            await DisplayAlert("Stammdaten", "Das zugeordnete Mitglied konnte nicht geladen werden.", "OK");
            return;
        }

        _memberContextState.SetSelectedMember(member);
        if (Shell.Current is IAppShellInitializer shellInitializer)
            shellInitializer.BuildMenu();

        await Shell.Current.GoToAsync("//memberdetails");
    }

    private static Border CreateSection(string title, params View[] children)
    {
        var stack = new VerticalStackLayout { Spacing = 8 };
        stack.Children.Add(new Label { Text = title, FontAttributes = FontAttributes.Bold });
        foreach (var child in children)
            stack.Children.Add(child);

        return new Border
        {
            Stroke = Colors.LightGray,
            Padding = 12,
            Content = stack
        };
    }

    private static View CreateValueLabel(string title, string path)
    {
        return new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                new Label { Text = title, FontAttributes = FontAttributes.Bold, FontSize = 12, TextColor = Colors.Gray },
                CreateBoundLabel(path)
            }
        };
    }

    private static View CreateEditorEntry(string title, string path)
    {
        var entry = new Entry();
        entry.SetBinding(InputView.TextProperty, path, BindingMode.TwoWay);

        return CreateEditorField(title, entry);
    }

    private static View CreateEditorSwitch(string title, string path)
    {
        var control = new Switch();
        control.SetBinding(Switch.IsToggledProperty, path, BindingMode.TwoWay);
        return CreateEditorField(title, control);
    }

    private static View CreateEditorField(string title, View control)
    {
        return new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label { Text = title, FontAttributes = FontAttributes.Bold, FontSize = 12, TextColor = Colors.Gray },
                control
            }
        };
    }

    private static Label CreateBoundLabel(string path)
    {
        var label = new Label { LineBreakMode = Microsoft.Maui.LineBreakMode.WordWrap };
        label.SetBinding(Label.TextProperty, path);
        return label;
    }

    private sealed class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool flag ? !flag : true;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
