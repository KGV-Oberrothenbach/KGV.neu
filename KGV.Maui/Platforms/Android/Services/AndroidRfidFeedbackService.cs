using Android.App;
using Android.Media;
using Android.Net;
using Android.Provider;
using KGV.Maui.Services;

namespace KGV.Maui.Platforms.Android.Services;

public sealed class AndroidRfidFeedbackService : IRfidFeedbackService
{
    public Task PlaySuccessAsync()
    {
        try
        {
            var uri = global::Android.Provider.Settings.System.DefaultNotificationUri
                      ?? global::Android.Provider.Settings.System.DefaultRingtoneUri;

            if (uri != null)
            {
                var ringtone = RingtoneManager.GetRingtone(Application.Context, uri);
                ringtone?.Play();
            }
        }
        catch
        {
        }

        return Task.CompletedTask;
    }
}
