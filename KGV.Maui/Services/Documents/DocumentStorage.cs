using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel;

namespace KGV.Maui.Services.Documents;

public static class DocumentStorage
{
    public static string GetPersistentRootDirectory()
    {
        var root = Path.Combine(FileSystem.AppDataDirectory, "documents", "persistent");
        Directory.CreateDirectory(root);
        return root;
    }

    public static string GetPersistentFilePath(string fileName)
    {
        fileName = Path.GetFileName(fileName);
        return Path.Combine(GetPersistentRootDirectory(), fileName);
    }

    public static async Task<bool> OpenTemporaryFileAsync(string? fileName, string? mimeType, byte[] content)
    {
        if (content is not { Length: > 0 })
            return false;

        var safeFileName = Path.GetFileName(string.IsNullOrWhiteSpace(fileName) ? "dokument" : fileName);
        if (string.IsNullOrWhiteSpace(safeFileName))
            safeFileName = "dokument";

        var directory = Path.Combine(FileSystem.CacheDirectory, "documents");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"{Guid.NewGuid():N}-{safeFileName}");
        await File.WriteAllBytesAsync(path, content);

        return await Launcher.Default.OpenAsync(new OpenFileRequest(
            safeFileName,
            new ReadOnlyFile(path, string.IsNullOrWhiteSpace(mimeType) ? "application/octet-stream" : mimeType)));
    }
}
