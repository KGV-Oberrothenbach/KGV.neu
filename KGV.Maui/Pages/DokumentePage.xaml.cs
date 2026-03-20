namespace KGV.Maui.Pages;

public class DokumentePage : ContentPage
{
    public DokumentePage()
    {
        Title = "Dokumente";

        Content = new VerticalStackLayout
        {
            Padding = 24,
            Spacing = 12,
            Children =
            {
                new Label { Text = "Dokumente", FontSize = 24, FontAttributes = FontAttributes.Bold },
                new Label { Text = "Platzhalter" }
            }
        };
    }
}
