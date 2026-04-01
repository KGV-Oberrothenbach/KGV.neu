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
    private readonly Entry _zaehlernummerEntry;
    private readonly Entry _eichjahrEntry;
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
        _zaehlernummerEntry = new Entry { Placeholder = "Zählernummer" };
        _eichjahrEntry = new Entry { Placeholder = "Eichjahr", Keyboard = Keyboard.Numeric, Text = DateTime.Today.Year.ToString(CultureInfo.InvariantCulture) };

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
                        Text = "Für den bekannten RFID-Kontext ohne aktiven Zähler wird hier nur der neue Zähler angelegt. Die Erstablesung folgt direkt im bestehenden Ablese-Flow und enthält genau ein Foto für diesen Vorgang.",
                        LineBreakMode = LineBreakMode.WordWrap
                    },
                    CreateSection("Scan-Kontext", _contextLabel),
                    CreateSection(
                        "Einbau",
                        CreateField("Einbau-Datum", _einbauDatumPicker),
                        CreateField("Zählernummer", _zaehlernummerEntry),
                        CreateField("Eichjahr", _eichjahrEntry),
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

        if (string.IsNullOrWhiteSpace(_zaehlernummerEntry.Text))
        {
            await DisplayAlert("Validierung", "Bitte eine Zählernummer eingeben.", "OK");
            _zaehlernummerEntry.Focus();
            return;
        }

        if (!TryParseYear(_eichjahrEntry.Text, out var eichjahr))
        {
            await DisplayAlert("Validierung", "Bitte ein gültiges Eichjahr eingeben.", "OK");
            _eichjahrEntry.Focus();
            return;
        }

        var context = _context.Context;
        var medium = context.Medium;
        var einbauDatum = _einbauDatumPicker.Date;
        var zaehlernummer = _zaehlernummerEntry.Text.Trim();
        var eichdatum = new DateTime(eichjahr, 1, 1);

        _isBusy = true;
        try
        {
            var meterCreated = string.Equals(medium, "wasser", StringComparison.OrdinalIgnoreCase)
                ? await _supabaseService.TryAddWasserzaehlerAsync(new WasserzaehlerInsertRecord
                {
                    ParzelleId = context.ParzelleId,
                    Zaehlernummer = zaehlernummer,
                    Eichdatum = eichdatum,
                    EingebautAm = einbauDatum
                })
                : await _supabaseService.TryAddStromzaehlerAsync(new StromzaehlerInsertRecord
                {
                    ParzelleId = context.ParzelleId,
                    Zaehlernummer = zaehlernummer,
                    Eichdatum = eichdatum,
                    EingebautAm = einbauDatum
                });

            if (!meterCreated.Success)
            {
                _statusLabel.Text = string.IsNullOrWhiteSpace(meterCreated.UserMessage)
                    ? "Neuer Zähler konnte nicht angelegt werden."
                    : meterCreated.UserMessage;
                return;
            }

            var activeMeterId = await ResolveNewMeterIdAsync(context.ParzelleId, medium, einbauDatum, zaehlernummer);
            if (activeMeterId <= 0)
            {
            _statusLabel.Text = "Der neu angelegte Zähler konnte für den anschließenden Ablese-Flow nicht wieder aufgelöst werden.";
                return;
            }

        _workflowState.SetPendingAblesungFlow(
            CreateInitialReadingContext(context, activeMeterId, zaehlernummer, eichdatum, einbauDatum),
            AblesungArt.Einbau,
            einbauDatum,
            "Neuer Zähler angelegt. Jetzt folgt direkt die Anfangsablesung mit `Art = einbau` und genau einem Foto in diesem Schritt.");

        await DisplayAlert("OK", "Zählereinbau erfolgreich gespeichert. Die Anfangsablesung folgt jetzt im bestehenden Ablese-Flow.", "OK");
        await Shell.Current.GoToAsync(nameof(AblesungErfassenPage));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ZaehlerwechselEinbauPage] Save failed: {ex}");
            _statusLabel.Text = "Unerwarteter Fehler beim Speichern.";
        }
        finally
        {
            _isBusy = false;
            UpdateBusyState();
        }
    }

    private void UpdateBusyState()
    {
        _einbauDatumPicker.IsEnabled = !_isBusy;
        _zaehlernummerEntry.IsEnabled = !_isBusy;
        _eichjahrEntry.IsEnabled = !_isBusy;
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

    private static RfidScanContextResult CreateInitialReadingContext(
        RfidScanContextRecord sourceContext,
        long activeMeterId,
        string zaehlernummer,
        DateTime eichdatum,
        DateTime einbauDatum)
    {
        var context = new RfidScanContextRecord
        {
            ParzelleId = sourceContext.ParzelleId,
            Anlage = sourceContext.Anlage,
            GartenNr = sourceContext.GartenNr,
            Medium = sourceContext.Medium,
            RfidTagUid = sourceContext.RfidTagUid,
            AktiverZaehlerId = Convert.ToInt32(activeMeterId),
            Zaehlernummer = zaehlernummer,
            Eichdatum = eichdatum,
            EingebautAm = einbauDatum,
            Status = "Aktiv"
        };

        return new RfidScanContextResult
        {
            NormalizedUid = sourceContext.RfidTagUid?.Trim() ?? string.Empty,
            State = RfidScanContextState.KnownWithActiveMeter,
            Context = context,
            Message = "Neuer Zähler angelegt. Die Anfangsablesung kann jetzt erfasst werden."
        };
    }

    private static bool TryParseYear(string? value, out int year)
    {
        if (!int.TryParse((value ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out year))
            return false;

        return year >= 1900 && year <= 9999;
    }

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
