using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KGV.Core.Models
{
    [Table("saison")]
    public class SaisonRecord : BaseModel
    {
        [PrimaryKey("id")]
        [Column("id")]
        public int Id { get; set; }

        [Column("jahr")]
        public int Jahr { get; set; }

        [Column("pflichtstunden_soll")]
        public decimal PflichtstundenSoll { get; set; }

        [Column("euro_pro_fehlstunde")]
        public decimal EuroProFehlstunde { get; set; }

        [Column("bemerkung")]
        public string? Bemerkung { get; set; }

        [Column("pacht_pro_qm")]
        public decimal? PachtProQm { get; set; }

        [Column("mitgliedsbeitrag")]
        public decimal? Mitgliedsbeitrag { get; set; }

        [Column("mitgliedsbeitrag_nebenmitglied")]
        public decimal? MitgliedsbeitragNebenmitglied { get; set; }

        [Column("aufnahmegebuehr")]
        public decimal? Aufnahmegebuehr { get; set; }

        [Column("gebuehr_bauantrag")]
        public decimal? GebuehrBauantrag { get; set; }
    }
}
