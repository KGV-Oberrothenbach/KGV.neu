using KGV.Maui.Settings;
using Microsoft.Maui.Networking;

namespace KGV.Maui.Services.PendingPhotos;

public static class PendingPhotoUploadDecision
{
    private const string PendingNetworkCheckReason = "Netzwerkverbindung wird noch geprüft. Foto bleibt lokal gespeichert und wird später erneut versucht.";
    private const string PendingWifiCheckReason = "WLAN-Verbindung wird noch geprüft. Foto bleibt lokal gespeichert und wird später erneut versucht.";

    public static bool CanUploadNow(out string reason)
    {
        reason = string.Empty;

        try
        {
            var connectivity = Connectivity.Current;
            var networkAccess = connectivity.NetworkAccess;

            if (networkAccess != NetworkAccess.Internet)
            {
                reason = networkAccess == NetworkAccess.Unknown
                    ? PendingNetworkCheckReason
                    : "Aktuell keine Internetverbindung.";
                return false;
            }

            if (!PhotoUploadPreferences.WifiOnly)
                return true;

            var profiles = connectivity.ConnectionProfiles?.ToArray() ?? [];
            if (profiles.Contains(ConnectionProfile.WiFi))
                return true;

            reason = profiles.Length == 0 || profiles.Contains(ConnectionProfile.Unknown)
                ? PendingWifiCheckReason
                : "Upload ist aktuell auf WLAN beschränkt.";
            return false;
        }
        catch
        {
            reason = PhotoUploadPreferences.WifiOnly
                ? PendingWifiCheckReason
                : PendingNetworkCheckReason;
            return false;
        }
    }
}
