using Android.Content;
using AndroidX.Core.Content;

namespace KGV.Maui.Platforms.Android;

public static class FileOpener
{
    // Tries to open a file via an ACTION_VIEW chooser using FileProvider and
    // grants temporary read permission to the target app. Returns true on success.
    public static bool TryOpenPdfExternally(string filePath)
    {
        try
        {
            var context = global::Android.App.Application.Context;
            var file = new Java.IO.File(filePath);
            if (!file.Exists())
                return false;

            // authority must match the provider declared in AndroidManifest (microsoft.maui.essentials.fileProvider)
            var authority = "de.kgv.oberrothenbach.fileProvider";
            var uri = global::AndroidX.Core.Content.FileProvider.GetUriForFile(context, authority, file);

            var intent = new Intent(Intent.ActionView);
            intent.SetDataAndType(uri, "application/pdf");
            intent.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.NewTask);

            var chooser = Intent.CreateChooser(intent, "Öffnen mit");
            context.StartActivity(chooser);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
