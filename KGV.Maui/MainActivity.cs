using System;
using System.Linq;
using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Activity;
using KGV.Maui.Services.Diagnostics;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

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
    private ActivityBackPressedCallback? _backPressedCallback;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        AppFileLog.Info("KGV.Navigation", "MainActivity.OnCreate registriert Android-BackCallback.");
        base.OnCreate(savedInstanceState);

        if (_backPressedCallback == null)
            _backPressedCallback = new ActivityBackPressedCallback(this);

        OnBackPressedDispatcher.AddCallback(this, _backPressedCallback);
    }

    public override void OnBackPressed()
    {
        AppFileLog.Info("KGV.Navigation", "MainActivity.OnBackPressed erreicht.");

        if (TryHandleMauiBackNavigation())
            return;

        base.OnBackPressed();
    }

    public static void SetLandscapeOrientationEnabled(bool enabled)
    {
        try
        {
            if (Microsoft.Maui.ApplicationModel.Platform.CurrentActivity is not MainActivity activity)
                return;

            activity.RunOnUiThread(() =>
            {
                activity.RequestedOrientation = enabled
                    ? ScreenOrientation.SensorLandscape
                    : ScreenOrientation.Unspecified;
            });
        }
        catch (Exception ex)
        {
            AppFileLog.Warning("KGV.Navigation", $"MainActivity.SetLandscapeOrientationEnabled fehlgeschlagen: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private bool TryHandleMauiBackNavigation()
    {
        try
        {
            var rootPage = Microsoft.Maui.Controls.Application.Current?
                .Windows
                .FirstOrDefault()?
                .Page;

            if (rootPage == null)
            {
                AppFileLog.Warning("KGV.Navigation", "MainActivity.TryHandleMauiBackNavigation: keine RootPage vorhanden.");
                return false;
            }

            var handled = rootPage.SendBackButtonPressed();

            AppFileLog.Info(
                "KGV.Navigation",
                $"MainActivity.TryHandleMauiBackNavigation: RootPage={rootPage.GetType().Name}, Handled={handled}");

            return handled;
        }
        catch (Exception ex)
        {
            AppFileLog.Warning(
                "KGV.Navigation",
                $"MainActivity.TryHandleMauiBackNavigation fehlgeschlagen: {ex.GetType().Name}: {ex.Message}");
            return false;
        }
    }

    private void InvokeSystemBackFallback()
    {
        if (_backPressedCallback != null)
            _backPressedCallback.Enabled = false;

        try
        {
            base.OnBackPressed();
        }
        finally
        {
            if (_backPressedCallback != null)
                _backPressedCallback.Enabled = true;
        }
    }

    protected override void OnDestroy()
    {
        try
        {
            _backPressedCallback?.Remove();
            _backPressedCallback?.Dispose();
            _backPressedCallback = null;
        }
        catch
        {
            // bewusst schluckend
        }

        base.OnDestroy();
    }

    private sealed class ActivityBackPressedCallback : OnBackPressedCallback
    {
        private readonly MainActivity _activity;

        public ActivityBackPressedCallback(MainActivity activity) : base(true)
        {
            _activity = activity;
        }

        public override void HandleOnBackPressed()
        {
            AppFileLog.Info("KGV.Navigation", "MainActivity.ActivityBackPressedCallback.HandleOnBackPressed erreicht.");

            if (_activity.TryHandleMauiBackNavigation())
                return;

            _activity.InvokeSystemBackFallback();
        }
    }
}