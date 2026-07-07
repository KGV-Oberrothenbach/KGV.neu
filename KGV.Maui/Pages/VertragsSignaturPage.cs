using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KGV.Core.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.ApplicationModel;

namespace KGV.Maui.Pages;

public sealed class VertragsSignaturPage : ContentPage
{
    private readonly TaskCompletionSource<DigitalSignatureCapture?> _resultSource = new();
    private readonly SignaturePadDrawable _drawable = new();
    private readonly GraphicsView _graphicsView;
    private bool _landscapeForced;
    private ScrollView? _parentScrollView;
    private readonly Label _hintLabel;

    // signerName: optional display name to append to title (e.g. "Vorname Nachname"). If signerName == "Vorstand" we skip appending.
    public VertragsSignaturPage(DocumentInfo sourceDocument, string? unterschriftTitel = null, bool isLastSignature = true, string? signerName = null)
    {
        var dokumentName = sourceDocument.FormularDokumentTypAnzeige == "-"
            ? "Vertragsdokument"
            : sourceDocument.FormularDokumentTypAnzeige;
        var captureTitle = string.IsNullOrWhiteSpace(unterschriftTitel)
            ? "digitale Signatur"
            : unterschriftTitel.Trim();

        Title = "Digital signieren";
        BackgroundColor = Colors.White;

        _hintLabel = new Label
        {
            Text = $"Bitte unterschreiben Sie im Querformat. Erfasst wird die {captureTitle} für {dokumentName}.",
            TextColor = Colors.Gray
        };

        _graphicsView = new GraphicsView
        {
            Drawable = _drawable,
            BackgroundColor = Colors.White,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.FillAndExpand,
            InputTransparent = false,
            IsEnabled = true
        };
        _graphicsView.StartInteraction += OnStartInteraction;
        _graphicsView.DragInteraction += OnDragInteraction;
        _graphicsView.EndInteraction += OnEndInteraction;
        _graphicsView.CancelInteraction += (_, _) =>
        {
            _drawable.EndStroke();
            _graphicsView.Invalidate();
            try { _parentScrollView.IsEnabled = true; } catch { }
        };

        var clearButton = new Button { Text = "Leeren" };
        clearButton.Clicked += (_, _) =>
        {
            _drawable.Clear();
            _graphicsView.Invalidate();
        };

        var saveText = isLastSignature ? "Speichern" : "Speichern und Weiter";
        var saveButton = new Button { Text = saveText };
        saveButton.Clicked += async (_, _) => await AcceptAsync(isLastSignature);

        var cancelButton = new Button { Text = "Abbrechen" };
        cancelButton.Clicked += async (_, _) => await CancelAsync();

        // Use a Grid so the GraphicsView fills available space and the button bar stays reachable
        var grid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto), // title
                new RowDefinition(GridLength.Auto), // hint
                new RowDefinition(new GridLength(1, GridUnitType.Star)), // graphics
                new RowDefinition(GridLength.Auto) // buttons
            },
            Padding = new Microsoft.Maui.Thickness(12)
        };

        var titleLabel = new Label
        {
            Text = string.IsNullOrWhiteSpace(unterschriftTitel) ? "Digitale Signatur" : unterschriftTitel.Trim(),
            FontSize = 24,
            FontAttributes = FontAttributes.Bold
        };

        try
        {
            if (!string.IsNullOrWhiteSpace(signerName) && !string.Equals(signerName, "Vorstand", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(unterschriftTitel) && unterschriftTitel.Contains("Antragsteller", StringComparison.OrdinalIgnoreCase))
            {
                titleLabel.Text = titleLabel.Text + " — " + signerName.Trim();
            }
        }
        catch { }

        grid.Add(titleLabel, 0, 0);
        grid.Add(_hintLabel, 0, 1);

        var border = new Border
        {
            Stroke = Colors.LightGray,
            StrokeThickness = 1,
            Padding = 6,
            Content = _graphicsView
        };

        grid.Add(border, 0, 2);

        var buttonBar = new HorizontalStackLayout
        {
            Spacing = 12,
            HorizontalOptions = LayoutOptions.End
        };
        buttonBar.Children.Add(clearButton);
        buttonBar.Children.Add(cancelButton);
        buttonBar.Children.Add(saveButton);

        grid.Add(buttonBar, 0, 3);

        // Wrap grid in ScrollView to ensure on very small screens buttons remain reachable
        var scroll = new ScrollView { Content = grid };
        _parentScrollView = scroll;
        Content = scroll;
    }

    public Task<DigitalSignatureCapture?> WaitForResultAsync() => _resultSource.Task;

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Only force landscape on larger devices (tablets). On phones keep default orientation so the UI fits.
        try
        {
            var display = Microsoft.Maui.Devices.DeviceDisplay.MainDisplayInfo;
            var widthDp = display.Width / display.Density;
            var heightDp = display.Height / display.Density;
            var smallestDp = Math.Min(widthDp, heightDp);
            // Consider devices with smallest dimension >= 600dp as tablets
            if (smallestDp >= 600)
            {
                MainActivity.SetLandscapeOrientationEnabled(true);
                _landscapeForced = true;
            }
            else
            {
                _landscapeForced = false;
            }
        }
        catch { _landscapeForced = false; }

        // After orientation change the view may need a short delay to be measured and receive touches.
        // Dispatch a short invalidate to ensure the GraphicsView is ready for interaction.
        try
        {
            Microsoft.Maui.Controls.Application.Current?.Dispatcher.Dispatch(async () =>
            {
                try
                {
                    await Task.Delay(120);
                    _graphicsView.Invalidate();
                }
                catch { }
            });
        }
        catch { }
    }

    protected override void OnDisappearing()
    {
        if (_landscapeForced)
        {
            try { MainActivity.SetLandscapeOrientationEnabled(false); } catch { }
            _landscapeForced = false;
        }
        base.OnDisappearing();
    }

    protected override bool OnBackButtonPressed()
    {
        _resultSource.TrySetResult(null);
        return base.OnBackButtonPressed();
    }

    private void OnStartInteraction(object? sender, TouchEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("OnStartInteraction: touches=" + (e?.Touches?.Length ?? 0));
        var point = TryGetTouchPoint(e);
        if (point == null)
            return;

        // Disable parent scrolling while drawing so touch moves are delivered continuously
        try { if (_parentScrollView != null) _parentScrollView.IsEnabled = false; } catch { }

        // Light haptic feedback on start
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

        _drawable.BeginStroke(point.Value);
        _graphicsView.Invalidate();
    }

    private void OnDragInteraction(object? sender, TouchEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("OnDragInteraction: touches=" + (e?.Touches?.Length ?? 0));
        var point = TryGetTouchPoint(e);
        if (point == null)
            return;

        _drawable.AddPoint(point.Value);
        _graphicsView.Invalidate();
    }

    private void OnEndInteraction(object? sender, TouchEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("OnEndInteraction: touches=" + (e?.Touches?.Length ?? 0));
        var point = TryGetTouchPoint(e);
        if (point != null)
            _drawable.AddPoint(point.Value);
        _drawable.EndStroke();
        _graphicsView.Invalidate();

        // Re-enable parent scrolling after finishing the stroke
        try { if (_parentScrollView != null) _parentScrollView.IsEnabled = true; } catch { }
    }

    private async Task AcceptAsync(bool isLastSignature = true)
    {
        // ensure view has been measured
        if (_graphicsView.Width <= 0 || _graphicsView.Height <= 0)
        {
            await Task.Delay(120);
            _graphicsView.Invalidate();
        }

        var width = _graphicsView.Width > 0 ? _graphicsView.Width : 1;
        var height = _graphicsView.Height > 0 ? _graphicsView.Height : 1;

        var signature = _drawable.Export(width, height);
        if (!signature.HasContent)
        {
            await DisplayAlert("Signatur", "Bitte zuerst unterschreiben.", "OK");
            return;
        }

        // Close modal first, then complete the waiting task so the caller will only continue
        // after the UI has dismissed this page. This avoids the caller pushing another modal
        // while this one is still open which caused the observed race/abort behavior.
        try
        {
            await Navigation.PopModalAsync();
        }
        catch { }

        // Debug: inform the user that the signature was captured
        try
        {
            var msg = isLastSignature ? "Signatur gespeichert." : "Signatur gespeichert. Weiter zur nächsten Unterschrift...";
            await MainThread.InvokeOnMainThreadAsync(async () => await DisplayAlert("Signatur", msg, "OK"));
        }
        catch { }

        _resultSource.TrySetResult(signature);
    }

    private async Task CancelAsync()
    {
        try
        {
            await Navigation.PopModalAsync();
        }
        catch { }

        _resultSource.TrySetResult(null);
    }

    private static Point? TryGetTouchPoint(TouchEventArgs e)
    {
        var touches = e?.Touches;
        if (touches == null || touches.Length == 0)
            return null;

        var point = touches[0];
        return new Point(point.X, point.Y);
    }

    private sealed class SignaturePadDrawable : IDrawable
    {
        private readonly List<List<Point>> _strokes = new();
        private List<Point>? _activeStroke;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.FillColor = Colors.White;
            canvas.FillRectangle(dirtyRect);
            canvas.StrokeColor = Colors.Black;
            canvas.StrokeSize = 3;
            canvas.StrokeLineCap = LineCap.Round;
            canvas.StrokeLineJoin = LineJoin.Round;

            foreach (var stroke in _strokes)
                DrawStroke(canvas, stroke);
        }

        public void BeginStroke(Point point)
        {
            _activeStroke = new List<Point> { point };
            _strokes.Add(_activeStroke);
        }

        public void AddPoint(Point point)
        {
            _activeStroke ??= new List<Point>();
            if (!_strokes.Contains(_activeStroke))
                _strokes.Add(_activeStroke);

            _activeStroke.Add(point);
        }

        public void EndStroke() => _activeStroke = null;

        public void Clear()
        {
            _strokes.Clear();
            _activeStroke = null;
        }

        public DigitalSignatureCapture Export(double width, double height)
        {
            return new DigitalSignatureCapture
            {
                CanvasWidth = width,
                CanvasHeight = height,
                SignedAt = DateTime.Now,
                Strokes = _strokes
                    .Where(stroke => stroke.Count > 0)
                    .Select(stroke => new DigitalSignatureStroke
                    {
                        Points = stroke.Select(point => new DigitalSignaturePoint
                        {
                            X = point.X,
                            Y = point.Y
                        }).ToArray()
                    })
                    .ToArray()
            };
        }

        private static void DrawStroke(ICanvas canvas, IReadOnlyList<Point> stroke)
        {
            if (stroke.Count == 1)
            {
                canvas.FillColor = Colors.Black;
                canvas.FillCircle((float)stroke[0].X, (float)stroke[0].Y, 1.5f);
                return;
            }

            // Simple smoothing: moving-average over triplets to reduce jitter and produce smoother lines.
            var smoothed = new List<Point>(stroke.Count);
            // keep first point
            smoothed.Add(stroke[0]);

            // average each triplet for intermediate points
            for (var i = 1; i < stroke.Count - 1; i++)
            {
                var p0 = stroke[i - 1];
                var p1 = stroke[i];
                var p2 = stroke[i + 1];
                var sx = (p0.X + p1.X + p2.X) / 3.0;
                var sy = (p0.Y + p1.Y + p2.Y) / 3.0;
                smoothed.Add(new Point(sx, sy));
            }

            // keep last point
            smoothed.Add(stroke[stroke.Count - 1]);

            for (var i = 1; i < smoothed.Count; i++)
            {
                canvas.DrawLine((float)smoothed[i - 1].X, (float)smoothed[i - 1].Y, (float)smoothed[i].X, (float)smoothed[i].Y);
            }
        }
    }
}