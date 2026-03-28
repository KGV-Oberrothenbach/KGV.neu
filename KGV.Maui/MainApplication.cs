using Android.App;
using Android.Runtime;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace KGV.Maui;

[Application(
    Icon = "@mipmap/appicon",
    RoundIcon = "@mipmap/appicon_round",
    Label = "KGV")]
public class MainApplication : MauiApplication
{
    public MainApplication(nint handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
