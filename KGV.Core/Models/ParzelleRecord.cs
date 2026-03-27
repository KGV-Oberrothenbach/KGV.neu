// File: Core/Models/ParzelleRecord.cs
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Linq;

namespace KGV.Core.Models
{
    [Table("parzelle")]
    public class ParzelleRecord : BaseModel
    {
        [PrimaryKey("id", false)]
        [Column("id")]
        public int Id { get; set; }

        [Column("garten_nr")]
        public string GartenNr { get; set; } = string.Empty;

        [Column("Anlage")]
        public string Anlage { get; set; } = string.Empty;

        [Column("flaeche_qm")]
        public decimal? FlaecheQm { get; set; }

        [Column("hat_strom")]
        public bool HatStrom { get; set; }

        [Column("hat_wasser")]
        public bool HatWasser { get; set; }

        [Column("rfid_strom")]
        public string? RfidStrom { get; set; }

        [Column("rfid_wasser")]
        public string? RfidWasser { get; set; }

        [Column("aktiv")]
        public bool Aktiv { get; set; }

        [Column("is_demo")]
        public bool IsDemo { get; set; }

        public string Name
        {
            get => Anlage;
            set => Anlage = value;
        }

        public string DisplayName => string.IsNullOrWhiteSpace(Anlage) ? GartenNr : $"{GartenNr} - {Anlage}";
        public string StromRfidDisplay => string.IsNullOrWhiteSpace(RfidStrom) ? "Nicht hinterlegt" : RfidStrom.Trim();
        public string WasserRfidDisplay => string.IsNullOrWhiteSpace(RfidWasser) ? "Nicht hinterlegt" : RfidWasser.Trim();
        public string GartenNrSortKey => new string(GartenNr.Where(char.IsDigit).ToArray()).PadLeft(8, '0') + "|" + GartenNr;
    }
}
