using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KGV.Core.Models;

[Table("fachnummer_reservierung")]
public sealed class FachnummerReservierungRecord : BaseModel
{
    [Column("is_demo")] public bool IsDemo { get; set; }
    [Column("nummer")] public int Nummer { get; set; }
    [Column("art")] public string Art { get; set; } = string.Empty;
    [Column("quelle_art")] public string QuelleArt { get; set; } = string.Empty;
    [Column("quelle_id")] public long QuelleId { get; set; }
}
