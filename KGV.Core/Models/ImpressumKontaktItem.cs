using System;

namespace KGV.Core.Models
{
    public sealed class ImpressumKontaktItem
    {
        public string Funktion { get; set; } = string.Empty;
        public string Name { get; set; } = "Aktuell nicht hinterlegt.";
        public string Email { get; set; } = string.Empty;
        public string Telefon { get; set; } = string.Empty;

        public bool HasEmail => !string.IsNullOrWhiteSpace(Email);
        public bool HasTelefon => !string.IsNullOrWhiteSpace(Telefon);

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Name))
                    return Name.Trim();

                return "Aktuell nicht hinterlegt.";
            }
        }
    }
}
