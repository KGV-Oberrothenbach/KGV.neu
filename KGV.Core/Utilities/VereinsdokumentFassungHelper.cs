using System.Security.Cryptography;
using KGV.Core.Models;

namespace KGV.Core.Utilities;

public static class VereinsdokumentFassungHelper
{
    public static VereinsdokumentFassungRecord? GetValidClubDocumentVersion(
        IEnumerable<VereinsdokumentFassungRecord>? versions, string documentType, DateTime date)
    {
        var matches = (versions ?? Array.Empty<VereinsdokumentFassungRecord>())
            .Where(x => string.Equals(x.DokumenttypCode, documentType, StringComparison.Ordinal)
                && x.GueltigAb.Date <= date.Date
                && (!x.GueltigBis.HasValue || x.GueltigBis.Value.Date >= date.Date))
            .OrderByDescending(x => x.GueltigAb)
            .ToList();
        if (matches.Count > 1) throw new InvalidOperationException($"Mehrere gültige Fassungen für {documentType} am {date:yyyy-MM-dd}.");
        return matches.SingleOrDefault();
    }

    public static DateTime GetPredecessorEnd(DateTime successorStart) => successorStart.Date.AddDays(-1);

    public static bool IsFuture(VereinsdokumentFassungRecord version, DateTime date) => version.GueltigAb.Date > date.Date;

    public static string CreateStoragePath(string documentType, string version, string fileName)
    {
        if (string.IsNullOrWhiteSpace(documentType) || string.IsNullOrWhiteSpace(version) || string.IsNullOrWhiteSpace(fileName))
            throw new InvalidOperationException("Dokumenttyp, Fassung und Dateiname sind erforderlich.");
        if (!fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Es sind ausschließlich PDF-Dateien zulässig.");
        if (new[] { documentType, version, fileName }.Any(x => x.Contains('/') || x.Contains('\\')))
            throw new InvalidOperationException("Dokumenttyp, Fassung und Dateiname dürfen keinen Pfadtrenner enthalten.");
        return $"vereinsunterlagen/{documentType}/{version}/{fileName}";
    }

    public static string ComputeSha256(byte[] content) => Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
}
