using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KGV.Core.Models
{
    [Table("dokument")]
    public sealed class DokumentInsertRecord : BaseModel
    {
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Column("mitglied_id")]
        public int? MitgliedId { get; set; }

        [Column("parzelle_id")]
        public int? ParzelleId { get; set; }

        [Column("bucket")]
        public string Bucket { get; set; } = "dokumente";

        [Column("storage_path")]
        public string StoragePath { get; set; } = string.Empty;

        [Column("titel")]
        public string? Titel { get; set; }

        [Column("dateiname")]
        public string? Dateiname { get; set; }

        [Column("mime_type")]
        public string? MimeType { get; set; }

        [Column("size_bytes")]
        public long? SizeBytes { get; set; }

        [Column("created_by")]
        public Guid? CreatedBy { get; set; }

        [Column("drive_file_id")]
        public string? DriveFileId { get; set; }
    }
}
