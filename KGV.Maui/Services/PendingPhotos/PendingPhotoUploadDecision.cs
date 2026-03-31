using KGV.Maui.Settings;
using Microsoft.Maui.Networking;

namespace KGV.Maui.Services.PendingPhotos;

public static class PendingPhotoUploadDecision
{
    public static bool CanUploadNow(out string reason)
    {
        reason = string.Empty;

        try
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                reason = "Aktuell keine Internetverbindung.";
                return false;
            }

            if (!PhotoUploadPreferences.WifiOnly)
                return true;

            var profiles = Connectivity.Current.ConnectionProfiles;
            if (profiles.Contains(ConnectionProfile.WiFi))
                return true;

            reason = "Upload ist aktuell auf WLAN beschränkt.";
            return false;
        }
        catch
        {
            reason = "Netzwerkstatus konnte nicht ermittelt werden.";
            return false;
        }
    }
}
