using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Maui.Models;
using KGV.Maui.Services.PendingPhotos;
using KGV.Maui.State;
using KGV.Maui.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Media;
using Microsoft.Maui.ApplicationModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using KGV.Maui.Settings;
using System;
using System.Collections.Generic;

namespace KGV.Maui.Pages;

public sealed class AblesungErfassenPage : ContentPage, IQueryAttributable
{
    private readonly ISupabaseService _supabaseService;
    private readonly IPhotoUploadTestService _photoUploadService;
    private readonly ZaehlerwechselWorkflowState _workflowState;
    private readonly PendingPhotoService _pendingPhotoService;
    private readonly RfidScanContextViewModel _scanContext;
    private readonly Label _introLabel;
    private readonly Label _flowHintLabel;
    private readonly Label _contextLabel;
    private readonly Label _decisionLabel;
    private readonly Label _statusLabel;
    private readonly Label _photoLabel;
    private readonly Border _scanSection;
    private readonly View _fallbackSection;
    private readonly Border _contextSection;
    private readonly Border _decisionSection;
    private readonly Border _formSection;
    private readonly DatePicker _ablesedatumPicker;
    private readonly Entry _standEntry;
    private readonly Button _capturePhotoButton;
    private readonly Button _pickPhotoButton;
    private readonly Button _clearPhotoButton;
    private readonly Button _saveButton;
    private readonly Button _resetButton;
    private bool _initialized;
    private bool _isBusy;
    private bool _isPendingInitialFlow;
    private bool _hasRequestedFallbackContext;
    private string _currentArt = AblesungArt.Normal;
    private int? _requestedParzelleId;
    private string _requestedMedium = "strom";
    private RfidScanContextResult? _activeResolution;
    private byte[]? _selectedPhotoContent;
    private string _selectedPhotoFileName = string.Empty;
    private string _selectedPhotoContentType = "application/octet-stream";

