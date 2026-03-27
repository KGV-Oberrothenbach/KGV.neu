using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KGV.Core.Models;

[Table("stromzaehler")]
public sealed class StromzaehlerInsertRecord : BaseModel
{
    [Column("parzelle_id")]
    public long ParzelleId { get; set; }

    [Column("zaehlernummer")]
    public string Zaehlernummer { get; set; } = string.Empty;

    [Column("eichdatum")]
    public DateTime Eichdatum { get; set; }

    [Column("eingebaut_am")]
    public DateTime EingebautAm { get; set; }
}