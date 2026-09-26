using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage;

#if ANDROID
using Android.Graphics;
using Android.Graphics.Pdf;
using Android.OS;
#endif

namespace KGV.Maui.Pages;

/// <summary>
/// Zeigt ein bereits autorisiert geladenes PDF an. Die Datei wird ausschließlich
/// im App-Cache abgelegt und beim Verlassen der Seite wieder entfernt.
/// </summary>
public sealed class PdfViewerPage : ContentPage
{
    private readonly string _cachePath;
    private readonly Image _pageImage;
    private readonly Label _pageLabel;
    private readonly Button _previousButton;
    private readonly Button _nextButton;
    private readonly Button _zoomOutButton;
    private readonly Button _zoomInButton;
    private readonly Button _resetZoomButton;
    private int _currentPageIndex;
    private int _pageCount;
    private double _zoomStartScale = 1d;
    private double _zoomScale = 1d;
    private double _panStartX;
    private double _panStartY;

#if ANDROID
    private PdfRenderer? _renderer;
    private ParcelFileDescriptor? _fileDescriptor;
#endif

    public PdfViewerPage(string? title, byte[] pdfContent)
    {
        if (pdfContent is not { Length: > 0 })
            throw new ArgumentException("Für die PDF-Ansicht fehlt ein Dokumentinhalt.", nameof(pdfContent));

        if (pdfContent.Length < 5
            || pdfContent[0] != (byte)'%'
            || pdfContent[1] != (byte)'P'
            || pdfContent[2] != (byte)'D'
            || pdfContent[3] != (byte)'F'
            || pdfContent[4] != (byte)'-')
        {
            throw new ArgumentException("Der Server hat keine gültige PDF-Datei geliefert.", nameof(pdfContent));
        }

        Title = string.IsNullOrWhiteSpace(title) ? "Dokument" : title.Trim();
        _cachePath = System.IO.Path.Combine(FileSystem.CacheDirectory, $"kgv-pdf-{Guid.NewGuid():N}.pdf");
        File.WriteAllBytes(_cachePath, pdfContent);

        _pageImage = new Image
        {
            Aspect = Aspect.AspectFit,
            BackgroundColor = Colors.White,
        };
        _pageLabel = new Label
        {
            HorizontalTextAlignment = TextAlignment.Center,
            TextColor = Colors.Gray,
        };
        _previousButton = new Button { Text = "‹ Zurück" };
        _nextButton = new Button { Text = "Weiter ›" };
        _zoomOutButton = new Button { Text = "−", WidthRequest = 48 };
        _zoomInButton = new Button { Text = "+", WidthRequest = 48 };
        _resetZoomButton = new Button { Text = "Ansicht zurücksetzen", IsVisible = false };
        _previousButton.Clicked += async (_, _) => await ShowPageAsync(_currentPageIndex - 1);
        _nextButton.Clicked += async (_, _) => await ShowPageAsync(_currentPageIndex + 1);
        _zoomOutButton.Clicked += (_, _) => ChangeZoom(-0.5d);
        _zoomInButton.Clicked += (_, _) => ChangeZoom(0.5d);
        _resetZoomButton.Clicked += (_, _) => ResetZoom();

        var pinch = new PinchGestureRecognizer();
        pinch.PinchUpdated += OnPinchUpdated;
        var pan = new PanGestureRecognizer();
        pan.PanUpdated += OnPanUpdated;

        var navigationBar = new Grid
        {
            ColumnSpacing = 12,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
            },
            HorizontalOptions = LayoutOptions.Fill,
        };
        _previousButton.HorizontalOptions = LayoutOptions.Start;
        _nextButton.HorizontalOptions = LayoutOptions.End;
        _pageLabel.VerticalTextAlignment = TextAlignment.Center;
        navigationBar.Add(_previousButton, 0, 0);
        navigationBar.Add(_pageLabel, 1, 0);
        navigationBar.Add(_zoomOutButton, 2, 0);
        navigationBar.Add(_zoomInButton, 3, 0);
        navigationBar.Add(_nextButton, 4, 0);
        Grid.SetRow(navigationBar, 1);

        var documentViewport = new Grid
        {
            IsClippedToBounds = true,
            BackgroundColor = Colors.White,
            Children = { _pageImage },
        };
        documentViewport.GestureRecognizers.Add(pinch);
        documentViewport.GestureRecognizers.Add(pan);

