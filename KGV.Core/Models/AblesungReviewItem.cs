using System;

namespace KGV.Core.Models;

public sealed class AblesungReviewItem
{
    public long AblesungId { get; set; }
    public long ZaehlerId { get; set; }
    public int ParzelleId { get; set; }
    public string GartenNr { get; set; } = string.Empty;
    public string Anlage { get; set; } = string.Empty;
    public string Medium { get; set; } = string.Empty;
    public string Zaehlernummer { get; set; } = string.Empty;
    public DateTime Ablesedatum { get; set; }
    public decimal Stand { get; set; }
    public string Pruefstatus { get; set; } = AblesungPruefstatus.Eingereicht;
    public string? Pruefkommentar { get; set; }
    public int? GeprueftVon { get; set; }
    public DateTime? GeprueftAm { get; set; }
    public int? MitgliedId { get; set; }
    public string? MitgliedName { get; set; }
    public long Mitgliedsnummer { get; set; }
    public string? QuelleHinweis { get; set; }
    public string? FotoPfad { get; set; }
    public string? FotoDateiname { get; set; }
    public string? FotoDriveFileId { get; set; }

    public string ParzelleDisplayName => string.IsNullOrWhiteSpace(Anlage)
        ? $"Garten {GartenNr}"
        : $"Garten {GartenNr} - {Anlage}";

    public string MediumDisplay => string.Equals(Medium, "wasser", StringComparison.OrdinalIgnoreCase)
        ? "Wasser"
        : "Strom";

    public string MitgliedDisplay
    {
        get
        {
            if (Mitgliedsnummer <= 0)
                return string.IsNullOrWhiteSpace(MitgliedName)
                    ? "Quelle im Modell nicht verfügbar"
                    : "Mitgliedsnummer nicht verfügbar";

            return string.IsNullOrWhiteSpace(MitgliedName)
                ? $"Mgl.-Nr. {Mitgliedsnummer}"
                : $"{MitgliedName} · Mgl.-Nr. {Mitgliedsnummer}";
        }
    }

    public string PruefstatusDisplay => AblesungPruefstatus.Normalize(Pruefstatus) switch
    {
        AblesungPruefstatus.Freigegeben => "Freigegeben",
        AblesungPruefstatus.Abgelehnt => "Abgelehnt",
        _ => "Eingereicht"
    };

    public bool HasFoto => !string.IsNullOrWhiteSpace(FotoPfad) || !string.IsNullOrWhiteSpace(FotoDriveFileId);
}
