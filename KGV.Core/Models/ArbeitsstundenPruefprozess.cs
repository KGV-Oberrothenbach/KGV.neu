using System;

namespace KGV.Core.Models;

public static class ArbeitsstundenPruefprozess
{
    public const string StatusOffen = "offen";
    public const string StatusGenehmigt = "genehmigt";
    public const string StatusAbgelehnt = "abgelehnt";

    public const string AktionFreigegeben = "freigegeben";
    public const string AktionAbgelehnt = "abgelehnt";
    public const string AktionKorrigiert = "korrigiert";
    public const string AktionGeloescht = "geloescht";

    public static string NormalizeKommentar(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    public static bool HasRequiredKommentar(string? value)
        => !string.IsNullOrWhiteSpace(NormalizeKommentar(value));

    public static string NormalizeStatus(string? status, bool freigegeben)
    {
        if (freigegeben)
            return StatusGenehmigt;

        var normalized = string.IsNullOrWhiteSpace(status)
            ? string.Empty
            : status.Trim();

        if (string.Equals(normalized, StatusAbgelehnt, StringComparison.OrdinalIgnoreCase))
            return StatusAbgelehnt;

        if (string.Equals(normalized, StatusGenehmigt, StringComparison.OrdinalIgnoreCase))
            return StatusGenehmigt;

        return StatusOffen;
    }

    public static string BuildFreigegebenStatus(string kommentar)
        => StatusGenehmigt;

    public static string BuildAbgelehntStatus(string kommentar)
        => StatusAbgelehnt;

    public static string BuildKorrigiertStatus(string kommentar)
        => StatusGenehmigt;

    public static string BuildGeloeschtStatus(string kommentar)
        => StatusAbgelehnt;

    public static bool IsOffenerPrueffall(string? status, bool freigegeben)
    {
        var normalizedStatus = NormalizeStatus(status, freigegeben);
        return !freigegeben
            && !string.Equals(normalizedStatus, StatusAbgelehnt, StringComparison.OrdinalIgnoreCase);
    }

    public static string BuildStatusDisplay(string? status, bool freigegeben)
    {
        return NormalizeStatus(status, freigegeben) switch
        {
            StatusGenehmigt => "Freigegeben",
            StatusAbgelehnt => "Abgelehnt",
            _ => "Offen"
        };
    }

    public static string GetAktionDisplay(string aktion)
    {
        return aktion switch
        {
            AktionFreigegeben => "Freigegeben",
            AktionAbgelehnt => "Abgelehnt",
            AktionKorrigiert => "Korrigiert",
            AktionGeloescht => "Gelöscht",
            _ => aktion
        };
    }
}
