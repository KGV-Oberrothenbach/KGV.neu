using Android.App;
using Android.Content;
using Android.Nfc;
using Android.OS;
using Android.Provider;
using KGV.Maui.Services;
using Microsoft.Maui.ApplicationModel;

namespace KGV.Maui.Platforms.Android.Services;

public sealed class AndroidNfcScanService : Java.Lang.Object, INfcScanService, NfcAdapter.IReaderCallback
{
    private static readonly Context AppContext = global::Android.App.Application.Context;
    private NfcAdapter? _adapter;
    private Activity? _activity;
    private bool _isScanning;

    public event EventHandler<string>? TagScanned;

    public Task<NfcAvailabilityInfo> GetAvailabilityAsync()
    {
        var activity = Platform.CurrentActivity;
        var context = activity ?? AppContext;
        var adapter = NfcAdapter.GetDefaultAdapter(context);

        if (adapter == null)
        {
            return Task.FromResult(new NfcAvailabilityInfo(
                NfcAvailabilityState.NotSupported,
                "Dieses Gerät unterstützt kein NFC. Nutze den fachlichen Ersatzweg über Parzelle und Medium."));
        }

        if (!adapter.IsEnabled)
        {
            return Task.FromResult(new NfcAvailabilityInfo(
                NfcAvailabilityState.Disabled,
                "NFC ist auf dem Gerät vorhanden, aber aktuell deaktiviert. Aktiviere NFC oder nutze den fachlichen Ersatzweg über Parzelle und Medium."));
        }

        return Task.FromResult(new NfcAvailabilityInfo(
            NfcAvailabilityState.Available,
            "NFC ist aktiv. Halte den RFID-Tag an das Gerät."));
    }

    public async Task<NfcAvailabilityInfo> StartScanningAsync()
    {
        var availability = await GetAvailabilityAsync();
        if (availability.State != NfcAvailabilityState.Available)
            return availability;

        var activity = Platform.CurrentActivity;
        if (activity == null)
        {
            return new NfcAvailabilityInfo(
                NfcAvailabilityState.Unavailable,
                "Die NFC-Schnittstelle ist aktuell noch nicht bereit. Öffne die Seite erneut oder nutze den fachlichen Ersatzweg über Parzelle und Medium.");
        }

        _adapter ??= NfcAdapter.GetDefaultAdapter(activity);
        if (_adapter == null)
        {
            return new NfcAvailabilityInfo(
                NfcAvailabilityState.NotSupported,
                "Dieses Gerät unterstützt kein NFC. Nutze den fachlichen Ersatzweg über Parzelle und Medium.");
        }

        if (_isScanning && ReferenceEquals(_activity, activity))
            return availability;

        if (_isScanning && _activity != null)
            _adapter.DisableReaderMode(_activity);

        _activity = activity;
        _adapter.EnableReaderMode(
            activity,
            this,
            NfcReaderFlags.NfcA
                | NfcReaderFlags.NfcB
                | NfcReaderFlags.NfcF
                | NfcReaderFlags.NfcV
                | NfcReaderFlags.NfcBarcode
                | NfcReaderFlags.SkipNdefCheck
                | NfcReaderFlags.NoPlatformSounds,
            new Bundle());

        _isScanning = true;
        return availability;
    }

    public Task StopScanningAsync()
    {
        if (_isScanning && _adapter != null && _activity != null)
            _adapter.DisableReaderMode(_activity);

        _isScanning = false;
        _activity = null;
        return Task.CompletedTask;
    }

    public Task OpenSettingsAsync()
    {
        var intent = new Intent(global::Android.Provider.Settings.ActionNfcSettings);
        intent.AddFlags(ActivityFlags.NewTask);
        AppContext.StartActivity(intent);
        return Task.CompletedTask;
    }

    public void OnTagDiscovered(Tag? tag)
    {
        var uidBytes = tag?.GetId();
        if (uidBytes == null || uidBytes.Length == 0)
            return;

        var uid = BitConverter.ToString(uidBytes).Replace("-", string.Empty, StringComparison.Ordinal);
        if (string.IsNullOrWhiteSpace(uid))
            return;

        MainThread.BeginInvokeOnMainThread(() => TagScanned?.Invoke(this, uid));
    }
}
