using KGV.Core.Models;
using KGV.Maui.Services;
using KGV.Maui.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Threading.Tasks;

namespace KGV.Maui.Pages;

public sealed class RfidEinrichtenPage : ContentPage
{
    private readonly RfidEinrichtenViewModel _viewModel;
    private readonly INfcScanService _nfcScanService;
    private readonly Label _nfcStatusLabel;
    private readonly Button _openNfcSettingsButton;
    private readonly Button _restartScanButton;

    public RfidEinrichtenPage()
    {
        var services = IPlatformApplication.Current?.Services
            ?? throw new InvalidOperationException("MAUI-Services sind aktuell nicht verfügbar.");

        _viewModel = services.GetRequiredService<RfidEinrichtenViewModel>();
        _nfcScanService = services.GetRequiredService<INfcScanService>();
        BindingContext = _viewModel;
        Title = "RFID einrichten";

        var titleLabel = new Label { Text = "RFID einrichten", FontSize = 24, FontAttributes = FontAttributes.Bold };
        var descriptionLabel = new Label
        {
            Text = "Der Einstieg startet jetzt fachlich zuerst nur mit dem Scan. Erst wenn der RFID-Tag noch nicht bekannt ist, werden Parzelle, Medium und Speichern eingeblendet.",
            LineBreakMode = LineBreakMode.WordWrap
        };

        _nfcStatusLabel = new Label { TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
        _openNfcSettingsButton = new Button { Text = "NFC-Einstellungen öffnen", IsVisible = false };
        _openNfcSettingsButton.Clicked += async (_, _) => await _nfcScanService.OpenSettingsAsync();
        _restartScanButton = new Button { Text = "Scan aktivieren" };
        _restartScanButton.Clicked += async (_, _) =>
        {
            _viewModel.ResetForNewScan();
            await StartNfcAsync();
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

        var existingTagBorder = new Border
        {
            Stroke = Colors.LightGray,
            Padding = 14,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(14) },
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    new Label { Text = "Vorhandener RFID-Tag", FontAttributes = FontAttributes.Bold },
                    CreateValueLabel("Gelesene UID", nameof(RfidEinrichtenViewModel.ScannedUidDisplay)),
                    CreateValueLabel("Einordnung", nameof(RfidEinrichtenViewModel.ExistingTagSummary))
                }
            }
        };
        existingTagBorder.SetBinding(IsVisibleProperty, nameof(RfidEinrichtenViewModel.ShowKnownTagResult));

        var assignmentBorder = new Border
        {
            Stroke = Colors.LightGray,
            Padding = 14,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(14) },
            Content = new VerticalStackLayout
            {
                Spacing = 12,
                Children =
                {
                    new Label { Text = "Neuen RFID-Tag zuordnen", FontAttributes = FontAttributes.Bold },
                    CreateValueLabel("Gelesene UID", nameof(RfidEinrichtenViewModel.ScannedUidDisplay)),
                    new Label { Text = "Parzelle", FontAttributes = FontAttributes.Bold },
                    parzellePicker,
                    stromValue,
                    wasserValue,
                    new Label { Text = "Medium", FontAttributes = FontAttributes.Bold },
                    mediumPicker,
                    checkButton,
                    saveButton
                }
            }
        };
        assignmentBorder.SetBinding(IsVisibleProperty, nameof(RfidEinrichtenViewModel.ShowAssignmentStep));

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
                    new Border
                    {
                        Stroke = Colors.LightGray,
                        Padding = 14,
                        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(14) },
                        Content = new VerticalStackLayout
                        {
                            Spacing = 8,
                            Children =
                            {
                                new Label { Text = "RFID-Tag scannen", FontAttributes = FontAttributes.Bold },
                                _nfcStatusLabel,
                                new HorizontalStackLayout
                                {
                                    Spacing = 8,
                                    Children = { _restartScanButton, _openNfcSettingsButton }
                                }
                            }
                        }
                    },
                    statusLabel,
                    existingTagBorder,
                    assignmentBorder,
                    backToOverviewButton
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
        _viewModel.ResetForNewScan();
        _nfcScanService.TagScanned -= OnTagScanned;
        _nfcScanService.TagScanned += OnTagScanned;
        await StartNfcAsync();
    }

    protected override async void OnDisappearing()
    {
        _nfcScanService.TagScanned -= OnTagScanned;
        await _nfcScanService.StopScanningAsync();
        base.OnDisappearing();
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

    private async Task StartNfcAsync()
    {
        var availability = await _nfcScanService.StartScanningAsync();
        _nfcStatusLabel.Text = availability.Message;
        _openNfcSettingsButton.IsVisible = availability.State == NfcAvailabilityState.Disabled;
        _restartScanButton.IsEnabled = availability.State == NfcAvailabilityState.Available;
    }

    private async void OnTagScanned(object? sender, string uid)
    {
        if (string.IsNullOrWhiteSpace(uid))
            return;

        _viewModel.UidInput = uid;
        await _viewModel.ResolveUidAsync();
        await _nfcScanService.StopScanningAsync();
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
