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
    private readonly Label _hintLabel;

    public VertragsSignaturPage(DocumentInfo sourceDocument)
    {
        var dokumentName = sourceDocument.FormularDokumentTypAnzeige == "-"
            ? "Vertragsdokument"
            : sourceDocument.FormularDokumentTypAnzeige;

        Title = "Digital signieren";
        BackgroundColor = Colors.White;

        _hintLabel = new Label
        {
            Text = $"Bitte unterschreiben Sie im Querformat. Die digitale Signatur wird als eigene signierte Fassung für {dokumentName} gespeichert.",
            TextColor = Colors.Gray
        };

        _graphicsView = new GraphicsView
        {
            Drawable = _drawable,
            HeightRequest = 320,
            BackgroundColor = Colors.White,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
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

        var acceptButton = new Button { Text = "Übernehmen" };
        acceptButton.Clicked += async (_, _) => await AcceptAsync();

        var cancelButton = new Button { Text = "Abbrechen" };
        cancelButton.Clicked += async (_, _) => await CancelAsync();

        var layout = new VerticalStackLayout
        {
            Padding = new Microsoft.Maui.Thickness(24),
            Spacing = 16
        };
        layout.Children.Add(new Label
        {
            Text = "Digitale Signatur",
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
        buttonBar.Children.Add(clearButton);
        buttonBar.Children.Add(cancelButton);
        buttonBar.Children.Add(acceptButton);
        layout.Children.Add(buttonBar);

        Content = layout;
    }

    public Task<DigitalSignatureCapture?> WaitForResultAsync() => _resultSource.Task;

    protected override void OnAppearing()
    {
        base.OnAppearing();
        MainActivity.SetLandscapeOrientationEnabled(true);
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
        var point = TryGetTouchPoint(e);
        if (point == null)
            return;

        _drawable.BeginStroke(point.Value);
        _graphicsView.Invalidate();
    }

    private void OnDragInteraction(object? sender, TouchEventArgs e)
    {
        var point = TryGetTouchPoint(e);
        if (point == null)
            return;

        _drawable.AddPoint(point.Value);
        _graphicsView.Invalidate();
    }

    private void OnEndInteraction(object? sender, TouchEventArgs e)
    {
        var point = TryGetTouchPoint(e);
        if (point != null)
            _drawable.AddPoint(point.Value);

        _drawable.EndStroke();
        _graphicsView.Invalidate();
    }

    private async Task AcceptAsync()
    {
        var signature = _drawable.Export(_graphicsView.Width, _graphicsView.Height);
        if (!signature.HasContent)
        {
            await DisplayAlert("Signatur", "Bitte zuerst unterschreiben.", "OK");
            return;
        }

        _resultSource.TrySetResult(signature);
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