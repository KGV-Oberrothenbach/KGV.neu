namespace KGV.Maui.Pages;

public sealed class AblesenOverviewPage : ContentPage
{
    public AblesenOverviewPage()
    {
        Title = "Ablesen";

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 12,
                Children =
                {
                    new Label { Text = "Ablesen", FontSize = 24, FontAttributes = FontAttributes.Bold },
                    new Label { Text = "Bitte wähle eine Funktion.", LineBreakMode = LineBreakMode.WordWrap },
                    CreateTile("Ablesung erfassen", "NFC/RFID-Scan ist im aktuellen MAUI-Stand noch nicht direkt aktiv; UID-Prüfung läuft derzeit manuell als Fallback.", () => Shell.Current.GoToAsync(nameof(AblesungErfassenPage))),
                    CreateTile("Zählerwechsel", "NFC/RFID-Scan ist im aktuellen MAUI-Stand noch nicht direkt aktiv; UID-Prüfung läuft derzeit manuell als Fallback.", () => Shell.Current.GoToAsync(nameof(ZaehlerwechselPage))),
                    CreateTile("RFID einrichten", "Parzelle wählen, Medium wählen und Tag zuordnen", () => Shell.Current.GoToAsync(nameof(RfidEinrichtenPage))),
                    CreateTile("Fällige Zähler", "Zähler mit naher Eichfälligkeit anzeigen", () => Shell.Current.GoToAsync(nameof(FaelligeZaehlerPage)))
                }
            }
        };
    }

    private static View CreateTile(string title, string subtitle, Func<Task> navigateAsync)
    {
        var border = new Border
        {
            Padding = 18,
            Stroke = Colors.LightGray,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(16) },
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    new Label { Text = title, FontSize = 18, FontAttributes = FontAttributes.Bold },
                    new Label { Text = subtitle, LineBreakMode = LineBreakMode.WordWrap, TextColor = Colors.Gray }
                }
            }
        };

        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += async (_, _) => await navigateAsync();
        border.GestureRecognizers.Add(tapGesture);
        return border;
    }
}
