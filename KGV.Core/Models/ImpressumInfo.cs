using System.Collections.Generic;

namespace KGV.Core.Models
{
    public sealed class ImpressumInfo
    {
        public List<ImpressumKontaktItem> Vorstand { get; set; } = new();
        public List<ImpressumKontaktItem> Bauausschuss { get; set; } = new();

        public bool HasVorstand => Vorstand.Count > 0;
        public bool HasBauausschuss => Bauausschuss.Count > 0;
    }
}
