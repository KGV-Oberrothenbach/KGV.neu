using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Maui.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;

namespace KGV.Maui.Pages;

public sealed class AblesungErfassenPage : RfidScanWorkflowPage
{
    public AblesungErfassenPage()
        : base(
            "Ablesung erfassen",
            "RFID-Tag an das Gerät halten, produktiv über den bestehenden Kontextpfad auflösen und den Ablese-Kontext prüfen.",
            "Einordnung für Ablesung",
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
            RfidScanContextState.KnownWithActiveMeter => "Aktiver Zähler gefunden. Der gemeinsame Ablese-Kontext ist damit produktiv vorbereitet.",
            RfidScanContextState.KnownWithoutActiveMeter => "Der Tag ist bekannt, aktuell aber ohne aktiven Zähler. Eine Ablesung ist damit noch nicht sinnvoll.",
            _ => "Der Tag ist unbekannt. Für die Ablesung kann kein produktiver Kontext vorbereitet werden."
        };
    }
}
