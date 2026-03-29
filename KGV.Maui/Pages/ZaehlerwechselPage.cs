using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Maui.State;
using KGV.Maui.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;

namespace KGV.Maui.Pages;

public sealed class ZaehlerwechselPage : RfidScanWorkflowPage
{
    public ZaehlerwechselPage()
        : base(
            "Zählerwechsel",
            "RFID-Tag an das Gerät halten, produktiv auflösen und daraus den Ausbau- oder Einbaupfad fachlich ableiten.",
            "Einordnung für Zählerwechsel",
            CreateViewModel(),
            GetDecisionText,
            CreateActionSection)
    {
    }

    private static RfidScanContextViewModel CreateViewModel()
    {
        var services = Application.Current?.Handler?.MauiContext?.Services
            ?? throw new InvalidOperationException("MAUI-Services sind aktuell nicht verfügbar.");

        return new RfidScanContextViewModel(
            services.GetRequiredService<ISupabaseService>(),
            services.GetRequiredService<IAuthService>(),
            services.GetRequiredService<KGV.Maui.Services.INfcScanService>(),
            services.GetRequiredService<KGV.Maui.Services.IRfidFeedbackService>());
    }

    private static string GetDecisionText(RfidScanContextResult? resolution)
    {
        if (resolution == null)
            return "Noch kein RFID-Kontext geladen.";

        return resolution.State switch
        {
            RfidScanContextState.KnownWithActiveMeter => "Aktiver Zähler gefunden. Für den Zählerwechsel ist damit als nächster Schritt der Ausbaupfad vorbereitet.",
            RfidScanContextState.KnownWithoutActiveMeter => "Bekannter Tag ohne aktiven Zähler. Für den Zählerwechsel ist damit als nächster Schritt der Einbaupfad vorbereitet.",
            _ => "Der Tag ist unbekannt. Für den Zählerwechsel kann kein produktiver Kontext vorbereitet werden."
        };
    }

    private static View CreateActionSection(RfidScanContextViewModel viewModel)
    {
        var continueToRemovalButton = new Button { Text = "Weiter zum Ausbau" };
        continueToRemovalButton.SetBinding(IsVisibleProperty, nameof(RfidScanContextViewModel.CanContinueToMeterRemoval));
        continueToRemovalButton.SetBinding(IsEnabledProperty, nameof(RfidScanContextViewModel.CanContinueToMeterRemoval));
        continueToRemovalButton.Clicked += async (_, _) => await ContinueAsync(viewModel, nameof(ZaehlerwechselAusbauPage));

        var continueToInstallationButton = new Button { Text = "Weiter zum Einbau" };
        continueToInstallationButton.SetBinding(IsVisibleProperty, nameof(RfidScanContextViewModel.CanContinueToMeterInstallation));
        continueToInstallationButton.SetBinding(IsEnabledProperty, nameof(RfidScanContextViewModel.CanContinueToMeterInstallation));
        continueToInstallationButton.Clicked += async (_, _) => await ContinueAsync(viewModel, nameof(ZaehlerwechselEinbauPage));

        return new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                continueToRemovalButton,
                continueToInstallationButton
            }
        };
    }

    private static async Task ContinueAsync(RfidScanContextViewModel viewModel, string route)
    {
        if (viewModel.Resolution == null)
            return;

        var services = Application.Current?.Handler?.MauiContext?.Services
            ?? throw new InvalidOperationException("MAUI-Services sind aktuell nicht verfügbar.");

        services.GetRequiredService<ZaehlerwechselWorkflowState>().SetContext(viewModel.Resolution);
        await Shell.Current.GoToAsync(route);
    }
}
