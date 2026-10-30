using System.Collections.Generic;

namespace KGV.Core.Models;

public sealed class SerienbriefExportContext
{
    public string ExportKey { get; init; } = string.Empty;
    public string ExportTitel { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, object?> Filter { get; init; } = new Dictionary<string, object?>();
    public IReadOnlyList<SerienbriefExportFeld> Felder { get; init; } = [];
    public IReadOnlyList<SerienbriefExportDatensatz> Datensaetze { get; init; } = [];
}
public sealed class SerienbriefExportFeld { public string Schluessel { get; init; } = string.Empty; public string Anzeigename { get; init; } = string.Empty; public string? Gruppe { get; init; } }
public sealed class SerienbriefExportDatensatz { public long? MitgliedId { get; init; } public long? ParzelleId { get; init; } public string EmpfaengerStatus { get; init; } = string.Empty; public IReadOnlyDictionary<string, object?> Werte { get; init; } = new Dictionary<string, object?>(); }
