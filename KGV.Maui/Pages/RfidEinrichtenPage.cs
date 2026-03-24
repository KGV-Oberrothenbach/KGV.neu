using KGV.Core.Models;
using KGV.Maui.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace KGV.Maui.Pages;

public sealed class RfidEinrichtenPage : ContentPage
{
    private readonly RfidEinrichtenViewModel _viewModel;
    private bool _initialized;

    public RfidEinrichtenPage()
    {
        var services = IPlatformApplication.Current?.Services
            ?? throw new InvalidOperationException("MAUI-Services sind aktuell nicht verfügbar.");

        _viewModel = services.GetRequiredService<RfidEinrichtenViewModel>();
        BindingContext = _viewModel;
        Title = "RFID einrichten";

        var titleLabel = new Label { Text = "RFID einrichten", FontSize = 24, FontAttributes = FontAttributes.Bold };
        var descriptionLabel = new Label
        {
            Text = "Parzelle wählen, Medium festlegen, UID prüfen und anschließend produktiv speichern.",
            LineBreakMode = LineBreakMode.WordWrap
        };

        var parzellePicker = new Picker { Title = "Parzelle wählen" };
        parzellePicker.ItemDisplayBinding = new Binding(nameof(ParzelleRecord.DisplayName));
        parzellePicker.SetBinding(Picker.ItemsSourceProperty, nameof(RfidEinrichtenViewModel.Parzellen));
        parzellePicker.SetBinding(Picker.SelectedItemProperty, nameof(RfidEinrichtenViewModel.SelectedParzelle), BindingMode.TwoWay);

        var stromValue = CreateValueLabel("Aktuelle Strom-RFID", nameof(RfidEinrichtenViewModel.CurrentStromRfid));
        var wasserValue = CreateValueLabel("Aktuelle Wasser-RFID", nameof(RfidEinrichtenViewModel.CurrentWasserRfid));

        var mediumPicker = new Picker { Title = "Medium wählen" };
        mediumPicker.ItemDisplayBinding = new Binding(nameof(RfidMediumOption.DisplayName));
        mediumPicker.SetBinding(Picker.ItemsSourceProperty, nameof(RfidEinrichtenViewModel.MediumOptions));
        mediumPicker.SetBinding(Picker.SelectedItemProperty, nameof(RfidEinrichtenViewModel.SelectedMedium), BindingMode.TwoWay);

        var uidEntry = new Entry { Placeholder = "RFID-UID eingeben" };
        uidEntry.SetBinding(Entry.TextProperty, nameof(RfidEinrichtenViewModel.UidInput), BindingMode.TwoWay);

        var uidHintLabel = new Label
        {
            Text = "UID wird vor Prüfung und Speicherung getrimmt und in Großbuchstaben normalisiert.",
            TextColor = Colors.Gray,
            LineBreakMode = LineBreakMode.WordWrap
        };

        var checkButton = new Button { Text = "Prüfen" };
        checkButton.SetBinding(IsEnabledProperty, nameof(RfidEinrichtenViewModel.CanCheck));
        checkButton.Clicked += async (_, _) => await _viewModel.CheckAsync();

        var saveButton = new Button { Text = "Speichern" };
        saveButton.SetBinding(IsEnabledProperty, nameof(RfidEinrichtenViewModel.CanSave));
        saveButton.Clicked += async (_, _) => await SaveAsync();

        var backToOverviewButton = new Button { Text = "Zur Ablesen-Übersicht" };
        backToOverviewButton.Clicked += async (_, _) => await Shell.Current.GoToAsync("//ablesen");

        var statusLabel = new Label { TextColor = Colors.DarkSlateBlue, LineBreakMode = LineBreakMode.WordWrap };
        statusLabel.SetBinding(Label.TextProperty, nameof(RfidEinrichtenViewModel.StatusMessage));
        statusLabel.SetBinding(IsVisibleProperty, nameof(RfidEinrichtenViewModel.HasStatusMessage));

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
                    new Label { Text = "Parzelle", FontAttributes = FontAttributes.Bold },
                    parzellePicker,
                    stromValue,
                    wasserValue,
                    new Label { Text = "Medium", FontAttributes = FontAttributes.Bold },
                    mediumPicker,
                    new Label { Text = "UID", FontAttributes = FontAttributes.Bold },
                    uidEntry,
                    uidHintLabel,
                    checkButton,
                    statusLabel,
                    saveButton,
                    backToOverviewButton
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
        _initialized = true;
    }

    private async Task SaveAsync()
    {
        var check = await _viewModel.CheckAsync();
        if (!check.IsValid)
            return;

        var overwriteExisting = false;
        if (check.RequiresOverwriteConfirmation)
        {
            overwriteExisting = await DisplayAlert(
                "RFID überschreiben",
                check.Message + "\n\nSoll die bestehende RFID ersetzt werden?",
                "Überschreiben",
                "Abbrechen");

            if (!overwriteExisting)
                return;
        }

        var result = await _viewModel.SaveAsync(overwriteExisting);
        if (result.Success)
            await DisplayAlert("OK", result.Message, "OK");
    }

    private static View CreateValueLabel(string title, string path)
    {
        var valueLabel = new Label { LineBreakMode = LineBreakMode.WordWrap };
        valueLabel.SetBinding(Label.TextProperty, path);

        return new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                new Label { Text = title, FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Colors.Gray },
                valueLabel
            }
        };
    }
}
