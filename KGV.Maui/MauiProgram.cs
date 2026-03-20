using KGV.Core.Security;
using KGV.Infrastructure.DependencyInjection;
using KGV.Maui.Pages;
using KGV.Maui.Settings;
using KGV.Maui.State;
using KGV.Maui.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KGV.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>();

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

        builder.Services.AddKgvServices(builder.Configuration);

        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RoleChoicePage>();
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<MemberSearchViewModel>();
        builder.Services.AddTransient<MemberSearchPage>();
        builder.Services.AddTransient<MyProfilePage>();
        builder.Services.AddTransient<NebenmitgliedPage>();
        builder.Services.AddTransient<MyArbeitsstundenPage>();
        builder.Services.AddTransient<ArbeitsstundenReviewPage>();
        builder.Services.AddTransient<ExitPage>();

        builder.Services.AddSingleton<AdminShell>();
        builder.Services.AddSingleton<UserShell>();

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
