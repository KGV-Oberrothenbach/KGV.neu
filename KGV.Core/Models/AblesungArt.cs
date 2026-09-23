namespace KGV.Core.Models;

public static class AblesungArt
{
    public const string Normal = "normal";
    public const string Einbau = "einbau";
    public const string Ausbau = "ausbau";
    public const string JahresEnde = "jea";
    public const string PachtAnfang = "pa";
    public const string PachtEnde = "pe";

    public static string Normalize(string? value)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        return normalized switch
        {
            Einbau => Einbau,
            Ausbau => Ausbau,
            JahresEnde => JahresEnde,
            PachtAnfang => PachtAnfang,
            PachtEnde => PachtEnde,
            _ => Normal
        };
    }
}
