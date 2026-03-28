using Android.Util;
using KGV.Core.Security;
using KGV.Infrastructure.DependencyInjection;
using KGV.Maui.Pages;
using KGV.Maui.Platforms.Android.Services;
using KGV.Maui.Services.Diagnostics;
using KGV.Maui.Services;
using KGV.Maui.Settings;
using KGV.Maui.State;
using KGV.Maui.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Storage;
using System.Diagnostics;

namespace KGV.Maui;

public static class MauiProgram
{
    private const string StartupLogTag = "KGV.Maui";

    public static MauiApp CreateMauiApp()
    {
        AppFileLog.Marker("APP_START");
        AppFileLog.Info(StartupLogTag, "Appstart initialisiert.");
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>();

        ShellRouteRegistrar.RegisterCommonRoutes();

        builder.Logging.AddDebug();
        builder.Logging.AddProvider(new AppFileLoggerProvider());
        builder.Logging.SetMinimumLevel(LogLevel.Information);

        RegisterUnhandledExceptionLogging();

        // Use the same appsettings.json as the WPF app.
        // For Android this must be packaged as an app asset (see `KGV.Maui.csproj`).
        AddAppSettings(builder.Configuration);
        ValidateSupabaseConfiguration(builder.Configuration);

        AppSettings.Load();
        AppFileLog.Info(StartupLogTag, $"AppSettings geladen. Diagnose-Log: {AppFileLog.LogFilePath}");

        builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

        builder.Services.AddSingleton<UserContextState>();
        builder.Services.AddSingleton<IUserContextAccessor>(sp => sp.GetRequiredService<UserContextState>());
        builder.Services.AddSingleton<MemberContextState>();
        builder.Services.AddSingleton<ParzellenContextState>();
        builder.Services.AddSingleton<HomeContextState>();
        builder.Services.AddSingleton<ArbeitsstundenReviewState>();
        builder.Services.AddSingleton<ArbeitseinsaetzeManagementState>();
        builder.Services.AddSingleton<ArbeitseinsaetzeUserState>();
        builder.Services.AddSingleton<TermineUserState>();
        builder.Services.AddSingleton<INfcScanService, AndroidNfcScanService>();

        builder.Services.AddKgvServices(builder.Configuration);

        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<HomeSectionDetailPage>();
        builder.Services.AddTransient<HomeManagementPage>();
        builder.Services.AddTransient<BekanntmachungenManagementPage>();
        builder.Services.AddTransient<BekanntmachungEditorPage>();
        builder.Services.AddTransient<TermineManagementPage>();
        builder.Services.AddTransient<TermineEditorPage>();
        builder.Services.AddTransient<ArbeitseinsaetzeManagementPage>();
        builder.Services.AddTransient<ArbeitseinsaetzeEditorPage>();
        builder.Services.AddTransient<ExportPage>();
        builder.Services.AddTransient<MemberSearchViewModel>();
        builder.Services.AddTransient<MemberSearchPage>();
        builder.Services.AddTransient<WartungsvertraegePage>();
        builder.Services.AddTransient<MemberWartungsvertraegePage>();
        builder.Services.AddTransient<WartungsvertragDetailPage>();
        builder.Services.AddTransient<WartungsvertragEditorPage>();
        builder.Services.AddTransient<WartungsvertragAssignMembersPage>();
        builder.Services.AddTransient<MeineDatenPage>();
        builder.Services.AddTransient<MemberGardensPage>();
        builder.Services.AddTransient<DokumentePage>();
        builder.Services.AddTransient<UserManagementViewModel>();
        builder.Services.AddTransient<UserManagementPage>();
        builder.Services.AddTransient<ParzellenViewModel>();
        builder.Services.AddTransient<ParzellenPage>();
        builder.Services.AddTransient<RfidEinrichtenViewModel>();
        builder.Services.AddTransient<FaelligeZaehlerViewModel>();
        builder.Services.AddTransient<MyProfilePage>();
        builder.Services.AddTransient<NebenmitgliedPage>();
        builder.Services.AddTransient<MyArbeitsstundenPage>();
        builder.Services.AddTransient<ArbeitsstundenEditorPage>();
        builder.Services.AddTransient<ArbeitsstundenReviewPage>();
        builder.Services.AddTransient<ArbeitsstundenReviewDetailPage>();

        builder.Services.AddTransient<AdminShell>();
        builder.Services.AddTransient<UserShell>();

        return builder.Build();
    }

    private static void AddAppSettings(IConfigurationBuilder configuration)
    {
        try
        {
            using var stream = FileSystem.OpenAppPackageFileAsync("appsettings.json").GetAwaiter().GetResult();
            configuration.AddJsonStream(stream);
            AppFileLog.Marker("APPSETTINGS_LOAD_OK");
            AppFileLog.Info(StartupLogTag, "`appsettings.json` erfolgreich aus dem App-Paket geladen.");
        }
        catch (Exception ex)
        {
            AppFileLog.Marker("APPSETTINGS_LOAD_FAIL");
            AppFileLog.Error(StartupLogTag, "`appsettings.json` konnte nicht aus dem App-Paket geladen werden.", ex);
            LogStartupError("`appsettings.json` konnte nicht aus dem App-Paket geladen werden.", ex);
            throw new InvalidOperationException("`appsettings.json` konnte nicht aus dem App-Paket geladen werden.", ex);
        }
    }

    private static void ValidateSupabaseConfiguration(IConfiguration configuration)
    {
        var supabaseUrl = configuration["Supabase:Url"];
        var supabaseKey = configuration["Supabase:PublishableKey"] ?? configuration["Supabase:Key"];

        if (!string.IsNullOrWhiteSpace(supabaseUrl) && !string.IsNullOrWhiteSpace(supabaseKey))
        {
            AppFileLog.Marker("SUPABASE_CONFIG_PRESENT_YES");
            AppFileLog.Info(StartupLogTag, "Supabase-Konfiguration vorhanden.");
            return;
        }

        var missingParts = new List<string>();
        if (string.IsNullOrWhiteSpace(supabaseUrl))
        {
            missingParts.Add("Supabase:Url");
        }

        if (string.IsNullOrWhiteSpace(supabaseKey))
        {
            missingParts.Add("Supabase:PublishableKey");
        }

        var message = $"Supabase-Konfiguration fehlt in `appsettings.json`: {string.Join(", ", missingParts)}";
        AppFileLog.Marker("SUPABASE_CONFIG_PRESENT_NO");
        AppFileLog.Warning(StartupLogTag, message);
        LogStartupError(message);
        throw new InvalidOperationException(message);
    }

    private static void RegisterUnhandledExceptionLogging()
    {
        AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        TaskScheduler.UnobservedTaskException -= TaskScheduler_UnobservedTaskException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            LogStartupError("Unbehandelte Ausnahme in der MAUI-App.", exception);
            return;
        }

        LogStartupError("Unbehandelte Ausnahme in der MAUI-App ohne Exception-Objekt.");
    }

    private static void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogStartupError("Nicht beobachtete Task-Ausnahme in der MAUI-App.", e.Exception);
    }

    private static void LogStartupError(string message, Exception? ex = null)
    {
        AppFileLog.Error(StartupLogTag, message, ex);
        Debug.WriteLine($"[{StartupLogTag}] {message}");

        if (ex is null)
        {
            Log.Error(StartupLogTag, message);
            return;
        }

        Debug.WriteLine(ex);
        Log.Error(StartupLogTag, $"{message} {ex.GetType().Name}: {ex.Message}");
    }
}
