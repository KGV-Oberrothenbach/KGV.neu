using KGV.Core.Models;
using KGV.Maui.ViewModels;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace KGV.Maui.Pages;

public abstract class RfidScanWorkflowPage : ContentPage
{
    private readonly RfidScanContextViewModel _scanContext;
    private readonly Func<RfidScanContextResult?, string> _decisionFactory;
    private readonly Label _decisionLabel = new() { LineBreakMode = LineBreakMode.WordWrap };
    private bool _initialized;

    protected RfidScanWorkflowPage(
        string title,
        string description,
        string workflowSectionTitle,
        RfidScanContextViewModel scanContext,
        Func<RfidScanContextResult?, string> decisionFactory)
    {
        _scanContext = scanContext;
        _decisionFactory = decisionFactory;
        _scanContext.PropertyChanged += OnScanContextPropertyChanged;

        BindingContext = _scanContext;
        Title = title;

        var statusLabel = new Label { TextColor = Colors.DarkSlateBlue, LineBreakMode = LineBreakMode.WordWrap };
        statusLabel.SetBinding(Label.TextProperty, nameof(RfidScanContextViewModel.StatusMessage));
        statusLabel.SetBinding(IsVisibleProperty, nameof(RfidScanContextViewModel.HasStatusMessage));

        var resetButton = new Button { Text = "Anderen Tag scannen" };
        resetButton.Clicked += async (_, _) =>
        {
            _scanContext.Reset();
            _decisionLabel.Text = _decisionFactory(_scanContext.Resolution);
            await _scanContext.StartNfcSessionAsync();
        };

        var startScanButton = new Button { Text = "Scan aktivieren" };
        startScanButton.SetBinding(IsEnabledProperty, nameof(RfidScanContextViewModel.CanStartNfcScan));
        startScanButton.Clicked += async (_, _) => await _scanContext.StartNfcSessionAsync();

        var openNfcSettingsButton = new Button { Text = "NFC-Einstellungen öffnen" };
        openNfcSettingsButton.SetBinding(IsVisibleProperty, nameof(RfidScanContextViewModel.CanOpenNfcSettings));
        openNfcSettingsButton.Clicked += async (_, _) => await _scanContext.OpenNfcSettingsAsync();

        var backToOverviewButton = new Button { Text = "Zur Ablesen-Übersicht" };
        backToOverviewButton.Clicked += async (_, _) => await Shell.Current.GoToAsync("//ablesen");

        var contextBorder = new Border
        {
            Stroke = Colors.LightGray,
            Padding = 14,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(14) },
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    CreateBoundValueLabel(nameof(RfidScanContextViewModel.StateDisplay), true),
                    CreateDetailRow("RFID", nameof(RfidScanContextViewModel.NormalizedUid)),
                    CreateDetailRow("Anlage / Garten", nameof(RfidScanContextViewModel.ParzelleDisplayName)),
                    CreateDetailRow("Medium", nameof(RfidScanContextViewModel.MediumDisplay)),
                    CreateDetailRow("Aktiver Zähler", nameof(RfidScanContextViewModel.ActiveMeterDisplay)),
                    CreateDetailRow("Zählernummer", nameof(RfidScanContextViewModel.ZaehlernummerDisplay)),
                    CreateDetailRow("Status", nameof(RfidScanContextViewModel.StatusDisplay)),
                    CreateDetailRow("Eichdatum", nameof(RfidScanContextViewModel.EichdatumDisplay)),
                    CreateDetailRow("Eichfälligkeit", nameof(RfidScanContextViewModel.EichfaelligDisplay))
                }
            }
        };
        contextBorder.SetBinding(IsVisibleProperty, nameof(RfidScanContextViewModel.HasResolution));

        var decisionBorder = new Border
        {
            Stroke = Colors.LightGray,
            Padding = 14,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(14) },
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new Label { Text = workflowSectionTitle, FontAttributes = FontAttributes.Bold },
                    _decisionLabel
                }
            }
        };
        decisionBorder.SetBinding(IsVisibleProperty, nameof(RfidScanContextViewModel.HasResolution));

        _decisionLabel.Text = _decisionFactory(_scanContext.Resolution);

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 12,
                Children =
                {
                    new Label { Text = title, FontSize = 24, FontAttributes = FontAttributes.Bold },
                    new Label { Text = description, LineBreakMode = LineBreakMode.WordWrap },
                    new Border
                    {
                        Stroke = Colors.LightGray,
                        Padding = 14,
                        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(14) },
                        Content = new VerticalStackLayout
                        {
                            Spacing = 6,
                            Children =
                            {
                                CreateBoundValueLabel(nameof(RfidScanContextViewModel.NfcStatusTitle), true),
                                CreateBoundValueLabel(nameof(RfidScanContextViewModel.NfcStatusMessage)),
                                new HorizontalStackLayout
                                {
                                    Spacing = 8,
                                    Children = { startScanButton, openNfcSettingsButton, resetButton }
                                }
                            }
                        }
                    },
                    statusLabel,
                    contextBorder,
                    decisionBorder,
                    backToOverviewButton
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_initialized)
        {
            await _scanContext.RefreshNfcAvailabilityAsync();
            await _scanContext.StartNfcSessionAsync();
            _decisionLabel.Text = _decisionFactory(_scanContext.Resolution);
            return;
        }

        await _scanContext.InitializeAsync();
        await _scanContext.StartNfcSessionAsync();
        _decisionLabel.Text = _decisionFactory(_scanContext.Resolution);
        _initialized = true;
    }

    protected override async void OnDisappearing()
    {
        await _scanContext.StopNfcSessionAsync();
        base.OnDisappearing();
    }

    private void OnScanContextPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RfidScanContextViewModel.Resolution))
            _decisionLabel.Text = _decisionFactory(_scanContext.Resolution);
    }

    private View CreateUidEntry()
    {
        var entry = new Entry { Placeholder = "RFID-UID eingeben" };
        entry.SetBinding(Entry.TextProperty, nameof(RfidScanContextViewModel.UidInput), BindingMode.TwoWay);
        return entry;
    }

    private View CreateResolveButton()
    {
        var button = new Button { Text = "UID als Notfallweg prüfen" };
        button.SetBinding(IsEnabledProperty, nameof(RfidScanContextViewModel.CanResolve));
        button.Clicked += async (_, _) => await _scanContext.ResolveAsync();
        return button;
    }

    private View CreateFallbackSection()
    {
        var parzellePicker = new Picker { Title = "Parzelle wählen" };
        parzellePicker.ItemDisplayBinding = new Binding(nameof(ParzelleRecord.DisplayName));
        parzellePicker.SetBinding(Picker.ItemsSourceProperty, nameof(RfidScanContextViewModel.FallbackParzellen));
        parzellePicker.SetBinding(Picker.SelectedItemProperty, nameof(RfidScanContextViewModel.SelectedFallbackParzelle), BindingMode.TwoWay);

        var mediumPicker = new Picker { Title = "Medium wählen" };
        mediumPicker.ItemDisplayBinding = new Binding(nameof(RfidMediumOption.DisplayName));
        mediumPicker.SetBinding(Picker.ItemsSourceProperty, nameof(RfidScanContextViewModel.FallbackMediumOptions));
        mediumPicker.SetBinding(Picker.SelectedItemProperty, nameof(RfidScanContextViewModel.SelectedFallbackMedium), BindingMode.TwoWay);

        var fallbackButton = new Button { Text = "Kontext ohne NFC laden" };
        fallbackButton.SetBinding(IsEnabledProperty, nameof(RfidScanContextViewModel.CanApplyFallbackContext));
        fallbackButton.Clicked += async (_, _) => await _scanContext.ApplyFallbackContextAsync();

        return new Border
        {
            Stroke = Colors.LightGray,
            Padding = 14,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(14) },
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    new Label { Text = "Fallback ohne NFC", FontAttributes = FontAttributes.Bold },
                    new Label
                    {
                        Text = "Wenn NFC nicht verfügbar oder deaktiviert ist, kann der fachliche Kontext über Parzelle und Medium geladen werden, ohne UID-Tippen als Normalweg zu verwenden.",
                        LineBreakMode = LineBreakMode.WordWrap,
                        TextColor = Colors.Gray
                    },
                    parzellePicker,
                    mediumPicker,
                    fallbackButton
                }
            }
        };
    }

    private View CreateManualEmergencySection()
    {
        return new Border
        {
            Stroke = Colors.LightGray,
            Padding = 14,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(14) },
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    new Label { Text = "Technischer Notfallweg: UID manuell eingeben", FontAttributes = FontAttributes.Bold },
                    new Label
                    {
                        Text = "Nur verwenden, wenn weder NFC noch der fachliche Ersatzweg ausreichen. Die Auflösung läuft weiterhin unverändert über den bestehenden Produktivpfad `v_rfid_scan_context`.",
                        TextColor = Colors.Gray,
                        LineBreakMode = LineBreakMode.WordWrap
                    },
                    CreateUidEntry(),
                    CreateResolveButton()
                }
            }
        };
    }

    private static View CreateBoundValueLabel(string path, bool bold = false)
    {
        var label = new Label { LineBreakMode = LineBreakMode.WordWrap };
        if (bold)
            label.FontAttributes = FontAttributes.Bold;
        label.SetBinding(Label.TextProperty, path);
        return label;
    }

    private static View CreateDetailRow(string title, string path)
    {
        var valueLabel = new Label { HorizontalOptions = LayoutOptions.End, HorizontalTextAlignment = TextAlignment.End };
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
}