    public AblesungErfassenPage()
    {
        var services = Application.Current?.Handler?.MauiContext?.Services
            ?? throw new InvalidOperationException("MAUI-Services sind aktuell nicht verfügbar.");

        _supabaseService = services.GetRequiredService<ISupabaseService>();
        _photoUploadService = services.GetRequiredService<IPhotoUploadTestService>();
        _workflowState = services.GetRequiredService<ZaehlerwechselWorkflowState>();
        _pendingPhotoService = services.GetRequiredService<PendingPhotoService>();
        _scanContext = new RfidScanContextViewModel(
            _supabaseService,
            services.GetRequiredService<IAuthService>(),
            services.GetRequiredService<KGV.Maui.Services.INfcScanService>(),
            services.GetRequiredService<KGV.Maui.Services.IRfidFeedbackService>());
        _scanContext.PropertyChanged += OnScanContextPropertyChanged;

        BindingContext = _scanContext;
        Title = "Ablesung erfassen";

        _introLabel = new Label
        {
            Text = "RFID-Tag scannen oder den fachlichen Ersatzweg über Parzelle und Medium nutzen. Normale Ablesungen speichern mit `Art = normal`, Anfangsablesungen nach Einbau mit `Art = einbau`.",
            LineBreakMode = LineBreakMode.WordWrap
        };
        _flowHintLabel = new Label { TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
        _contextLabel = new Label { LineBreakMode = LineBreakMode.WordWrap };
        _decisionLabel = new Label { LineBreakMode = LineBreakMode.WordWrap };
        _statusLabel = new Label { TextColor = Colors.DarkSlateBlue, LineBreakMode = LineBreakMode.WordWrap };
        _photoLabel = new Label { Text = "Noch kein Foto gewählt.", LineBreakMode = LineBreakMode.WordWrap, TextColor = Colors.Gray };
        _ablesedatumPicker = new DatePicker { Date = DateTime.Today };
        _standEntry = new Entry { Placeholder = "Zählerstand", Keyboard = Keyboard.Numeric };

        _capturePhotoButton = new Button { Text = "Foto aufnehmen" };
        _capturePhotoButton.Clicked += async (_, _) => await SelectPhotoAsync(capture: true);

        _pickPhotoButton = new Button { Text = "Foto übernehmen" };
        _pickPhotoButton.Clicked += async (_, _) => await SelectPhotoAsync(capture: false);

        _clearPhotoButton = new Button { Text = "Foto entfernen" };
        _clearPhotoButton.Clicked += (_, _) => ClearPhotoSelection();

        _saveButton = new Button { Text = "Ablesung speichern" };
        _saveButton.Clicked += OnSaveClicked;

        _resetButton = new Button { Text = "Anderen Tag scannen" };
        _resetButton.Clicked += async (_, _) => await ResetAndRestartScanAsync(clearWorkflow: true);

        var nfcStatusTitleLabel = new Label { FontAttributes = FontAttributes.Bold, LineBreakMode = LineBreakMode.WordWrap };
        nfcStatusTitleLabel.SetBinding(Label.TextProperty, nameof(RfidScanContextViewModel.NfcStatusTitle));

        var nfcStatusMessageLabel = new Label { LineBreakMode = LineBreakMode.WordWrap };
        nfcStatusMessageLabel.SetBinding(Label.TextProperty, nameof(RfidScanContextViewModel.NfcStatusMessage));

        var startScanButton = new Button { Text = "Scan aktivieren" };
        startScanButton.SetBinding(IsEnabledProperty, nameof(RfidScanContextViewModel.CanStartNfcScan));
        startScanButton.Clicked += async (_, _) => await _scanContext.StartNfcSessionAsync();

        var openNfcSettingsButton = new Button { Text = "NFC-Einstellungen öffnen" };
        openNfcSettingsButton.SetBinding(IsVisibleProperty, nameof(RfidScanContextViewModel.CanOpenNfcSettings));
        openNfcSettingsButton.Clicked += async (_, _) => await _scanContext.OpenNfcSettingsAsync();

        _scanSection = CreateSection(
            "RFID-Scan",
            nfcStatusTitleLabel,
            nfcStatusMessageLabel,
            new HorizontalStackLayout
            {
                Spacing = 8,
                Children = { startScanButton, openNfcSettingsButton, _resetButton }
            });

        _fallbackSection = CreateFallbackSection();

        _contextSection = CreateSection("Ablesekontext", _contextLabel);
        _contextSection.IsVisible = false;

        _decisionSection = CreateSection("Einordnung", _decisionLabel);
        _decisionSection.IsVisible = false;

        _formSection = CreateSection(
            "Ablesung",
            _flowHintLabel,
            CreateField("Ablesedatum", _ablesedatumPicker),
            CreateField("Zählerstand", _standEntry),
            CreateField(
                "Foto",
                new VerticalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        new HorizontalStackLayout
                        {
                            Spacing = 8,
                            Children = { _capturePhotoButton, _pickPhotoButton, _clearPhotoButton }
                        },
                        _photoLabel
                    }
                }),
            _statusLabel,
            _saveButton);
        _formSection.IsVisible = false;

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 12,
                Children =
                {
                    new Label { Text = "Ablesung erfassen", FontSize = 24, FontAttributes = FontAttributes.Bold },
                    _introLabel,
                    _scanSection,
                    _fallbackSection,
                    _contextSection,
                    _decisionSection,
                    _formSection,
                    CreateNavigationSection()
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!_initialized)
        {
            await _scanContext.InitializeAsync();
            _initialized = true;
        }
        else
        {
            await _scanContext.RefreshNfcAvailabilityAsync();
        }

        var pendingFlow = _workflowState.ConsumePendingAblesungFlow();
        if (pendingFlow?.Context?.Context?.AktiverZaehlerId is > 0)
        {
            _isPendingInitialFlow = true;
            _currentArt = AblesungArt.Normalize(pendingFlow.Art);
            _ablesedatumPicker.Date = pendingFlow.DefaultDate.Date;
            _scanContext.ApplyResolvedContext(pendingFlow.Context, pendingFlow.Hint);
            ApplyResolution(pendingFlow.Context, pendingFlow.Hint);
            return;
        }

        if (_hasRequestedFallbackContext && _requestedParzelleId is > 0)
        {
            _hasRequestedFallbackContext = false;
            _workflowState.Clear();
            _isPendingInitialFlow = false;
            _currentArt = AblesungArt.Normal;
            _ablesedatumPicker.Date = DateTime.Today;
            ClearPhotoSelection();
            await _scanContext.LoadFallbackContextAsync(_requestedParzelleId.Value, _requestedMedium);
            ApplyResolution(_scanContext.Resolution, _scanContext.StatusMessage);
            return;
        }

