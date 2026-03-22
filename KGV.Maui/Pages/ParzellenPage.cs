using KGV.Core.Models;
using KGV.Maui.ViewModels;
using System.Linq;

namespace KGV.Maui.Pages;

public sealed class ParzellenPage : ContentPage
{
    private readonly ParzellenViewModel _viewModel;
    private bool _initialized;

    public ParzellenPage(ParzellenViewModel viewModel)
    {
        _viewModel = viewModel;
        BindingContext = _viewModel;
        Title = "Parzellen";

        var titleLabel = new Label { FontSize = 24, FontAttributes = FontAttributes.Bold };
        titleLabel.SetBinding(Label.TextProperty, nameof(ParzellenViewModel.Title));

        var descriptionLabel = new Label { LineBreakMode = LineBreakMode.WordWrap };
        descriptionLabel.SetBinding(Label.TextProperty, nameof(ParzellenViewModel.Description));

        var hintLabel = new Label { TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
        hintLabel.SetBinding(Label.TextProperty, nameof(ParzellenViewModel.DetailHint));

        var refreshButton = new Button { Text = "Aktualisieren" };
        refreshButton.Clicked += async (_, _) => await _viewModel.RefreshAsync();

        var parzellenView = new CollectionView
        {
            SelectionMode = SelectionMode.Single,
            HeightRequest = 240,
            ItemTemplate = new DataTemplate(() =>
            {
                var title = new Label { FontAttributes = FontAttributes.Bold };
                title.SetBinding(Label.TextProperty, nameof(ParzelleVerwaltungItem.DisplayText));

                var subtitle = new Label { FontSize = 12, TextColor = Colors.Gray };
                subtitle.SetBinding(Label.TextProperty, nameof(ParzelleVerwaltungItem.MitgliedName));

                return new VerticalStackLayout
                {
                    Padding = new Thickness(0, 8),
                    Children = { title, subtitle }
                };
            })
        };
        parzellenView.SetBinding(ItemsView.ItemsSourceProperty, nameof(ParzellenViewModel.Items));
        parzellenView.SetBinding(SelectableItemsView.SelectedItemProperty, nameof(ParzellenViewModel.SelectedItem), BindingMode.TwoWay);

        var selectionHint = new Label { Text = "Bitte Parzelle auswählen.", TextColor = Colors.Gray };
        selectionHint.SetBinding(IsVisibleProperty, nameof(ParzellenViewModel.ShowSelectionHint));

        var detailContainer = new VerticalStackLayout { Spacing = 10 };
        detailContainer.SetBinding(IsVisibleProperty, nameof(ParzellenViewModel.HasSelectedDetail));

        detailContainer.Children.Add(CreateSection("Stammdaten",
            CreateValueLabel("Parzellen-ID", "SelectedDetail.ParzelleId"),
            CreateValueLabel("Garten", "SelectedDetail.GartenNr"),
            CreateValueLabel("Anlage", "SelectedDetail.Anlage")));

        detailContainer.Children.Add(CreateSection("Belegung / Zuordnung",
            CreateValueLabel("Status", "SelectedDetail.StatusText"),
            CreateValueLabel("Mitglied", "SelectedDetail.MitgliedDisplayText"),
            CreateValueLabel("Kontakt", "SelectedDetail.MitgliedKontaktText"),
            CreateValueLabel("Zeitraum", "SelectedDetail.BelegungText")));

        detailContainer.Children.Add(CreateSection("Anschlüsse / Zähler",
            CreateBoundLabel("SelectedDetail.StromStatusText"),
            CreateBoundLabel("SelectedDetail.WasserStatusText")));

        var documentsLabel = CreateBoundLabel("SelectedDetail.DokumenteText");
        var documentsView = new CollectionView
        {
            SelectionMode = SelectionMode.Single,
            HeightRequest = 180,
            ItemTemplate = new DataTemplate(() =>
            {
                var name = new Label { FontAttributes = FontAttributes.Bold };
                name.SetBinding(Label.TextProperty, nameof(DocumentInfo.Name));

                var updatedAt = new Label { FontSize = 12, TextColor = Colors.Gray };
                updatedAt.SetBinding(Label.TextProperty, new Binding(nameof(DocumentInfo.UpdatedAt), stringFormat: "{0:dd.MM.yyyy HH:mm}"));

                return new VerticalStackLayout
                {
                    Padding = new Thickness(0, 6),
                    Children = { name, updatedAt }
                };
            })
        };
        documentsView.SetBinding(ItemsView.ItemsSourceProperty, "SelectedDetail.DokumenteVorschau");
        documentsView.SetBinding(IsVisibleProperty, "SelectedDetail.HasDokumente");
        documentsView.SelectionChanged += async (_, e) =>
        {
            var document = e.CurrentSelection?.FirstOrDefault() as DocumentInfo;
            if (document != null)
                await _viewModel.OpenDocumentAsync(document);

            documentsView.SelectedItem = null;
        };

        detailContainer.Children.Add(CreateSection("Dokumente", documentsLabel, documentsView));

        var statusLabel = new Label { TextColor = Colors.DarkSlateBlue, LineBreakMode = LineBreakMode.WordWrap };
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
                    parzellenView,
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

        if (_initialized)
            return;

        await _viewModel.InitializeAsync();
        _initialized = true;
    }

    private static View CreateSection(string title, params View[] children)
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

    private Label CreateBoundLabel(string path)
    {
        var label = new Label { LineBreakMode = LineBreakMode.WordWrap };
        label.SetBinding(Label.TextProperty, path);
        return label;
    }

    private View CreateValueLabel(string title, string path)
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
 }
