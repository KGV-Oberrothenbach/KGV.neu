using Microsoft.Maui.Storage;

namespace KGV.Maui.Settings;

public static class PhotoUploadPreferences
{
    private const string WifiOnlyKey = "photo_upload_wifi_only";

    public static bool WifiOnly
    {
        get => Preferences.Default.Get(WifiOnlyKey, false);
        set => Preferences.Default.Set(WifiOnlyKey, value);
    }
}
