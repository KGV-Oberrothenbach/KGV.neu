using System.Collections.Generic;
using System.Linq;

namespace KGV.Core.Models
{
    public sealed class ImpressumInfo
    {
        public const string VereinsName = "Kleingartenverein Oberrothenbach e.V.";
        public const string VereinsRegister = "Amtsgericht Chemnitz VR 70502";
        public const string VerantwortlichName = "Mary Krüger-Rau";
        public const string VerantwortlichStrasse = "Scheringer Str. 12";
        public const string VerantwortlichOrt = "08056 Zwickau";
        public const string VereinsEmail = "kgvoberrothenbach@gmx.de";
        public const string DatenschutzHinweis = "Die Datenschutzerklärung zur App ist online abrufbar.";
        public const string DatenschutzUrl = "https://kgv-oberrothenbach.github.io/KGV.neu/datenschutz.html";

        public List<ImpressumKontaktItem> Vorstand { get; set; } = new();
        public List<ImpressumKontaktItem> Bauausschuss { get; set; } = new();

        public bool HasVorstand => Vorstand.Count > 0;
        public bool HasBauausschuss => Bauausschuss.Count > 0;
        public IReadOnlyList<ImpressumKontaktItem> WeitereVorstandsmitglieder => Vorstand
            .Where(x => x != null && !x.IsVorstandsvorsitzende)
            .ToList();
        public IReadOnlyList<ImpressumKontaktItem> WeitereBauausschussmitglieder => Bauausschuss
            .Where(x => x != null && !x.IsVorstandsvorsitzende)
            .ToList();
    }
}
