namespace KGV.Core.Models;

public enum Vertragsnummernart { Mitgliedsantrag, Pachtvertrag, HistorischerKombivertrag, Wartungsvertrag }

public static class Fachnummer
{
    public static long Mitglied(long technischeId) => technischeId > 0 ? checked(technischeId + 1000) : throw new ArgumentOutOfRangeException(nameof(technischeId));
    public static (int Von, int Bis) Bereich(Vertragsnummernart art) => art switch
    {
        Vertragsnummernart.Mitgliedsantrag => (10000, 19999), Vertragsnummernart.Pachtvertrag => (20000, 29999),
        Vertragsnummernart.HistorischerKombivertrag => (80000, 89999), Vertragsnummernart.Wartungsvertrag => (90000, 99999), _ => throw new ArgumentOutOfRangeException(nameof(art))
    };
    public static bool IstGueltig(Vertragsnummernart art, int nummer) { var (von, bis) = Bereich(art); return nummer >= von && nummer <= bis; }
}
