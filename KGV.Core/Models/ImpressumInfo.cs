using System.Collections.Generic;

namespace KGV.Core.Models
{
    public sealed class ImpressumInfo
    {
        // Legacy-Fallbacks für bestehende PDF-/Export- und Signaturpfade.
        // Das Impressum selbst verwendet ausschließlich Vereinskonfiguration und Funktionsslots.
        public const string VereinsName = "Kleingartenverein Oberrothenbach e.V.";
        public const string VereinsRegister = "Amtsgericht Chemnitz VR 70502";
        public const string VereinsEmail = "kgvoberrothenbach@gmx.de";
        public const string DatenschutzHinweis = "Die Datenschutzerklärung zur App ist online abrufbar.";
        public const string DatenschutzUrl = "https://kgv-oberrothenbach.github.io/KGV.neu/datenschutz.html";

        public List<ImpressumKontaktGruppe> Kontaktgruppen { get; set; } = new();
        public VereinskonfigurationRecord? Vereinskonfiguration { get; set; }
        public List<ImpressumKontaktItem> Verantwortliche { get; set; } = new();
    }

    public sealed class ImpressumKontaktGruppe
    {
        public string Schluessel { get; set; } = string.Empty;
        public string Bezeichnung { get; set; } = string.Empty;
        public short SortOrder { get; set; }
        public List<ImpressumKontaktItem> Kontakte { get; set; } = new();
    }
}
