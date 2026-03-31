using Microsoft.Maui.Storage;

namespace KGV.Maui.Services.PendingPhotos;

public static class PendingPhotoStorage
{
    public static string GetPendingRootDirectory()
    {
        var root = Path.Combine(FileSystem.AppDataDirectory, "pending", "photos");
        Directory.CreateDirectory(root);
        return root;
    }

    public static string GetPendingFilePath(string fileName)
    {
        fileName = Path.GetFileName(fileName);
        return Path.Combine(GetPendingRootDirectory(), fileName);
    }
}
