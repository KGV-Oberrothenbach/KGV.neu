using KGV.Core.Models;
using KGV.Maui.ViewModels;
using System.ComponentModel;

namespace KGV.Maui.Pages;

public abstract class RfidScanWorkflowPage : ContentPage
{
    private readonly RfidScanContextViewModel _scanContext;
    private readonly Func<RfidScanContextResult?, string> _decisionFactory;
    private readonly Label _decisionLabel;
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

        var resetButton = new Button { Text = "Neuen Scan beginnen" };
        resetButton.Clicked += (_, _) =>
        {
            _scanContext.Reset();
            _decisionLabel.Text = _decisionFactory(_scanContext.Resolution);
        };

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

        _decisionLabel = new Label
        {
            Text = _decisionFactory(_scanContext.Resolution),
            LineBreakMode = LineBreakMode.WordWrap
        };

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
                                new Label { Text = "NFC-Status", FontAttributes = FontAttributes.Bold },
                                new Label
                                {
                                    Text = "Direktes NFC-/RFID-Lesen ist im aktuellen MAUI-Stand noch nicht aktiv am Gerät angebunden. Die manuelle UID-Eingabe bleibt daher vorläufig der Fallback und wird hier ausdrücklich so gekennzeichnet.",
                                    LineBreakMode = LineBreakMode.WordWrap,
                                    TextColor = Colors.Gray
                                }
                            }
                        }
                    },
                    new Label { Text = "RFID-UID manuell eingeben (Fallback)", FontAttributes = FontAttributes.Bold },
                    CreateUidEntry(),
                    new Label
                    {
                        Text = "Wenn kein direkter NFC-Scan verfügbar ist, kann die UID hier manuell geprüft werden. Die Auflösung läuft weiterhin zentral und produktiv über v_rfid_scan_context.",
                        TextColor = Colors.Gray,
                        LineBreakMode = LineBreakMode.WordWrap
                    },
                    new HorizontalStackLayout
                    {
                        Spacing = 8,
                        Children = { CreateResolveButton(), resetButton }
                    },
                    statusLabel,
                    contextBorder,
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
                                new Label { Text = workflowSectionTitle, FontAttributes = FontAttributes.Bold },
                                _decisionLabel
                            }
                        }
                    },
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
            _decisionLabel.Text = _decisionFactory(_scanContext.Resolution);
            return;
        }

        await _scanContext.InitializeAsync();
        _decisionLabel.Text = _decisionFactory(_scanContext.Resolution);
        _initialized = true;
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
        var button = new Button { Text = "Manuelle UID prüfen" };
        button.SetBinding(IsEnabledProperty, nameof(RfidScanContextViewModel.CanResolve));
        button.Clicked += async (_, _) => await _scanContext.ResolveAsync();
        return button;
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
