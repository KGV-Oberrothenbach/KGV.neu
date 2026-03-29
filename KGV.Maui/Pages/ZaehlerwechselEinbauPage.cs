using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Maui.State;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System.Globalization;

namespace KGV.Maui.Pages;

public sealed class ZaehlerwechselEinbauPage : ContentPage
{
    private readonly ISupabaseService _supabaseService;
    private readonly ZaehlerwechselWorkflowState _workflowState;
    private readonly Label _contextLabel;
    private readonly Label _statusLabel;
    private readonly DatePicker _einbauDatumPicker;
    private readonly Entry _anfangsstandEntry;
    private readonly Entry _zaehlernummerEntry;
    private readonly DatePicker _eichdatumPicker;
    private RfidScanContextResult? _context;
    private bool _isBusy;

    public ZaehlerwechselEinbauPage(ISupabaseService supabaseService, ZaehlerwechselWorkflowState workflowState)
    {
        _supabaseService = supabaseService;
        _workflowState = workflowState;

        Title = "Zählereinbau";

        _contextLabel = new Label { LineBreakMode = LineBreakMode.WordWrap };
        _statusLabel = new Label { TextColor = Colors.DarkSlateBlue, LineBreakMode = LineBreakMode.WordWrap };
        _einbauDatumPicker = new DatePicker { Date = DateTime.Today };
        _anfangsstandEntry = new Entry { Placeholder = "Anfangsstand", Keyboard = Keyboard.Numeric };
        _zaehlernummerEntry = new Entry { Placeholder = "Zählernummer" };
        _eichdatumPicker = new DatePicker { Date = DateTime.Today };

        var cancelButton = new Button { Text = "Zurück zum Zählerwechsel" };
        cancelButton.Clicked += async (_, _) => await Shell.Current.GoToAsync(nameof(ZaehlerwechselPage));

        var saveButton = new Button { Text = "Einbau speichern" };
        saveButton.Clicked += OnSaveClicked;

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 12,
                Children =
                {
                    new Label { Text = "Zählereinbau", FontSize = 24, FontAttributes = FontAttributes.Bold },
                    new Label
                    {
                        Text = "Für den bekannten RFID-Kontext ohne aktiven Zähler wird ein neuer Zähler angelegt und direkt die Anfangsablesung gespeichert.",
                        LineBreakMode = LineBreakMode.WordWrap
                    },
                    CreateSection("Scan-Kontext", _contextLabel),
                    CreateSection(
                        "Einbau",
                        CreateField("Einbau-Datum", _einbauDatumPicker),
                        CreateField("Anfangsstand", _anfangsstandEntry),
                        CreateField("Zählernummer", _zaehlernummerEntry),
                        CreateField("Eichdatum", _eichdatumPicker),
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
        if (_context?.State != RfidScanContextState.KnownWithoutActiveMeter || _context.Context == null)
        {
            _contextLabel.Text = "Kein Einbau-Kontext vorhanden. Bitte zuerst im Zählerwechsel einen bekannten RFID-Tag ohne aktiven Zähler scannen.";
            return;
        }

        var context = _context.Context;
        _contextLabel.Text = $"Parzelle: {context.ParzelleDisplayName}{Environment.NewLine}" +
                             $"Medium: {context.MediumDisplay}{Environment.NewLine}" +
                             $"RFID-Kontext: {context.RfidDisplay}";
        _statusLabel.Text = string.Empty;
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (_isBusy)
            return;

        if (_context?.Context == null)
        {
            await DisplayAlert("Hinweis", "Es liegt kein Einbau-Kontext vor.", "OK");
            return;
        }

        if (!TryParseDecimal(_anfangsstandEntry.Text, out var stand) || stand < 0)
        {
            await DisplayAlert("Validierung", "Bitte einen gültigen Anfangsstand eingeben.", "OK");
            _anfangsstandEntry.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(_zaehlernummerEntry.Text))
        {
            await DisplayAlert("Validierung", "Bitte eine Zählernummer eingeben.", "OK");
            _zaehlernummerEntry.Focus();
            return;
        }

        var context = _context.Context;
        var medium = context.Medium;
        var einbauDatum = _einbauDatumPicker.Date;
        var zaehlernummer = _zaehlernummerEntry.Text.Trim();

        _isBusy = true;
        try
        {
            var meterCreated = string.Equals(medium, "wasser", StringComparison.OrdinalIgnoreCase)
                ? await _supabaseService.AddWasserzaehlerAsync(new WasserzaehlerInsertRecord
                {
                    ParzelleId = context.ParzelleId,
                    Zaehlernummer = zaehlernummer,
                    Eichdatum = _eichdatumPicker.Date,
                    EingebautAm = einbauDatum
                })
                : await _supabaseService.AddStromzaehlerAsync(new StromzaehlerInsertRecord
                {
                    ParzelleId = context.ParzelleId,
                    Zaehlernummer = zaehlernummer,
                    Eichdatum = _eichdatumPicker.Date,
                    EingebautAm = einbauDatum
                });

            if (!meterCreated)
            {
                _statusLabel.Text = "Neuer Zähler konnte nicht angelegt werden.";
                return;
            }

            var activeMeterId = await ResolveNewMeterIdAsync(context.ParzelleId, medium, einbauDatum, zaehlernummer);
            if (activeMeterId <= 0)
            {
                _statusLabel.Text = "Der neu angelegte Zähler konnte für die Anfangsablesung nicht wieder aufgelöst werden.";
                return;
            }

            var readingSaved = await _supabaseService.AddAblesungAsync(new AblesungInsertRecord
            {
                ZaehlerId = activeMeterId,
                ZaehlerTyp = GetZaehlerTyp(medium),
                Ablesedatum = einbauDatum,
                Stand = stand,
                FotoPfad = null,
                Freigegeben = true
            });

            if (!readingSaved)
            {
                _statusLabel.Text = "Anfangsablesung konnte nicht gespeichert werden.";
                return;
            }

            _workflowState.Clear();
            await DisplayAlert("OK", "Zählereinbau erfolgreich gespeichert.", "OK");
            await NavigateToFreshScanAsync();
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

    private async Task<long> ResolveNewMeterIdAsync(int parzelleId, string? medium, DateTime onDate, string zaehlernummer)
    {
        if (string.Equals(medium, "wasser", StringComparison.OrdinalIgnoreCase))
        {
            var meter = await _supabaseService.GetActiveWasserzaehlerAsync(parzelleId, onDate);
            if (meter?.Id > 0 && string.Equals(meter.Zaehlernummer?.Trim(), zaehlernummer, StringComparison.OrdinalIgnoreCase))
                return meter.Id;

            return meter?.Id ?? 0;
        }

        var stromMeter = await _supabaseService.GetActiveStromzaehlerAsync(parzelleId, onDate);
        if (stromMeter?.Id > 0 && string.Equals(stromMeter.Zaehlernummer?.Trim(), zaehlernummer, StringComparison.OrdinalIgnoreCase))
            return stromMeter.Id;

        return stromMeter?.Id ?? 0;
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

    private static async Task NavigateToFreshScanAsync()
    {
        await Shell.Current.GoToAsync("//ablesen");
        await Shell.Current.GoToAsync(nameof(ZaehlerwechselPage));
    }
}
