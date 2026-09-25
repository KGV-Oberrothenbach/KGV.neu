using System;
using System.IO;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace KGV.Maui.Pages;

/// <summary>
/// Zeigt ein bereits autorisiert geladenes Bild ausschließlich innerhalb der App.
/// </summary>
public sealed class ImageViewerPage : ContentPage
{
    public ImageViewerPage(string? title, byte[] imageContent)
    {
        if (imageContent is not { Length: > 0 })
            throw new ArgumentException("Für die Bildansicht fehlt ein Bildinhalt.", nameof(imageContent));

        Title = string.IsNullOrWhiteSpace(title) ? "Foto" : title.Trim();

        var image = new Image
        {
            Source = ImageSource.FromStream(() => new MemoryStream(imageContent, writable: false)),
            Aspect = Aspect.AspectFit,
            BackgroundColor = Colors.Black,
        };

        Content = new Grid
        {
            Padding = 8,
            BackgroundColor = Colors.Black,
            Children = { new ScrollView { Content = image } },
        };
    }
}
