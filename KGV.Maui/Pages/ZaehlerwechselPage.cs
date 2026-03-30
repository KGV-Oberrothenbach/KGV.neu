using KGV.Core.Interfaces;
using KGV.Core.Models;
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
            "RFID-Tag an das Gerät halten. Der produktive 3-Fall-Flow öffnet danach direkt RFID hinzufügen, Zählereinbau oder nach Bestätigung den Ausbaupfad.",
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
                    await OpenRfidHinzufuegenAsync(_viewModel.Resolution.NormalizedUid);
                    break;

                case RfidScanContextState.KnownWithoutActiveMeter:
                    await ContinueToWorkflowAsync(_viewModel.Resolution, nameof(ZaehlerwechselEinbauPage));
                    break;

                case RfidScanContextState.KnownWithActiveMeter:
                    var confirmRemoval = await DisplayAlert(
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

    private static async Task OpenRfidHinzufuegenAsync(string? normalizedUid)
    {
        var route = nameof(RfidEinrichtenPage);
        if (!string.IsNullOrWhiteSpace(normalizedUid))
            route += $"?uid={Uri.EscapeDataString(normalizedUid)}";

        await Shell.Current.GoToAsync(route);
    }
}
