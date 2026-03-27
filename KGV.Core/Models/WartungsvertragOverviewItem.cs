using System.Globalization;

namespace KGV.Core.Models;

public class WartungsvertragOverviewItem
{
    public long Id { get; init; }
    public string Titel { get; init; } = string.Empty;
    public string Kurzbeschreibung { get; init; } = string.Empty;
    public int MaxKontingent { get; init; }
    public int Belegt { get; init; }
    public int Frei { get; init; }
    public bool Aktiv { get; init; }

    public string BelegungText => $"{Belegt.ToString(CultureInfo.CurrentCulture)} von {MaxKontingent.ToString(CultureInfo.CurrentCulture)} belegt";
    public string FreiText => Frei.ToString(CultureInfo.CurrentCulture);
    public string AktivText => Aktiv ? "Aktiv" : "Inaktiv";
}
