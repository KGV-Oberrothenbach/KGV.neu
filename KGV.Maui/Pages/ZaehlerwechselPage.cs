using KGV.Core.Interfaces;
using KGV.Core.Models;
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
            GetDecisionText)
    {
    }

    private static RfidScanContextViewModel CreateViewModel()
    {
        var services = Application.Current?.Handler?.MauiContext?.Services
            ?? throw new InvalidOperationException("MAUI-Services sind aktuell nicht verfügbar.");

        return new RfidScanContextViewModel(
            services.GetRequiredService<ISupabaseService>(),
            services.GetRequiredService<IAuthService>(),
            services.GetRequiredService<KGV.Maui.Services.INfcScanService>());
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
