using System;
namespace KGV.Core.Models;
public sealed record SerienbriefPdfRenderJob(string RowKey,long MitgliedId,long? ParzelleId,string VorlagenName,string Betreff,string Briefanrede,string HtmlInhalt,string Vorname,string Nachname,string Strasse,string Plz,string Ort,string Datum,string DateinameKontext);
public sealed record SerienbriefPdfResult(string RowKey,long MitgliedId,long? ParzelleId,string Dateiname,byte[]? PdfBytes,string? Fehler);
