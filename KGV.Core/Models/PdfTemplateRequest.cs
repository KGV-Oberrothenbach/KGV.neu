namespace KGV.Core.Models;

/// <summary>
/// Document-owned compatibility requirements for a catalog PDF template.
/// </summary>
public sealed record PdfTemplateRequest(
    string TemplateKey,
    string ExpectedFieldSpecVersion,
    string ExpectedRendererVersion);
