namespace KGV.Core.Models
{
    public sealed class ParzelleVerwaltungItem
    {
        public int ParzelleId { get; set; }
        public string GartenNr { get; set; } = string.Empty;
        public string Anlage { get; set; } = string.Empty;
        public int? MitgliedId { get; set; }
        public string MitgliedName { get; set; } = string.Empty;
        public bool IstVergeben { get; set; }
        public string StatusText { get; set; } = string.Empty;

        public string DisplayText
        {
            get
            {
                var basis = string.IsNullOrWhiteSpace(Anlage) ? GartenNr : $"{GartenNr} - {Anlage}";
                return string.IsNullOrWhiteSpace(StatusText) ? basis : $"{basis} ({StatusText})";
            }
        }
    }
}
