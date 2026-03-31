using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Maui.State;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Media;
using System.IO;
using System.Globalization;

namespace KGV.Maui.Pages;

public sealed class ZaehlerwechselEinbauPage : ContentPage
{
    private readonly ISupabaseService _supabaseService;
    private readonly IPhotoUploadTestService _photoUploadService;
    private readonly ZaehlerwechselWorkflowState _workflowState;
    private readonly Label _contextLabel;
    private readonly Label _statusLabel;
    private readonly DatePicker _einbauDatumPicker;
    private readonly Entry _zaehlernummerEntry;
    private readonly Entry _eichjahrEntry;
    private readonly Label _photoLabel;
    private readonly Button _capturePhotoButton;
    private readonly Button _pickPhotoButton;
    private readonly Button _clearPhotoButton;
    private byte[]? _selectedPhotoContent;
    private string _selectedPhotoFileName = string.Empty;
    private string _selectedPhotoContentType = "application/octet-stream";
    private RfidScanContextResult? _context;
    private bool _isBusy;

    public ZaehlerwechselEinbauPage(ISupabaseService supabaseService, IPhotoUploadTestService photoUploadService, ZaehlerwechselWorkflowState workflowState)
    {
        _supabaseService = supabaseService;
        _photoUploadService = photoUploadService;
        _workflowState = workflowState;

        Title = "Zählereinbau";

        _contextLabel = new Label { LineBreakMode = LineBreakMode.WordWrap };
        _statusLabel = new Label { TextColor = Colors.DarkSlateBlue, LineBreakMode = LineBreakMode.WordWrap };
        _einbauDatumPicker = new DatePicker { Date = DateTime.Today };
        _zaehlernummerEntry = new Entry { Placeholder = "Zählernummer" };
        _eichjahrEntry = new Entry { Placeholder = "Eichjahr", Keyboard = Keyboard.Numeric, Text = DateTime.Today.Year.ToString(CultureInfo.InvariantCulture) };

        _photoLabel = new Label { Text = "Noch kein Foto gewählt.", LineBreakMode = LineBreakMode.WordWrap, TextColor = Colors.Gray };
        _capturePhotoButton = new Button { Text = "Foto aufnehmen" };
        _capturePhotoButton.Clicked += async (_, _) => await SelectPhotoAsync(capture: true);
        _pickPhotoButton = new Button { Text = "Foto übernehmen" };
        _pickPhotoButton.Clicked += async (_, _) => await SelectPhotoAsync(capture: false);
        _clearPhotoButton = new Button { Text = "Foto entfernen" };
        _clearPhotoButton.Clicked += (_, _) => ClearPhotoSelection();

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
                        Text = "Für den bekannten RFID-Kontext ohne aktiven Zähler wird nur der neue Zähler angelegt. Das Foto wird erst im nachgelagerten Ablese-Flow hochgeladen und in der Anfangsablesung gespeichert (kein Upload ohne Datensatz).",
                        LineBreakMode = LineBreakMode.WordWrap
                    },
                    CreateSection("Scan-Kontext", _contextLabel),
                    CreateSection(
                        "Einbau",
                        CreateField("Einbau-Datum", _einbauDatumPicker),
                        CreateField("Zählernummer", _zaehlernummerEntry),
                        CreateField("Eichjahr", _eichjahrEntry),
                        CreateField("Foto", _photoLabel),
                        new HorizontalStackLayout
                        {
                            Spacing = 8,
                            Children = { _capturePhotoButton, _pickPhotoButton, _clearPhotoButton }
                        },
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

    private async Task SelectPhotoAsync(bool capture)
    {
        if (_isBusy)
            return;

        try
        {
            if (capture && !MediaPicker.Default.IsCaptureSupported)
            {
                _photoLabel.Text = "Fotoaufnahme wird auf diesem Gerät aktuell nicht unterstützt.";
                return;
            }

            var fileResult = capture
                ? await MediaPicker.Default.CapturePhotoAsync()
                : await MediaPicker.Default.PickPhotoAsync();

            if (fileResult == null)
                return;

            await using var stream = await fileResult.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);

            _selectedPhotoContent = memoryStream.ToArray();
            _selectedPhotoFileName = string.IsNullOrWhiteSpace(fileResult.FileName)
                ? $"einbau-{DateTime.Now:yyyyMMdd-HHmmss}.jpg"
                : fileResult.FileName;
            _selectedPhotoContentType = string.IsNullOrWhiteSpace(fileResult.ContentType)
                ? GetContentType(_selectedPhotoFileName)
                : fileResult.ContentType;
            _photoLabel.Text = $"Foto gewählt: {_selectedPhotoFileName}";
            _statusLabel.Text = string.Empty;
        }
        catch (Exception ex)
        {
            _photoLabel.Text = ex.Message;
        }
    }

    private void ClearPhotoSelection()
    {
        _selectedPhotoContent = null;
        _selectedPhotoFileName = string.Empty;
        _selectedPhotoContentType = "application/octet-stream";
        _photoLabel.Text = "Noch kein Foto gewählt.";
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

        if (_selectedPhotoContent == null || _selectedPhotoContent.Length == 0)
        {
            await DisplayAlert("Validierung", "Bitte zuerst ein Foto aufnehmen oder übernehmen.", "OK");
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
            "Neuer Zähler angelegt. Jetzt folgt direkt die Anfangsablesung mit `Art = einbau`. Das Foto wird erst dort hochgeladen und gespeichert.");

        if (_workflowState.PendingAblesungFlow != null)
        {
            _workflowState.PendingAblesungFlow.PendingPhotoContent = _selectedPhotoContent;
            _workflowState.PendingAblesungFlow.PendingPhotoFileName = _selectedPhotoFileName;
            _workflowState.PendingAblesungFlow.PendingPhotoContentType = _selectedPhotoContentType;
        }

        await DisplayAlert("OK", "Zählereinbau erfolgreich gespeichert. Die Anfangsablesung folgt jetzt im bestehenden Ablese-Flow.", "OK");
        await Shell.Current.GoToAsync(nameof(AblesungErfassenPage));
        }
        catch (Exception ex)
        {
            _statusLabel.Text = ex.Message;
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
        _capturePhotoButton.IsEnabled = !_isBusy && MediaPicker.Default.IsCaptureSupported;
        _pickPhotoButton.IsEnabled = !_isBusy;
        _clearPhotoButton.IsEnabled = !_isBusy;
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

    private static string NormalizeMedium(string? medium)
        => string.Equals(medium, "wasser", StringComparison.OrdinalIgnoreCase) ? "wasser" : "strom";

    private static string GetContentType(string? fileName)
    {
        var extension = Path.GetExtension(fileName)?.Trim().ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            ".gif" => "image/gif",
            _ => "application/octet-stream"
        };
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
