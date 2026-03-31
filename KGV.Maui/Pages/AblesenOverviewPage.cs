using KGV.Core.Interfaces;
using KGV.Core.Models;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using KGV.Maui.Settings;
using KGV.Maui.Services.PendingPhotos;

namespace KGV.Maui.Pages;

public sealed class AblesenOverviewPage : ContentPage
{
    private readonly IPhotoUploadTestService _photoUploadTestService;
    private readonly PendingPhotoSyncService _pendingPhotoSyncService;
    private readonly Button _capturePhotoButton;
    private readonly Button _pickPhotoButton;
    private readonly Button _uploadButton;
    private readonly Label _selectedFileLabel;
    private readonly Label _statusLabel;
    private readonly Label _resultLabel;
    private readonly Entry _anlageEntry;
    private readonly Entry _gartenEntry;
    private readonly Entry _zaehlernummerEntry;
    private readonly Picker _kindPicker;
    private readonly Picker _mediumPicker;
    private readonly DatePicker _datumPicker;
    private readonly Switch _wifiOnlySwitch;
    private readonly Label _wifiOnlyHelpLabel;
    private byte[]? _selectedFileContent;
    private string _selectedFileName = string.Empty;
    private string _selectedContentType = "application/octet-stream";
    private bool _isBusy;

    public AblesenOverviewPage(IPhotoUploadTestService photoUploadTestService, PendingPhotoSyncService pendingPhotoSyncService)
    {
        _photoUploadTestService = photoUploadTestService;
        _pendingPhotoSyncService = pendingPhotoSyncService;
        Title = "Ablesen";

        _capturePhotoButton = new Button { Text = "Foto aufnehmen" };
        _capturePhotoButton.Clicked += async (_, _) => await CapturePhotoAsync();

        _pickPhotoButton = new Button { Text = "Foto hinzufügen" };
        _pickPhotoButton.Clicked += async (_, _) => await PickPhotoAsync();

        _selectedFileLabel = new Label
        {
            Text = "Noch kein Foto ausgewählt.",
            TextColor = Colors.Gray,
            LineBreakMode = LineBreakMode.WordWrap
        };

        _kindPicker = new Picker { Title = "Typ" };
        _kindPicker.ItemsSource = new[] { "ablesung", "ausbau", "einbau" };
        _kindPicker.SelectedIndex = 0;

        _mediumPicker = new Picker { Title = "Medium" };
        _mediumPicker.ItemsSource = new[] { "strom", "wasser" };
        _mediumPicker.SelectedIndex = 0;

        _anlageEntry = new Entry { Placeholder = "Anlage" };
        _anlageEntry.TextChanged += (_, _) => UpdateButtonStates();

        _gartenEntry = new Entry { Placeholder = "Garten" };
        _gartenEntry.TextChanged += (_, _) => UpdateButtonStates();

        _zaehlernummerEntry = new Entry { Placeholder = "Zählernummer (optional)" };
        _datumPicker = new DatePicker { Date = DateTime.Today };

        _wifiOnlySwitch = new Switch { IsToggled = PhotoUploadPreferences.WifiOnly };
        _wifiOnlySwitch.Toggled += (_, e) => PhotoUploadPreferences.WifiOnly = e.Value;

        _wifiOnlyHelpLabel = new Label
        {
            Text = "Wenn aktiviert, werden Fotos lokal zwischengespeichert und erst bei WLAN hochgeladen.",
            TextColor = Colors.Gray,
            LineBreakMode = LineBreakMode.WordWrap
        };

        _uploadButton = new Button { Text = "Upload testen" };
        _uploadButton.Clicked += async (_, _) => await UploadPhotoAsync();

        _statusLabel = new Label
        {
            TextColor = Colors.DarkSlateBlue,
            LineBreakMode = LineBreakMode.WordWrap,
            IsVisible = false
        };

        _resultLabel = new Label
        {
            TextColor = Colors.Gray,
            LineBreakMode = LineBreakMode.WordWrap,
            IsVisible = false
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 12,
                Children =
                {
                    new Label { Text = "Ablesen", FontSize = 24, FontAttributes = FontAttributes.Bold },
                    new VerticalStackLayout
                    {
                        Spacing = 6,
                        Children =
                        {
                            new HorizontalStackLayout
                            {
                                Spacing = 12,
                                Children =
                                {
                                    new Label
                                    {
                                        Text = "Fotos nur über WLAN hochladen",
                                        VerticalOptions = LayoutOptions.Center
                                    },
                                    _wifiOnlySwitch
                                }
                            },
                            _wifiOnlyHelpLabel
                        }
                    },
                    new Label { Text = "Bitte wähle eine Funktion.", LineBreakMode = LineBreakMode.WordWrap },
                    CreateTile("Ablesung erfassen", "RFID-Tag am Gerät scannen; wenn NFC nicht nutzbar ist, steht ein fachlicher Ersatzweg über Parzelle und Medium bereit.", () => Shell.Current.GoToAsync(nameof(AblesungErfassenPage))),
                    CreateTile("Zählerwechsel", "RFID-Tag am Gerät scannen; wenn NFC nicht nutzbar ist, steht ein fachlicher Ersatzweg über Parzelle und Medium bereit.", () => Shell.Current.GoToAsync(nameof(ZaehlerwechselPage))),
                    CreateTile("RFID einrichten", "RFID-Tag am Gerät scannen und der gewählten Parzelle für das gewählte Medium zuordnen.", () => Shell.Current.GoToAsync(nameof(RfidEinrichtenPage))),
                    CreateTile("Fällige Zähler", "Zähler mit naher Eichfälligkeit anzeigen", () => Shell.Current.GoToAsync(nameof(FaelligeZaehlerPage))),
                    CreatePhotoTestSection()
                }
            }
        };

