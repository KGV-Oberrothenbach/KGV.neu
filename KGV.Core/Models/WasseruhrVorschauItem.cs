using System;

namespace KGV.Core.Models
{
    public sealed class WasseruhrVorschauItem
    {
        public int ParzelleId { get; set; }
        public string GartenNr { get; set; } = string.Empty;
        public string Zaehlernummer { get; set; } = string.Empty;
        public DateTime? Eichdatum { get; set; }
        public DateTime? WechselBis { get; set; }
        public int TageBisWechsel { get; set; }
        public string HinweisText { get; set; } = string.Empty;
    }
}