        Content = new Grid
        {
            Padding = 12,
            RowDefinitions =
            {
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
            },
            Children =
            {
                documentViewport,
                navigationBar,
                _resetZoomButton,
            },
        };
        Grid.SetRow(_resetZoomButton, 2);
        _resetZoomButton.HorizontalOptions = LayoutOptions.Center;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await EnsureRendererAndShowPageAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PdfViewerPage] PDF render failed: {ex}");
            _pageLabel.Text = "PDF konnte nicht angezeigt werden.";
            await DisplayAlertAsync("PDF-Ansicht", ex.Message, "OK");
        }
    }

    protected override void OnDisappearing()
    {
        DisposeRenderer();
        base.OnDisappearing();
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        if (args.NavigationType is NavigationType.Pop or NavigationType.Remove)
        {
            try
            {
                if (File.Exists(_cachePath))
                    File.Delete(_cachePath);
            }
            catch
            {
                // Der Cache wird spätestens durch das Betriebssystem bereinigt.
            }
        }

        base.OnNavigatedFrom(args);
    }

    private async Task EnsureRendererAndShowPageAsync()
    {
#if ANDROID
        if (_renderer == null)
        {
            var fileDescriptor = ParcelFileDescriptor.Open(new Java.IO.File(_cachePath), ParcelFileMode.ReadOnly)
                ?? throw new InvalidOperationException("Die temporäre PDF-Datei konnte nicht geöffnet werden.");
            _fileDescriptor = fileDescriptor;
            _renderer = new PdfRenderer(fileDescriptor);
            _pageCount = _renderer.PageCount;
        }

        if (_pageCount <= 0)
            throw new InvalidOperationException("Das PDF enthält keine Seiten.");

        await ShowPageAsync(Math.Clamp(_currentPageIndex, 0, _pageCount - 1));
#else
        throw new PlatformNotSupportedException("Die interne PDF-Ansicht ist derzeit für Android verfügbar.");
#endif
    }

    private async Task ShowPageAsync(int pageIndex)
    {
#if ANDROID
        if (_renderer == null || pageIndex < 0 || pageIndex >= _pageCount)
            return;

        _previousButton.IsEnabled = false;
        _nextButton.IsEnabled = false;
        try
        {
            var renderedPage = await Task.Run(() => RenderPage(_renderer, pageIndex));
            _pageImage.Source = ImageSource.FromStream(() => new MemoryStream(renderedPage));
            _currentPageIndex = pageIndex;
            _pageLabel.Text = $"Seite {_currentPageIndex + 1} von {_pageCount}";
            ResetZoom();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PdfViewerPage] Page {pageIndex + 1} render failed: {ex}");
            await DisplayAlertAsync("PDF-Ansicht", "Die nächste Seite konnte nicht angezeigt werden. Die App bleibt geöffnet; bitte versuche es erneut.", "OK");
        }
        finally
        {
            _previousButton.IsEnabled = _currentPageIndex > 0;
            _nextButton.IsEnabled = _currentPageIndex < _pageCount - 1;
        }
#else
        await Task.CompletedTask;
#endif
    }

    private void OnPinchUpdated(object? sender, PinchGestureUpdatedEventArgs e)
    {
        switch (e.Status)
        {
            case GestureStatus.Started:
                _zoomStartScale = _zoomScale;
                break;
            case GestureStatus.Running:
                // Der ScrollView wurde bewusst entfernt: Er fängt auf Android die
                // Mehrfingerbewegung teilweise ab und der Zoom wirkt dadurch kaum.
                SetZoom(Math.Clamp(_zoomStartScale * e.Scale, 1d, 8d));
                break;
        }
    }

    private void ChangeZoom(double difference) => SetZoom(Math.Clamp(_zoomScale + difference, 1d, 8d));

    private void SetZoom(double zoom)
    {
        _zoomScale = zoom;
        _pageImage.Scale = zoom;
        _resetZoomButton.IsVisible = zoom > 1.01d;
        _zoomOutButton.IsEnabled = zoom > 1.01d;
        _zoomInButton.IsEnabled = zoom < 7.99d;
    }

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (_zoomScale <= 1.01d)
            return;

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _panStartX = _pageImage.TranslationX;
                _panStartY = _pageImage.TranslationY;
                break;
            case GestureStatus.Running:
                var maxX = Math.Max(0d, _pageImage.Width * (_zoomScale - 1d) / 2d);
                var maxY = Math.Max(0d, _pageImage.Height * (_zoomScale - 1d) / 2d);
                _pageImage.TranslationX = Math.Clamp(_panStartX + e.TotalX, -maxX, maxX);
                _pageImage.TranslationY = Math.Clamp(_panStartY + e.TotalY, -maxY, maxY);
                break;
        }
    }

    private void ResetZoom()
    {
        _zoomStartScale = 1d;
        SetZoom(1d);
        _pageImage.TranslationX = 0d;
        _pageImage.TranslationY = 0d;
    }

#if ANDROID
    private static byte[] RenderPage(PdfRenderer renderer, int pageIndex)
    {
        using var page = renderer.OpenPage(pageIndex);
        // Genügend Reserve für einen lesbaren Zoom auf aktuellen Handy-Displays.
        const int targetWidth = 2160;
        var scale = targetWidth / (double)Math.Max(page.Width, 1);
        var targetHeight = Math.Max(1, (int)Math.Ceiling(page.Height * scale));
        using var bitmap = Bitmap.CreateBitmap(targetWidth, targetHeight, Bitmap.Config.Argb8888!);
        bitmap.EraseColor(Android.Graphics.Color.White);
        page.Render(bitmap, null, null, PdfRenderMode.ForDisplay);

        using var stream = new MemoryStream();
        bitmap.Compress(Bitmap.CompressFormat.Png!, 100, stream);
        return stream.ToArray();
    }
#endif

    private void DisposeRenderer()
    {
#if ANDROID
        _renderer?.Close();
        _renderer?.Dispose();
        _renderer = null;
        _fileDescriptor?.Close();
        _fileDescriptor?.Dispose();
        _fileDescriptor = null;
#endif
    }
}
