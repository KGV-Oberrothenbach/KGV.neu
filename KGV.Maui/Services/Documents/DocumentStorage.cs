using Microsoft.Maui.Storage;

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
}
