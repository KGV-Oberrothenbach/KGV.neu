namespace KGV.Maui;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new ContentPage { Content = new Label { Text = "Recovered placeholder App.xaml.cs" } };
    }
}
