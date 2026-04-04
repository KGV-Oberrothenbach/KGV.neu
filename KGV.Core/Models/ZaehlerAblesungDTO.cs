// File: Core/Models/ZaehlerAblesungDTO.cs
using System;

namespace KGV.Core.Models
{
    public sealed class ZaehlerAblesungDTO
    {
        public long AblesungId { get; set; }
        public long ZaehlerId { get; set; }
        public DateTime Ablesedatum { get; set; }
        public decimal Stand { get; set; }
        public string Zaehlernummer { get; set; } = string.Empty;
        public DateTime Eichdatum { get; set; }
        public bool Freigegeben { get; set; }
        public string Pruefstatus { get; set; } = AblesungPruefstatus.Eingereicht;
        public string? Pruefkommentar { get; set; }
        public int? GeprueftVon { get; set; }
        public DateTime? GeprueftAm { get; set; }

        public string? FotoPfad { get; set; }
        public string? FotoDateiname { get; set; }
        public string? FotoDriveFileId { get; set; }
    }
}
