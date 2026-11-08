using System;

namespace KGV.Core.Models;

/// <summary>Gemeinsamer, UI-neutraler Vertrag für aktuell auswählbare Zähler.</summary>
public sealed record SelectableActiveMeter(
    long Id,
    string Medium,
    string Zaehlernummer,
    long? ParzelleId,
    bool IstHauptzaehler,
    string? Anlagenbereich,
    DateTime EingebautAm)
{
    public string DisplayName => BuildDisplayName(Medium, Zaehlernummer, IstHauptzaehler, Anlagenbereich);

    public static string BuildDisplayName(string? medium, string? zaehlernummer, bool istHauptzaehler, string? anlagenbereich)
    {
        var mediumDisplay = string.Equals(medium, "wasser", StringComparison.OrdinalIgnoreCase) ? "Wasser" : "Strom";
        var bereich = string.IsNullOrWhiteSpace(anlagenbereich) ? string.Empty : $" {anlagenbereich.Trim()}";
        return istHauptzaehler ? $"Hauptzähler {mediumDisplay}{bereich}" : $"{mediumDisplay} – {zaehlernummer}";
    }
}

public sealed record SelectableActiveMeterQuery(long? ParzelleId, bool HauptzaehlerModus, DateTime Stichtag);
