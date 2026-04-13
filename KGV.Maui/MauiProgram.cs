using KGV.Core.Security;
using KGV.Infrastructure.DependencyInjection;
using KGV.Maui.Pages;
using KGV.Maui.Platforms.Android.Services;
using KGV.Maui.Services.Diagnostics;
using KGV.Maui.Services;
using KGV.Maui.Services.PendingPhotos;
using KGV.Maui.Settings;
using KGV.Maui.State;
using KGV.Maui.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Storage;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace KGV.Maui;

public static class MauiProgram
{
    private const string StartupLogTag = "KGV.Maui";

    public static MauiApp CreateMauiApp()
    {
        AppFileLog.Marker("APP_START");
        AppFileLog.Info(StartupLogTag, "Appstart initialisiert.");
        RegisterUnhandledExceptionLogging();
        var useMauiAppCompleted = false;

        try
        {
            AppFileLog.Marker("MAUI_CREATE_BUILDER_BEGIN");
            var builder = MauiApp.CreateBuilder();
            AppFileLog.Marker("MAUI_CREATE_BUILDER_OK");

            AppFileLog.Info(
                StartupLogTag,
                "Keine explizite VisualElement-/Culture-Probe mehr vor UseMauiApp. Controls werden nur noch im regulären MAUI-Startup initialisiert.");

            RunStartupStep("USE_MAUI_APP_MINIMAL", () =>
            {
                builder.UseMauiApp<App>();
            });
            useMauiAppCompleted = true;

            RunStartupStep("REGISTER_COMMON_ROUTES", ShellRouteRegistrar.RegisterCommonRoutes);

            RunStartupStep("CONFIGURE_LOGGING", () =>
            {
                builder.Logging.AddDebug();
                builder.Logging.AddProvider(new AppFileLoggerProvider());
                builder.Logging.SetMinimumLevel(LogLevel.Information);
            });

            RunStartupStep("ADD_APPSETTINGS", () => AddAppSettings(builder.Configuration));
            RunStartupStep("VALIDATE_SUPABASE_CONFIGURATION", () => ValidateSupabaseConfiguration(builder.Configuration));
            RunStartupStep("LOAD_APPSETTINGS", () =>
            {
                AppSettings.Load();
                var externalLogPath = AppFileLog.ExternalLogFilePath;
                var logTargetInfo = string.IsNullOrWhiteSpace(externalLogPath)
                    ? $"Diagnose-Log intern: {AppFileLog.LogFilePath}"
                    : $"Diagnose-Log intern: {AppFileLog.LogFilePath} | extern lesbar: {externalLogPath}";
                AppFileLog.Info(StartupLogTag, $"AppSettings geladen. {logTargetInfo}");
            });

            RunStartupStep("REGISTER_CONFIGURATION", () =>
            {
                builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
            });

            RunStartupStep("REGISTER_MAUI_STATE_SERVICES", () => RegisterStateServices(builder.Services));
            RunStartupStep("REGISTER_KGV_SERVICES", () => builder.Services.AddKgvServices(builder.Configuration));
            RunStartupStep("REGISTER_MAUI_PAGES_AND_VIEWMODELS", () => RegisterPageServices(builder.Services));

            AppFileLog.Marker("MAUI_BUILDER_BUILD_BEGIN");
            var app = builder.Build();
            AppFileLog.Marker("MAUI_BUILDER_BUILD_OK");
            return app;
        }
        catch (Exception ex)
        {
            if (!useMauiAppCompleted)
            {
                AppFileLog.Warning(
                    StartupLogTag,
                    "Der Crash liegt bereits im minimalen `.UseMauiApp<App>()`-Pfad. Spätere Registrierungen für Routen, Logging, Konfiguration, Services und Seiten wurden noch nicht erreicht.");
            }

            LogStartupError("CreateMauiApp ist im frühen MAUI-Startup fehlgeschlagen.", ex);
            throw;
        }
    }

    private static void RegisterStateServices(IServiceCollection services)
    {
        services.AddSingleton<UserContextState>();
        services.AddSingleton<IUserContextAccessor>(sp => sp.GetRequiredService<UserContextState>());
        services.AddSingleton<MemberContextState>();
        services.AddSingleton<ParzellenContextState>();
        services.AddSingleton<HomeContextState>();
        services.AddSingleton<ArbeitsstundenReviewState>();
        services.AddSingleton<ArbeitseinsaetzeManagementState>();
        services.AddSingleton<ArbeitseinsaetzeUserState>();
        services.AddSingleton<TermineUserState>();
        services.AddSingleton<ZaehlerwechselWorkflowState>();
        services.AddSingleton<PendingPhotoQueue>();
        services.AddSingleton<PendingPhotoService>();
        services.AddSingleton<PendingPhotoSyncService>();
        services.AddSingleton<PendingPhotoMenuState>();
        services.AddSingleton<INfcScanService, AndroidNfcScanService>();
        services.AddSingleton<IRfidFeedbackService, AndroidRfidFeedbackService>();
    }

