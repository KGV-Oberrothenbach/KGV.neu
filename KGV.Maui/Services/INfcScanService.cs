namespace KGV.Maui.Services;

public interface INfcScanService
{
    event EventHandler<string>? TagScanned;

    Task<NfcAvailabilityInfo> GetAvailabilityAsync();
    Task<NfcAvailabilityInfo> StartScanningAsync();
    Task StopScanningAsync();
    Task OpenSettingsAsync();
}
