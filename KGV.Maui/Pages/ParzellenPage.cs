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

        ZaehlerAblesungDTO? selectedStromReading = null;
        ZaehlerAblesungDTO? selectedWasserReading = null;

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

        var stromMeterNumber = new Entry { Placeholder = "Neue Stromzählernummer" };
        var stromMeterEichdatum = new DatePicker { Date = DateTime.Today };
        var stromMeterWechselDatum = new DatePicker { Date = DateTime.Today };
        var stromReadingDate = new DatePicker { Date = DateTime.Today };
        var stromReadingStand = new Entry { Placeholder = "Zählerstand", Keyboard = Keyboard.Numeric };
        var stromReadingFoto = new Entry { Placeholder = "Foto-URL oder Pfad (optional)" };
        var stromSelectionLabel = new Label { TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap, IsVisible = false };

        var stromReadingsView = new CollectionView
        {
            SelectionMode = SelectionMode.Single,
            HeightRequest = 180,
            ItemTemplate = new DataTemplate(() =>
            {
                var date = new Label { FontAttributes = FontAttributes.Bold };
                date.SetBinding(Label.TextProperty, new Binding(nameof(ZaehlerAblesungDTO.Ablesedatum), stringFormat: "{0:dd.MM.yyyy}"));

                var stand = new Label { TextColor = Colors.Gray };
                stand.SetBinding(Label.TextProperty, new Binding(nameof(ZaehlerAblesungDTO.Stand), stringFormat: "Stand: {0}"));

                return new VerticalStackLayout { Padding = new Thickness(0, 6), Children = { date, stand } };
            })
        };
        stromReadingsView.SetBinding(ItemsView.ItemsSourceProperty, nameof(ParzellenViewModel.StromAblesungen));
        stromReadingsView.SelectionChanged += (_, e) =>
        {
            selectedStromReading = e.CurrentSelection?.FirstOrDefault() as ZaehlerAblesungDTO;
            if (selectedStromReading == null)
            {
                stromSelectionLabel.IsVisible = false;
                return;
            }

            stromReadingDate.Date = selectedStromReading.Ablesedatum.Date;
            stromReadingStand.Text = selectedStromReading.Stand.ToString();
            stromReadingFoto.Text = selectedStromReading.FotoPfad ?? string.Empty;
            stromSelectionLabel.Text = $"Bearbeite Strom-Ablesung vom {selectedStromReading.Ablesedatum:dd.MM.yyyy}.";
            stromSelectionLabel.IsVisible = true;
        };

        var saveStromReadingButton = new Button { Text = "Strom-Ablesung speichern" };
        saveStromReadingButton.Clicked += async (_, _) =>
        {
            if (!TryParseDecimal(stromReadingStand.Text, out var stand))
            {
                await DisplayAlert("Fehler", "Bitte einen gültigen Strom-Zählerstand eingeben.", "OK");
                return;
            }

            var ok = await _viewModel.SaveStromReadingAsync(stromReadingDate.Date, stand, stromReadingFoto.Text, selectedStromReading);
            if (!ok)
                return;

            selectedStromReading = null;
            stromReadingsView.SelectedItem = null;
            stromReadingStand.Text = string.Empty;
            stromReadingFoto.Text = string.Empty;
            stromSelectionLabel.IsVisible = false;
            await DisplayAlert("OK", "Strom-Ablesung gespeichert.", "OK");
        };

        var replaceStromMeterButton = new Button { Text = "Stromzähler tauschen" };
        replaceStromMeterButton.Clicked += async (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(stromMeterNumber.Text))
            {
                await DisplayAlert("Fehler", "Bitte eine Stromzählernummer eingeben.", "OK");
                return;
            }

            var ok = await _viewModel.ReplaceStromMeterAsync(stromMeterNumber.Text, stromMeterEichdatum.Date, stromMeterWechselDatum.Date);
            if (!ok)
                return;

            stromMeterNumber.Text = string.Empty;
            await DisplayAlert("OK", "Stromzähler gespeichert.", "OK");
        };

        detailContainer.Children.Add(CreateSection("Strom",
            CreateBoundLabel("SelectedDetail.StromStatusText"),
            new Label { Text = "Ablesungen", FontAttributes = FontAttributes.Bold, FontSize = 12, TextColor = Colors.Gray },
            stromReadingsView,
            stromSelectionLabel,
            new Label { Text = "Ablesedatum", FontAttributes = FontAttributes.Bold, FontSize = 12, TextColor = Colors.Gray },
            stromReadingDate,
            new Label { Text = "Zählerstand", FontAttributes = FontAttributes.Bold, FontSize = 12, TextColor = Colors.Gray },
            stromReadingStand,
            new Label { Text = "Foto", FontAttributes = FontAttributes.Bold, FontSize = 12, TextColor = Colors.Gray },
            stromReadingFoto,
            saveStromReadingButton,
            new Label { Text = "Zählernummer", FontAttributes = FontAttributes.Bold, FontSize = 12, TextColor = Colors.Gray },
            stromMeterNumber,
            new Label { Text = "Eichdatum", FontAttributes = FontAttributes.Bold, FontSize = 12, TextColor = Colors.Gray },
            stromMeterEichdatum,
            new Label { Text = "Wechseldatum", FontAttributes = FontAttributes.Bold, FontSize = 12, TextColor = Colors.Gray },
            stromMeterWechselDatum,
            replaceStromMeterButton));

        var wasserMeterNumber = new Entry { Placeholder = "Neue Wasserzählernummer" };
        var wasserMeterEichdatum = new DatePicker { Date = DateTime.Today };
        var wasserMeterEinbauDatum = new DatePicker { Date = DateTime.Today };
        var wasserMeterAusbauDatum = new DatePicker { Date = DateTime.Today };
        var wasserReadingDate = new DatePicker { Date = DateTime.Today };
        var wasserReadingStand = new Entry { Placeholder = "Zählerstand", Keyboard = Keyboard.Numeric };
        var wasserReadingFoto = new Entry { Placeholder = "Foto-URL oder Pfad (optional)" };
        var wasserSelectionLabel = new Label { TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap, IsVisible = false };

        var wasserReadingsView = new CollectionView
        {
            SelectionMode = SelectionMode.Single,
            HeightRequest = 180,
            ItemTemplate = new DataTemplate(() =>
            {
                var date = new Label { FontAttributes = FontAttributes.Bold };
                date.SetBinding(Label.TextProperty, new Binding(nameof(ZaehlerAblesungDTO.Ablesedatum), stringFormat: "{0:dd.MM.yyyy}"));

                var stand = new Label { TextColor = Colors.Gray };
                stand.SetBinding(Label.TextProperty, new Binding(nameof(ZaehlerAblesungDTO.Stand), stringFormat: "Stand: {0}"));

                return new VerticalStackLayout { Padding = new Thickness(0, 6), Children = { date, stand } };
            })
        };
        wasserReadingsView.SetBinding(ItemsView.ItemsSourceProperty, nameof(ParzellenViewModel.WasserAblesungen));
        wasserReadingsView.SelectionChanged += (_, e) =>
        {
            selectedWasserReading = e.CurrentSelection?.FirstOrDefault() as ZaehlerAblesungDTO;
            if (selectedWasserReading == null)
            {
                wasserSelectionLabel.IsVisible = false;
                return;
            }

            wasserReadingDate.Date = selectedWasserReading.Ablesedatum.Date;
            wasserReadingStand.Text = selectedWasserReading.Stand.ToString();
            wasserReadingFoto.Text = selectedWasserReading.FotoPfad ?? string.Empty;
            wasserSelectionLabel.Text = $"Bearbeite Wasser-Ablesung vom {selectedWasserReading.Ablesedatum:dd.MM.yyyy}.";
            wasserSelectionLabel.IsVisible = true;
        };

        var saveWasserReadingButton = new Button { Text = "Wasser-Ablesung speichern" };
        saveWasserReadingButton.Clicked += async (_, _) =>
        {
            if (!TryParseDecimal(wasserReadingStand.Text, out var stand))
            {
                await DisplayAlert("Fehler", "Bitte einen gültigen Wasser-Zählerstand eingeben.", "OK");
                return;
            }

            var ok = await _viewModel.SaveWasserReadingAsync(wasserReadingDate.Date, stand, wasserReadingFoto.Text, selectedWasserReading);
            if (!ok)
                return;

            selectedWasserReading = null;
            wasserReadingsView.SelectedItem = null;
            wasserReadingStand.Text = string.Empty;
            wasserReadingFoto.Text = string.Empty;
            wasserSelectionLabel.IsVisible = false;
            await DisplayAlert("OK", "Wasser-Ablesung gespeichert.", "OK");
        };

        var installWasserMeterButton = new Button { Text = "Wasserzähler einbauen" };
        installWasserMeterButton.Clicked += async (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(wasserMeterNumber.Text))
            {
                await DisplayAlert("Fehler", "Bitte eine Wasserzählernummer eingeben.", "OK");
                return;
            }

            var ok = await _viewModel.InstallWasserMeterAsync(wasserMeterNumber.Text, wasserMeterEichdatum.Date, wasserMeterEinbauDatum.Date);
            if (!ok)
                return;

            wasserMeterNumber.Text = string.Empty;
            await DisplayAlert("OK", "Wasserzähler gespeichert.", "OK");
        };

        var removeWasserMeterButton = new Button { Text = "Wasserzähler ausbauen" };
        removeWasserMeterButton.Clicked += async (_, _) =>
        {
            var ok = await _viewModel.RemoveWasserMeterAsync(wasserMeterAusbauDatum.Date);
            if (!ok)
                return;

            await DisplayAlert("OK", "Wasserzähler ausgebaut.", "OK");
        };

        detailContainer.Children.Add(CreateSection("Wasser",
            CreateBoundLabel("SelectedDetail.WasserStatusText"),
            new Label { Text = "Ablesungen", FontAttributes = FontAttributes.Bold, FontSize = 12, TextColor = Colors.Gray },
            wasserReadingsView,
            wasserSelectionLabel,
            new Label { Text = "Ablesedatum", FontAttributes = FontAttributes.Bold, FontSize = 12, TextColor = Colors.Gray },
            wasserReadingDate,
            new Label { Text = "Zählerstand", FontAttributes = FontAttributes.Bold, FontSize = 12, TextColor = Colors.Gray },
            wasserReadingStand,
            new Label { Text = "Foto", FontAttributes = FontAttributes.Bold, FontSize = 12, TextColor = Colors.Gray },
            wasserReadingFoto,
            saveWasserReadingButton,
            new Label { Text = "Zählernummer", FontAttributes = FontAttributes.Bold, FontSize = 12, TextColor = Colors.Gray },
            wasserMeterNumber,
            new Label { Text = "Eichdatum", FontAttributes = FontAttributes.Bold, FontSize = 12, TextColor = Colors.Gray },
            wasserMeterEichdatum,
            new Label { Text = "Einbaudatum", FontAttributes = FontAttributes.Bold, FontSize = 12, TextColor = Colors.Gray },
            wasserMeterEinbauDatum,
            installWasserMeterButton,
            new Label { Text = "Ausbaudatum", FontAttributes = FontAttributes.Bold, FontSize = 12, TextColor = Colors.Gray },
            wasserMeterAusbauDatum,
            removeWasserMeterButton));

        documentsView.SetBinding(ItemsView.ItemsSourceProperty, nameof(ParzellenViewModel.Dokumente));
        detailContainer.Children.Add(CreateSection("Dokumente", documentsLabel, documentsView));

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

    private static bool TryParseDecimal(string? value, out decimal result)
    {
        return decimal.TryParse(value, out result)
               || decimal.TryParse(value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out result);
    }
 }
