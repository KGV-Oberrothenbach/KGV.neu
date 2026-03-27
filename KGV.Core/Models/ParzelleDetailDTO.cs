using System;
using System.Collections.Generic;
using System.Linq;

namespace KGV.Core.Models
{
    public sealed class ParzelleDetailDTO
    {
        public int ParzelleId { get; set; }
        public int? BelegungId { get; set; }
        public string GartenNr { get; set; } = string.Empty;
        public string Anlage { get; set; } = string.Empty;
        public decimal? FlaecheQm { get; set; }
        public bool HatStrom { get; set; }
        public bool HatWasser { get; set; }
        public string? RfidStrom { get; set; }
        public string? RfidWasser { get; set; }
        public bool IstVergeben { get; set; }
        public int? MitgliedId { get; set; }
        public string MitgliedName { get; set; } = string.Empty;
        public string MitgliedEmail { get; set; } = string.Empty;
        public DateTime? VonDatum { get; set; }
        public DateTime? BisDatum { get; set; }
        public StromzaehlerRecord? AktiverStromzaehler { get; set; }
        public WasserzaehlerRecord? AktiverWasserzaehler { get; set; }
        public int StromAblesungenCount { get; set; }
        public int WasserAblesungenCount { get; set; }
        public List<DocumentInfo> Dokumente { get; set; } = new();

        public string DisplayName => string.IsNullOrWhiteSpace(Anlage) ? GartenNr : $"{GartenNr} - {Anlage}";
        public string FlaecheText => FlaecheQm.HasValue ? $"{FlaecheQm.Value:0.##} m²" : "Nicht hinterlegt";
        public string HatStromText => HatStrom ? "Ja" : "Nein";
        public string HatWasserText => HatWasser ? "Ja" : "Nein";
        public string RfidStromText => string.IsNullOrWhiteSpace(RfidStrom) ? "Nicht hinterlegt" : RfidStrom.Trim();
        public string RfidWasserText => string.IsNullOrWhiteSpace(RfidWasser) ? "Nicht hinterlegt" : RfidWasser.Trim();
        public string StatusText => IstVergeben ? "vergeben" : "frei";
        public string MitgliedDisplayText => string.IsNullOrWhiteSpace(MitgliedName) ? "Kein Mitglied zugeordnet." : MitgliedName;
        public string MitgliedKontaktText => string.IsNullOrWhiteSpace(MitgliedEmail) ? "Keine E-Mail hinterlegt." : MitgliedEmail;
        public string BelegungText => !IstVergeben
            ? "Aktuell keiner Person zugeordnet."
            : $"Seit {(VonDatum.HasValue ? VonDatum.Value.ToString("dd.MM.yyyy") : "unbekannt")}{(BisDatum.HasValue ? $", bis {BisDatum.Value:dd.MM.yyyy}" : string.Empty)}";
        public string StromStatusText => FormatMeterStatus("Strom", AktiverStromzaehler?.Zaehlernummer, AktiverStromzaehler?.EingebautAm, StromAblesungenCount);
        public string WasserStatusText => FormatMeterStatus("Wasser", AktiverWasserzaehler?.Zaehlernummer, AktiverWasserzaehler?.EingebautAm, WasserAblesungenCount);
        public bool HasDokumente => Dokumente.Count > 0;
        public string DokumenteText => HasDokumente ? $"{Dokumente.Count} Dokument(e) vorhanden." : "Keine Dokumente zur Parzelle gefunden.";
        public List<DocumentInfo> DokumenteVorschau => Dokumente.Take(5).ToList();

        private static string FormatMeterStatus(string typ, string? nummer, DateTime? eingebautAm, int ablesungenCount)
        {
            if (string.IsNullOrWhiteSpace(nummer))
                return $"Kein aktiver {typ.ToLowerInvariant()}zähler. Ablesungen gesamt: {ablesungenCount}.";

            return $"Aktiver {typ.ToLowerInvariant()}zähler {nummer} seit {(eingebautAm.HasValue ? eingebautAm.Value.ToString("dd.MM.yyyy") : "unbekannt")}. Ablesungen gesamt: {ablesungenCount}.";
        }
    }
}
