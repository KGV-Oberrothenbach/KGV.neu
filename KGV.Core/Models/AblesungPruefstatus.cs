using System;

namespace KGV.Core.Models;

public static class AblesungPruefstatus
{
    public const string Eingereicht = "eingereicht";
    public const string Freigegeben = "freigegeben";
    public const string Abgelehnt = "abgelehnt";

    public static string Normalize(string? pruefstatus, bool freigegeben = false)
    {
        if (string.IsNullOrWhiteSpace(pruefstatus))
            return freigegeben ? Freigegeben : Eingereicht;

        var normalized = pruefstatus.Trim().ToLowerInvariant();
        return normalized switch
        {
            Freigegeben or "genehmigt" or "approved" => Freigegeben,
            Abgelehnt or "rejected" => Abgelehnt,
            Eingereicht or "offen" or "pending" => freigegeben ? Freigegeben : Eingereicht,
            _ => freigegeben ? Freigegeben : Eingereicht
        };
    }

    public static bool IsFreigegeben(string? pruefstatus)
        => string.Equals(Normalize(pruefstatus), Freigegeben, StringComparison.Ordinal);

    public static bool IsAbgelehnt(string? pruefstatus)
        => string.Equals(Normalize(pruefstatus), Abgelehnt, StringComparison.Ordinal);
}
