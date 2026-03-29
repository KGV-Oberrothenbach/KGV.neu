using Android.App;
using Android.Media;
using Android.Net;
using Android.Provider;
using KGV.Maui.Services;
using AndroidApplication = Android.App.Application;

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
                var ringtone = RingtoneManager.GetRingtone(AndroidApplication.Context, uri);
                ringtone?.Play();
            }
        }
        catch
        {
        }

        return Task.CompletedTask;
    }
}
