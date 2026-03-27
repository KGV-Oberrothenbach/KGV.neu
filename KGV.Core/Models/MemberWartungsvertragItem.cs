using System;

namespace KGV.Core.Models;

public sealed class MemberWartungsvertragItem : WartungsvertragOverviewItem
{
    public long ZuordnungId { get; init; }
    public DateTime GueltigAb { get; init; }
    public DateTime? GueltigBis { get; init; }
    public string StatusText => GueltigBis.HasValue ? "Beendet" : "Aktiv";

    public string GueltigkeitText => GueltigBis.HasValue
        ? $"Aktiv vom {GueltigAb:dd.MM.yyyy} bis {GueltigBis.Value:dd.MM.yyyy}"
        : $"Aktiv seit {GueltigAb:dd.MM.yyyy}";
}
