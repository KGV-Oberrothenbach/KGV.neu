using KGV.Maui.Models;
using KGV.Maui.Services.PendingPhotos;
using KGV.Core.Interfaces;
using KGV.Maui.State;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace KGV.Maui.Pages;

public sealed class PendingPhotoUploadsPage : ContentPage
{
    private readonly PendingPhotoQueue _queue;
    private readonly PendingPhotoService _pendingPhotoService;
    private readonly PendingPhotoSyncService _syncService;
    private readonly PendingPhotoMenuState _menuState;
    private readonly IVereinskontext _vereinskontext;
    private readonly UserContextState _userContextState;

    private readonly Label _statusLabel;
    private readonly Label _emptyLabel;
    private readonly CollectionView _list;
    private readonly Button _retryAllButton;
    private readonly Button _deleteLegacyButton;
    private bool _isBusy;

    public PendingPhotoUploadsPage(PendingPhotoQueue queue, PendingPhotoService pendingPhotoService, PendingPhotoSyncService syncService, PendingPhotoMenuState menuState, IVereinskontext vereinskontext, UserContextState userContextState)
    {
        _queue = queue;
        _pendingPhotoService = pendingPhotoService;
        _syncService = syncService;
        _menuState = menuState;
        _vereinskontext = vereinskontext;
        _userContextState = userContextState;

        Title = "Foto-Uploads";

        var infoLabel = new Label
        {
            Text = "Offene Foto-Uploads werden lokal gespeichert und bei passendem Netz (ggf. nur WLAN) erneut versucht.",
            TextColor = Colors.Gray,
            LineBreakMode = Microsoft.Maui.LineBreakMode.WordWrap
        };

        _retryAllButton = new Button { Text = "Offene Uploads erneut versuchen" };
        _retryAllButton.Clicked += async (_, _) => await RetryAllAsync();

        _deleteLegacyButton = new Button { Text = "Nicht zuordenbare Alt-Daten löschen", IsVisible = false };
        _deleteLegacyButton.Clicked += async (_, _) => await DeleteLegacyAsync();

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
                    _deleteLegacyButton,
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
        var vereinId = _vereinskontext.Aktuell?.VereinId;
        var ownerUserId = _userContextState.CurrentUserId;
        var items = vereinId.HasValue && ownerUserId.HasValue
            ? _queue.GetOpenForOwner(vereinId.Value, ownerUserId.Value).OrderByDescending(x => x.CreatedAtUtc).ToList()
            : new List<PendingPhotoUpload>();

        _list.ItemsSource = items;
        _menuState.Refresh();

        _retryAllButton.IsEnabled = !_isBusy && items.Count > 0;
        _emptyLabel.IsVisible = items.Count == 0;
        var legacyCount = vereinId.HasValue ? _queue.CountLegacyForVerein(vereinId.Value) : 0;
        _deleteLegacyButton.IsVisible = legacyCount > 0;
        _deleteLegacyButton.IsEnabled = !_isBusy && legacyCount > 0;
        _deleteLegacyButton.Text = legacyCount == 1
            ? "1 nicht zuordenbaren Alt-Eintrag löschen"
            : $"{legacyCount} nicht zuordenbare Alt-Einträge löschen";

        if (Shell.Current is AdminShell adminShell)
            adminShell.RefreshPendingPhotoUploadsMenu();
    }

    private View CreateItemTemplate()
    {
        var fileLabel = new Label { FontAttributes = FontAttributes.Bold, LineBreakMode = Microsoft.Maui.LineBreakMode.TailTruncation };
        fileLabel.SetBinding(Label.TextProperty, nameof(PendingPhotoUpload.FileName));

        var metaLabel = new Label { TextColor = Colors.Gray, LineBreakMode = Microsoft.Maui.LineBreakMode.WordWrap, FontSize = 12 };
        metaLabel.SetBinding(Label.TextProperty, new Binding(path: ".", converter: new PendingPhotoMetaConverter()));

        var statusLabel = new Label { LineBreakMode = Microsoft.Maui.LineBreakMode.WordWrap, FontSize = 12 };
        statusLabel.SetBinding(Label.TextProperty, new Binding(path: ".", converter: new PendingPhotoStatusConverter()));

        var deleteButton = new Button { Text = "Löschen", HorizontalOptions = LayoutOptions.Start };
        deleteButton.Clicked += async (sender, _) =>
        {
            if (sender is Button { BindingContext: PendingPhotoUpload item })
                await DeleteItemAsync(item);
        };

        return new Border
        {
            Stroke = Colors.LightGray,
            StrokeThickness = 1,
            Padding = 14,
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                Children = { fileLabel, metaLabel, statusLabel, deleteButton }
            }
        };
    }

    private async Task DeleteItemAsync(PendingPhotoUpload item)
    {
        if (_isBusy)
            return;

        var vereinId = _vereinskontext.Aktuell?.VereinId;
        var ownerUserId = _userContextState.CurrentUserId;
        if (!vereinId.HasValue || !ownerUserId.HasValue)
            return;

        var confirmed = await DisplayAlertAsync(
            "Lokales Foto löschen",
            "Das Foto wird endgültig aus der lokalen Warteschlange und vom Gerät gelöscht. Ein automatischer Upload ist danach nicht mehr möglich.",
            "Endgültig löschen",
            "Abbrechen");
        if (!confirmed)
            return;

        if (!_syncService.TryReserveForDeletion(item.Id))
        {
            _statusLabel.IsVisible = true;
            _statusLabel.Text = "Dieses Foto wird gerade hochgeladen und kann erst danach gelöscht werden.";
            return;
        }

        _isBusy = true;
        try
        {
            var result = _pendingPhotoService.DeleteOwnedPendingPhoto(item.Id, vereinId.Value, ownerUserId.Value);
            _statusLabel.IsVisible = true;
            _statusLabel.Text = result == PendingPhotoDeleteResult.Deleted
                ? "Lokales Foto wurde endgültig gelöscht."
                : "Das Foto gehört nicht zum aktuellen Benutzer oder ist nicht mehr vorhanden.";
            RefreshList();
        }
        finally
        {
            _syncService.ReleaseDeletionReservation(item.Id);
            _isBusy = false;
        }
    }

    private async Task DeleteLegacyAsync()
    {
        if (_isBusy)
            return;

        var vereinId = _vereinskontext.Aktuell?.VereinId;
        if (!vereinId.HasValue)
            return;

        var confirmed = await DisplayAlertAsync(
            "Alt-Daten löschen",
            "Nicht zuordenbare Alt-Einträge werden endgültig aus der lokalen Warteschlange und vom Gerät gelöscht. Sie werden keinem Benutzer zugeordnet und nicht hochgeladen.",
            "Endgültig löschen",
            "Abbrechen");
        if (!confirmed)
            return;

        _isBusy = true;
        try
        {
            var removed = _pendingPhotoService.DeleteLegacyPendingPhotos(vereinId.Value);
            _statusLabel.IsVisible = true;
            _statusLabel.Text = $"{removed} Alt-Einträge wurden endgültig gelöscht.";
            RefreshList();
        }
        finally
        {
            _isBusy = false;
        }
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
