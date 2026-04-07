using Android.App;
using Android.Runtime;
using KGV.Maui.Services.Diagnostics;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace KGV.Maui;

[Application(
    Icon = "@mipmap/appicon",
    RoundIcon = "@mipmap/appicon_round",
    Label = "KGV")]
public class MainApplication : MauiApplication
{
    private const string StartupLogTag = "KGV.Maui";

    public MainApplication(nint handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    protected override MauiApp CreateMauiApp()
    {
        AppFileLog.Marker("MAIN_APPLICATION_CREATE_MAUI_APP_BEGIN");

        try
        {
            var app = MauiProgram.CreateMauiApp();
            AppFileLog.Marker("MAIN_APPLICATION_CREATE_MAUI_APP_OK");
            return app;
        }
        catch (Exception ex)
        {
            AppFileLog.Marker("MAIN_APPLICATION_CREATE_MAUI_APP_FAIL");
            AppFileLog.ErrorDetailed(StartupLogTag, "MainApplication.CreateMauiApp ist fehlgeschlagen.", ex);
            throw;
        }
    }
}
