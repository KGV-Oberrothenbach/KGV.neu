namespace KGV.Core.Models;

/// <summary>
/// Document-owned compatibility requirements for a catalog PDF template.
/// </summary>
public sealed record PdfTemplateRequest(
    string TemplateKey,
    string ExpectedFieldSpecVersion,
    string ExpectedRendererVersion)
{
    public IReadOnlyList<string> AdditionalFieldSpecVersions { get; init; } = Array.Empty<string>();

    public bool SupportsFieldSpecVersion(string version) =>
        string.Equals(ExpectedFieldSpecVersion, version, StringComparison.Ordinal) ||
        AdditionalFieldSpecVersions.Contains(version, StringComparer.Ordinal);
}
