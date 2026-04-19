using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace KGV.Core.Models
{
    [Table("app_export_definition")]
    public sealed class AppExportDefinitionRecord : BaseModel
    {
        [PrimaryKey("id", false)]
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        public string? Name { get; set; }

        [Column("title")]
        public string? Title { get; set; }

        [Column("quelle_typ")]
        public string? QuelleTyp { get; set; }

        [Column("quelle_name")]
        public string? QuelleName { get; set; }

        [Column("aktiv")]
        public bool Aktiv { get; set; }
    }

    [Table("app_export_filter_definition")]
    public sealed class AppExportFilterDefinitionRecord : BaseModel
    {
        [PrimaryKey("id", false)]
        [Column("id")]
        public int Id { get; set; }

        [Column("export_definition_id")]
        public int ExportDefinitionId { get; set; }

        [Column("name")]
        public string? Name { get; set; }

        [Column("label")]
        public string? Label { get; set; }

        [Column("type")]
        public string? Type { get; set; }

        [Column("options_rpc")]
        public string? OptionsRpc { get; set; }
    }

    [Table("app_export_column_definition")]
    public sealed class AppExportColumnDefinitionRecord : BaseModel
    {
        [PrimaryKey("id", false)]
        [Column("id")]
        public int Id { get; set; }

        [Column("export_definition_id")]
        public int ExportDefinitionId { get; set; }

        [Column("name")]
        public string? Name { get; set; }

        [Column("label")]
        public string? Label { get; set; }

        [Column("visible")]
        public bool Visible { get; set; }

        [Column("sort_order")]
        public int SortOrder { get; set; }
    }
}
