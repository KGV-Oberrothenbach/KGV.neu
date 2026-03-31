using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KGV.Core.Models;

[Table("zaehler")]
public sealed class ZaehlerInsertRecord : BaseModel
{
    [Column("parzelle_id")]
    public long ParzelleId { get; set; }

    [Column("medium")]
    public string Medium { get; set; } = string.Empty;

    [Column("zaehlernummer")]
    public string Zaehlernummer { get; set; } = string.Empty;

    [Column("eichdatum")]
    public DateTime Eichdatum { get; set; }

    [Column("eingebaut_am")]
    public DateTime EingebautAm { get; set; }

    [Column("einbau_foto_pfad")]
    public string? EinbauFotoPfad { get; set; }

    [Column("einbau_foto_dateiname")]
    public string? EinbauFotoDateiname { get; set; }

    [Column("einbau_foto_drive_file_id")]
    public string? EinbauFotoDriveFileId { get; set; }
}
