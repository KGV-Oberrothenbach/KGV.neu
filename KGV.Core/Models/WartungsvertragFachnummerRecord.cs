using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KGV.Core.Models;

[Table("v_wartungsvertrag_fachnummer")]
public sealed class WartungsvertragFachnummerRecord : BaseModel
{
    [PrimaryKey("wartungsvertrag_id", false)]
    [Column("wartungsvertrag_id")]
    public long WartungsvertragId { get; set; }

    [Column("vertragsnummer")]
    public int? Vertragsnummer { get; set; }
}
