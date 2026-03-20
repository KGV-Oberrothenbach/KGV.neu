namespace KGV.Core.Models
{
    public sealed class RfidScanContextRecord
    {
        public int Id { get; set; }
        public string Kontext { get; set; } = string.Empty;
        public string? Beschreibung { get; set; }
        public string? Zieltyp { get; set; }
        public bool Aktiv { get; set; }
    }
}
