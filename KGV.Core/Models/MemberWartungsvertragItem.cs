using System;

namespace KGV.Core.Models;

public sealed class MemberWartungsvertragItem : WartungsvertragOverviewItem
{
    public DateTime GueltigAb { get; init; }
    public DateTime? GueltigBis { get; init; }

    public string GueltigkeitText => GueltigBis.HasValue
        ? $"Aktiv vom {GueltigAb:dd.MM.yyyy} bis {GueltigBis.Value:dd.MM.yyyy}"
        : $"Aktiv seit {GueltigAb:dd.MM.yyyy}";
}
