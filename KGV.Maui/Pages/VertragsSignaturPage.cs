using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KGV.Core.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace KGV.Maui.Pages;

public sealed class VertragsSignaturPage : ContentPage
{
    private readonly TaskCompletionSource<DigitalSignatureCapture?> _resultSource = new();
    private readonly SignaturePadDrawable _drawable = new();
    private readonly GraphicsView _graphicsView;
    private readonly bool _isLastSignature;
    private readonly Label _hintLabel;

        public VertragsSignaturPage(DocumentInfo sourceDocument, string? unterschriftTitel = null, bool isLastSignature = true, string? signerName = null)
    {
            _isLastSignature = isLastSignature;
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
            IsEnabled = true,
            HeightRequest = 320
        };
        _graphicsView.StartInteraction += OnStartInteraction;
        _graphicsView.DragInteraction += OnDragInteraction;
        _graphicsView.EndInteraction += OnEndInteraction;
        _graphicsView.CancelInteraction += (_, _) =>
        {
            _drawable.EndStroke();
            _graphicsView.Invalidate();
        };

        var clearButton = new Button { Text = "Leeren" };
        clearButton.Clicked += (_, _) =>
        {
            _drawable.Clear();
            _graphicsView.Invalidate();
        };

        var saveText = isLastSignature ? "Speichern" : "Speichern und Weiter";
        var saveButton = new Button { Text = saveText };
        saveButton.Clicked += async (_, _) => await AcceptAsync();

        var cancelButton = new Button { Text = "Abbrechen" };
        cancelButton.Clicked += async (_, _) => await CancelAsync();

        var layout = new VerticalStackLayout
        {
            Padding = new Microsoft.Maui.Thickness(24),
            Spacing = 16
        };
        layout.Children.Add(new Label
        {
            Text = string.IsNullOrWhiteSpace(unterschriftTitel) ? "Digitale Signatur" : unterschriftTitel.Trim(),
            FontSize = 24,
            FontAttributes = FontAttributes.Bold
        });
        layout.Children.Add(_hintLabel);
        layout.Children.Add(new Border
        {
            Stroke = Colors.LightGray,
            StrokeThickness = 1,
            Padding = 12,
            Content = _graphicsView
        });

        var buttonBar = new HorizontalStackLayout
        {
            Spacing = 12,
            HorizontalOptions = LayoutOptions.End
        };
        // Keep clear and cancel left of the final Save button; Save always at the end
        buttonBar.Children.Add(clearButton);
        buttonBar.Children.Add(cancelButton);
        buttonBar.Children.Add(saveButton);
        layout.Children.Add(buttonBar);

        Content = layout;
    }

    public Task<DigitalSignatureCapture?> WaitForResultAsync() => _resultSource.Task;

    protected override void OnAppearing()
    {
        base.OnAppearing();
        MainActivity.SetLandscapeOrientationEnabled(true);

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
        MainActivity.SetLandscapeOrientationEnabled(false);
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
    }

    private async Task AcceptAsync()
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

        _resultSource.TrySetResult(signature);

        // Show a short confirmation so the user notices the save action.
        try
        {
            var message = _isLastSignature
                ? "Unterschrift gespeichert."
                : "Unterschrift gespeichert. Bitte die nächste Unterschrift erfassen.";
            await DisplayAlert("Signatur", message, "OK");
        }
        catch { }

        await Navigation.PopModalAsync();
    }

    private async Task CancelAsync()
    {
        _resultSource.TrySetResult(null);
        await Navigation.PopModalAsync();
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

            for (var i = 1; i < stroke.Count; i++)
                canvas.DrawLine((float)stroke[i - 1].X, (float)stroke[i - 1].Y, (float)stroke[i].X, (float)stroke[i].Y);
        }
    }
}