using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KGV.Core.Models;

[Table("pdf_template_catalog")]
public sealed class PdfTemplateCatalogRecord : BaseModel
{
    [PrimaryKey("id", false)] [Column("id")] public long Id { get; set; }
    [Column("template_key")] public string TemplateKey { get; set; } = string.Empty;
    [Column("version")] public string Version { get; set; } = string.Empty;
    [Column("storage_bucket")] public string StorageBucket { get; set; } = string.Empty;
    [Column("storage_path")] public string StoragePath { get; set; } = string.Empty;
    [Column("content_type")] public string ContentType { get; set; } = string.Empty;
    [Column("checksum")] public string Checksum { get; set; } = string.Empty;
    [Column("field_spec_path")] public string FieldSpecPath { get; set; } = string.Empty;
    [Column("field_spec_checksum")] public string FieldSpecChecksum { get; set; } = string.Empty;
    [Column("field_spec_version")] public string FieldSpecVersion { get; set; } = string.Empty;
    [Column("renderer_version")] public string RendererVersion { get; set; } = string.Empty;
    [Column("status")] public string Status { get; set; } = string.Empty;
    [Column("aktiv")] public bool Aktiv { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; }
}
