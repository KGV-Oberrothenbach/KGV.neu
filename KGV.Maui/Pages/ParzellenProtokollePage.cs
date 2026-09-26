using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace KGV.Maui.Pages;

/// <summary>Gemeinsamer Einstieg für Mitglieds-Protokolle mit Parzellenbezug.</summary>
public sealed class ParzellenProtokollePage : ContentPage
{
    public ParzellenProtokollePage()
    {
        Title = "Protokolle";
        BackgroundColor = Colors.White;

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 20,
                Spacing = 14,
                Children =
                {
                    new Label { Text = "Parzellen-Protokolle", FontSize = 24, FontAttributes = FontAttributes.Bold },
                    new Label
                    {
                        Text = "Protokolle werden dem Mitglied zugeordnet. Die Parzelle, Ablesungen, Fotos und Unterschriften bilden den nachvollziehbaren fachlichen Bezug.",
                        LineBreakMode = LineBreakMode.WordWrap,
                        TextColor = Colors.DimGray
                    },
                    CreateCard("Übernahmeprotokoll", "Pachtbeginn dokumentieren: Zustand, Zählerstände, bis zu zehn Fotos und Unterschriften."),
                    CreateCard("Rückgabeprotokoll", "Pachtende dokumentieren: Endstände, offene Punkte, Fotos und Unterschriften."),
                    CreateCard("Begehungsprotokoll", "Feststellungen, Fristen, Fotos und Kenntnisnahmen strukturiert erfassen."),
                    new Label
                    {
                        Text = "Die Erfassungsmaske wird im nächsten Schritt ergänzt.",
                        TextColor = Colors.Gray,
                        FontSize = 12
                    }
                }
            }
        };
    }

    private static Border CreateCard(string title, string description) => new()
    {
        Stroke = Color.FromArgb("B7C3BB"),
        StrokeThickness = 1,
        BackgroundColor = Color.FromArgb("F6FAF6"),
        Padding = 14,
        Content = new VerticalStackLayout
        {
            Spacing = 5,
            Children =
            {
                new Label { Text = title, FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("174A2C") },
                new Label { Text = description, LineBreakMode = LineBreakMode.WordWrap }
            }
        }
    };
}
