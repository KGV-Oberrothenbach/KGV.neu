using KGV.Maui.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace KGV.Maui.Pages;

public sealed class FotoUploadTestPage : ContentPage
{
    private readonly FotoUploadTestViewModel _viewModel;
    private bool _initialized;

    public FotoUploadTestPage()
    {
        var services = IPlatformApplication.Current?.Services
            ?? throw new InvalidOperationException("MAUI-Services sind aktuell nicht verfügbar.");

        _viewModel = services.GetRequiredService<FotoUploadTestViewModel>();
        BindingContext = _viewModel;
        Title = "Foto-Upload testen";

        var fileNameLabel = new Label { LineBreakMode = LineBreakMode.WordWrap };
        fileNameLabel.SetBinding(Label.TextProperty, nameof(FotoUploadTestViewModel.SelectedFileName));

        var kindPicker = new Picker { Title = "Kind" };
        kindPicker.SetBinding(Picker.ItemsSourceProperty, nameof(FotoUploadTestViewModel.KindOptions));
        kindPicker.SetBinding(Picker.SelectedItemProperty, nameof(FotoUploadTestViewModel.SelectedKind), BindingMode.TwoWay);

        var mediumPicker = new Picker { Title = "Medium" };
        mediumPicker.SetBinding(Picker.ItemsSourceProperty, nameof(FotoUploadTestViewModel.MediumOptions));
        mediumPicker.SetBinding(Picker.SelectedItemProperty, nameof(FotoUploadTestViewModel.SelectedMedium), BindingMode.TwoWay);

        var anlageEntry = new Entry { Placeholder = "Anlage" };
        anlageEntry.SetBinding(Entry.TextProperty, nameof(FotoUploadTestViewModel.Anlage), BindingMode.TwoWay);

        var gartenEntry = new Entry { Placeholder = "Garten" };
        gartenEntry.SetBinding(Entry.TextProperty, nameof(FotoUploadTestViewModel.Garten), BindingMode.TwoWay);

        var zaehlerEntry = new Entry { Placeholder = "Zählernummer (optional)" };
        zaehlerEntry.SetBinding(Entry.TextProperty, nameof(FotoUploadTestViewModel.Zaehlernummer), BindingMode.TwoWay);

        var datePicker = new DatePicker();
        datePicker.SetBinding(DatePicker.DateProperty, nameof(FotoUploadTestViewModel.Datum), BindingMode.TwoWay);

        var pickButton = new Button { Text = "Bild wählen" };
        pickButton.SetBinding(IsEnabledProperty, nameof(FotoUploadTestViewModel.CanPickImage));
        pickButton.Clicked += async (_, _) => await _viewModel.PickImageAsync();

        var uploadButton = new Button { Text = "Upload testen" };
        uploadButton.SetBinding(IsEnabledProperty, nameof(FotoUploadTestViewModel.CanUpload));
        uploadButton.Clicked += async (_, _) => await _viewModel.UploadAsync();

        var statusLabel = new Label { TextColor = Colors.DarkSlateBlue, LineBreakMode = LineBreakMode.WordWrap };
        statusLabel.SetBinding(Label.TextProperty, nameof(FotoUploadTestViewModel.StatusMessage));
        statusLabel.SetBinding(IsVisibleProperty, nameof(FotoUploadTestViewModel.HasStatusMessage));

        var diagnosticsView = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                CreateDetailRow("HTTP-Status", nameof(FotoUploadTestViewModel.HttpStatusDisplay)),
                CreateDetailRow("file_id", nameof(FotoUploadTestViewModel.FileId)),
                CreateDetailRow("file_name", nameof(FotoUploadTestViewModel.ResultFileName)),
                CreateDetailRow("relative_path", nameof(FotoUploadTestViewModel.RelativePath)),
                CreateDetailRow("Exception", nameof(FotoUploadTestViewModel.ExceptionMessage)),
                new Label { Text = "Rohantwort", FontAttributes = FontAttributes.Bold },
                CreateRawResponseEditor()
            }
        };
        diagnosticsView.SetBinding(IsVisibleProperty, nameof(FotoUploadTestViewModel.HasResult));

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 12,
                Children =
                {
                    new Label { Text = "Foto-Upload testen", FontSize = 24, FontAttributes = FontAttributes.Bold },
                    new Label { Text = "Temporäre Admin-Diagnosefläche für den echten Upload gegen kgv-upload-photo. Rohantworten und Transportfehler bleiben absichtlich sichtbar.", LineBreakMode = LineBreakMode.WordWrap },
                    pickButton,
                    fileNameLabel,
                    kindPicker,
                    mediumPicker,
                    anlageEntry,
                    gartenEntry,
                    zaehlerEntry,
                    datePicker,
                    statusLabel,
                    uploadButton,
                    diagnosticsView
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

    private static View CreateDetailRow(string title, string path)
    {
        var valueLabel = new Label { HorizontalOptions = LayoutOptions.End, HorizontalTextAlignment = TextAlignment.End, LineBreakMode = LineBreakMode.WordWrap };
        valueLabel.SetBinding(Label.TextProperty, path);

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star }
            },
            Children =
            {
                new Label { Text = title, FontAttributes = FontAttributes.Bold },
                valueLabel
            }
        };

        Grid.SetColumn(valueLabel, 1);
        return grid;
    }

    private static View CreateRawResponseEditor()
    {
        var editor = new Editor { IsReadOnly = true, AutoSize = EditorAutoSizeOption.TextChanges, MinimumHeightRequest = 180 };
        editor.SetBinding(Editor.TextProperty, nameof(FotoUploadTestViewModel.RawResponseBody));
        return editor;
    }
}
