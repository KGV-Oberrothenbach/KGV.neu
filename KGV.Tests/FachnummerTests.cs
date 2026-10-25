using KGV.Core.Models;

namespace KGV.Tests;

public sealed class FachnummerTests
{
    [Theory] [InlineData(1, 1001)] [InlineData(81, 1081)] public void Mitgliedsnummer_is_id_plus_1000(long id, long nummer) => Assert.Equal(nummer, Fachnummer.Mitglied(id));
    [Theory]
    [InlineData(Vertragsnummernart.Mitgliedsantrag, 10000)] [InlineData(Vertragsnummernart.Pachtvertrag, 20000)]
    [InlineData(Vertragsnummernart.HistorischerKombivertrag, 80000)] [InlineData(Vertragsnummernart.Wartungsvertrag, 90000)]
    public void Each_contract_range_starts_at_its_required_first_number(Vertragsnummernart art, int nummer) => Assert.True(Fachnummer.IstGueltig(art, nummer));
    [Fact] public void Contract_ranges_do_not_overlap_or_exceed_their_maximum() { Assert.False(Fachnummer.IstGueltig(Vertragsnummernart.Pachtvertrag, 19999)); Assert.False(Fachnummer.IstGueltig(Vertragsnummernart.Wartungsvertrag, 100000)); }
}
