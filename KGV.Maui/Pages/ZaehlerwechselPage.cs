using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Maui.State;
using KGV.Maui.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;

namespace KGV.Maui.Pages;

public sealed class ZaehlerwechselPage : RfidScanWorkflowPage
{
    private readonly RfidScanContextViewModel _viewModel;
    private bool _handlingResolution;

    public ZaehlerwechselPage()
        : base(
            "Zählerwechsel",
            "RFID beschleunigt den Einstieg. Alternativ kann der Kontext über Parzelle und Medium geladen werden; beide Wege führen zum selben Zählerwechsel.",
            "Einordnung für Zählerwechsel",
            CreateViewModel(),
            GetDecisionText)
    {
        _viewModel = (RfidScanContextViewModel)BindingContext;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private static RfidScanContextViewModel CreateViewModel()
    {
        var services = Application.Current?.Handler?.MauiContext?.Services
            ?? throw new InvalidOperationException("MAUI-Services sind aktuell nicht verfügbar.");

        return new RfidScanContextViewModel(
            services.GetRequiredService<ISupabaseService>(),
            services.GetRequiredService<UserContextState>(),
            services.GetRequiredService<KGV.Maui.Services.INfcScanService>(),
            services.GetRequiredService<KGV.Maui.Services.IRfidFeedbackService>(),
            PermissionFlags.CanManageMeterChanges);
    }

    private static string GetDecisionText(RfidScanContextResult? resolution)
    {
        if (resolution == null)
            return "Noch kein RFID-Kontext geladen.";

        return resolution.State switch
        {
            RfidScanContextState.KnownWithActiveMeter => "Aktiver Zähler gefunden. Vor dem Ausbau wird eine Bestätigung angezeigt.",
            RfidScanContextState.KnownWithoutActiveMeter => "Bekannter Tag ohne aktiven Zähler. Der Zählereinbau wird direkt geöffnet.",
            _ => "Der Tag ist unbekannt. Der bestehende Flow `RFID hinzufügen` wird direkt geöffnet."
        };
    }

    protected override void OnAppearing()
    {
        _handlingResolution = false;
        base.OnAppearing();
    }

    protected override void OnDisappearing()
    {
        _handlingResolution = false;
        base.OnDisappearing();
    }

    private async void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(RfidScanContextViewModel.Resolution) || _handlingResolution || _viewModel.Resolution == null)
            return;

        _handlingResolution = true;

        try
        {
            switch (_viewModel.Resolution.State)
            {
                case RfidScanContextState.Unknown:
                    var hauptzaehlerResolution = await TryResolveHauptzaehlerAsync(_viewModel.Resolution.NormalizedUid);
                    if (hauptzaehlerResolution == null)
                    {
                        await OpenRfidHinzufuegenAsync(_viewModel.Resolution.NormalizedUid);
                        break;
                    }

                    var confirmHauptzaehlerRemoval = await DisplayAlertAsync(
                        "Bestätigung",
                        "Wollen Sie den Hauptzähler ausbauen?",
                        "Ja",
                        "Nein");

                    if (confirmHauptzaehlerRemoval)
                        await ContinueToWorkflowAsync(hauptzaehlerResolution, nameof(ZaehlerwechselAusbauPage));
                    break;

                case RfidScanContextState.KnownWithoutActiveMeter:
                    await ContinueToWorkflowAsync(_viewModel.Resolution, nameof(ZaehlerwechselEinbauPage));
                    break;

                case RfidScanContextState.KnownWithActiveMeter:
                    var confirmRemoval = await DisplayAlertAsync(
                        "Bestätigung",
                        "Wollen Sie den Zähler ausbauen?",
                        "Ja",
                        "Nein");

                    if (confirmRemoval)
                        await ContinueToWorkflowAsync(_viewModel.Resolution, nameof(ZaehlerwechselAusbauPage));

                    break;
            }
        }
        finally
        {
            _handlingResolution = false;
        }
    }

    private static async Task ContinueToWorkflowAsync(RfidScanContextResult resolution, string route)
    {
        var services = Application.Current?.Handler?.MauiContext?.Services
            ?? throw new InvalidOperationException("MAUI-Services sind aktuell nicht verfügbar.");

        services.GetRequiredService<ZaehlerwechselWorkflowState>().SetContext(resolution);
        await Shell.Current.GoToAsync(route);
    }

    /// <summary>
    /// Hauptzähler-Tags gehören zur Messstelle und deshalb nicht zur bestehenden
    /// Parzellen-RFID-Auflösung. Diese Ergänzung liest nur die Messstelle; sie
    /// erzeugt oder ändert keine RFID-Zuordnung.
    /// </summary>
    private static async Task<RfidScanContextResult?> TryResolveHauptzaehlerAsync(string? normalizedUid)
    {
        if (string.IsNullOrWhiteSpace(normalizedUid))
            return null;

        var services = Application.Current?.Handler?.MauiContext?.Services
            ?? throw new InvalidOperationException("MAUI-Services sind aktuell nicht verfügbar.");
        var supabaseService = services.GetRequiredService<ISupabaseService>();
        var messstelle = await supabaseService.GetHauptzaehlerMessstelleByRfidAsync(normalizedUid);
        if (messstelle == null)
            return null;

        var meter = await supabaseService.GetActiveHauptzaehlerForMessstelleAsync(
            messstelle.Medium,
            messstelle.Anlagenbereich,
            DateTime.Today);
        if (meter == null)
            return null;

        return new RfidScanContextResult
        {
            NormalizedUid = normalizedUid,
            State = RfidScanContextState.KnownWithActiveMeter,
            Message = "Hauptzähler-Messstelle mit aktivem Hauptzähler gefunden.",
            Context = new RfidScanContextRecord
            {
                ParzelleId = null,
                IstHauptzaehler = true,
                Anlagenbereich = messstelle.Anlagenbereich,
                Medium = messstelle.Medium,
                RfidTagUid = normalizedUid,
                AktiverZaehlerId = checked((int)meter.Id),
                Zaehlernummer = meter.Zaehlernummer,
                EingebautAm = meter.EingebautAm,
                Status = "aktiv"
            }
        };
    }

    private static async Task OpenRfidHinzufuegenAsync(string? normalizedUid)
    {
        var route = nameof(RfidEinrichtenPage);
        if (!string.IsNullOrWhiteSpace(normalizedUid))
            route += $"?uid={Uri.EscapeDataString(normalizedUid)}";

        await Shell.Current.GoToAsync(route);
    }
}
