using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KGV.Core.Models;

[Table("ablesung")]
public sealed class AblesungInsertRecord : BaseModel
{
    [Column("zaehler_id")]
    public long ZaehlerId { get; set; }

    [Column("stand")]
    public decimal Stand { get; set; }

    [Column("zaehler_typ")]
    public short ZaehlerTyp { get; set; }

    [Column("ablesedatum")]
    public DateTime Ablesedatum { get; set; }

    [Column("freigegeben")]
    public bool Freigegeben { get; set; }

    [Column("foto_pfad")]
    public string? FotoPfad { get; set; }
}