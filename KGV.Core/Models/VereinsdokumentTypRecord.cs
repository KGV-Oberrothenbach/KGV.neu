using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KGV.Core.Models;

[Table("vereinsdokument_typ")]
public sealed class VereinsdokumentTypRecord : BaseModel
{
    [PrimaryKey("code", false)]
    [Column("code")]
    public string Code { get; set; } = string.Empty;

    [Column("bezeichnung")]
    public string Bezeichnung { get; set; } = string.Empty;

    [Column("pflicht")]
    public bool Pflicht { get; set; }
}
