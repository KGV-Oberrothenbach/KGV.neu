using System.IO;
using KGV.ReleaseManager.Models;

namespace KGV.ReleaseManager.Services;

public sealed class ReleaseFolderService
{
    public ReleaseFolderPreparationResult PrepareVersionFolder(string rootPath, string version)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            return new ReleaseFolderPreparationResult
            {
                Message = "Basisordner für Veröffentlichungen ist nicht konfiguriert."
            };
        }

        if (string.IsNullOrWhiteSpace(version))
        {
            return new ReleaseFolderPreparationResult
            {
                Message = "Zielversion ist leer und kann nicht als Veröffentlichungsordner verwendet werden."
            };
        }

        var safeVersion = version.Trim();
        if (safeVersion.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            return new ReleaseFolderPreparationResult
            {
                Message = $"Zielversion enthält ungültige Zeichen: {safeVersion}"
            };
        }

        var folder = Path.Combine(rootPath, safeVersion);
        var existedBefore = Directory.Exists(folder);

        Directory.CreateDirectory(folder);
        Directory.CreateDirectory(Path.Combine(folder, "WPF"));
        Directory.CreateDirectory(Path.Combine(folder, "Android", "APK"));
        Directory.CreateDirectory(Path.Combine(folder, "Android", "AAB"));
        Directory.CreateDirectory(Path.Combine(folder, "Dokumentation"));

        return new ReleaseFolderPreparationResult
        {
            Success = true,
            ExistedBefore = existedBefore,
            VersionFolderPath = folder,
            Message = existedBefore
                ? $"Veröffentlichungsordner war bereits vorhanden und wurde geprüft: {folder}"
                : $"Veröffentlichungsordner vorbereitet: {folder}"
        };
    }
}
