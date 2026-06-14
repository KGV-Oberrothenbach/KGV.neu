using System;
using System.IO;
using System.Threading.Tasks;
using KGV.Core.Models;

namespace KGV.Maui.Services.Documents
{
    public sealed class LocalDocumentStatus
    {
        public string LocalPath { get; init; } = string.Empty;
        public bool Exists { get; init; }
        public DateTime? LastModified { get; init; }
        public bool IsUploaded { get; init; }
    }

    public static class LocalDocumentService
    {
        public static string GetLocalPath(string fileName)
        {
            return DocumentStorage.GetPersistentFilePath(fileName);
        }

        public static LocalDocumentStatus GetStatus(DocumentInfo document)
        {
            var fileName = string.IsNullOrWhiteSpace(document?.Dateiname) ? document?.Name ?? document?.Title ?? string.Empty : document.Dateiname;
            var path = GetLocalPath(fileName ?? string.Empty);
            if (string.IsNullOrWhiteSpace(path))
                return new LocalDocumentStatus { LocalPath = string.Empty, Exists = false, IsUploaded = !string.IsNullOrWhiteSpace(document?.StoragePath) };

            var exists = File.Exists(path);
            DateTime? lastModified = null;
            if (exists)
            {
                try { lastModified = File.GetLastWriteTimeUtc(path); } catch { lastModified = null; }
            }

            // IsUploaded is best-effort: if DocumentInfo.StoragePath is set and looks like a remote ref, consider uploaded
            var isUploaded = !string.IsNullOrWhiteSpace(document?.StoragePath) && !Path.IsPathRooted(document.StoragePath);

            return new LocalDocumentStatus
            {
                LocalPath = path,
                Exists = exists,
                LastModified = lastModified,
                IsUploaded = isUploaded
            };
        }

        public static async Task<string> SavePersistentCopyAsync(byte[] content, string fileName)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentNullException(nameof(fileName));

            var path = GetLocalPath(fileName);
            var dir = Path.GetDirectoryName(path) ?? DocumentStorage.GetPersistentRootDirectory();
            Directory.CreateDirectory(dir);
            await File.WriteAllBytesAsync(path, content);
            return path;
        }
    }
}
