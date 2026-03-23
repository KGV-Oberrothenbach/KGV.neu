using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Maui.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace KGV.Maui.Pages;

public sealed class ZaehlerwechselPage : RfidScanWorkflowPage
{
    public ZaehlerwechselPage()
        : base(
            "Zählerwechsel",
            "RFID-UID eingeben oder scannen, produktiv auflösen und daraus den Ausbau- oder Einbaupfad fachlich ableiten.",
            "Einordnung für Zählerwechsel",
            CreateViewModel(),
            GetDecisionText)
    {
    }

    private static RfidScanContextViewModel CreateViewModel()
    {
        var services = IPlatformApplication.Current?.Services
            ?? throw new InvalidOperationException("MAUI-Services sind aktuell nicht verfügbar.");

        return new RfidScanContextViewModel(
            services.GetRequiredService<ISupabaseService>(),
            services.GetRequiredService<IAuthService>());
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
}
