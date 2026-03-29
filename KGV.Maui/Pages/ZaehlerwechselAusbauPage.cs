using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Maui.State;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System.Globalization;

namespace KGV.Maui.Pages;

public sealed class ZaehlerwechselAusbauPage : ContentPage
{
    private readonly ISupabaseService _supabaseService;
    private readonly ZaehlerwechselWorkflowState _workflowState;
    private readonly Label _contextLabel;
    private readonly Label _statusLabel;
    private readonly DatePicker _ausbauDatumPicker;
    private readonly Entry _standEntry;
    private RfidScanContextResult? _context;
    private bool _isBusy;

    public ZaehlerwechselAusbauPage(ISupabaseService supabaseService, ZaehlerwechselWorkflowState workflowState)
    {
        _supabaseService = supabaseService;
        _workflowState = workflowState;

        Title = "Zählerausbau";

        _contextLabel = new Label { LineBreakMode = LineBreakMode.WordWrap };
        _statusLabel = new Label { TextColor = Colors.DarkSlateBlue, LineBreakMode = LineBreakMode.WordWrap };
        _ausbauDatumPicker = new DatePicker { Date = DateTime.Today };
        _standEntry = new Entry { Placeholder = "Ausbau-Zählerstand", Keyboard = Keyboard.Numeric };

        var cancelButton = new Button { Text = "Zurück zum Zählerwechsel" };
        cancelButton.Clicked += async (_, _) => await Shell.Current.GoToAsync(nameof(ZaehlerwechselPage));

        var saveButton = new Button { Text = "Ausbau speichern" };
        saveButton.Clicked += OnSaveClicked;

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 12,
                Children =
                {
                    new Label { Text = "Zählerausbau", FontSize = 24, FontAttributes = FontAttributes.Bold },
                    new Label
                    {
                        Text = "Der gescannte aktive Zähler wird ausgebaut. Zuerst wird die Schlussablesung gespeichert, danach wird der Zähler mit `ausgebaut_am` beendet.",
                        LineBreakMode = LineBreakMode.WordWrap
                    },
                    CreateSection("Scan-Kontext", _contextLabel),
                    CreateSection(
                        "Ausbau",
                        CreateField("Ausbau-Datum", _ausbauDatumPicker),
                        CreateField("Ausbau-Zählerstand", _standEntry),
                        _statusLabel),
                    cancelButton,
                    saveButton
                }
            }
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadContext();
    }

    private void LoadContext()
    {
        _context = _workflowState.CurrentContext;
        if (_context?.State != RfidScanContextState.KnownWithActiveMeter || _context.Context?.AktiverZaehlerId is not > 0)
        {
            _contextLabel.Text = "Kein aktiver Ausbau-Kontext vorhanden. Bitte zuerst im Zählerwechsel einen bekannten RFID-Tag mit aktivem Zähler scannen.";
            return;
        }

        var context = _context.Context;
        _contextLabel.Text = $"Parzelle: {context.ParzelleDisplayName}{Environment.NewLine}" +
                             $"Medium: {context.MediumDisplay}{Environment.NewLine}" +
                             $"Aktiver Zähler: {context.ActiveMeterDisplay}{Environment.NewLine}" +
                             $"Zählernummer: {context.ZaehlernummerDisplay}";
        _statusLabel.Text = string.Empty;
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (_isBusy)
            return;

        if (_context?.Context?.AktiverZaehlerId is not > 0)
        {
            await DisplayAlert("Hinweis", "Es liegt kein aktiver Zähler für den Ausbau vor.", "OK");
            return;
        }

        if (!TryParseDecimal(_standEntry.Text, out var stand) || stand < 0)
        {
            await DisplayAlert("Validierung", "Bitte einen gültigen Ausbau-Zählerstand eingeben.", "OK");
            _standEntry.Focus();
            return;
        }

        var context = _context.Context;
        var ablesung = new AblesungInsertRecord
        {
            ZaehlerId = context.AktiverZaehlerId!.Value,
            ZaehlerTyp = GetZaehlerTyp(context.Medium),
            Ablesedatum = _ausbauDatumPicker.Date,
            Stand = stand,
            FotoPfad = null,
            Freigegeben = true
        };

        _isBusy = true;
        try
        {
            var readingSaved = await _supabaseService.AddAblesungAsync(ablesung);
            if (!readingSaved)
            {
                _statusLabel.Text = "Schlussablesung konnte nicht gespeichert werden.";
                return;
            }

            var meterStopped = string.Equals(context.Medium, "wasser", StringComparison.OrdinalIgnoreCase)
                ? await _supabaseService.SetWasserzaehlerAusgebautAmAsync(context.AktiverZaehlerId.Value, _ausbauDatumPicker.Date)
                : await _supabaseService.SetStromzaehlerAusgebautAmAsync(context.AktiverZaehlerId.Value, _ausbauDatumPicker.Date);

            if (!meterStopped)
            {
                _statusLabel.Text = "Der aktive Zähler konnte nicht mit Ausbau-Datum beendet werden.";
                return;
            }

            _workflowState.Clear();
            await DisplayAlert("OK", "Zählerausbau erfolgreich gespeichert.", "OK");
            await Shell.Current.GoToAsync(nameof(ZaehlerwechselPage));
        }
        catch (Exception ex)
        {
            _statusLabel.Text = ex.Message;
        }
        finally
        {
            _isBusy = false;
        }
    }

    private static short GetZaehlerTyp(string? medium)
        => string.Equals(medium, "wasser", StringComparison.OrdinalIgnoreCase) ? (short)2 : (short)1;

    private static bool TryParseDecimal(string? value, out decimal result)
        => decimal.TryParse((value ?? string.Empty).Trim().Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out result);

    private static Border CreateSection(string title, params View[] children)
    {
        var stack = new VerticalStackLayout { Spacing = 8 };
        stack.Children.Add(new Label { Text = title, FontAttributes = FontAttributes.Bold, FontSize = 18 });
        foreach (var child in children)
            stack.Children.Add(child);

        return new Border
        {
            Stroke = Colors.LightGray,
            StrokeThickness = 1,
            Padding = 14,
            Content = stack
        };
    }

    private static View CreateField(string title, View input)
    {
        return new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label { Text = title, FontAttributes = FontAttributes.Bold, FontSize = 12, TextColor = Colors.Gray },
                input
            }
        };
    }
}