        _isPendingInitialFlow = false;
        _currentArt = AblesungArt.Normal;
        await _scanContext.StartNfcSessionAsync();
        ApplyResolution(_scanContext.Resolution, _scanContext.StatusMessage);
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _requestedParzelleId = TryGetQueryInt(query, "parzelleId");
        _requestedMedium = TryGetQueryString(query, "medium") ?? "strom";
        _hasRequestedFallbackContext = _requestedParzelleId is > 0;
    }

    protected override async void OnDisappearing()
    {
        await _scanContext.StopNfcSessionAsync();
        base.OnDisappearing();
    }

    private void OnScanContextPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(RfidScanContextViewModel.Resolution) or nameof(RfidScanContextViewModel.StatusMessage))
            ApplyResolution(_scanContext.Resolution, _scanContext.StatusMessage);
    }

    private void ApplyResolution(RfidScanContextResult? resolution, string? statusMessage)
    {
        _activeResolution = resolution;
        _statusLabel.Text = statusMessage ?? string.Empty;

        var hasActiveReadingContext = resolution?.Context?.AktiverZaehlerId is > 0
            && resolution.State == RfidScanContextState.KnownWithActiveMeter;

        _introLabel.IsVisible = !hasActiveReadingContext;
        _scanSection.IsVisible = !hasActiveReadingContext;
        _fallbackSection.IsVisible = !hasActiveReadingContext;

        if (resolution?.Context == null)
        {
            _contextSection.IsVisible = false;
            _decisionSection.IsVisible = false;
            _formSection.IsVisible = false;
            _decisionLabel.Text = "Noch kein RFID-Kontext geladen.";
            return;
        }

        var context = resolution.Context;
        _contextLabel.Text = $"Parzelle: {context.ParzelleDisplayName}{Environment.NewLine}" +
                             $"Medium: {context.MediumDisplay}{Environment.NewLine}" +
                             $"RFID: {context.RfidDisplay}{Environment.NewLine}" +
                             $"Zählernummer: {context.ZaehlernummerDisplay}{Environment.NewLine}" +
                             $"Eichjahr: {(context.Eichdatum?.Year.ToString(CultureInfo.InvariantCulture) ?? "—")}";
        _contextSection.IsVisible = true;
        _decisionSection.IsVisible = true;

        if (resolution.State == RfidScanContextState.KnownWithActiveMeter && context.AktiverZaehlerId is > 0)
        {
            _flowHintLabel.Text = _currentArt == AblesungArt.Einbau
                ? "Bitte Anfangsstand und Foto erfassen. Der Zähler selbst wurde bereits angelegt; jetzt folgt separat die Anfangsablesung."
                : "Bitte aktuelle Ablesung und Foto erfassen. Normale Ablesungen werden mit `Art = normal` gespeichert.";
            _decisionSection.IsVisible = false;
            _formSection.IsVisible = true;
            return;
        }

        _decisionLabel.Text = resolution.State == RfidScanContextState.KnownWithoutActiveMeter
            ? "Der Tag ist bekannt, aktuell aber ohne aktiven Zähler. Eine normale Ablesung ist damit nicht möglich."
            : "Der Tag ist unbekannt. Für die Ablesung kann kein produktiver Kontext vorbereitet werden.";
        _formSection.IsVisible = false;
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

            if (capture)
            {
                var permissionStatus = await Permissions.CheckStatusAsync<Permissions.Camera>();
                if (permissionStatus != PermissionStatus.Granted)
                    permissionStatus = await Permissions.RequestAsync<Permissions.Camera>();

                if (permissionStatus != PermissionStatus.Granted)
                {
                    _photoLabel.Text = "Kamera-Berechtigung wurde nicht erteilt.";
                    return;
                }
            }

            var fileResult = capture
                ? await MediaPicker.Default.CapturePhotoAsync()
                : await MediaPicker.Default.PickPhotoAsync();

            if (fileResult == null)
            {
                _photoLabel.Text = "Fotoauswahl abgebrochen.";
                return;
            }

            await using var stream = await fileResult.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);

            _selectedPhotoContent = memoryStream.ToArray();
            _selectedPhotoFileName = string.IsNullOrWhiteSpace(fileResult.FileName) ? $"ablesung-{DateTime.Now:yyyyMMdd-HHmmss}.jpg" : fileResult.FileName;
            _selectedPhotoContentType = string.IsNullOrWhiteSpace(fileResult.ContentType)
                ? GetContentType(_selectedPhotoFileName)
                : fileResult.ContentType;
            _photoLabel.Text = $"Foto gewählt: {_selectedPhotoFileName}";
            _statusLabel.Text = string.Empty;
        }
        catch (Exception ex)
        {
            logErrorForUser("Fotoaufnahme fehlgeschlagen", ex);
        }
    }

    private void logErrorForUser(string title, Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"[AblesungErfassenPage] {title}: {ex}");
        _photoLabel.Text = title;
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

        if (_activeResolution?.Context?.AktiverZaehlerId is not > 0)
        {
            await DisplayAlert("Hinweis", "Es liegt kein aktiver Ablese-Kontext vor.", "OK");
            return;
        }

        if (!TryParseDecimal(_standEntry.Text, out var stand) || stand < 0)
        {
            await DisplayAlert("Validierung", "Bitte einen gültigen Zählerstand eingeben.", "OK");
            _standEntry.Focus();
            return;
        }

        if (_selectedPhotoContent == null || _selectedPhotoContent.Length == 0)
        {
            await DisplayAlert("Validierung", "Bitte zuerst ein Foto aufnehmen oder übernehmen.", "OK");
            return;
        }

        _isBusy = true;
        UpdateBusyState();

        try
        {
            var context = _activeResolution.Context;

            var operationType = MapPhotoKind(_currentArt);
            var pending = _pendingPhotoService.SaveAndEnqueue(
                _selectedPhotoContent ?? Array.Empty<byte>(),
                operationType,
                context.ParzelleDisplayName,
                NormalizeMedium(context.Medium),
                _selectedPhotoContentType);

            byte[]? pendingContent = null;
            if (!_pendingPhotoService.TryLoadContent(pending, out pendingContent) || pendingContent is not { Length: > 0 })
            {
                _pendingPhotoService.MarkFailed(pending, "PENDING_FILE_READ_FAIL");
                _statusLabel.Text = "Das Foto konnte lokal nicht vorbereitet werden.";
                return;
            }

            if (!PendingPhotoUploadDecision.CanUploadNow(out _))
            {
                _statusLabel.Text = PhotoUploadPreferences.WifiOnly
                    ? "Foto wurde lokal gespeichert und wird automatisch bei WLAN hochgeladen."
                    : "Foto wurde lokal gespeichert und wird automatisch hochgeladen, sobald wieder Internet verfügbar ist.";
                return;
            }

            var photoResult = await _photoUploadService.UploadAsync(new PhotoUploadTestRequest
            {
                FileName = pending.FileName,
                ContentType = pending.ContentType,
                FileContent = pendingContent,
                Kind = operationType,
                Medium = NormalizeMedium(context.Medium),
                Anlage = context.Anlage?.Trim() ?? string.Empty,
                Garten = context.GartenNr?.Trim() ?? string.Empty,
                Zaehlernummer = string.IsNullOrWhiteSpace(context.Zaehlernummer) ? null : context.Zaehlernummer.Trim(),
                Datum = _ablesedatumPicker.Date
            });

            if (!photoResult.Success || string.IsNullOrWhiteSpace(photoResult.RelativePath))
            {
                _pendingPhotoService.MarkFailed(pending, photoResult.DiagnosticCode ?? "UPLOAD_FAILED");
                var message = string.IsNullOrWhiteSpace(photoResult.ErrorSummary)
                    ? "Das Foto konnte nicht hochgeladen werden."
                    : photoResult.ErrorSummary;

                if (!string.IsNullOrWhiteSpace(photoResult.RequestId))
                    message = $"{message}{Environment.NewLine}Support-ID: {photoResult.RequestId}";

                _statusLabel.Text = message;
                return;
            }

            _pendingPhotoService.MarkUploadedAndDeleteLocal(pending);

            var readingSaved = await _supabaseService.AddAblesungAsync(new AblesungInsertRecord
            {
                ZaehlerId = context.AktiverZaehlerId.Value,
                Ablesedatum = _ablesedatumPicker.Date,
                Stand = stand,
                Art = _currentArt,
                FotoPfad = photoResult.RelativePath,
                Freigegeben = true
            });

            if (!readingSaved)
            {
                _statusLabel.Text = "Die Ablesung konnte nicht gespeichert werden.";
                return;
            }

            await DisplayAlert("OK", _currentArt == AblesungArt.Einbau ? "Anfangsablesung gespeichert." : "Ablesung gespeichert.", "OK");

            if (_isPendingInitialFlow)
            {
                _workflowState.Clear();
                await Shell.Current.GoToAsync("//ablesen");
                return;
            }

            await ResetAndRestartScanAsync(clearWorkflow: false);
            _statusLabel.Text = "Ablesung gespeichert. Der nächste RFID-Scan kann beginnen.";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            _statusLabel.Text = "Unerwarteter Fehler beim Speichern.";
        }
        finally
        {
            _isBusy = false;
            UpdateBusyState();
        }
    }

    private async Task ResetAndRestartScanAsync(bool clearWorkflow)
    {
        if (clearWorkflow)
            _workflowState.Clear();

        _isPendingInitialFlow = false;
        _currentArt = AblesungArt.Normal;
        _activeResolution = null;
        _ablesedatumPicker.Date = DateTime.Today;
        _standEntry.Text = string.Empty;
        ClearPhotoSelection();
        _scanContext.Reset();
        ApplyResolution(null, string.Empty);
        await _scanContext.StartNfcSessionAsync();
    }

    private void UpdateBusyState()
    {
        _ablesedatumPicker.IsEnabled = !_isBusy;
        _standEntry.IsEnabled = !_isBusy;
        _capturePhotoButton.IsEnabled = !_isBusy && MediaPicker.Default.IsCaptureSupported;
        _pickPhotoButton.IsEnabled = !_isBusy;
        _clearPhotoButton.IsEnabled = !_isBusy;
        _saveButton.IsEnabled = !_isBusy;
        _resetButton.IsEnabled = !_isBusy;
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

        return CreateSection(
            "Fallback ohne NFC",
            new Label
            {
                Text = "Wenn NFC nicht verfügbar ist, kann der fachliche Ablese-Kontext über Parzelle und Medium geladen werden.",
                LineBreakMode = LineBreakMode.WordWrap,
                TextColor = Colors.Gray
            },
            parzellePicker,
            mediumPicker,
            fallbackButton);
    }

    private View CreateNavigationSection()
    {
        var backToOverviewButton = new Button { Text = "Zur Ablesen-Übersicht" };
        backToOverviewButton.Clicked += async (_, _) =>
        {
            _workflowState.Clear();
            await Shell.Current.GoToAsync("//ablesen");
        };

        return new VerticalStackLayout
        {
            Spacing = 8,
            Children = { backToOverviewButton }
        };
    }

    private static string MapPhotoKind(string art)
    {
        return AblesungArt.Normalize(art) switch
        {
            AblesungArt.Einbau => "einbau",
            AblesungArt.Ausbau => "ausbau",
            _ => "ablesung"
        };
    }

    private static string NormalizeMedium(string? medium)
        => string.Equals(medium, "wasser", StringComparison.OrdinalIgnoreCase) ? "wasser" : "strom";

    private static int? TryGetQueryInt(IDictionary<string, object> query, string key)
    {
        var raw = TryGetQueryString(query, key);
        return int.TryParse(raw, out var value) && value > 0 ? value : null;
    }

    private static string? TryGetQueryString(IDictionary<string, object> query, string key)
    {
        if (!query.TryGetValue(key, out var raw) || raw == null)
            return null;

        var value = raw.ToString();
        return string.IsNullOrWhiteSpace(value) ? null : Uri.UnescapeDataString(value);
    }

    private static bool TryParseDecimal(string? value, out decimal result)
        => decimal.TryParse((value ?? string.Empty).Trim().Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out result);

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
