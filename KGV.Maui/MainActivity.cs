using Android.App;
using Android.OS;
using Android.Content.PM;
using KGV.Maui.Services.Diagnostics;
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
        AppFileLog.Marker("MAIN_ACTIVITY_ONCREATE_BEGIN");

        try
        {
            base.OnCreate(null);
            AppFileLog.Marker("MAIN_ACTIVITY_ONCREATE_OK");
        }
        catch (Exception ex)
        {
            AppFileLog.Marker("MAIN_ACTIVITY_ONCREATE_FAIL");
            AppFileLog.ErrorDetailed("KGV.Maui", "MainActivity.OnCreate ist fehlgeschlagen.", ex);
            throw;
        }
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
