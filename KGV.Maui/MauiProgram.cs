using KGV.Core.Security;
using KGV.Infrastructure.DependencyInjection;
using KGV.Maui.Pages;
using KGV.Maui.Platforms.Android.Services;
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

namespace KGV.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>();

        ShellRouteRegistrar.RegisterCommonRoutes();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Use the same appsettings.json as the WPF app.
        // For Android this must be packaged as an app asset (see `KGV.Maui.csproj`).
        TryAddAppSettings(builder.Configuration);

        AppSettings.Load();

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

    private static void TryAddAppSettings(IConfigurationBuilder configuration)
    {
        try
        {
            using var stream = FileSystem.OpenAppPackageFileAsync("appsettings.json").GetAwaiter().GetResult();
            configuration.AddJsonStream(stream);
        }
        catch
        {
        }
    }
}
