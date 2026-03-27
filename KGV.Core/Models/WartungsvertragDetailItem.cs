using System;
using System.Collections.Generic;
using System.Linq;

namespace KGV.Core.Models;

public sealed class WartungsvertragDetailItem : WartungsvertragOverviewItem
{
    public string Beschreibung { get; init; } = string.Empty;
    public List<WartungsvertragAssignedMemberItem> ZugeordneteMitglieder { get; init; } = new();
    public string BeschreibungText => string.IsNullOrWhiteSpace(Beschreibung) ? "Keine Beschreibung hinterlegt." : Beschreibung.Trim();
}

public sealed class WartungsvertragAssignedMemberItem
{
    public int MitgliedId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string GartenNummern { get; init; } = string.Empty;
    public DateTime GueltigAb { get; init; }
    public DateTime? GueltigBis { get; init; }

    public string GartenText => string.IsNullOrWhiteSpace(GartenNummern)
        ? "Kein Garten hinterlegt"
        : $"Garten: {string.Join(", ", GartenNummern.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0))}";

    public string GueltigkeitText => GueltigBis.HasValue
        ? $"Gültig {GueltigAb:dd.MM.yyyy} bis {GueltigBis.Value:dd.MM.yyyy}"
        : $"Gültig seit {GueltigAb:dd.MM.yyyy}";

    public string Subtitle => $"{GartenText} · {GueltigkeitText}";
}
