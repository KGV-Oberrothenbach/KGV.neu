using System;

namespace KGV.Core.Models
{
    public sealed class NebenmitgliedCreateDTO
    {
        public int HauptmitgliedId { get; set; }
        public string Vorname { get; set; } = string.Empty;
        public string Nachname { get; set; } = string.Empty;
        public bool AdresseUebernehmen { get; set; }
        public string? Telefon { get; set; }
        public string? Handy { get; set; }
        public string? Adresse { get; set; }
        public string? Plz { get; set; }
        public string? Ort { get; set; }
        public string? Email { get; set; }
        public DateTime? Geburtsdatum { get; set; }
        public DateTime? MitgliedSeit { get; set; }
        public bool WhatsappEinwilligung { get; set; }
    }
}