    private static void RegisterPageServices(IServiceCollection services)
    {
        services.AddTransient<LoginPage>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<HomePage>();
        services.AddTransient<AblesenOverviewPage>();
        services.AddTransient<PendingPhotoUploadsPage>();
        services.AddTransient<HomeSectionDetailPage>();
        services.AddTransient<HomeManagementPage>();
        services.AddTransient<BekanntmachungenManagementPage>();
        services.AddTransient<BekanntmachungEditorPage>();
        services.AddTransient<TermineManagementPage>();
        services.AddTransient<TermineEditorPage>();
        services.AddTransient<ArbeitseinsaetzeManagementPage>();
        services.AddTransient<ArbeitseinsaetzeEditorPage>();
        services.AddTransient<ExportPage>();
        services.AddTransient<ImpressumPage>();
        services.AddSingleton<MemberSearchRefreshState>();
        services.AddTransient<MemberSearchViewModel>();
        services.AddTransient<MemberSearchPage>();
        services.AddTransient<MemberDetailPage>();
        services.AddTransient<SaisonverwaltungPage>();
        services.AddTransient<VereinskonfigurationPage>();
        services.AddTransient<WartungsvertraegePage>();
        services.AddTransient<MemberWartungsvertraegePage>();
        services.AddTransient<WartungsvertragDetailPage>();
        services.AddTransient<WartungsvertragEditorPage>();
        services.AddTransient<WartungsvertragAssignMembersPage>();
        services.AddTransient<MeineDatenPage>();
        services.AddTransient<AdminMenuPage>();
        services.AddTransient<MemberGardensPage>();
        services.AddTransient<MemberParzellenDetailPage>();
        services.AddTransient<DokumentePage>();
        services.AddTransient<VertragsSignaturPage>();
        services.AddTransient<UserManagementViewModel>();
        services.AddTransient<UserManagementPage>();
        services.AddTransient<ParzellenViewModel>();
        services.AddTransient<ParzellenPage>();
        services.AddTransient<RfidEinrichtenViewModel>();
        services.AddTransient<FaelligeZaehlerViewModel>();
        services.AddTransient<MyProfilePage>();
        services.AddTransient<NebenmitgliedPage>();
        services.AddTransient<MyArbeitsstundenPage>();
        services.AddTransient<ArbeitsstundenEditorPage>();
        services.AddTransient<ArbeitsstundenReviewPage>();
        services.AddTransient<ArbeitsstundenReviewDetailPage>();
        services.AddTransient<ParzellenAblesungenPage>();
        services.AddTransient<ZaehlerwechselAusbauPage>();
        services.AddTransient<ZaehlerwechselEinbauPage>();
        services.AddTransient<AblesungenFreigabePage>();
        services.AddTransient<AdminShell>();
        services.AddTransient<UserShell>();
    }

    private static void AddAppSettings(IConfigurationBuilder configuration)
    {
        try
        {
                using var stream = FileSystem.OpenAppPackageFileAsync("appsettings.json").GetAwaiter().GetResult();
                using var memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);

                var appSettingsBytes = memoryStream.ToArray();
                configuration.AddJsonStream(new MemoryStream(appSettingsBytes));

            AppFileLog.Marker("APPSETTINGS_LOAD_OK");
                AppFileLog.Info(StartupLogTag, $"`appsettings.json` erfolgreich aus dem App-Paket geladen. sha256={ComputeSha256(appSettingsBytes)}");
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
            AppFileLog.Info(StartupLogTag, $"Supabase-Konfiguration vorhanden. {BuildSupabaseConfigurationFingerprint(supabaseUrl, supabaseKey)}");
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
        if (ex == null)
        {
            AppFileLog.Error(StartupLogTag, message);
        }
        else
        {
            AppFileLog.ErrorDetailed(StartupLogTag, message, ex);
        }

        Debug.WriteLine($"[{StartupLogTag}] {message}");

        if (ex is null)
        {
            return;
        }

        Debug.WriteLine(ex.ToString());
    }

    private static void RunStartupStep(string stepName, Action action)
    {
        AppFileLog.Marker($"STARTUP_STEP_BEGIN:{stepName}");

        try
        {
            action();
            AppFileLog.Marker($"STARTUP_STEP_OK:{stepName}");
        }
        catch (Exception ex)
        {
            AppFileLog.Marker($"STARTUP_STEP_FAIL:{stepName}");
            LogStartupError($"Startup-Schritt {stepName} ist fehlgeschlagen.", ex);
            throw;
        }
    }

    private static string BuildSupabaseConfigurationFingerprint(string? url, string? key)
    {
        var host = Uri.TryCreate(url, UriKind.Absolute, out var uri)
            ? uri.Host
            : "invalid";

        var trimmedKey = (key ?? string.Empty).Trim();
        var suffix = trimmedKey.Length >= 6
            ? trimmedKey[^6..]
            : trimmedKey;

        return $"host={host} keyLength={trimmedKey.Length} keySuffix={suffix} keySha256={ComputeSha256(Encoding.UTF8.GetBytes(trimmedKey))}";
    }

    private static string ComputeSha256(byte[] content)
    {
        if (content.Length == 0)
            return "empty";

        var hash = SHA256.HashData(content);
        return Convert.ToHexString(hash);
    }

}