        UpdateButtonStates();
    }

    private static View CreateTile(string title, string subtitle, Func<Task> navigateAsync)
    {
        var border = new Border
        {
            Padding = 18,
            Stroke = Colors.LightGray,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(16) },
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    new Label { Text = title, FontSize = 18, FontAttributes = FontAttributes.Bold },
                    new Label { Text = subtitle, LineBreakMode = LineBreakMode.WordWrap, TextColor = Colors.Gray }
                }
            }
        };

        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += async (_, _) => await navigateAsync();
        border.GestureRecognizers.Add(tapGesture);
        return border;
    }

    private View CreatePhotoTestSection()
    {
        return new Border
        {
            Padding = 18,
            Margin = new Thickness(0, 8, 0, 0),
            Stroke = Colors.LightGray,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(16) },
            Content = new VerticalStackLayout
            {
                Spacing = 10,
                Children =
                {
                    new Label { Text = "Foto-Upload testen", FontSize = 18, FontAttributes = FontAttributes.Bold },
                    new Label
                    {
                        Text = "Kleiner Diagnosepfad für den vorhandenen Upload gegen `kgv-upload-photo`. Der normale Produktfluss bleibt unverändert.",
                        TextColor = Colors.Gray,
                        LineBreakMode = LineBreakMode.WordWrap
                    },
                    new HorizontalStackLayout
                    {
                        Spacing = 8,
                        Children = { _capturePhotoButton, _pickPhotoButton }
                    },
                    _selectedFileLabel,
                    _kindPicker,
                    _mediumPicker,
                    _anlageEntry,
                    _gartenEntry,
                    _zaehlernummerEntry,
                    _datumPicker,
                    _uploadButton,
                    _statusLabel,
                    _resultLabel
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_isBusy)
            return;

        try
        {
            await _pendingPhotoSyncService.TrySyncOnceAsync();
        }
        catch
        {
        }
    }

    private async Task CapturePhotoAsync()
    {
        if (!MediaPicker.Default.IsCaptureSupported)
        {
            SetStatus("Fotoaufnahme ist auf diesem Gerät aktuell nicht verfügbar.", success: false);
            return;
        }

        try
        {
            var permissionStatus = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (permissionStatus != PermissionStatus.Granted)
                permissionStatus = await Permissions.RequestAsync<Permissions.Camera>();

            if (permissionStatus != PermissionStatus.Granted)
            {
                SetStatus("Kamera-Berechtigung fehlt. Bitte in den App-Einstellungen aktivieren.", success: false);
                return;
            }

            var file = await MediaPicker.Default.CapturePhotoAsync();

            if (file == null)
            {
                SetStatus("Fotoauswahl abgebrochen.", success: false);
                return;
            }

            await LoadSelectedFileAsync(file);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            SetStatus("Fotoaufnahme fehlgeschlagen.", success: false);
        }
    }

    private async Task PickPhotoAsync()
    {
        try
        {
            var file = await MediaPicker.Default.PickPhotoAsync();

            if (file == null)
            {
                SetStatus("Fotoauswahl abgebrochen.", success: false);
                return;
            }

            await LoadSelectedFileAsync(file);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            SetStatus("Fotoauswahl fehlgeschlagen.", success: false);
        }
    }

    private async Task LoadSelectedFileAsync(FileResult? file)
    {
        if (file == null)
            return;

        await using var stream = await file.OpenReadAsync();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);

        _selectedFileContent = memoryStream.ToArray();
        _selectedFileName = file.FileName;
        _selectedContentType = string.IsNullOrWhiteSpace(file.ContentType)
            ? GetContentType(file.FileName)
            : file.ContentType;

        _selectedFileLabel.Text = $"Ausgewählt: {_selectedFileName}";
        _selectedFileLabel.TextColor = Colors.Black;
        _resultLabel.IsVisible = false;
        _resultLabel.Text = string.Empty;
        SetStatus("Foto bereit zum Upload.", success: true);
        UpdateButtonStates();
    }

    private async Task UploadPhotoAsync()
    {
        if (_isBusy)
            return;

        if (_selectedFileContent == null || _selectedFileContent.Length == 0)
        {
            SetStatus("Bitte zuerst ein Foto aufnehmen oder hinzufügen.", success: false);
            return;
        }

        if (string.IsNullOrWhiteSpace(_anlageEntry.Text) || string.IsNullOrWhiteSpace(_gartenEntry.Text))
        {
            SetStatus("Bitte Anlage und Garten angeben.", success: false);
            return;
        }

        _isBusy = true;
        UpdateButtonStates();
        SetStatus("Foto wird hochgeladen...", success: true);
        _resultLabel.IsVisible = false;
        _resultLabel.Text = string.Empty;

        try
        {
            var result = await _photoUploadTestService.UploadAsync(new PhotoUploadTestRequest
            {
                FileName = _selectedFileName,
                ContentType = _selectedContentType,
                FileContent = _selectedFileContent,
                Kind = _kindPicker.SelectedItem?.ToString() ?? "ablesung",
                Medium = _mediumPicker.SelectedItem?.ToString() ?? "strom",
                Anlage = _anlageEntry.Text?.Trim() ?? string.Empty,
                Garten = _gartenEntry.Text?.Trim() ?? string.Empty,
                Zaehlernummer = string.IsNullOrWhiteSpace(_zaehlernummerEntry.Text) ? null : _zaehlernummerEntry.Text.Trim(),
                Datum = _datumPicker.Date
            });

            SetStatus(result.Success
                ? "Upload erfolgreich abgeschlossen."
                : BuildFailureStatus(result), result.Success);

            _resultLabel.Text = BuildResultText(result);
            _resultLabel.IsVisible = true;
        }
        catch (Exception)
        {
            SetStatus("Upload fehlgeschlagen. Diagnose: UPLOAD_UNHANDLED", success: false);
            _resultLabel.Text = $"Diagnose: UPLOAD_UNHANDLED{Environment.NewLine}Hinweis: Unerwarteter UI-/Aufruferfehler beim Testpfad.";
            _resultLabel.IsVisible = true;
        }
        finally
        {
            _isBusy = false;
            UpdateButtonStates();
        }
    }

    private void UpdateButtonStates()
    {
        var hasPhoto = _selectedFileContent is { Length: > 0 };
        var hasTarget = !string.IsNullOrWhiteSpace(_anlageEntry.Text) && !string.IsNullOrWhiteSpace(_gartenEntry.Text);

        _capturePhotoButton.IsEnabled = !_isBusy && MediaPicker.Default.IsCaptureSupported;
        _pickPhotoButton.IsEnabled = !_isBusy;
        _uploadButton.IsEnabled = !_isBusy && hasPhoto && hasTarget;
        _kindPicker.IsEnabled = !_isBusy;
        _mediumPicker.IsEnabled = !_isBusy;
        _anlageEntry.IsEnabled = !_isBusy;
        _gartenEntry.IsEnabled = !_isBusy;
        _zaehlernummerEntry.IsEnabled = !_isBusy;
        _datumPicker.IsEnabled = !_isBusy;
    }

    private void SetStatus(string text, bool success)
    {
        _statusLabel.Text = text;
        _statusLabel.TextColor = success ? Colors.DarkSlateBlue : Colors.Firebrick;
        _statusLabel.IsVisible = !string.IsNullOrWhiteSpace(text);
    }

    private static string BuildResultText(PhotoUploadTestResult result)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"HTTP: {(result.HttpStatusCode?.ToString() ?? "—")} {result.HttpStatusText}".TrimEnd());
        if (!string.IsNullOrWhiteSpace(result.DiagnosticCode))
            builder.AppendLine($"Diagnose: {result.DiagnosticCode}");
        if (!string.IsNullOrWhiteSpace(result.RequestId))
            builder.AppendLine($"Support-ID: {result.RequestId}");
        builder.AppendLine($"FileId: {DisplayValue(result.FileId)}");
        builder.AppendLine($"Dateiname: {DisplayValue(result.FileName)}");
        builder.AppendLine($"Pfad: {DisplayValue(result.RelativePath)}");

        if (!result.Success)
        {
            var detail = result.RawResponseBody;
            if (!string.IsNullOrWhiteSpace(detail))
            {
                const int maxLen = 300;
                detail = detail.Trim();
                if (detail.Length > maxLen)
                    detail = detail[..maxLen] + "…";

                builder.AppendLine($"Details (gekürzt): {detail}");
            }
        }

        return builder.ToString().Trim();
    }

    private static string DisplayValue(string? value)
        => string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();

    private static string BuildFailureStatus(PhotoUploadTestResult result)
    {
        var message = string.IsNullOrWhiteSpace(result.ErrorSummary)
            ? "Upload fehlgeschlagen."
            : result.ErrorSummary;

        return string.IsNullOrWhiteSpace(result.DiagnosticCode)
            ? message
            : $"{message} Diagnose: {result.DiagnosticCode}";
    }

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
}
