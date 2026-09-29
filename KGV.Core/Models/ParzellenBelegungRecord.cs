// File: Core/Models/ParzellenBelegungRecord.cs
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace KGV.Core.Models
{
    /// <summary>
    /// DB-Record für Parzellen-Belegungen (Supabase / PostgREST).
    /// </summary>
    [Table("parzellen_belegung")]
    public class ParzellenBelegungRecord : BaseModel
    {
        [PrimaryKey("id", false)]
        [Column("id")]
        public int Id { get; set; }

        [Column("parzelle_id")]
        public int ParzelleId { get; set; }

        [Column("mitglied_id")]
        public int MitgliedId { get; set; }

        [Column("von_datum")]
        public DateTime? VonDatum { get; set; }

        [Column("bis_datum")]
        public DateTime? BisDatum { get; set; }

        /// <summary>
        /// Fachlicher Grund für das Ende der Belegung, etwa <c>tod</c> oder
        /// <c>kuendigung</c>. Der Wert ist für die Abschlussberechnung relevant.
        /// </summary>
        [Column("beendigungsgrund")]
        public string? Beendigungsgrund { get; set; }
    }
}
