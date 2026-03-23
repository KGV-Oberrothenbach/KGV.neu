using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Maui.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace KGV.Maui.Pages;

public sealed class AblesungErfassenPage : RfidScanWorkflowPage
{
    public AblesungErfassenPage()
        : base(
            "Ablesung erfassen",
            "RFID-UID eingeben oder scannen, produktiv über v_rfid_scan_context auflösen und den Ablese-Kontext prüfen.",
            "Einordnung für Ablesung",
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
            RfidScanContextState.KnownWithActiveMeter => "Aktiver Zähler gefunden. Der gemeinsame Ablese-Kontext ist damit produktiv vorbereitet.",
            RfidScanContextState.KnownWithoutActiveMeter => "Der Tag ist bekannt, aktuell aber ohne aktiven Zähler. Eine Ablesung ist damit noch nicht sinnvoll.",
            _ => "Der Tag ist unbekannt. Für die Ablesung kann kein produktiver Kontext vorbereitet werden."
        };
    }
}
