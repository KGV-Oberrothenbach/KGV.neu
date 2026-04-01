using Android.Media;
using KGV.Maui.Services;
using AndroidStream = global::Android.Media.Stream;

namespace KGV.Maui.Platforms.Android.Services;

public sealed class AndroidRfidFeedbackService : IRfidFeedbackService
{
    public Task PlaySuccessAsync()
    {
        try
        {
            using var toneGenerator = new ToneGenerator(AndroidStream.Notification, 80);
            toneGenerator.StartTone(Tone.PropBeep, 150);
        }
        catch
        {
        }

        return Task.CompletedTask;
    }
}
