using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace KGV.Core.Models
{
    [Table("impressum_funktion_slot")]
    public sealed class ImpressumFunktionSlotRecord : BaseModel
    {
        [PrimaryKey("id", false)]
        [Column("id")]
        public long Id { get; set; }

        [Column("slot_key")]
        public string SlotKey { get; set; } = string.Empty;

        [Column("funktion")]
        public string Funktion { get; set; } = string.Empty;

        [Column("sort_order")]
        public short SortOrder { get; set; }

        [Column("mitglied_id")]
        public long? MitgliedId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}
