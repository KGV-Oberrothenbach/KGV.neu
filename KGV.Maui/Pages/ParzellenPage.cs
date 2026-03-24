using KGV.Core.Models;
using KGV.Maui.ViewModels;
using System.Collections.ObjectModel;
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

        var currentReadings = new ObservableCollection<CurrentReadingItem>();

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
        parzellenView.SetBinding(IsVisibleProperty, nameof(ParzellenViewModel.IsContextBound), converter: new InverseBooleanConverter());

        var selectionHint = new Label { Text = "Bitte Parzelle auswählen.", TextColor = Colors.Gray };
        selectionHint.SetBinding(IsVisibleProperty, nameof(ParzellenViewModel.ShowSelectionHint));

        var contextInfoLabel = new Label
        {
            Text = "Gartenkontext aus dem ausgewählten Mitglied. Strom, Wasser und Garten-Dokumente werden darunter direkt geladen.",
            TextColor = Colors.Gray,
            LineBreakMode = LineBreakMode.WordWrap
        };
        contextInfoLabel.SetBinding(IsVisibleProperty, nameof(ParzellenViewModel.IsContextBound));

        var backToMemberButton = new Button { Text = "Zur Stammdatenansicht" };
        backToMemberButton.SetBinding(IsVisibleProperty, nameof(ParzellenViewModel.IsContextBound));
        backToMemberButton.Clicked += async (_, _) => await Shell.Current.GoToAsync(nameof(MeineDatenPage));

        var clearContextButton = new Button { Text = "Zur globalen Parzellenübersicht" };
        clearContextButton.SetBinding(IsVisibleProperty, nameof(ParzellenViewModel.IsContextBound));
        clearContextButton.Clicked += async (_, _) => await _viewModel.ClearRequestedContextAsync();

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
            CreateValueLabel("Aktiver Stromzähler", "SelectedDetail.AktiverStromzaehler.Zaehlernummer"),
            CreateValueLabel("Strom eingebaut seit", "SelectedDetail.AktiverStromzaehler.EingebautAm"),
            CreateValueLabel("Aktiver Wasserzähler", "SelectedDetail.AktiverWasserzaehler.Zaehlernummer"),
            CreateValueLabel("Wasser eingebaut seit", "SelectedDetail.AktiverWasserzaehler.EingebautAm"),
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

        var currentReadingsEmptyLabel = new Label
        {
            Text = "Aktuell liegen keine belastbaren Ablesedaten vor.",
            TextColor = Colors.Gray,
            LineBreakMode = LineBreakMode.WordWrap
        };

        var currentReadingsView = new CollectionView
        {
            SelectionMode = SelectionMode.None,
            HeightRequest = 220,
            ItemsSource = currentReadings,
            ItemTemplate = new DataTemplate(() =>
            {
                var mediumLabel = new Label { FontAttributes = FontAttributes.Bold };
                mediumLabel.SetBinding(Label.TextProperty, nameof(CurrentReadingItem.Medium));

                var standLabel = new Label();
                standLabel.SetBinding(Label.TextProperty, nameof(CurrentReadingItem.StandDisplay));

                var dateLabel = new Label { TextColor = Colors.Gray };
                dateLabel.SetBinding(Label.TextProperty, nameof(CurrentReadingItem.DateDisplay));

                var meterLabel = new Label { TextColor = Colors.Gray };
                meterLabel.SetBinding(Label.TextProperty, nameof(CurrentReadingItem.MeterDisplay));

                var photoButton = new Button { Text = "Foto öffnen" };
                photoButton.SetBinding(IsVisibleProperty, nameof(CurrentReadingItem.HasPhoto));
                photoButton.Clicked += async (sender, _) =>
                {
                    if (sender is Button button && button.BindingContext is CurrentReadingItem item)
                        await OpenPhotoAsync(item);
                };

                return new Border
                {
                    Padding = 12,
                    Margin = new Thickness(0, 0, 0, 8),
                    Stroke = Colors.LightGray,
                    Content = new VerticalStackLayout
                    {
                        Spacing = 4,
                        Children = { mediumLabel, standLabel, dateLabel, meterLabel, photoButton }
                    }
                };
            })
        };

        var openReadingsWorkflowButton = new Button { Text = "Zum Ablesen-Bereich" };
        openReadingsWorkflowButton.Clicked += async (_, _) => await Shell.Current.GoToAsync("//ablesen");

        detailContainer.Children.Add(CreateSection("Aktuelle Ablesedaten",
            new Label
            {
                Text = "Im Parzellen-Detail bleibt nur der aktuelle ReadOnly-Kontext sichtbar. Operative Ablesungen und Zählerwechsel laufen weiter im eigenen Ablesen-Bereich.",
                TextColor = Colors.Gray,
                LineBreakMode = LineBreakMode.WordWrap
            },
            currentReadingsView,
            currentReadingsEmptyLabel,
            openReadingsWorkflowButton));

        documentsView.SetBinding(ItemsView.ItemsSourceProperty, nameof(ParzellenViewModel.Dokumente));
        detailContainer.Children.Add(CreateSection("Garten-Dokumente",
            documentsLabel,
            new Label
            {
                Text = "Mitgliedsdokumente bleiben in den Stammdaten; hier werden die Dokumente der ausgewählten Parzelle angezeigt.",
                TextColor = Colors.Gray,
                LineBreakMode = LineBreakMode.WordWrap
            },
            documentsView));

        var assignPicker = new Picker { Title = "Mitglied auswählen" };
        assignPicker.SetBinding(Picker.ItemsSourceProperty, nameof(ParzellenViewModel.AssignableMembers));
        assignPicker.SetBinding(Picker.SelectedItemProperty, nameof(ParzellenViewModel.SelectedAssignMember), BindingMode.TwoWay);
        assignPicker.ItemDisplayBinding = new Binding(nameof(MemberDTO.DisplayName));
        assignPicker.SetBinding(IsEnabledProperty, nameof(ParzellenViewModel.CanManageAssignment));

        var assignDatePicker = new DatePicker();
        assignDatePicker.SetBinding(DatePicker.DateProperty, nameof(ParzellenViewModel.AssignVonDatum), BindingMode.TwoWay);
        assignDatePicker.SetBinding(IsEnabledProperty, nameof(ParzellenViewModel.CanManageAssignment));

        var assignButton = new Button { Text = "Zuordnen" };
        assignButton.SetBinding(IsEnabledProperty, nameof(ParzellenViewModel.CanAssign));
        assignButton.Clicked += async (_, _) =>
        {
            var ok = await _viewModel.AssignAsync();
            if (ok)
                await DisplayAlert("OK", "Parzelle erfolgreich zugeordnet.", "OK");
        };

        var endButton = new Button { Text = "Aktive Belegung beenden" };
        endButton.SetBinding(IsEnabledProperty, nameof(ParzellenViewModel.CanEndAssignment));
        endButton.Clicked += async (_, _) =>
        {
            var ok = await _viewModel.EndAssignmentAsync();
            if (ok)
                await DisplayAlert("OK", "Aktive Belegung beendet.", "OK");
        };

        detailContainer.Children.Add(CreateSection("Verwaltung",
            CreateValueLabel("Mitglied zuordnen", null),
            assignPicker,
            CreateValueLabel("Start", null),
            assignDatePicker,
            new HorizontalStackLayout
            {
                Spacing = 8,
                Children = { assignButton, endButton }
            },
            new Label
            {
                Text = "Zuordnung und Beendigung laufen mobil über denselben Parzellen-Belegungspfad wie in WPF.",
                TextColor = Colors.Gray,
                LineBreakMode = LineBreakMode.WordWrap
            }));

        void RebuildCurrentReadings()
        {
            currentReadings.Clear();

            AddReading("Strom", _viewModel.StromAblesungen.FirstOrDefault());
            AddReading("Wasser", _viewModel.WasserAblesungen.FirstOrDefault());

            currentReadingsEmptyLabel.IsVisible = currentReadings.Count == 0;
            currentReadingsView.IsVisible = currentReadings.Count > 0;
        }

        void AddReading(string medium, ZaehlerAblesungDTO? reading)
        {
            if (reading == null)
                return;

            currentReadings.Add(new CurrentReadingItem(
                medium,
                $"Letzter Stand: {reading.Stand}",
                $"Datum: {reading.Ablesedatum:dd.MM.yyyy}",
                string.IsNullOrWhiteSpace(reading.Zaehlernummer) ? "Zähler: —" : $"Zähler: {reading.Zaehlernummer}",
                reading.FotoPfad));
        }

        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ParzellenViewModel.SelectedDetail))
                RebuildCurrentReadings();
        };
        _viewModel.StromAblesungen.CollectionChanged += (_, _) => RebuildCurrentReadings();
        _viewModel.WasserAblesungen.CollectionChanged += (_, _) => RebuildCurrentReadings();
        RebuildCurrentReadings();

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
                    contextInfoLabel,
                    backToMemberButton,
                    clearContextButton,
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

        if (!_initialized)
        {
            await _viewModel.InitializeAsync();
            _initialized = true;
            return;
        }

        await _viewModel.ApplyRequestedContextAsync();
        await _viewModel.RefreshSelectedDetailAsync();
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

    private View CreateValueLabel(string title, string? path)
    {
        return new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                new Label { Text = title, FontAttributes = FontAttributes.Bold, FontSize = 12, TextColor = Colors.Gray },
                string.IsNullOrWhiteSpace(path) ? new Label() : CreateBoundLabel(path)
            }
        };
     }

    private async Task OpenPhotoAsync(CurrentReadingItem item)
    {
        if (!item.HasPhoto || string.IsNullOrWhiteSpace(item.PhotoPath))
            return;

        try
        {
            await Launcher.Default.OpenAsync(item.PhotoPath);
        }
        catch (Exception)
        {
            await DisplayAlert("Foto", "Der Fotopfad konnte auf diesem Gerät nicht direkt geöffnet werden.", "OK");
        }
    }

    private static bool TryParseDecimal(string? value, out decimal result)
    {
        return decimal.TryParse(value, out result)
               || decimal.TryParse(value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out result);
    }

    private sealed record CurrentReadingItem(string Medium, string StandDisplay, string DateDisplay, string MeterDisplay, string? PhotoPath)
    {
        public bool HasPhoto => !string.IsNullOrWhiteSpace(PhotoPath);
    }

    private sealed class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
            => value is bool b ? !b : true;

        public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
            => throw new NotSupportedException();
    }
 }
