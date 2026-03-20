namespace KGV.Maui.Pages;

public sealed class ExitPage : ContentPage
{
    public ExitPage()
    {
        Title = "Beenden";

        Content = new VerticalStackLayout
        {
            Padding = 24,
            Spacing = 12,
            Children =
            {
                new Label { Text = "Beenden…", FontSize = 18, FontAttributes = FontAttributes.Bold },
                new Label { Text = "Die App wird geschlossen." }
            }
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            Application.Current?.Quit();
        }
        catch
        {
        }
    }
}
