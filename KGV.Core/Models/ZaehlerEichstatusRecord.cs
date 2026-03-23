using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Linq;

namespace KGV.Core.Models
{
    [Table("v_zaehler_eichstatus")]
    public sealed class ZaehlerEichstatusRecord : BaseModel
    {
        [PrimaryKey("id", false)]
        [Column("id")]
        public long Id { get; set; }

        [Column("parzelle_id")]
        public long ParzelleId { get; set; }

        [Column("anlage")]
        public string? Anlage { get; set; }

        [Column("garten_nr")]
        public string? GartenNr { get; set; }

        [Column("medium")]
        public string? Medium { get; set; }

        [Column("zaehlernummer")]
        public string? Zaehlernummer { get; set; }

        [Column("eichdatum")]
        public DateTime? Eichdatum { get; set; }

        [Column("eichfaellig_am")]
        public DateTime? EichfaelligAm { get; set; }

        [Column("eingebaut_am")]
        public DateTime? EingebautAm { get; set; }

        [Column("status")]
        public string? Status { get; set; }

        [Column("tage_bis_faellig")]
        public int? TageBisFaellig { get; set; }

        [Column("eichstatus")]
        public string? Eichstatus { get; set; }

        public string AnlageDisplay => string.IsNullOrWhiteSpace(Anlage) ? "—" : Anlage.Trim();
        public string GartenDisplay => string.IsNullOrWhiteSpace(GartenNr) ? "—" : GartenNr.Trim();
        public string MediumDisplay => NormalizeMedium(Medium) == "wasser" ? "Wasser" : "Strom";
        public string ZaehlerDisplay => string.IsNullOrWhiteSpace(Zaehlernummer) ? "—" : Zaehlernummer.Trim();
        public string EichdatumDisplay => Eichdatum?.ToString("dd.MM.yyyy") ?? "—";
        public string EichfaelligDisplay => EichfaelligAm?.ToString("dd.MM.yyyy") ?? "—";
        public string EichstatusDisplay => NormalizeEichstatus(Eichstatus) switch
        {
            "ueberfaellig" => "Überfällig",
            "bald_faellig" => "Bald fällig",
            _ => "OK"
        };
        public string TageDisplay => TageBisFaellig.HasValue ? TageBisFaellig.Value.ToString() : "—";
        public int SortPriority => NormalizeEichstatus(Eichstatus) switch
        {
            "ueberfaellig" => 0,
            "bald_faellig" => 1,
            _ => 2
        };
        public int SortDays => TageBisFaellig ?? int.MaxValue;
        public string GartenSortKey => new string((GartenNr ?? string.Empty).Where(char.IsDigit).ToArray()).PadLeft(8, '0') + "|" + (GartenNr ?? string.Empty);
        public string SearchText => string.Join(" ", new[]
        {
            AnlageDisplay,
            GartenDisplay,
            MediumDisplay,
            ZaehlerDisplay,
            EichdatumDisplay,
            EichfaelligDisplay,
            EichstatusDisplay,
            TageDisplay
        });

        private static string NormalizeMedium(string? medium)
        {
            if (string.IsNullOrWhiteSpace(medium))
                return "strom";

            var normalized = medium.Trim().ToLowerInvariant();
            return normalized is "wasser" or "strom" ? normalized : "strom";
        }

        private static string NormalizeEichstatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return "ok";

            var normalized = status.Trim().ToLowerInvariant();
            return normalized is "ueberfaellig" or "bald_faellig" ? normalized : "ok";
        }
    }
}
