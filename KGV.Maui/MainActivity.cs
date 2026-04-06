using Android.App;
using Android.OS;
using Android.Content.PM;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using System.Linq;
using MauiApplication = Microsoft.Maui.Controls.Application;

namespace KGV.Maui;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize
        | ConfigChanges.Orientation
        | ConfigChanges.UiMode
        | ConfigChanges.ScreenLayout
        | ConfigChanges.SmallestScreenSize
        | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(null);
    }

    public override void OnBackPressed()
    {
        var currentPage = GetActiveWindowPage();
        if (currentPage?.SendBackButtonPressed() == true)
            return;

        base.OnBackPressed();
    }

    private static Page? GetActiveWindowPage()
    {
        return MauiApplication.Current?.Windows
            .LastOrDefault(window => window.Page != null)
            ?.Page;
    }
}
