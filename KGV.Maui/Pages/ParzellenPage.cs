using KGV.Core.Models;
using KGV.Maui.State;
using KGV.Maui.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Threading.Tasks;

namespace KGV.Maui.Pages;

public sealed class ParzellenPage : ContentPage
{
    private readonly ParzellenViewModel _viewModel;
    private bool _initialized;

    public ParzellenPage(ParzellenViewModel viewModel, MemberContextState memberContextState)
    {
        _viewModel = viewModel;
        BindingContext = _viewModel;
        Title = "Parzellen";

        var titleLabel = new Label { FontSize = 24, FontAttributes = FontAttributes.Bold };
        titleLabel.SetBinding(Label.TextProperty, nameof(ParzellenViewModel.Title));

        var descriptionLabel = new Label { LineBreakMode = Microsoft.Maui.LineBreakMode.WordWrap };
        descriptionLabel.SetBinding(Label.TextProperty, nameof(ParzellenViewModel.Description));

        var hintLabel = new Label { TextColor = Colors.Gray, LineBreakMode = Microsoft.Maui.LineBreakMode.WordWrap };
        hintLabel.SetBinding(Label.TextProperty, nameof(ParzellenViewModel.DetailHint));

        var refreshButton = new Button { Text = "Aktualisieren" };
        refreshButton.SetBinding(IsVisibleProperty, nameof(ParzellenViewModel.ShowGlobalActions));
        refreshButton.Clicked += async (_, _) => await _viewModel.RefreshAsync();

        var searchBar = new SearchBar { Placeholder = "Nach Garten Nr oder Pächter suchen" };
        searchBar.SetBinding(IsVisibleProperty, nameof(ParzellenViewModel.ShowSearch));
        searchBar.SetBinding(SearchBar.TextProperty, nameof(ParzellenViewModel.SearchText), BindingMode.TwoWay);

        var listEmptyLabel = new Label
        {
            TextColor = Colors.Gray,
            HorizontalTextAlignment = Microsoft.Maui.TextAlignment.Center,
            VerticalTextAlignment = Microsoft.Maui.TextAlignment.Center
        };
        listEmptyLabel.SetBinding(Label.TextProperty, nameof(ParzellenViewModel.EmptyListText));

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

        var listSectionTitleLabel = new Label { FontAttributes = FontAttributes.Bold, FontSize = 18 };
        listSectionTitleLabel.SetBinding(Label.TextProperty, nameof(ParzellenViewModel.ListTitle));

        var listSection = new Border
        {
            Stroke = Colors.LightGray,
            Padding = 16,
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    listSectionTitleLabel,
                    searchBar,
                    parzellenList
                }
            }
        };
        listSection.SetBinding(IsVisibleProperty, nameof(ParzellenViewModel.ShowListSection));

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
        editButton.SetBinding(IsVisibleProperty, nameof(ParzellenViewModel.ShowGlobalActions));
        editButton.SetBinding(IsEnabledProperty, nameof(ParzellenViewModel.CanEditStammdaten));
        editButton.Clicked += (_, _) => _viewModel.BeginEditMode();

        var stromButton = new Button();
        stromButton.SetBinding(Button.TextProperty, nameof(ParzellenViewModel.StromButtonText));
        stromButton.SetBinding(IsEnabledProperty, nameof(ParzellenViewModel.CanOpenStromAction));
        stromButton.Clicked += async (_, _) => await OpenAblesungAsync("strom");

        var wasserButton = new Button();
        wasserButton.SetBinding(Button.TextProperty, nameof(ParzellenViewModel.WasserButtonText));
        wasserButton.SetBinding(IsEnabledProperty, nameof(ParzellenViewModel.CanOpenWasserAction));
        wasserButton.Clicked += async (_, _) => await OpenAblesungAsync("wasser");

        var dokumenteButton = new Button();
        dokumenteButton.SetBinding(Button.TextProperty, nameof(ParzellenViewModel.DokumenteButtonText));
        dokumenteButton.SetBinding(IsEnabledProperty, nameof(ParzellenViewModel.CanOpenDokumenteAction));
        dokumenteButton.Clicked += async (_, _) => await OpenDokumenteAsync();

        var memberContextDetailSection = new Border
        {
            Stroke = Colors.LightGray,
            Padding = 12,
            Content = new VerticalStackLayout
            {
                Spacing = 10,
                Children =
                {
                    currentParzelleLabel,
                    CreateSection("Parzellen-Details",
                        CreateValueLabel("Größe / Fläche", nameof(ParzellenViewModel.SelectedParzelleSizeText)),
                        CreateValueLabel("Strom", nameof(ParzellenViewModel.SelectedParzelleStromAvailabilityText)),
                        CreateValueLabel("Wasser", nameof(ParzellenViewModel.SelectedParzelleWasserAvailabilityText))),
                    CreateSection("Aktionen",
                        new Label
                        {
                            Text = "Die Aktionen beziehen sich nur auf die aktuell ausgewählte Parzelle dieses Mitglieds.",
                            TextColor = Colors.Gray,
                            LineBreakMode = Microsoft.Maui.LineBreakMode.WordWrap
                        },
                        new HorizontalStackLayout
                        {
                            Spacing = 8,
                            Children = { stromButton, wasserButton, dokumenteButton }
                        })
                }
            }
        };
        memberContextDetailSection.SetBinding(IsVisibleProperty, nameof(ParzellenViewModel.ShowMemberContextDetail));
        detailContainer.Children.Add(memberContextDetailSection);

        var readOnlyStammdatenSection = new Border
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
        };
        readOnlyStammdatenSection.SetBinding(IsVisibleProperty, nameof(ParzellenViewModel.ShowGlobalDetail));
        detailContainer.Children.Add(readOnlyStammdatenSection);

        var editSection = CreateSection("Stammdaten bearbeiten",
            CreateEditorEntry("Fläche", nameof(ParzellenViewModel.EditFlaeche)),
            CreateEditorSwitch("hat Wasser", nameof(ParzellenViewModel.EditHatWasser)),
            CreateEditorSwitch("hat Strom", nameof(ParzellenViewModel.EditHatStrom)));
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

        if (IsGlobalParzellenRoute() && _viewModel.IsContextBound)
        {
            await _viewModel.ClearRequestedContextAsync();
            _initialized = true;
            return;
        }

        if (!_initialized)
        {
            await _viewModel.InitializeAsync();
            _initialized = true;
            return;
        }

        await _viewModel.ApplyRequestedContextAsync();
        await _viewModel.RefreshSelectedDetailAsync();
    }

    private static bool IsGlobalParzellenRoute()
    {
        var location = Shell.Current?.CurrentState?.Location?.OriginalString;
        return !string.IsNullOrWhiteSpace(location)
            && location.Contains("/parzellen", StringComparison.OrdinalIgnoreCase)
            && !location.Contains(nameof(ParzellenPage), StringComparison.Ordinal);
    }

    private async Task SaveStammdatenAsync()
    {
        if (_viewModel.HasFlaecheChanged())
        {
            var confirm = await DisplayAlert(
                "Bestätigung",
                "Bist du dir sicher, dass du die Fläche der Parzelle ändern möchtest?",
                "Ja",
                "Abbrechen");

            if (!confirm)
                return;
        }

        var ok = await _viewModel.SaveStammdatenAsync();
        if (ok)
            await DisplayAlert("OK", "Parzellen-Stammdaten gespeichert.", "OK");
    }

    private async Task OpenAblesungAsync(string medium)
    {
        var detail = _viewModel.SelectedDetail;
        if (detail == null)
            return;

        var hasMedium = string.Equals(medium, "wasser", StringComparison.OrdinalIgnoreCase)
            ? detail.HatWasser
            : detail.HatStrom;
        if (!hasMedium)
        {
            await DisplayAlert("Hinweis", string.Equals(medium, "wasser", StringComparison.OrdinalIgnoreCase)
                ? "Für diese Parzelle ist kein Wasseranschluss hinterlegt."
                : "Für diese Parzelle ist kein Stromanschluss hinterlegt.", "OK");
            return;
        }

        await Shell.Current.GoToAsync($"{nameof(ParzellenAblesungenPage)}?parzelleId={detail.ParzelleId}&medium={medium}");
    }

    private async Task OpenDokumenteAsync()
    {
        var detail = _viewModel.SelectedDetail;
        if (detail == null)
            return;

        await Shell.Current.GoToAsync($"{nameof(DokumentePage)}?scope=parzelle&parzelleId={detail.ParzelleId}");
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

}
