using System;

namespace KGV.Core.Models;

public sealed class MemberWartungsvertragItem : WartungsvertragOverviewItem
{
    public int? Vertragsnummer { get; init; }
    public long ZuordnungId { get; init; }
    public DateTime GueltigAb { get; init; }
    public DateTime? GueltigBis { get; init; }
    public WartungsvertragZeitstatus Zeitstatus { get; init; }
    public string StatusText => WartungsvertragZeitmodell.Bezeichnung(Zeitstatus);

    public string GueltigkeitText => GueltigBis.HasValue
        ? $"Gültig ab {GueltigAb:dd.MM.yyyy} bis {GueltigBis.Value:dd.MM.yyyy}"
        : $"Gültig ab {GueltigAb:dd.MM.yyyy} bis offen";
}
