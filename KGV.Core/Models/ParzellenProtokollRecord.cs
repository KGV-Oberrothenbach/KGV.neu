using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KGV.Core.Models;

[Table("parzellen_protokoll")]
public sealed class ParzellenProtokollRecord : BaseModel
{
    [PrimaryKey("id", false)] public long Id { get; set; }
    [Column("parzelle_id")] public long ParzelleId { get; set; }
    [Column("mitglied_id")] public long MitgliedId { get; set; }
    [Column("protokoll_typ")] public string ProtokollTyp { get; set; } = string.Empty;
    [Column("protokoll_datum")] public DateTime ProtokollDatum { get; set; }
    [Column("vorstand_mitglied_id")] public long VorstandMitgliedId { get; set; }
    [Column("vorstand2_mitglied_id")] public long Vorstand2MitgliedId { get; set; }
    [Column("begleitperson_mitglied_id")] public long? BegleitpersonMitgliedId { get; set; }
    [Column("begleitperson_name")] public string? BegleitpersonName { get; set; }
    [Column("zustand_bemerkung")] public string? ZustandBemerkung { get; set; }
    [Column("vereinbarung")] public string? Vereinbarung { get; set; }
    [Column("dokument_id")] public long? DokumentId { get; set; }
    [Column("status")] public string Status { get; set; } = "entwurf";
}

[Table("parzellen_protokoll")]
public sealed class ParzellenProtokollInsertRecord : BaseModel
{
    [Column("parzelle_id")] public long ParzelleId { get; set; }
    [Column("mitglied_id")] public long MitgliedId { get; set; }
    [Column("protokoll_typ")] public string ProtokollTyp { get; set; } = string.Empty;
    [Column("protokoll_datum")] public DateTime ProtokollDatum { get; set; }
    [Column("status")] public string Status { get; set; } = "entwurf";
    [Column("vorstand_mitglied_id")] public long VorstandMitgliedId { get; set; }
    [Column("vorstand2_mitglied_id")] public long Vorstand2MitgliedId { get; set; }
    [Column("begleitperson_mitglied_id")] public long? BegleitpersonMitgliedId { get; set; }
    [Column("begleitperson_name")] public string? BegleitpersonName { get; set; }
    [Column("anlass")] public string? Anlass { get; set; }
    [Column("zustand_bemerkung")] public string? ZustandBemerkung { get; set; }
    [Column("vereinbarung")] public string? Vereinbarung { get; set; }
    [Column("paechter_signiert_am")] public DateTime? PaechterSigniertAm { get; set; }
    [Column("begleitperson_signiert_am")] public DateTime? BegleitpersonSigniertAm { get; set; }
    [Column("vorstand1_signiert_am")] public DateTime? Vorstand1SigniertAm { get; set; }
    [Column("vorstand2_signiert_am")] public DateTime? Vorstand2SigniertAm { get; set; }
    [Column("erstellt_von")] public Guid? ErstelltVon { get; set; }
    [Column("updated_at")] public DateTime UpdatedAt { get; set; }
}

[Table("parzellen_protokoll_ablesung")]
public sealed class ParzellenProtokollAblesungInsertRecord : BaseModel
{
    [Column("protokoll_id")] public long ProtokollId { get; set; }
    [Column("medium")] public string Medium { get; set; } = string.Empty;
    [Column("zaehler_id")] public long? ZaehlerId { get; set; }
    [Column("ablesung_id")] public long? AblesungId { get; set; }
    [Column("quelle")] public string Quelle { get; set; } = string.Empty;
    [Column("ablesedatum")] public DateTime Ablesedatum { get; set; }
    [Column("zaehlernummer")] public string? Zaehlernummer { get; set; }
    [Column("stand")] public decimal Stand { get; set; }
    [Column("foto_pfad")] public string? FotoPfad { get; set; }
    [Column("foto_dateiname")] public string? FotoDateiname { get; set; }
    [Column("foto_drive_file_id")] public string? FotoDriveFileId { get; set; }
}

public sealed class ParzellenProtokollCreateRequest
{
    public ParzellenProtokollInsertRecord Protokoll { get; set; } = new();
    public IReadOnlyList<ParzellenProtokollAblesungInsertRecord> Ablesungen { get; set; } = Array.Empty<ParzellenProtokollAblesungInsertRecord>();
}

public sealed class ParzellenProtokollSaveResult
{
    public bool Success { get; init; }
    public long ProtokollId { get; init; }
    public string Message { get; init; } = string.Empty;

    public static ParzellenProtokollSaveResult Ok(long protokollId)
        => new() { Success = true, ProtokollId = protokollId, Message = "Protokoll-Entwurf wurde gespeichert." };

    public static ParzellenProtokollSaveResult Fail(string message)
        => new() { Success = false, Message = message };
}

public sealed class ParzellenProtokollPdfRequest
{
    public string FormularTitel { get; set; } = string.Empty;
    public DateTime ProtokollDatum { get; set; }
    public MitgliedRecord Mitglied { get; set; } = new();
    public ParzelleRecord Parzelle { get; set; } = new();
    public MitgliedRecord Vorstand1 { get; set; } = new();
    public MitgliedRecord Vorstand2 { get; set; } = new();
    public string? Begleitperson { get; set; }
    public string? Anlass { get; set; }
    public string? ZustandBemerkung { get; set; }
    public string? Vereinbarung { get; set; }
    public IReadOnlyList<ParzellenProtokollAblesungInsertRecord> Ablesungen { get; set; } = Array.Empty<ParzellenProtokollAblesungInsertRecord>();
    public IReadOnlyList<ParzellenProtokollFoto> Fotos { get; set; } = Array.Empty<ParzellenProtokollFoto>();
    public DigitalSignatureCapture? PaechterSignatur { get; set; }
    public DigitalSignatureCapture? BegleitpersonSignatur { get; set; }
    public DigitalSignatureCapture? Vorstand1Signatur { get; set; }
    public DigitalSignatureCapture? Vorstand2Signatur { get; set; }
}

public sealed class ParzellenProtokollFoto
{
    public string Dateiname { get; set; } = string.Empty;
    public byte[] Inhalt { get; set; } = Array.Empty<byte>();
}
