using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KGV.Core.Models
{
    [Table("v_rfid_scan_context")]
    public sealed class RfidScanContextRecord : BaseModel
    {
        [Column("parzelle_id")]
        public int ParzelleId { get; set; }

        [Column("anlage")]
        public string? Anlage { get; set; }

        [Column("garten_nr")]
        public string? GartenNr { get; set; }

        [Column("medium")]
        public string? Medium { get; set; }

        [Column("rfid_tag_uid")]
        public string? RfidTagUid { get; set; }

        [Column("aktiver_zaehler_id")]
        public int? AktiverZaehlerId { get; set; }

        [Column("zaehlernummer")]
        public string? Zaehlernummer { get; set; }

        [Column("eichdatum")]
        public DateTime? Eichdatum { get; set; }

        [Column("eichfaellig_am")]
        public DateTime? EichfaelligAm { get; set; }

        [Column("eingebaut_am")]
        public DateTime? EingebautAm { get; set; }

        [Column("ausgebaut_am")]
        public DateTime? AusgebautAm { get; set; }

        [Column("status")]
        public string? Status { get; set; }

        public string ParzelleDisplayName => string.IsNullOrWhiteSpace(Anlage) ? GartenNr ?? string.Empty : $"{GartenNr} - {Anlage}";
    }
}
