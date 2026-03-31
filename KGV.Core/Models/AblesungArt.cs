namespace KGV.Core.Models;

public static class AblesungArt
{
    public const string Normal = "normal";
    public const string Einbau = "einbau";
    public const string Ausbau = "ausbau";

    public static string Normalize(string? value)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        return normalized switch
        {
            Einbau => Einbau,
            Ausbau => Ausbau,
            _ => Normal
        };
    }
}
