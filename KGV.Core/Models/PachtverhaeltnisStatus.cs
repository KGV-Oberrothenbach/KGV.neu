namespace KGV.Core.Models;

public sealed record AktivesPachtverhaeltnisStatus(long? PachtverhaeltnisId, int Vertragsfassungen)
{
    public bool HatAktivesPachtverhaeltnis => PachtverhaeltnisId is > 0;
}
