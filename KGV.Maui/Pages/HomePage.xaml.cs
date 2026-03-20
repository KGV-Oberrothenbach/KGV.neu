namespace KGV.Maui.Pages;

public class HomePage : ContentPage
{
    public HomePage()
    {
        Title = "Home";

        Content = new VerticalStackLayout
        {
            Padding = 24,
            Spacing = 12,
            Children =
            {
                new Label { Text = "Home", FontSize = 24, FontAttributes = FontAttributes.Bold },
                new Label { Text = "Platzhalter" }
            }
        };
    }
}
