// File: Core/Models/AblesungRecord.cs
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace KGV.Core.Models
{
    [Table("zaehler_ablesung")]
    public class AblesungRecord : BaseModel
    {
        [PrimaryKey("id", false)]
        [Column("id")]
        public long Id { get; set; }

        [Column("zaehler_id")]
        public long ZaehlerId { get; set; }

        [Column("stand")]
        public decimal Stand { get; set; }

        [Column("ablesedatum")]
        public DateTime Ablesedatum { get; set; }

        [Column("art")]
        public string Art { get; set; } = AblesungArt.Normal;

        [Column("freigegeben")]
        public bool Freigegeben { get; set; }

        [Column("pruefstatus")]
        public string Pruefstatus { get; set; } = AblesungPruefstatus.Eingereicht;

        [Column("pruefkommentar")]
        public string? Pruefkommentar { get; set; }

        [Column("geprueft_von")]
        public int? GeprueftVon { get; set; }

        [Column("geprueft_am")]
        public DateTime? GeprueftAm { get; set; }

        [Column("foto_pfad")]
        public string? FotoPfad { get; set; }

        [Column("foto_dateiname")]
        public string? FotoDateiname { get; set; }

        [Column("foto_drive_file_id")]
        public string? FotoDriveFileId { get; set; }
    }
}
