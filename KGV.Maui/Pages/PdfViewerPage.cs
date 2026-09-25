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
    private int _currentPageIndex;
    private int _pageCount;

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
        _previousButton.Clicked += async (_, _) => await ShowPageAsync(_currentPageIndex - 1);
        _nextButton.Clicked += async (_, _) => await ShowPageAsync(_currentPageIndex + 1);

        var navigationBar = new Grid
        {
            ColumnSpacing = 12,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
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
        navigationBar.Add(_nextButton, 2, 0);
        Grid.SetRow(navigationBar, 1);

        Content = new Grid
        {
            Padding = 12,
            RowDefinitions =
            {
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto),
            },
            Children =
            {
                new ScrollView { Content = _pageImage },
                navigationBar,
            },
        };
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

#if ANDROID
    private static byte[] RenderPage(PdfRenderer renderer, int pageIndex)
    {
        using var page = renderer.OpenPage(pageIndex);
        const int targetWidth = 1440;
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
