using KGV.Core.Models;

namespace KGV.Core.Utilities;

/// <summary>Canonical private-storage paths for one immutable template version.</summary>
public static class PdfTemplateCatalogContract
{
    public static string BuildPdfPath(string templateKey, string version)
        => $"templates/{templateKey?.Trim()}/{version?.Trim()}/template.pdf";

    public static string BuildFieldSpecPath(string templateKey, string version)
        => $"templates/{templateKey?.Trim()}/{version?.Trim()}/field-spec.json";

    public static bool HasVersionedPaths(PdfTemplateCatalogRecord record, string requestedTemplateKey)
    {
        ArgumentNullException.ThrowIfNull(record);
        var templateKey = record.TemplateKey?.Trim() ?? string.Empty;
        var version = record.Version?.Trim() ?? string.Empty;
        return string.Equals(templateKey, requestedTemplateKey?.Trim(), StringComparison.Ordinal)
            && string.Equals(record.StoragePath?.Trim(), BuildPdfPath(templateKey, version), StringComparison.Ordinal)
            && string.Equals(record.FieldSpecPath?.Trim(), BuildFieldSpecPath(templateKey, version), StringComparison.Ordinal);
    }
}
