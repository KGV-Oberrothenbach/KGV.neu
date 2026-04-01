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

public sealed class ZaehlerwechselAusbauPage : ContentPage
{
    private readonly ISupabaseService _supabaseService;
    private readonly IPhotoUploadTestService _photoUploadService;
    private readonly ZaehlerwechselWorkflowState _workflowState;
    private readonly Label _contextLabel;
    private readonly Label _statusLabel;
    private readonly DatePicker _ausbauDatumPicker;
    private readonly Entry _standEntry;
    private readonly Label _photoLabel;
    private readonly Button _capturePhotoButton;
    private readonly Button _pickPhotoButton;
    private readonly Button _clearPhotoButton;
    private byte[]? _selectedPhotoContent;
    private string _selectedPhotoFileName = string.Empty;
    private string _selectedPhotoContentType = "application/octet-stream";
    private RfidScanContextResult? _context;
    private bool _isBusy;

    public ZaehlerwechselAusbauPage(ISupabaseService supabaseService, IPhotoUploadTestService photoUploadService, ZaehlerwechselWorkflowState workflowState)
    {
        _supabaseService = supabaseService;
        _photoUploadService = photoUploadService;
        _workflowState = workflowState;

        Title = "Zählerausbau";

        _contextLabel = new Label { LineBreakMode = LineBreakMode.WordWrap };
        _statusLabel = new Label { TextColor = Colors.DarkSlateBlue, LineBreakMode = LineBreakMode.WordWrap };
        _ausbauDatumPicker = new DatePicker { Date = DateTime.Today };
        _standEntry = new Entry { Placeholder = "Ausbau-Zählerstand", Keyboard = Keyboard.Numeric };

        _photoLabel = new Label { Text = "Noch kein Foto gewählt.", LineBreakMode = LineBreakMode.WordWrap, TextColor = Colors.Gray };
        _capturePhotoButton = new Button { Text = "Foto aufnehmen" };
        _capturePhotoButton.Clicked += async (_, _) => await SelectPhotoAsync(capture: true);
        _pickPhotoButton = new Button { Text = "Foto übernehmen" };
        _pickPhotoButton.Clicked += async (_, _) => await SelectPhotoAsync(capture: false);
        _clearPhotoButton = new Button { Text = "Foto entfernen" };
        _clearPhotoButton.Clicked += (_, _) => ClearPhotoSelection();

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
                ? $"ausbau-{DateTime.Now:yyyyMMdd-HHmmss}.jpg"
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

        if (_context?.Context?.AktiverZaehlerId is not > 0)
        {
            await DisplayAlert("Hinweis", "Es liegt kein aktiver Zähler für den Ausbau vor.", "OK");
            return;
        }

        if (_selectedPhotoContent == null || _selectedPhotoContent.Length == 0)
        {
            await DisplayAlert("Validierung", "Bitte zuerst ein Foto aufnehmen oder übernehmen.", "OK");
            return;
        }

        if (!TryParseDecimal(_standEntry.Text, out var stand) || stand < 0)
        {
            await DisplayAlert("Validierung", "Bitte einen gültigen Ausbau-Zählerstand eingeben.", "OK");
            _standEntry.Focus();
            return;
        }

        var context = _context.Context;
        _isBusy = true;
        try
        {
            var photoResult = await _photoUploadService.UploadAsync(new PhotoUploadTestRequest
            {
                FileName = _selectedPhotoFileName,
                ContentType = _selectedPhotoContentType,
                FileContent = _selectedPhotoContent,
                Kind = "ausbau",
                Medium = NormalizeMedium(context.Medium),
                Anlage = context.Anlage?.Trim() ?? string.Empty,
                Garten = context.GartenNr?.Trim() ?? string.Empty,
                Zaehlernummer = string.IsNullOrWhiteSpace(context.Zaehlernummer) ? null : context.Zaehlernummer.Trim(),
                Datum = _ausbauDatumPicker.Date
            });

            if (!photoResult.Success || string.IsNullOrWhiteSpace(photoResult.RelativePath))
            {
                var message = string.IsNullOrWhiteSpace(photoResult.ErrorSummary)
                    ? "Das Foto konnte nicht hochgeladen werden."
                    : photoResult.ErrorSummary;

                if (!string.IsNullOrWhiteSpace(photoResult.RequestId))
                    message = $"{message}{Environment.NewLine}Support-ID: {photoResult.RequestId}";

                _statusLabel.Text = message;
                return;
            }

            var ablesung = new AblesungInsertRecord
            {
                ZaehlerId = context.AktiverZaehlerId!.Value,
                Ablesedatum = _ausbauDatumPicker.Date,
                Stand = stand,
                Art = AblesungArt.Ausbau,
                FotoPfad = photoResult.RelativePath,
                FotoDateiname = string.IsNullOrWhiteSpace(photoResult.FileName) ? null : photoResult.FileName,
                FotoDriveFileId = string.IsNullOrWhiteSpace(photoResult.FileId) ? null : photoResult.FileId,
                Freigegeben = true
            };

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
            await NavigateToFreshScanAsync();
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
        _ausbauDatumPicker.IsEnabled = !_isBusy;
        _standEntry.IsEnabled = !_isBusy;
        _capturePhotoButton.IsEnabled = !_isBusy && MediaPicker.Default.IsCaptureSupported;
        _pickPhotoButton.IsEnabled = !_isBusy;
        _clearPhotoButton.IsEnabled = !_isBusy;
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
