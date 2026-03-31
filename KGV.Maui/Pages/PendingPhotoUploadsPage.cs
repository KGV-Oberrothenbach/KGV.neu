using KGV.Maui.Models;
using KGV.Maui.Services.PendingPhotos;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace KGV.Maui.Pages;

public sealed class PendingPhotoUploadsPage : ContentPage
{
    private readonly PendingPhotoQueue _queue;
    private readonly PendingPhotoSyncService _syncService;
    private readonly PendingPhotoMenuState _menuState;

    private readonly Label _statusLabel;
    private readonly Label _emptyLabel;
    private readonly CollectionView _list;
    private readonly Button _retryAllButton;
    private bool _isBusy;

    public PendingPhotoUploadsPage(PendingPhotoQueue queue, PendingPhotoSyncService syncService, PendingPhotoMenuState menuState)
    {
        _queue = queue;
        _syncService = syncService;
        _menuState = menuState;

        Title = "Foto-Uploads";

        var infoLabel = new Label
        {
            Text = "Offene Foto-Uploads werden lokal gespeichert und bei passendem Netz (ggf. nur WLAN) erneut versucht.",
            TextColor = Colors.Gray,
            LineBreakMode = Microsoft.Maui.LineBreakMode.WordWrap
        };

        _retryAllButton = new Button { Text = "Offene Uploads erneut versuchen" };
        _retryAllButton.Clicked += async (_, _) => await RetryAllAsync();

        _statusLabel = new Label
        {
            TextColor = Colors.DarkSlateBlue,
            LineBreakMode = Microsoft.Maui.LineBreakMode.WordWrap,
            IsVisible = false
        };

        _emptyLabel = new Label
        {
            Text = "Keine offenen Foto-Uploads.",
            TextColor = Colors.Gray,
            LineBreakMode = Microsoft.Maui.LineBreakMode.WordWrap,
            IsVisible = false
        };

        _list = new CollectionView
        {
            ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical) { ItemSpacing = 10 },
            ItemTemplate = new DataTemplate(() => CreateItemTemplate())
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 12,
                Children =
                {
                    new Label { Text = "Foto-Uploads", FontSize = 24, FontAttributes = FontAttributes.Bold },
                    infoLabel,
                    _retryAllButton,
                    _statusLabel,
                    _emptyLabel,
                    _list
                }
            }
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshList();
    }

    private async Task RetryAllAsync()
    {
        if (_isBusy)
            return;

        _isBusy = true;
        try
        {
            _statusLabel.IsVisible = true;
            _statusLabel.Text = "Uploads werden erneut versucht...";

            var result = await _syncService.TrySyncOnceAsync();

            if (!string.IsNullOrWhiteSpace(result.SkippedReason))
            {
                _statusLabel.Text = result.SkippedReason;
            }
            else
            {
                _statusLabel.Text = result.Uploaded > 0
                    ? $"Uploads abgeschlossen: {result.Uploaded} erfolgreich, {result.Failed} fehlgeschlagen."
                    : result.Failed > 0
                        ? "Kein Upload erfolgreich. Erneuter Versuch später möglich."
                        : "Keine offenen Uploads vorhanden.";
            }

            RefreshList();
        }
        catch
        {
            _statusLabel.IsVisible = true;
            _statusLabel.Text = "Upload-Versuch konnte nicht gestartet werden.";
        }
        finally
        {
            _isBusy = false;
        }
    }

    private void RefreshList()
    {
        var items = _queue
            .GetAll()
            .Where(x => x.Status is PendingPhotoUploadStatus.Pending or PendingPhotoUploadStatus.Failed or PendingPhotoUploadStatus.Uploading)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToList();

        _list.ItemsSource = items;
        _menuState.Refresh();

        _retryAllButton.IsEnabled = !_isBusy && items.Count > 0;
        _emptyLabel.IsVisible = items.Count == 0;

        if (Shell.Current is AdminShell adminShell)
            adminShell.RefreshPendingPhotoUploadsMenu();
    }

    private static View CreateItemTemplate()
    {
        var fileLabel = new Label { FontAttributes = FontAttributes.Bold, LineBreakMode = Microsoft.Maui.LineBreakMode.TailTruncation };
        fileLabel.SetBinding(Label.TextProperty, nameof(PendingPhotoUpload.FileName));

        var metaLabel = new Label { TextColor = Colors.Gray, LineBreakMode = Microsoft.Maui.LineBreakMode.WordWrap, FontSize = 12 };
        metaLabel.SetBinding(Label.TextProperty, new Binding(path: ".", converter: new PendingPhotoMetaConverter()));

        var statusLabel = new Label { LineBreakMode = Microsoft.Maui.LineBreakMode.WordWrap, FontSize = 12 };
        statusLabel.SetBinding(Label.TextProperty, new Binding(path: ".", converter: new PendingPhotoStatusConverter()));

        return new Border
        {
            Stroke = Colors.LightGray,
            StrokeThickness = 1,
            Padding = 14,
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                Children = { fileLabel, metaLabel, statusLabel }
            }
        };
    }

    private sealed class PendingPhotoMetaConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            if (value is not PendingPhotoUpload item)
                return string.Empty;

            var created = item.CreatedAtUtc.ToLocalTime().ToString("g", culture);
            return $"{item.Parzelle} · {item.Medium} · {created}";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
            => throw new NotSupportedException();
    }

    private sealed class PendingPhotoStatusConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            if (value is not PendingPhotoUpload item)
                return string.Empty;

            return item.Status switch
            {
                PendingPhotoUploadStatus.Pending => "Lokal gespeichert, Upload ausstehend",
                PendingPhotoUploadStatus.Uploading => "Wird hochgeladen...",
                PendingPhotoUploadStatus.Failed => string.IsNullOrWhiteSpace(item.LastError)
                    ? "Upload fehlgeschlagen, erneuter Versuch möglich"
                    : $"Upload fehlgeschlagen: {Shorten(item.LastError)}",
                _ => item.Status.ToString()
            };
        }

        private static string Shorten(string text)
        {
            var normalized = (text ?? string.Empty).Trim();
            const int maxLen = 160;
            return normalized.Length > maxLen ? normalized[..maxLen] + "…" : normalized;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
            => throw new NotSupportedException();
    }
}
