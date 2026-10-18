namespace KGV.Core.Models;

/// <summary>
/// Das exakt anhand seiner Datenbank-ID geladene Dokument einschließlich seines Inhalts.
/// </summary>
public sealed record LoadedDocument(DocumentInfo Document, byte[] Content);
